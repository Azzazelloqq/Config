using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Plastic.Antlr3.Runtime;

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
		
		var composite = new CompositeConfigParser(new JsonFileParser(), new RemoteParser());

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
	public IConfigPage[] Parse()
	{
		return new IConfigPage[] { };
	}

	public Task<IConfigPage[]> ParseAsync(CancellationToken token)
	{
		return null;
	}
}
}