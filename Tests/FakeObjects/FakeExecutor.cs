using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Azzazelloqq.Config.Tests
{
public class FakeExecutor : IParseExecutor
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