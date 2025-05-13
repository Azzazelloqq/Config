using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Azzazelloqq.Config
{
public class CompositeConfigParser : IConfigParser
{
	private readonly IConfigParser[] _parsers;

	public CompositeConfigParser(params IConfigParser[] parsers)
	{
		_parsers = parsers;
	}

	public IConfigPage[] Parse()
	{
		return _parsers
			.SelectMany(p => p.Parse())
			.ToArray();
	}

	public async Task<IConfigPage[]> ParseAsync(CancellationToken token)
	{
		var tasks = _parsers
			.Select(p => p.ParseAsync(token))
			.ToArray();

		var results = await Task.WhenAll(tasks);
		return results
			.SelectMany(r => r)
			.ToArray();
	}
}
}