using System;
using System.Threading;
using System.Threading.Tasks;

namespace Azzazelloqq.Config
{
/// <summary>
/// Defines the contract for parsing one or more <see cref="IConfigPage"/> instances
/// from a data source, in both synchronous and asynchronous fashions,
/// with optional progress reporting.
/// </summary>
public interface IConfigParser
{
	/// <summary>
	/// Parses all configuration pages synchronously.
	/// </summary>
	/// <returns>
	/// An array of parsed <see cref="IConfigPage"/> objects.
	/// </returns>
	/// <exception cref="InvalidOperationException">
	/// If required dependencies are missing or a cycle is detected.
	/// </exception>
	public IConfigPage[] Parse();
	
	/// <summary>
	/// Parses all configuration pages asynchronously.
	/// </summary>
	/// <param name="token">
	/// A <see cref="CancellationToken"/> that can be used to cancel the operation.
	/// </param>
	/// <returns>
	/// A <see cref="Task"/> that resolves to an array of parsed <see cref="IConfigPage"/> objects.
	/// </returns>
	/// <exception cref="OperationCanceledException">
	/// If <paramref name="token"/> is canceled before or during execution.
	/// </exception>
	/// <exception cref="InvalidOperationException">
	/// If required dependencies are missing or a cycle is detected.
	/// </exception>
	public Task<IConfigPage[]> ParseAsync(CancellationToken token);
	
	
	/// <summary>
	/// Parses all configuration pages asynchronously, reporting progress.
	/// </summary>
	/// <param name="progress">
	/// An <see cref="IProgress{ParseProgress}"/> instance that will receive
	/// periodic <see cref="ParseProgress"/> updates describing the fraction
	/// completed and an informational message.
	/// </param>
	/// <param name="token">
	/// A <see cref="CancellationToken"/> that can be used to cancel the operation.
	/// </param>
	/// <returns>
	/// A <see cref="Task"/> that resolves to an array of parsed <see cref="IConfigPage"/> objects.
	/// </returns>
	/// <exception cref="OperationCanceledException">
	/// If <paramref name="token"/> is canceled before or during execution.
	/// </exception>
	/// <exception cref="InvalidOperationException">
	/// If required dependencies are missing or a cycle is detected.
	/// </exception>
	public Task<IConfigPage[]> ParseAsync(IProgress<ParseProgress> progress, CancellationToken token);
	
	/// <summary>
	/// Parses all configuration pages asynchronously, reporting progress via callbacks,
	/// and invokes a completion callback when done.
	/// </summary>
	/// <param name="progress">
	/// A callback invoked with <see cref="ParseProgress"/> updates
	/// describing the fraction completed and an informational message.
	/// </param>
	/// <param name="onParsed">
	/// A callback invoked once all pages have been parsed,
	/// receiving the array of <see cref="IConfigPage"/> results.
	/// </param>
	/// <param name="token">
	/// A <see cref="CancellationToken"/> that can be used to cancel the operation.
	/// </param>
	/// <exception cref="OperationCanceledException">
	/// If <paramref name="token"/> is canceled before or during execution.
	/// </exception>
	/// <exception cref="InvalidOperationException">
	/// If required dependencies are missing or a cycle is detected.
	/// </exception>
	public void ParseAsync(Action<ParseProgress> progress, Action<IConfigPage[]> onParsed, CancellationToken token);
}
}