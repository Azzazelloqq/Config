using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Azzazelloqq.Config
{
public class Config : IConfig
{
	public bool IsInitialized { get; private set; }

	private Dictionary<Type, IConfigPage> _configDataContainer;
	private readonly IConfigParser _configParser;

	public Config(IConfigParser configParser)
	{
		_configParser = configParser;

		IsInitialized = false;
	}

	public void Initialize()
	{
		if (IsInitialized)
		{
			throw new InvalidOperationException("Config is already initialized");
		}

		_configDataContainer = ParseConfig();

		IsInitialized = true;
	}

	public async Task InitializeAsync(CancellationToken token)
	{
		if (IsInitialized)
		{
			throw new InvalidOperationException("Config is already initialized");
		}
		
		token.ThrowIfCancellationRequested();

		_configDataContainer = await ParseConfigAsync(token);

		IsInitialized = true;
	}

	public T GetConfigPage<T>() where T : IConfigPage
	{
		if (_configDataContainer == null)
		{
			throw new Exception("Config data container is null");
		}
		
		if (!_configDataContainer.TryGetValue(typeof(T), out var data))
		{
			throw new KeyNotFoundException($"Need add data {typeof(T).Name} in {_configParser.GetType().Name}");
		}

		if (data is T concreteData)
		{
			return concreteData;
		}
		
		throw new InvalidCastException($"Config entry for {typeof(T).Name} is of type {data.GetType().Name}");
	}

	private Dictionary<Type, IConfigPage> ParseConfig()
	{
		var configData = _configParser.Parse();
		var parsedData = new Dictionary<Type, IConfigPage>(configData.Length);

		foreach (var data in configData)
		{
			var type = data.GetType();
			parsedData[type] = data;
		}

		return parsedData;
	}

	private async Task<Dictionary<Type, IConfigPage>> ParseConfigAsync(CancellationToken token)
	{
		var configData = await _configParser.ParseAsync(token);
		var parsedData = new Dictionary<Type, IConfigPage>(configData.Length);

		foreach (var data in configData)
		{
			var type = data.GetType();
			parsedData[type] = data;
		}

		return parsedData;
	}
}
}