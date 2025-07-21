namespace Azzazelloqq.Config
{
/// <summary>
/// Describes the progress of a parsing operation, 
/// including a fractional completion value and an informational message.
/// </summary>
public readonly struct ParseProgress
{
	/// <summary>
	/// Gets the fraction of the parsing operation that has completed.
	/// Value is in the range [0.0 (no progress) … 1.0 (complete)].
	/// </summary>
	public float Progress { get; }
	
	/// <summary>
	/// Gets a human‑readable message describing the current step or status.
	/// </summary>
	public string Message { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ParseProgress"/> struct.
	/// </summary>
	/// <param name="progress">
	/// The fraction completed, between 0.0 and 1.0.
	/// </param>
	/// <param name="message">
	/// An informational message about the current progress.
	/// </param>
	public ParseProgress(float progress, string message)
	{
		Progress = progress;
		Message = message;
	}
}
}