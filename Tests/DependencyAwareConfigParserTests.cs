using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Azzazelloqq.Config.Tests
{
[TestFixture]
public class DependencyAwareConfigParserTests
{
	private SimpleResolver _resolver = new();

	#region Tests

	[Test]
	public void Parse_SingleExecutor_CalledOnceAndReturnsPage()
	{
		var called = false;
		var fake = new FakeExecutor(
			typeof(PageA),
			Enumerable.Empty<Type>(),
			ctx =>
			{
				called = true;
				return new PageA();
			});

		var parser = new DependencyAwareConfigParser(
			new[] { fake }, _resolver);

		var pages = parser.Parse();

		Assert.IsTrue(called, "Должен был вызваться единственный executor");
		Assert.AreEqual(1, pages.Length);
		Assert.IsInstanceOf<PageA>(pages[0]);
	}

	[Test]
	public async Task ParseAsync_SingleExecutor_CalledOnceAndReturnsPage()
	{
		var called = false;
		var fake = new FakeExecutor(
			typeof(PageA),
			Enumerable.Empty<Type>(),
			null,
			async (ctx, ct) =>
			{
				called = true;
				await Task.Yield();
				return new PageA();
			});

		var parser = new DependencyAwareConfigParser(
			new[] { fake }, _resolver);

		var pages = await parser.ParseAsync(CancellationToken.None);

		Assert.IsTrue(called, "Должен был вызваться единственный async‑executor");
		Assert.AreEqual(1, pages.Length);
		Assert.IsInstanceOf<PageA>(pages[0]);
	}

	// 3) Глубокая цепочка A → B → C → D
	[Test]
	public void Parse_LinearChain_ProducesCorrectOrder()
	{
		var a = new FakeExecutor(typeof(PageA), Enumerable.Empty<Type>());
		var b = new FakeExecutor(typeof(PageB), new[] { typeof(PageA) });
		var c = new FakeExecutor(typeof(PageC), new[] { typeof(PageB) });
		var d = new FakeExecutor(typeof(PageD), new[] { typeof(PageC) });

		var parser = new DependencyAwareConfigParser(
			new[] { d, c, b, a }, _resolver);

		var types = parser
			.Parse()
			.Select(p => p.GetType())
			.ToArray();

		Assert.AreEqual(
			new[] { typeof(PageA), typeof(PageB), typeof(PageC), typeof(PageD) },
			types);
	}

	// 4) Diamond‑граф в async: два first‑level executors параллельно, потом третий
	[Test]
	public async Task ParseAsync_DiamondGraph_ExecutesParallelAndOrdered()
	{
		var results = new ConcurrentQueue<string>();
		var gate    = new CountdownEvent(2);

		// Первый уровень: два async‑executor-а без зависимостей
		var a = new FakeExecutor(
			typeof(PageA),
			Enumerable.Empty<Type>(),
			sync: null,
			async: async (ctx, ct) =>
			{
				await Task.Delay(30, ct);
				results.Enqueue("A");
				gate.Signal();
				return new PageA();
			});

		var b = new FakeExecutor(
			typeof(PageB),
			Enumerable.Empty<Type>(),
			sync: null,
			async: async (ctx, ct) =>
			{
				await Task.Delay(30, ct);
				results.Enqueue("B");
				gate.Signal();
				return new PageB();
			});

		// Второй уровень: зависит от A и B
		var c = new FakeExecutor(
			typeof(PageC),
			new[] { typeof(PageA), typeof(PageB) },
			sync: null,
			async: async (ctx, ct) =>
			{
				Assert.IsTrue(ctx.ContainsKey(typeof(PageA)));
				Assert.IsTrue(ctx.ContainsKey(typeof(PageB)));
				results.Enqueue("C");
				return new PageC();
			});

		var parser = new DependencyAwareConfigParser(
			new IParseExecutor[] { a, b, c },
			_resolver);

		await parser.ParseAsync(CancellationToken.None);

		// Проверяем, что оба первого уровня выполнились («параллельность»).
		Assert.IsTrue(gate.Wait(100), "Первый уровень должен был завершиться одновременно");

		// Проверяем результат: всего 3 элемента, последний всегда "C",
		// первые два — это "A" и "B" в любом порядке.
		var seq = results.ToArray();
		Assert.AreEqual(3, seq.Length, "Должны быть три выполнения");
		Assert.AreEqual("C", seq[2], "Третий элемент всегда C");

		var firstTwo = seq.Take(2).ToArray();
		CollectionAssert.AreEquivalent(new[] { "A", "B" }, firstTwo, 
			"Первые два должны быть именно A и B в любом порядке");
	}

	// 5) CancellationToken проверка
	[Test]
	public void ParseAsync_AlreadyCanceled_ThrowsTaskCanceledException()
	{
		var fake = new FakeExecutor(typeof(PageA), Enumerable.Empty<Type>());
		var parser = new DependencyAwareConfigParser(
			new[] { fake }, _resolver);

		var cts = new CancellationTokenSource();
		cts.Cancel();

		Assert.ThrowsAsync<TaskCanceledException>(
			() => parser.ParseAsync(cts.Token));
	}

	// 6a) Missing dependency in sync
	[Test]
	public void Parse_MissingDependency_ThrowsInvalidOperationException()
	{
		var fake = new FakeExecutor(
			typeof(PageA),
			new[] { typeof(PageB) } // B нет в списке
		);
		var parser = new DependencyAwareConfigParser(
			new[] { fake }, _resolver);

		var ex = Assert.Throws<InvalidOperationException>(() => parser.Parse());

		StringAssert.Contains(nameof(PageB), ex.Message);
	}

	// 6b) Missing dependency в async
	[Test]
	public void ParseAsync_MissingDependency_ThrowsInvalidOperationException()
	{
		var fake = new FakeExecutor(
			typeof(PageA),
			new[] { typeof(PageB) }
		);
		var parser = new DependencyAwareConfigParser(
			new[] { fake }, _resolver);

		Assert.ThrowsAsync<InvalidOperationException>(() => parser.ParseAsync(CancellationToken.None));
	}

	// 7a) Исключение внутри Parse()
	[Test]
	public void Parse_ExecutorThrows_PropagatesException()
	{
		var fake = new FakeExecutor(
			typeof(PageA),
			Enumerable.Empty<Type>(),
			ctx => throw new InvalidOperationException("SyncFailure")
		);
		var parser = new DependencyAwareConfigParser(
			new[] { fake }, _resolver);

		var ex = Assert.Throws<InvalidOperationException>(() => parser.Parse());
		Assert.AreEqual("SyncFailure", ex.Message);
	}

	// 7b) Исключение внутри ParseAsync()
	[Test]
	public void ParseAsync_ExecutorThrows_PropagatesException()
	{
		var fake = new FakeExecutor(
			typeof(PageA),
			Enumerable.Empty<Type>(),
			null,
			(ctx, ct) => throw new InvalidOperationException("AsyncFailure")
		);
		var parser = new DependencyAwareConfigParser(
			new[] { fake }, _resolver);

		var ex = Assert.ThrowsAsync<InvalidOperationException>(() => parser.ParseAsync(CancellationToken.None));
		Assert.AreEqual("AsyncFailure", ex.Message);
	}

	#endregion

	private class PageA : IConfigPage
	{
	}

	private class PageB : IConfigPage
	{
	}

	private class PageC : IConfigPage
	{
	}

	private class PageD : IConfigPage
	{
	}

	private class FakeExecutor : IParseExecutor
	{
		public Type TargetType { get; }
		public IReadOnlyCollection<Type> Dependencies { get; }

		private readonly Func<IReadOnlyDictionary<Type, IConfigPage>, IConfigPage> _sync;
		private readonly Func<IReadOnlyDictionary<Type, IConfigPage>, CancellationToken, Task<IConfigPage>> _async;

		public FakeExecutor(
			Type targetType,
			IEnumerable<Type> dependencies,
			Func<IReadOnlyDictionary<Type, IConfigPage>, IConfigPage>? sync = null,
			Func<IReadOnlyDictionary<Type, IConfigPage>, CancellationToken, Task<IConfigPage>>? async = null)
		{
			TargetType = targetType;
			Dependencies = dependencies.ToArray();
			_sync = sync ?? (ctx => (IConfigPage)Activator.CreateInstance(targetType)!);
			_async = async ?? ((ctx, ct) => Task.FromResult((IConfigPage)Activator.CreateInstance(targetType)!));
		}

		public IConfigPage Parse(IReadOnlyDictionary<Type, IConfigPage> context)
		{
			return _sync(context);
		}

		public Task<IConfigPage> ParseAsync(
			IReadOnlyDictionary<Type, IConfigPage> context,
			CancellationToken ct)
		{
			return _async(context, ct);
		}
	}
}
}