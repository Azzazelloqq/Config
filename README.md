# Azzazazelloqq.Config 🗂️

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](LICENSE)  
[![GitHub Release](https://img.shields.io/github/release/Azzazelloqq/Azzazelloqq.Config.svg?style=flat-square&cacheSeconds=86400)](https://github.com/Azzazelloqq/Azzazelloqq.Config/releases)

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
