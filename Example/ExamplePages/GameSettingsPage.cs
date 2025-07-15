namespace Azzazelloqq.Config.Example
{
public class GameSettingsPage : IConfigPage
{
	public int MaxPlayers   { get; }
	public float MusicVolume { get; }

	public GameSettingsPage(int maxPlayers, float musicVolume)
	{
		MaxPlayers = maxPlayers;
		MusicVolume = musicVolume;
	}
}
}