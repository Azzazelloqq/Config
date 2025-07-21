using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Azzazelloqq.Config.Tests
{
public class FakeCancelExecutor: IParseExecutor
{
	public Type TargetType => typeof(PageA);
	public IReadOnlyCollection<Type> Dependencies => Array.Empty<Type>();

	public IConfigPage Parse(IReadOnlyDictionary<Type, IConfigPage> _)
	{
		throw new InvalidOperationException();
	}

	public Task<IConfigPage> ParseAsync(IReadOnlyDictionary<Type, IConfigPage> _, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();
		return Task.FromResult<IConfigPage>(new PageA());
	}
}
}