using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Azzazelloqq.Config
{
/// <summary>
/// Implementations may perform synchronous or asynchronous work and
/// can declare dependencies on other pages.
/// </summary
public interface IParseExecutor
{
	/// <summary>The concrete page type produced by this executor.</summary>
	public Type TargetType { get; }
	
	/// <summary>
	/// Types of pages that must be present in the context before
	/// this executor can run.
	/// </summary>
	public IReadOnlyCollection<Type> Dependencies { get; }
	
	/// <summary>
	/// Synchronously parse the page.
	/// </summary>
	public IConfigPage Parse(IReadOnlyDictionary<Type, IConfigPage> context);
	
	/// <summary>
	/// Asynchronously parse the page.
	/// </summary>
	public Task<IConfigPage> ParseAsync(IReadOnlyDictionary<Type, IConfigPage> context, CancellationToken ct);
}
}