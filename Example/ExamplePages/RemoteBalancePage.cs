namespace Azzazelloqq.Config.Example
{
public class RemoteBalancePage: IConfigPage
{
	public float EnemyHpMultiplier { get; }

	public RemoteBalancePage(float enemyHpMultiplier)
	{
		EnemyHpMultiplier = enemyHpMultiplier;
	}
}
}