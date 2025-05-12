# Azzazelloqq.Config 🗂️

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](LICENSE)  
[![GitHub release (latest by SemVer)](https://img.shields.io/github/release/Azzazelloqq/Config.svg?style=flat-square&cacheSeconds=86400)](https://github.com/Azzazelloqq/Config/releases)

A lightweight, type-safe configuration container for .NET and Unity projects.  
Leverage pluggable parsers to load your `IConfigPage` instances—synchronously or asynchronously—and retrieve them by type with zero magic.

> **Why use Azzazelloqq.Config?**
> - Strongly-typed access: No fragile string keys—just `GetConfigPage<T>()`.
> - Sync & Async Init: Call `Initialize()` or `InitializeAsync(token)` as fits your flow.
> - Custom parsers: Plug in JSON, XML, ScriptableObjects or your own format via `IConfigParser`.
> - Exception safety: Clear errors if you forget to initialize or ask for an unregistered page.

---

## ✨ Key Features

- **Dual Initialization**  
  - `Initialize()` for blocking load flows  
  - `InitializeAsync(CancellationToken)` for off-UI-thread parsing  

- **Type-based Retrieval**  
  ```csharp
  var page = config.GetConfigPage<MyConfigPage>();
  ```

- **Pluggable Parsers**  
  Implement `IConfigParser.Parse()` / `ParseAsync()` to support your data source.

- **Composite Parsers**  
  Combine multiple parsers via `CompositeConfigParser` to aggregate pages from different sources.

---

## 📦 Project Structure

```plaintext
Azzazelloqq.Config/
├── src/
│   └── Azzazelloqq.Config/
│       ├── Config.cs
│       ├── IConfig.cs
│       ├── IConfigParser.cs
│       ├── IConfigPage.cs
│       ├── IRemotePage.cs
│       └── CompositeConfigParser.cs
└── LICENSE
```

---

## 🚀 Quick Start

### 1. Define your pages

```csharp
public interface IGameSettingsPage : IConfigPage
{
    float MasterVolume { get; }
    int   MaxPlayers    { get; }
}
```

### 2. Implement a JSON parser

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
        var json = File.ReadAllText(_filePath);
        var settings = JsonConvert.DeserializeObject<GameSettingsPage>(json);
        return new IConfigPage[] { settings };
    }

    public Task<IConfigPage[]> ParseAsync(CancellationToken token)
        => Task.Run(Parse, token);
}
```

### 3. Implement a ScriptableObject parser

```csharp
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.Config;

public class SoConfigParser : IConfigParser
{
    private readonly AudioSettingsRemotePage _soPage;

    public SoConfigParser(AudioSettingsRemotePage soPage) => _soPage = soPage;

    public IConfigPage[] Parse()
    {
        return new IConfigPage[] {
            new AudioSettingsConfigPage(new AudioSettings {
                MasterVolume = _soPage.MasterVolume,
                MusicEnabled = _soPage.MusicEnabled
            })
        };
    }

    public Task<IConfigPage[]> ParseAsync(CancellationToken token)
        => Task.FromResult(Parse());
}
```

### 4. Combine and initialize

```csharp
var jsonParser = new JsonConfigParser("config.json");
var soParser   = new SoConfigParser(audioSettingsSoAsset);
var composite  = new CompositeConfigParser(jsonParser, soParser);

var config = new Config(composite);
await config.InitializeAsync(cancellationToken);

var gameSettings  = config.GetConfigPage<IGameSettingsPage>();
var audioSettings = config.GetConfigPage<AudioSettingsConfigPage>();
```

---

## 🤝 Contributing

Contributions, issues, and feature requests are welcome!  
Please open an issue or submit a pull request.

---

## 📄 License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.
