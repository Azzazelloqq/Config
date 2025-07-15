using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Azzazelloqq.Config.Example
{
public class RemoteBalanceExecutor : IParseExecutor
{
	public Type TargetType => typeof(RemoteBalancePage);
	
	public IReadOnlyCollection<Type> Dependencies { get; } = new[] { typeof(GameSettingsPage) };

	public IConfigPage Parse(IReadOnlyDictionary<Type, IConfigPage> ctx)
	{
		var settings = (GameSettingsPage)ctx[typeof(GameSettingsPage)];
		return CalcBalance(settings);
	}

	public Task<IConfigPage> ParseAsync(
		IReadOnlyDictionary<Type, IConfigPage> ctx,
		CancellationToken ct)
	{
		// imagine HTTP request here; demo just delegates
		return Task.FromResult(Parse(ctx));
	}

	private static RemoteBalancePage CalcBalance(GameSettingsPage page)
	{
		var enemyHpMultiplier = 1f + (page.MaxPlayers - 1) * 0.3f;
		
		return new RemoteBalancePage(enemyHpMultiplier);
	}
}
}