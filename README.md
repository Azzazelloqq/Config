# Azzazelloqq.Config 🗂️

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](LICENSE)  
[![GitHub release (latest by SemVer)](https://img.shields.io/github/release/Azzazelloqq/Config.svg?style=flat-square&cacheSeconds=86400)](https://github.com/Azzazelloqq/Config/releases)

A lightweight, type-safe configuration container for .NET and Unity projects.  
Leverage pluggable parsers to load your `IConfigPage` instances—synchronously or asynchronously—and retrieve them by type with zero magic.

> **Why use Azzazelloqq.Config?**
> - Strongly‑typed access – no brittle string keys, just GetConfigPage<T>().
> - Sync & Async init – use Initialize() or InitializeAsync(token) to fit your thread model.
> - Pluggable parsers – JSON, XML, ScriptableObjects, remote APIs… implement IConfigParser once and plug it in.
> - Dependency‑aware pipeline – declare page dependencies; we resolve order and execute independent stages in parallel.
> - Clear diagnostics – explicit exceptions for duplicate pages, missing dependencies and circular graphs.

---

## ✨ Key Features

- **Dual Initialization**  
  - `Initialize()` for blocking load flows  
  - `InitializeAsync(CancellationToken)` for off-UI-thread parsing  

- **Dependency‑Aware Execution**  
 - Describe each page with an IParseExecutor (target type + dependency list).
 - `SimpleResolver` topologically sorts executors, detecting cycles, duplicates and gaps.
 - `DependencyAwareConfigParser` runs executors level‑by‑level—parallelising pages that have no outstanding dependencies (sync via `Parallel.Fo`r, async via `Task.WhenAll`).

- **Type‑based Retrieval**  
  `var page = config.GetConfigPage<MyConfigPage>();`

- **Pluggable & Composite Parsers**  
  Mix and match data sources with CompositeConfigParser—combine JSON files, ScriptableObjects, executor pipelines, remote endpoints and more.

---

## 📦 Project Structure

```plaintext
Assets/Config/
├── Config.asmdef                  # main assembly definition
├── Source/                        # core library source
│   ├── Main/
│   │   ├── Config.cs
│   │   └── IConfig.cs
│   ├── Page/
│   │   └── IConfigPage.cs
│   ├── Parser/
│   │   ├── CompositeConfigParser.cs
│   │   ├── DependencyAwareConfigParser.cs
│   │   ├── IConfigParser.cs
│   │   └── IParseExecutor.cs
│   └── Resolver/
│       ├── IExecutorResolver.cs
│       └── SimpleResolver.cs
├── Example/                       # sample usage project
│   ├── Config.Example.asmdef
│   ├── ExamplePages/
│   │   ├── GameSettingsPage.cs
│   │   └── RemoteBalancePage.cs
│   ├── ExampleParseExecutors/
│   │   ├── GameSettingsExecutor.cs
│   │   └── RemoteBalanceExecutor.cs
│   └── Program.cs
├── Tests/                         # NUnit tests for library
│   ├── Config.Tests.asmdef
│   ├── ConfigPipelineTests.cs
│   └── DependencyAwareConfigParserTests.cs
├── LICENSE
├── package.json
└── README.md
```

---

## 🚀 Quick Start

### 1. Define your pages

```csharp
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
```

### 2. (Optional) Describe page dependencies with executors

If a page depends on data from others, wrap its creation in an IParseExecutor and list the required types:

```csharp
class GameSettingsExecutor : IParseExecutor
{
    public Type TargetType => typeof(GameSettingsPage);
    public IReadOnlyCollection<Type> Dependencies => Array.Empty<Type>();

    public IConfigPage Parse(IReadOnlyDictionary<Type, IConfigPage> ctx)
        => LoadSettings();

    public Task<IConfigPage> ParseAsync(
        IReadOnlyDictionary<Type, IConfigPage> ctx,
        CancellationToken ct) => Task.FromResult(Parse(ctx));

    private static GameSettingsPage LoadSettings() => new()
    {
        MasterVolume = 0.8f,
        MaxPlayers   = 4
    };
}
```

### 3.  Implement a JSON(Example) parser

```csharp
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.Config;
using Newtonsoft.Json; // Install via NuGet

public class JsonConfigParser : IConfigParser
{
    private readonly string _filePath;

    public JsonConfigParser(string filePath) => _filePath = filePath;

    public IConfigPage[] Parse()
    {
        var json     = File.ReadAllText(_filePath);
        var settings = JsonConvert.DeserializeObject<GameSettingsPage>(json);
        return new IConfigPage[] { settings };
    }

    public Task<IConfigPage[]> ParseAsync(CancellationToken token)
        => Task.Run(Parse, token);
}
```

### 4. Combine and initialize

```csharp
var jsonParser = new JsonConfigParser("config.json");

// Dependency‑aware pipeline with executors
var executors  = new IParseExecutor[] { new GameSettingsExecutor() };
var depParser  = new DependencyAwareConfigParser(executors, new SimpleResolver());

var composite  = new CompositeConfigParser(jsonParser, depParser);

var config = new Config(composite);
await config.InitializeAsync(cancellationToken);

var gameSettings = config.GetConfigPage<GameSettingsPage>();
```

---

## ⚙️ Extending

Custom Resolver Strategy

Need priority ties or custom order? Implement IExecutorResolver and supply it to DependencyAwareConfigParser.

Multiple Pipelines

Nest parsers however you like—composites of dependency‑aware groups, or vice‑versa.

---

## 🤝 Contributing

Contributions, issues, and feature requests are welcome!  
Please open an issue or submit a pull request.

---

## 📄 License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.
