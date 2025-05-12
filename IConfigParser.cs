using System.Threading;
using System.Threading.Tasks;

namespace Azzazelloqq.Config
{
public interface IConfigParser
{
	public IConfigPage[] Parse();
	public Task<IConfigPage[]> ParseAsync(CancellationToken token);
}
}