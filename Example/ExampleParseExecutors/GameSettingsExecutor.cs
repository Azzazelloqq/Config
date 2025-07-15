using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Azzazelloqq.Config.Example
{
public class GameSettingsExecutor : IParseExecutor
{
	public Type TargetType => typeof(GameSettingsPage);

	public IReadOnlyCollection<Type> Dependencies { get; } = Array.Empty<Type>();

	public IConfigPage Parse(IReadOnlyDictionary<Type, IConfigPage> _)
	{
		return ReadFromDisk();
	}

	public Task<IConfigPage> ParseAsync(
		IReadOnlyDictionary<Type, IConfigPage> _,
		CancellationToken __)
	{
		return Task.FromResult<IConfigPage>(ReadFromDisk());
	}

	private static GameSettingsPage ReadFromDisk()
	{
		return new GameSettingsPage(
			4,
			0.75f
		);
	}
}
}