using System.Diagnostics;

namespace DotNetToolbox.Algorithms.Sorting;

/// <summary>
/// Provides topological sorting over a directed acyclic graph (DAG).
/// </summary>
/// <typeparam name="T">Node type.</typeparam>
public static class TopologicalSorter<T> where T : notnull
{
    /// <summary>
    /// Returns <paramref name="nodes"/> ordered so that for every dependency edge (A → B),
    /// A appears before B in the result. When a cycle is detected the original node order
    /// is returned and a warning is emitted.
    /// </summary>
    /// <param name="nodes">All nodes in the graph.</param>
    /// <param name="dependencies">Directed edges as (from, to) tuples meaning "from must come before to".</param>
    /// <param name="comparer">
    /// Equality comparer for <typeparamref name="T"/>. Defaults to <see cref="EqualityComparer{T}.Default"/> when null.
    /// </param>
    /// <returns>
    /// A new list with nodes ordered by dependency. Nodes not mentioned in any edge retain their original relative
    /// order among themselves.
    /// </returns>
    /// <remarks>
    /// Thread-safe by construction: this method is pure and uses no shared state.
    /// </remarks>
    public static IReadOnlyList<T> Sort(
        IEnumerable<T> nodes,
        IEnumerable<(T From, T To)> dependencies,
        IEqualityComparer<T>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(dependencies);

        comparer ??= EqualityComparer<T>.Default;

        var nodeList = new List<T>();
        var nodeIndex = new Dictionary<T, int>(comparer);
        var canonicalByKey = new Dictionary<T, T>(comparer);
        foreach (var node in nodes)
        {
            if (canonicalByKey.TryGetValue(node, out var existing))
            {
                // Treat duplicates as one node, but keep the first instance as the canonical value.
                continue;
            }

            canonicalByKey.Add(node, node);
            nodeIndex.Add(node, nodeList.Count);
            nodeList.Add(node);
        }

        if (nodeList.Count == 0)
        {
            return Array.Empty<T>();
        }

        var adjacency = new Dictionary<T, List<T>>(comparer);
        var inDegree = new Dictionary<T, int>(comparer);
        foreach (var node in nodeList)
        {
            adjacency[node] = [];
            inDegree[node] = 0;
        }

        foreach (var (fromRaw, toRaw) in dependencies)
        {
            if (!canonicalByKey.TryGetValue(fromRaw, out var from) || !canonicalByKey.TryGetValue(toRaw, out var to))
            {
                continue;
            }

            adjacency[from].Add(to);
            inDegree[to] = inDegree[to] + 1;
        }

        var queue = new Queue<T>(nodeList.Where(n => inDegree[n] == 0).OrderBy(n => nodeIndex[n]));
        var result = new List<T>(capacity: nodeList.Count);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            result.Add(current);

            foreach (var next in adjacency[current])
            {
                var newDegree = inDegree[next] - 1;
                inDegree[next] = newDegree;
                if (newDegree == 0)
                {
                    queue.Enqueue(next);
                }
            }

            if (queue.Count > 1)
            {
                var stable = queue.OrderBy(n => nodeIndex[n]).ToArray();
                queue.Clear();
                foreach (var item in stable)
                {
                    queue.Enqueue(item);
                }
            }
        }

        if (result.Count != nodeList.Count)
        {
            Trace.TraceWarning("Topological sort cycle detected; returning original order.");
            return nodeList;
        }

        return result;
    }
}
