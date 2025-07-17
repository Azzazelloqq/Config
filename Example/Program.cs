using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Unity.Plastic.Newtonsoft.Json;

namespace Azzazelloqq.Config.Example
{
public class Program
{
	private readonly CancellationTokenSource _exampleTokenSource = new();
	
	public async void DependencyAwareConfigParserExample()
	{
		var token = _exampleTokenSource.Token;
		
		IExecutorResolver executorResolver = new SimpleResolver();

		var parseExecutors = new IParseExecutor[]
		{
			new GameSettingsExecutor(),
			new RemoteBalanceExecutor()
		};
		
		var parser = new DependencyAwareConfigParser(parseExecutors, executorResolver);
		var cfg = new Config(parser);
		
		await cfg.InitializeAsync(token);

		Console.WriteLine(cfg.GetConfigPage<RemoteBalancePage>().EnemyHpMultiplier);
	}

	private async void CompositeConfigParserExample()
	{
		var token = _exampleTokenSource.Token;
		
		var composite = new CompositeConfigParser(new JsonFileParser(string.Empty), new RemoteParser());

		var config = new Config(composite);
		await config.InitializeAsync(token);

		var page = config.GetConfigPage<RemoteBalancePage>();
		Console.WriteLine(page.EnemyHpMultiplier);
	}
}

internal class RemoteParser : IConfigParser
{
	public IConfigPage[] Parse()
	{
		return new IConfigPage[] { };
	}

	public Task<IConfigPage[]> ParseAsync(CancellationToken token)
	{
		return null;
	}
}

internal class JsonFileParser : IConfigParser
{
	private readonly string _filePath;

	public JsonFileParser(string filePath)
	{
		_filePath = filePath;
	}

	public IConfigPage[] Parse()
	{
		var json     = File.ReadAllText(_filePath);
		var settings = JsonConvert.DeserializeObject<GameSettingsPage>(json);
		return new IConfigPage[] { settings };
	}

	public Task<IConfigPage[]> ParseAsync(CancellationToken token)
		=> Task.Run(Parse, token);
}
}