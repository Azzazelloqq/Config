using System.Collections.Generic;

namespace Azzazelloqq.Config
{
/// <summary>
/// Resolves <see cref="IParseExecutor"/> instances into
/// a deterministic order that satisfies all declared dependencies.
/// </summary>
public interface IExecutorResolver
{
	/// <summary>
	/// Returns executors in a processed order or throws if a cycle /
	/// missing dependency is detected.
	/// </summary>
	public IReadOnlyList<IParseExecutor> Resolve(IParseExecutor[] parseExecutors);
}
}