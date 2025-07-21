using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Azzazelloqq.Config
{
public sealed class CompositeConfigParser : IConfigParser
{
	private readonly IConfigParser[] _parsers;

	public CompositeConfigParser(params IConfigParser[] parsers)
	{
		_parsers = parsers;
	}

	public IConfigPage[] Parse()
	{
		var list = new List<IConfigPage>(16);

		foreach (var p in _parsers)
		{
			var pages = p.Parse();
			list.AddRange(pages);
		}

		return list.ToArray();
	}

	public async Task<IConfigPage[]> ParseAsync(CancellationToken token)
	{
		if (token.IsCancellationRequested)
		{
			throw new OperationCanceledException(token);
		}

		if (_parsers.Length == 1)
		{
			return await _parsers[0].ParseAsync(token);
		}


		var tasks = new Task<IConfigPage[]>[_parsers.Length];
		for (var i = 0; i < _parsers.Length; i++)
		{
			tasks[i] = _parsers[i].ParseAsync(token);
		}

		var results = await Task.WhenAll(tasks).ConfigureAwait(false);

		var total = 0;
		foreach (var arr in results)
		{
			total += arr.Length;
		}

		var combined = new IConfigPage[total];
		var offset = 0;
		foreach (var arr in results)
		{
			Array.Copy(arr, 0, combined, offset, arr.Length);
			offset += arr.Length;
		}

		return combined;
	}

	public async Task<IConfigPage[]> ParseAsync(IProgress<ParseProgress> progress, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();

		token.ThrowIfCancellationRequested();
		var allPages = new List<IConfigPage>();
		int total = _parsers.Length, done = 0;

		foreach (var parser in _parsers)
		{
			var pages = await parser.ParseAsync(token)
						 .ConfigureAwait(false);
			allPages.AddRange(pages);

			done++;
			progress?.Report(new ParseProgress(
				(float)done / total,
				$"Composite: parsed [{parser.GetType().Name}] ({done}/{total})"
			));
		}

		return allPages.ToArray();
	}

	public void ParseAsync(Action<ParseProgress> progress, Action<IConfigPage[]> onParsed, CancellationToken token)
	{
		var prog = new Progress<ParseProgress>(progress);

		_ = Task.Run(async () =>
		{
			token.ThrowIfCancellationRequested();
			var result = await ParseAsync(prog, token)
				.ConfigureAwait(false);
			onParsed(result);
		}, token);
	}
}
}