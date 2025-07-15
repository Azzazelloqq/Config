using System;
using System.Collections.Generic;

namespace Azzazelloqq.Config
{
public sealed class SimpleResolver : IExecutorResolver
{
	public IReadOnlyList<IParseExecutor> Resolve(IParseExecutor[] parseExecutors)
	{
		if (parseExecutors == null)
		{
			throw new ArgumentNullException(nameof(parseExecutors));
		}

		var map = new Dictionary<Type, IParseExecutor>(parseExecutors.Length);
		foreach (var ex in parseExecutors)
		{
			var key = ex.TargetType;
			if (!map.TryAdd(key, ex))
			{
				throw new InvalidOperationException($"Duplicate executor for {key}");
			}
		}

		var sorted = new List<IParseExecutor>(parseExecutors.Length);
		var visited = new HashSet<Type>();
		var stack = new HashSet<Type>(); 

		foreach (var ex in parseExecutors)
		{
			Visit(ex, map, visited, stack, sorted);
		}

		return sorted;
	}

	private static void Visit(
		IParseExecutor executor,
		IDictionary<Type, IParseExecutor> map,
		HashSet<Type> visited,
		HashSet<Type> stack,
		IList<IParseExecutor> output)
	{
		if (visited.Contains(executor.TargetType))
		{
			return;
		}

		if (!stack.Add(executor.TargetType))
		{
			throw new InvalidOperationException($"Circular dependency at {executor.TargetType}");
		}

		foreach (var depType in executor.Dependencies)
		{
			if (!map.TryGetValue(depType, out var depEx))
			{
				throw new InvalidOperationException($"Missing dependency {depType}");
			}

			Visit(depEx, map, visited, stack, output);
		}

		stack.Remove(executor.TargetType);
		visited.Add(executor.TargetType);
		output.Add(executor);
	}
}
}