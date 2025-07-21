using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Azzazelloqq.Config
{
/// <summary>
/// Parses configuration pages in dependency order.
/// Executors that belong to the same “level” (no unsatisfied dependencies)
/// are executed in parallel — synchronously via <see>
///     <cref>Parallel.For</cref>
/// </see>
/// or asynchronously via <see>
///     <cref>Task.WhenAll</cref>
/// </see>
/// </summary>
public sealed class DependencyAwareConfigParser : IConfigParser
{
	private readonly IParseExecutor[] _execs;
	private readonly IExecutorResolver _resolver;

	public DependencyAwareConfigParser(
		IParseExecutor[] execs,
		IExecutorResolver resolver)
	{
		_execs = execs ?? throw new ArgumentNullException(nameof(execs));
		_resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
	}

	/// <inheritdoc/>
	public IConfigPage[] Parse()
	{
		var order = _resolver.Resolve(_execs);
		var context = new Dictionary<Type, IConfigPage>(order.Count);
		var output = new List<IConfigPage>(order.Count);

		ExecuteBatches(
			order,
			ex => ex.Parse(context),
			page =>
			{
				context.Add(page.GetType(), page);
				output.Add(page);
			});

		return output.ToArray();
	}

	/// <inheritdoc/>
	public async Task<IConfigPage[]> ParseAsync(CancellationToken token)
	{
		if (token.IsCancellationRequested)
		{
			throw new OperationCanceledException(token);
		}

		var order = _resolver.Resolve(_execs);
		var context = new Dictionary<Type, IConfigPage>(order.Count);
		var output = new List<IConfigPage>(order.Count);

		await ExecuteBatchesAsync(
				order,
				ex => ex.ParseAsync(context, token),
				page =>
				{
					context.Add(page.GetType(), page);
					output.Add(page);
				})
			.ConfigureAwait(false);

		return output.ToArray();
	}

	public async Task<IConfigPage[]> ParseAsync(IProgress<ParseProgress> progress, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();

		var order = _resolver.Resolve(_execs);
		var context = new Dictionary<Type, IConfigPage>(order.Count);
		var output = new List<IConfigPage>(order.Count);

		var total = order.Count;
		var done = 0;

		var remaining = BuildRemainingDeps(order);

		var queue = new Queue<IParseExecutor>();
		foreach (var ex in order)
		{
			if (remaining[ex] == 0)
			{
				queue.Enqueue(ex);
			}
		}

		while (queue.Count > 0)
		{
			var levelSize = queue.Count;
			var level = new IParseExecutor[levelSize];
			for (var i = 0; i < levelSize; i++)
			{
				level[i] = queue.Dequeue();
			}

			var tasks = new Task<IConfigPage>[levelSize];
			for (var i = 0; i < levelSize; i++)
			{
				tasks[i] = level[i].ParseAsync(context, token);
			}

			var pages = await Task.WhenAll(tasks).ConfigureAwait(false);

			foreach (var page in pages)
			{
				context.Add(page.GetType(), page);
				output.Add(page);

				done++;
				var frac = (float)done / total;
				progress?.Report(
					new ParseProgress(
						frac,
						$"Parsed page {page.GetType().Name} ({done}/{total})"
					)
				);
			}

			foreach (var ex in level)
			{
				foreach (var n in order)
				{
					if (!DependsOn(n, ex.TargetType))
					{
						continue;
					}

					var cnt = remaining[n] - 1;
					remaining[n] = cnt;
					if (cnt == 0)
					{
						queue.Enqueue(n);
					}
				}
			}
		}

		return output.ToArray();
	}

	public void ParseAsync(Action<ParseProgress> progress, Action<IConfigPage[]> onParsed, CancellationToken token)
	{
		var prog = new Progress<ParseProgress>(progress);

		_ = Task.Run(async () =>
		{
			token.ThrowIfCancellationRequested();
			var pages = await ParseAsync(prog, token)
				.ConfigureAwait(false);
			onParsed(pages);
		}, token);
	}

	private static void ExecuteBatches(
		IReadOnlyList<IParseExecutor> order,
		Func<IParseExecutor, IConfigPage> body,
		Action<IConfigPage> commit)
	{
		var remaining = BuildRemainingDeps(order);
		var queue = new Queue<IParseExecutor>(order.Count);

		EnqueueReady(order, remaining, queue);

		while (queue.Count > 0)
		{
			var levelSize = queue.Count;

			if (levelSize == 1)
			{
				Run(queue.Dequeue());
			}
			else
			{
				var batch = new IParseExecutor[levelSize];
				for (var i = 0; i < levelSize; i++)
				{
					batch[i] = queue.Dequeue();
				}

				Parallel.For(0, batch.Length, i => Run(batch[i]));
			}

			//–– local
			void Run(IParseExecutor ex)
			{
				var page = body(ex);
				commit(page);

				// unlock neighbours
				foreach (var n in order)
				{
					if (DependsOn(n, ex.TargetType) && --remaining[n] == 0)
					{
						queue.Enqueue(n);
					}
				}
			}
		}
	}

	private static async Task ExecuteBatchesAsync(
		IReadOnlyList<IParseExecutor> order,
		Func<IParseExecutor, Task<IConfigPage>> bodyAsync,
		Action<IConfigPage> commit)
	{
		var remaining = BuildRemainingDeps(order);
		var queue = new Queue<IParseExecutor>(order.Count);

		EnqueueReady(order, remaining, queue);

		while (queue.Count > 0)
		{
			var level = DequeueLevel(queue);

			var tasks = new Task<IConfigPage>[level.Count];
			for (var i = 0; i < level.Count; i++)
			{
				tasks[i] = bodyAsync(level[i]);
			}

			var pages = await Task.WhenAll(tasks).ConfigureAwait(false);
			foreach (var page in pages)
			{
				commit(page);
			}

			foreach (var ex in level)
			foreach (var n in order)
			{
				if (DependsOn(n, ex.TargetType) && --remaining[n] == 0)
				{
					queue.Enqueue(n);
				}
			}
		}
	}


	private static Dictionary<IParseExecutor, int>
		BuildRemainingDeps(IReadOnlyList<IParseExecutor> order)
	{
		var dict = new Dictionary<IParseExecutor, int>(order.Count);
		foreach (var ex in order)
		{
			dict[ex] = ex.Dependencies.Count;
		}

		return dict;
	}

	private static void EnqueueReady(
		IReadOnlyList<IParseExecutor> order,
		Dictionary<IParseExecutor, int> remaining,
		Queue<IParseExecutor> queue)
	{
		foreach (var ex in order)
		{
			if (remaining[ex] == 0)
			{
				queue.Enqueue(ex);
			}
		}
	}

	private static List<IParseExecutor> DequeueLevel(
		Queue<IParseExecutor> queue)
	{
		var count = queue.Count;
		var list = new List<IParseExecutor>(count);
		for (var i = 0; i < count; i++)
		{
			list.Add(queue.Dequeue());
		}

		return list;
	}

	private static bool DependsOn(
		IParseExecutor ex,
		Type dependencyType)
	{
		foreach (var t in ex.Dependencies)
		{
			if (t == dependencyType)
			{
				return true;
			}
		}

		return false;
	}
}
}