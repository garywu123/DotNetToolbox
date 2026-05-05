# API: DotNetToolbox.Algorithms

## Namespace: `DotNetToolbox.Algorithms.Sorting`

### `TopologicalSorter<T>`

`public static class TopologicalSorter<T> where T : notnull`

#### `Sort`

```csharp
public static IReadOnlyList<T> Sort(
    IEnumerable<T> nodes,
    IEnumerable<(T From, T To)> dependencies,
    IEqualityComparer<T>? comparer = null)
```

Orders <paramref name="nodes"/> so that for every dependency edge (A → B), A appears before B.

- Cycle handling: when a cycle is detected, returns the original node order and emits a warning via
  `System.Diagnostics.Trace.TraceWarning`.
- Stability: nodes with equal in-degree are emitted in their original relative order.
- Unknown nodes referenced by edges are ignored.
- Duplicate nodes are deduplicated by `comparer` (first occurrence is kept).

