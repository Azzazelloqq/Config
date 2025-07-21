using System;
using System.Threading;
using System.Threading.Tasks;

namespace Azzazelloqq.Config.Tests
{
public class FakeSimpleParser: IConfigParser
{
	private readonly IConfigPage[] _pages;

	public FakeSimpleParser(params IConfigPage[] pages)
	{
		_pages = pages;
	}

	public IConfigPage[] Parse()
	{
		return _pages;
	}

	public Task<IConfigPage[]> ParseAsync(CancellationToken _)
	{
		return Task.FromResult(_pages);
	}

	public Task<IConfigPage[]> ParseAsync(IProgress<ParseProgress> progress, CancellationToken token)
	{
		return Task.FromResult(_pages);
	}

	public void ParseAsync(Action<ParseProgress> progress, Action<IConfigPage[]> onParsed, CancellationToken token)
	{
		onParsed?.Invoke(_pages);
	}
}
}