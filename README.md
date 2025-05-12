# Azzazazelloqq.Config 🗂️

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](LICENSE)  
[![GitHub release (latest by SemVer)](https://img.shields.io/github/release/Azzazelloqq/Config.svg?style=flat-square&cacheSeconds=86400)](https://github.com/Azzazelloqq/Config/releases)

A lightweight, type-safe configuration container for .NET and Unity projects.  
Leverage a pluggable parser to load your `IConfigPage` instances—synchronously or asynchronously—and retrieve them by type with zero magic.

> **Why use Azzazelloqq.Config?**  
> - **Strongly-typed access**: No fragile string keys—just `GetConfigPage<T>()`.  
> - **Sync & Async Init**: Call `Initialize()` or `InitializeAsync(token)` as fits your flow.  
> - **Custom parsers**: Plug in JSON, XML, ScriptableObjects or your own format via `IConfigParser`.  
> - **Exception safety**: Clear errors if you forget to initialize or ask for an unregistered page.

---

## ✨ Key Features

- **Dual Initialization**  
  - **`Initialize()`** for blocking load flows  
  - **`InitializeAsync(CancellationToken)`** for off-UI-thread parsing  

- **Type-based Retrieval**  
  ```csharp
  T page = config.GetConfigPage<TPage>();

## 📦 Project Structure

```plaintext
Azzazelloqq.Config/
├── src/
│   ├── Azzazelloqq.Config/
│   │   ├── Config.cs              // Core container
│   │   ├── IConfig.cs             // Public interface
│   │   ├── IConfigParser.cs       // Parser interface
│   │   ├── IConfigPage.cs         // Marker for pages
│   │   └── IRemotePage.cs         // Optional tag for “raw” data pages
└── LICENSE
```
---

## 🚀 Quick Start

### 1. Define your pages
```csharp
public interface IGameSettingsPage : IConfigPage
{
    float MasterVolume { get; }
    int MaxPlayers    { get; }
}
```
### 2. Implement a parser
```csharp
public class JsonConfigParser : IConfigParser
{
    public IConfigPage[] Parse()
    {
        // Load JSON files, map to concrete pages
        return new IConfigPage[] { new GameSettingsPage(...), /* … */ };
    }

    public Task<IConfigPage[]> ParseAsync(CancellationToken token)
        => Task.Run(Parse, token);
}
```
