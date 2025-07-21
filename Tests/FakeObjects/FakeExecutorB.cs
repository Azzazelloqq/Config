using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Azzazelloqq.Config.Tests
{
public class FakeExecutorB : IParseExecutor
{
	public Type TargetType { get; }
	public IReadOnlyCollection<Type> Dependencies { get; }
	public PageStub Instance { get; }

	public FakeExecutorB(Type pageType, params Type[] deps)
	{
		TargetType = pageType;
		Dependencies = deps;
		Instance = (PageStub)Activator.CreateInstance(pageType)!;
	}

	public IConfigPage Parse(IReadOnlyDictionary<Type, IConfigPage> _)
	{
		return Instance;
	}

	public Task<IConfigPage> ParseAsync(IReadOnlyDictionary<Type, IConfigPage> _, CancellationToken __)
	{
		return Task.FromResult<IConfigPage>(Instance);
	}
}
}