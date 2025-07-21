using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Azzazelloqq.Config;

namespace Azzazelloqq.Config.Tests
{
[TestFixture]
public class ConfigParserProgressTests
{
	[SetUp]
	public void ClearSynchronizationContext()
	{
		// SynchronizationContext.SetSynchronizationContext(null);
	}

	#region CompositeConfigParser progress overloads

	[Test]
	public async Task CompositeParser_ParseAsyncWithProgress_ReportsAndReturnsPagesAsync()
	{
		var pageA = new PageA();
		var pageB = new PageB();
		var composite = new CompositeConfigParser(
			new FakeSimpleParser(pageA),
			new FakeSimpleParser(pageB)
		);

		var progressList = new List<ParseProgress>();
		// ждём ровно 2 сигнала прогресса
		var progressTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

		// Act
		var parseTask = composite.ParseAsync(
			new Progress<ParseProgress>(p =>
			{
				progressList.Add(p);
				if (progressList.Count == 2)
					progressTcs.TrySetResult(true);
			}),
			CancellationToken.None
		);

		// ждём два события или таймаут
		var winner = await Task.WhenAny(progressTcs.Task, Task.Delay(500));
		Assert.AreSame(progressTcs.Task, winner, "Не дождались ровно двух событий прогресса");

		// дожидаемся результата парсинга
		var pages = await parseTask;

		// Assert pages
		Assert.AreEqual(2, pages.Length);
		CollectionAssert.AreEqual(new IConfigPage[] { pageA, pageB }, pages);

		// Assert progress
		Assert.AreEqual(2, progressList.Count);
		Assert.AreEqual(0.5f, progressList[0].Progress);
		Assert.AreEqual(1.0f, progressList[1].Progress);
	}

	[Test]
	public async Task CompositeParser_ParseAsyncWithCallbacks_ReportsAndInvokesOnParsed()
	{
		var pageA = new PageA();
		var pageB = new PageB();
		var p1 = new FakeSimpleParser(pageA);
		var p2 = new FakeSimpleParser(pageB);
		var composite = new CompositeConfigParser(p1, p2);

		var progressEvents = new List<ParseProgress>();
		var progressTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var onParsedTcs = new TaskCompletionSource<IConfigPage[]>();

		// Act
		composite.ParseAsync(
			prog =>
			{
				progressEvents.Add(prog);
				if (progressEvents.Count == 2)
				{
					// как только получили второе событие — завершаем ожидание
					progressTcs.TrySetResult(true);
				}
			},
			pages => { onParsedTcs.TrySetResult(pages); },
			CancellationToken.None);

		// Ждём ровно двух событий прогресса (таймаут 1с)
		var completed = await Task.WhenAny(progressTcs.Task, Task.Delay(1000));
		Assert.AreSame(progressTcs.Task, completed, "Не дождались точно двух прогресс‑событий");

		// После этого уже ждём результата разбора (onParsed)
		var resultPages = await onParsedTcs.Task;

		// Assert: ровно две записи в списке
		Assert.AreEqual(2, progressEvents.Count, "Должно быть строго два прогресс‑события");
		Assert.AreEqual(0.5f, progressEvents[0].Progress, "Первое событие — 1/2");
		Assert.AreEqual(1.0f, progressEvents[1].Progress, "Второе событие — 2/2");

		// И в конце проверяем, что парсер действительно вернул оба объекта
		CollectionAssert.AreEqual(new IConfigPage[] { pageA, pageB }, resultPages);
	}

	#endregion

	#region DependencyAwareConfigParser progress overloads

	[Test]
	public async Task DependencyParser_ParseAsyncWithProgress_ReportsAndReturnsPagesAsync()
	{
		// Arrange
		var execA  = new FakeExecutorB(typeof(PageA));
		var execB  = new FakeExecutorB(typeof(PageB), typeof(PageA));
		var parser = new DependencyAwareConfigParser(
			new IParseExecutor[] { execA, execB },
			new SimpleResolver()
		);

		var progressList = new List<ParseProgress>();
		var progressTcs  = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

		// Act
		var parseTask = parser.ParseAsync(
			new Progress<ParseProgress>(p =>
			{
				progressList.Add(p);
				if (progressList.Count == 2)
					progressTcs.TrySetResult(true);
			}),
			CancellationToken.None
		);

		// ждём два события или таймаут
		var winner = await Task.WhenAny(progressTcs.Task, Task.Delay(500));
		Assert.AreSame(progressTcs.Task, winner, "Не дождались ровно двух событий прогресса");

		// дожидаемся результата парсинга
		var pages = await parseTask;

		// Assert pages
		Assert.AreEqual(2, pages.Length);
		Assert.IsInstanceOf<PageA>(pages[0]);
		Assert.IsInstanceOf<PageB>(pages[1]);

		// Assert progress
		Assert.AreEqual(2, progressList.Count);
		Assert.AreEqual(0.5f, progressList[0].Progress);
		StringAssert.Contains("Parsed page PageA", progressList[0].Message);
		Assert.AreEqual(1.0f, progressList[1].Progress);
		StringAssert.Contains("Parsed page PageB", progressList[1].Message);
	}

	[Test]
	public async Task DependencyParser_ParseAsyncWithCallbacks_ReportsAndInvokesOnParsed()
	{
		// Arrange
		var execA     = new FakeExecutorB(typeof(PageA));
		var execB     = new FakeExecutorB(typeof(PageB), typeof(PageA));
		var parser    = new DependencyAwareConfigParser(
			new IParseExecutor[] { execA, execB },
			new SimpleResolver()
		);

		var progressList = new List<ParseProgress>();
		var progressTcs  = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var parsedTcs    = new TaskCompletionSource<IConfigPage[]>();

		// Act
		parser.ParseAsync(
			prog =>
			{
				progressList.Add(prog);
				if (progressList.Count == 2)
					progressTcs.TrySetResult(true);
			},
			pages => parsedTcs.TrySetResult(pages),
			CancellationToken.None
		);

		// сначала ждём два события прогресса
		var winner = await Task.WhenAny(progressTcs.Task, Task.Delay(500));
		Assert.AreSame(progressTcs.Task, winner, "Не дождались ровно двух событий прогресса");

		// затем ждём onParsed
		var resultPages = await parsedTcs.Task;

		// Assert pages
		Assert.AreEqual(2, resultPages.Length);
		Assert.IsInstanceOf<PageA>(resultPages[0]);
		Assert.IsInstanceOf<PageB>(resultPages[1]);

		// Assert progress
		Assert.AreEqual(2, progressList.Count);
		Assert.AreEqual(0.5f, progressList[0].Progress);
		Assert.AreEqual(1.0f, progressList[1].Progress);
	}

	#endregion
}
}