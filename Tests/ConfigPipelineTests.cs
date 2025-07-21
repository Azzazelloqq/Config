using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Azzazelloqq.Config.Tests
{
/// <summary>
/// Unit‑tests for configuration parsing and resolution pipeline.
/// Covers SimpleResolver, DependencyAwareConfigParser, CompositeConfigParser,
/// and Config behavior.
/// </summary>
[TestFixture]
internal class ConfigPipelineTests
{
	private static FakeExecutor Ex<T>(params Type[] deps)
	{
		return new FakeExecutor(typeof(T), deps);
	}


	#region ───────── SimpleResolver ─────────

	[Test]
	public void Resolver_Returns_Topological_Order()
	{
		var execA = new FakeExecutorB(typeof(PageA));
		var execB = new FakeExecutorB(typeof(PageB), typeof(PageA));
		var execC = new FakeExecutorB(typeof(PageC), typeof(PageB));

		var resolver = new SimpleResolver();
		var sorted = resolver.Resolve(new IParseExecutor[] { execC, execB, execA });

		CollectionAssert.AreEqual(new[] { execA, execB, execC }, sorted);
	}

	[Test]
	public void Resolver_Throws_On_Cycle()
	{
		var ex1 = new FakeExecutorB(typeof(PageA), typeof(PageB));
		var ex2 = new FakeExecutorB(typeof(PageB), typeof(PageA));
		var resolver = new SimpleResolver();
		Assert.Throws<InvalidOperationException>(() => resolver.Resolve(new IParseExecutor[] { ex1, ex2 }));
	}

	[Test]
	public void Resolver_Throws_On_Missing_Dependency()
	{
		var exec = new FakeExecutorB(typeof(PageB), typeof(PageA));
		var resolver = new SimpleResolver();
		Assert.Throws<InvalidOperationException>(() => resolver.Resolve(new IParseExecutor[] { exec }));
	}

	[Test]
	public void Resolver_Empty_Returns_EmptyList()
	{
		var resolver = new SimpleResolver();
		var sorted = resolver.Resolve(Array.Empty<IParseExecutor>());
		Assert.IsEmpty(sorted);
	}
	
	[Test]
	public void Resolve_EmptyArray_ReturnsEmptyList()
	{
		var resolver = new SimpleResolver();
		var sorted = resolver.Resolve(Array.Empty<IParseExecutor>());
		Assert.IsNotNull(sorted);
		Assert.AreEqual(0, sorted.Count);
	}

	[Test]
	public void Resolve_SingleExecutor_ReturnsThatExecutor()
	{
		var resolver = new SimpleResolver();
		var a = Ex<PageA>();
		var sorted = resolver.Resolve(new[] { a });
		Assert.AreEqual(1, sorted.Count);
		Assert.AreSame(a, sorted[0]);
	}
	
	[Test]
	public void Resolve_NullInput_ThrowsArgumentNullException()
	{
		var resolver = new SimpleResolver();
		Assert.Throws<ArgumentNullException>(() => resolver.Resolve(null));
	}

	[Test]
	public void Resolve_MissingDependency_MessageContainsTypeName()
	{
		var resolver = new SimpleResolver();
		var a = Ex<PageA>(typeof(PageB));
		var ex = Assert.Throws<InvalidOperationException>(() =>
			resolver.Resolve(new[] { a }));
		StringAssert.Contains(nameof(PageB), ex.Message);
	}

	[Test]
	public void Resolve_DuplicateExecutor_MessageContainsTypeName()
	{
		var resolver = new SimpleResolver();
		var a1 = Ex<PageA>();
		var a2 = Ex<PageA>();
		var ex = Assert.Throws<InvalidOperationException>(() =>
			resolver.Resolve(new[] { a1, a2 }));
		StringAssert.Contains(nameof(PageA), ex.Message);
	}

	#endregion

	#region ───── Cycle detection ─────

	[Test]
	public void Resolve_Throws_On_SelfDependency()
	{
		var a = Ex<PageA>(typeof(PageA)); // A → A (самозависимость)
		var resolver = new SimpleResolver();

		Assert.Throws<InvalidOperationException>(() =>
			resolver.Resolve(new[] { a }));
	}

	[Test]
	public void Resolve_Throws_On_Cyclic_Dependency()
	{
		var a = Ex<PageA>(typeof(PageC)); // A → C
		var b = Ex<PageB>(typeof(PageA)); // B → A
		var c = Ex<PageC>(typeof(PageB)); // C → B     ← цикл A→C→B→A

		var resolver = new SimpleResolver();

		Assert.Throws<InvalidOperationException>(() =>
			resolver.Resolve(new[] { a, b, c }));
	}
	
	// A
	// ↙ ↘
	// B   C
	// ↘ ↙
	// D
	[Test]
	public void Resolve_DiamondGraph_CorrectOrder()
	{
		var resolver = new SimpleResolver();
		
		var a = Ex<PageA>();
		var b = Ex<PageB>(typeof(PageA));
		var c = Ex<PageC>(typeof(PageA));
		var d = Ex<PageD>(typeof(PageB), typeof(PageC));

		var sorted = resolver.Resolve(new[] { d, c, b, a })
			.Select(ex => ex.TargetType)
			.ToArray();

		// A перед B и C; B и C перед D
		Assert.IsTrue(Array.IndexOf(sorted, typeof(PageA)) < Array.IndexOf(sorted, typeof(PageB)));
		Assert.IsTrue(Array.IndexOf(sorted, typeof(PageA)) < Array.IndexOf(sorted, typeof(PageC)));
		Assert.IsTrue(Array.IndexOf(sorted, typeof(PageB)) < Array.IndexOf(sorted, typeof(PageD)));
		Assert.IsTrue(Array.IndexOf(sorted, typeof(PageC)) < Array.IndexOf(sorted, typeof(PageD)));
	}

	#endregion

	#region ───── Missing / duplicate deps ─────

	[Test]
	public void Resolve_Throws_On_Missing_Dependency()
	{
		var a = Ex<PageA>(typeof(PageB)); // B отсутствует
		var resolver = new SimpleResolver();

		Assert.Throws<InvalidOperationException>(() =>
			resolver.Resolve(new[] { a }));
	}

	[Test]
	public void Resolve_Throws_On_Duplicate_TargetType()
	{
		var a1 = Ex<PageA>();
		var a2 = Ex<PageA>(); // дубликат A
		var resolver = new SimpleResolver();

		Assert.Throws<InvalidOperationException>(() =>
			resolver.Resolve(new[] { a1, a2 }));
	}

	#endregion

	#region ───── Complex graph (happy path) ─────

	/*
	 D → E → C → A
      ↑   ↑
      B   F → G
	
	  Допустимый топологический порядок, например:
	  D, E, B, C, A, F, G
	*/
	[Test]
	public void Resolve_Sorts_Complex_Graph_Correctly()
	{
		var d = Ex<PageD>();
		var e = Ex<PageE>(typeof(PageD));
		var b = Ex<PageB>();                             // убрали зависимость от PageA
		var c = Ex<PageC>(typeof(PageB), typeof(PageE));
		var a = Ex<PageA>(typeof(PageC));
		var f = Ex<PageF>();
		var g = Ex<PageG>(typeof(PageC), typeof(PageF));

		var resolver = new SimpleResolver();

		// materialize в List, чтобы был FindIndex
		var sorted = resolver
			.Resolve(new IParseExecutor[] { a, b, c, d, e, f, g })
			.ToList();

		bool ComesBefore(Type x, Type y) =>
			sorted.FindIndex(ex => ex.TargetType == x) <
			sorted.FindIndex(ex => ex.TargetType == y);

		// Assert
		Assert.IsTrue(ComesBefore(typeof(PageD), typeof(PageE)));
		Assert.IsTrue(ComesBefore(typeof(PageE), typeof(PageC)));
		Assert.IsTrue(ComesBefore(typeof(PageB), typeof(PageC)));
		Assert.IsTrue(ComesBefore(typeof(PageC), typeof(PageA)));
		Assert.IsTrue(ComesBefore(typeof(PageF), typeof(PageG)));
	}
	
	[Test]
	public void Resolve_Throws_On_MultiNode_Cycle_With_Correct_Message()
	{
		// Arrange: A → C, C → B, B → A
		var a = Ex<PageA>(typeof(PageC));
		var b = Ex<PageB>(typeof(PageA));
		var c = Ex<PageC>(typeof(PageB));

		var resolver = new SimpleResolver();

		// Act & Assert
		var ex = Assert.Throws<InvalidOperationException>(() =>
			resolver.Resolve(new IParseExecutor[] { a, b, c }));

		// Проверяем, что сообщение начинается с "Circular dependency at"
		StringAssert.StartsWith("Circular dependency at", ex.Message);

		// И что в тексте есть имя типа, в котором цикл зафиксирован (здесь PageA)
		StringAssert.Contains(nameof(PageA), ex.Message);
	}
	
	[Test]
	public void Resolve_MultipleIndependent_RespectsInputOrder()
	{
		var resolver = new SimpleResolver();
		
		var a = Ex<PageA>();
		var b = Ex<PageB>();
		var c = Ex<PageC>();

		var sorted = resolver.Resolve(new[] { b, a, c });
		CollectionAssert.AreEqual(new[] { b, a, c }, sorted);
	}

	[Test]
	public void Resolve_LongChain_CorrectLinearOrder()
	{
		var resolver = new SimpleResolver();

		var a = Ex<PageA>();
		var b = Ex<PageB>(typeof(PageA));
		var c = Ex<PageC>(typeof(PageB));
		var d = Ex<PageD>(typeof(PageC));

		var sorted = resolver.Resolve(new[] { d, c, b, a })
			.Select(ex => ex.TargetType)
			.ToArray();

		Assert.AreEqual(new[] { typeof(PageA), typeof(PageB), typeof(PageC), typeof(PageD) }, sorted);
	}

	#endregion

	#region ───────── DependencyAwareConfigParser ─────────

	[Test]
	public void Parser_Returns_Pages_In_Dependency_Order()
	{
		var execA = new FakeExecutorB(typeof(PageA));
		var execB = new FakeExecutorB(typeof(PageB), typeof(PageA));
		var execC = new FakeExecutorB(typeof(PageC), typeof(PageB));

		var parser = new DependencyAwareConfigParser(
			new IParseExecutor[] { execC, execB, execA },
			new SimpleResolver());

		var pages = parser.Parse();
		CollectionAssert.AreEqual(new[] { execA.Instance, execB.Instance, execC.Instance }, pages);
	}

	[Test]
	public async Task ParserAsync_Returns_Same_Order()
	{
		var execA = new FakeExecutorB(typeof(PageA));
		var execB = new FakeExecutorB(typeof(PageB), typeof(PageA));

		var parser = new DependencyAwareConfigParser(
			new IParseExecutor[] { execB, execA },
			new SimpleResolver());

		var pages = await parser.ParseAsync(CancellationToken.None);
		CollectionAssert.AreEqual(new[] { execA.Instance, execB.Instance }, pages);
	}

	[Test]
	public void Parser_EmptyExecutors_Returns_Empty()
	{
		var parser = new DependencyAwareConfigParser(
			Array.Empty<IParseExecutor>(), new SimpleResolver());
		Assert.IsEmpty(parser.Parse());
	}

	[Test]
	public async Task ParserAsync_EmptyExecutors_Returns_Empty()
	{
		var parser = new DependencyAwareConfigParser(
			Array.Empty<IParseExecutor>(), new SimpleResolver());
		var pages = await parser.ParseAsync(CancellationToken.None);
		Assert.IsEmpty(pages);
	}

	#endregion

	#region ───────── CompositeConfigParser ─────────

	[Test]
	public void CompositeParser_Merges_Results_Synchronously()
	{
		var pageA = new PageA();
		var pageB = new PageB();
		var p1 = new FakeSimpleParser(pageA);
		var p2 = new FakeSimpleParser(pageB);

		var composite = new CompositeConfigParser(p1, p2);
		var pages = composite.Parse();

		CollectionAssert.AreEqual(new IConfigPage[] { pageA, pageB }, pages);
	}

	[Test]
	public async Task CompositeParser_Merges_Results_Asynchronously()
	{
		var pageA = new PageA();
		var pageB = new PageB();
		var p1 = new FakeSimpleParser(pageA);
		var p2 = new FakeSimpleParser(pageB);

		var composite = new CompositeConfigParser(p1, p2);
		var pages = await composite.ParseAsync(CancellationToken.None);

		CollectionAssert.AreEqual(new IConfigPage[] { pageA, pageB }, pages);
	}

	#endregion

	#region ───────── Config ─────────

	[Test]
	public void Config_Initialize_And_GetConfigPage_Works()
	{
		var page = new PageA();
		var parser = new FakeSimpleParser(page);
		var cfg = new Config(parser);

		cfg.Initialize();
		var result = cfg.GetConfigPage<PageA>();
		Assert.AreSame(page, result);
	}

	[Test]
	public void Config_Initialize_Twice_Throws()
	{
		var parser = new FakeSimpleParser(new PageA());
		var cfg = new Config(parser);
		cfg.Initialize();
		Assert.Throws<InvalidOperationException>(() => cfg.Initialize());
	}

	[Test]
	public async Task Config_InitializeAsync_Twice_Throws()
	{
		var parser = new FakeSimpleParser(new PageA());
		var cfg = new Config(parser);
		await cfg.InitializeAsync(CancellationToken.None);
		Assert.ThrowsAsync<InvalidOperationException>(async () => await cfg.InitializeAsync(CancellationToken.None));
	}

	[Test]
	public void Config_GetConfigPage_NotFound_Throws()
	{
		var parser = new FakeSimpleParser(new PageA());
		var cfg = new Config(parser);
		cfg.Initialize();
		Assert.Throws<KeyNotFoundException>(() => cfg.GetConfigPage<PageB>());
	}

	#endregion

	#region ───────── CancellationToken ─────────

	[Test]
	public void ParserAsync_Propagates_Cancellation()
	{
		var executor = new FakeCancelExecutor();
		var parser = new DependencyAwareConfigParser(
			new IParseExecutor[] { executor }, new SimpleResolver());

		using var cts = new CancellationTokenSource();
		cts.Cancel();
		Assert.That(async () => await parser.ParseAsync(cts.Token),
			Throws.TypeOf<OperationCanceledException>()
				.Or.TypeOf<TaskCanceledException>());
	}

	#endregion
}
}