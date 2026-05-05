# Spec: DotNetToolbox.Algorithms

## Scope

This spec defines the public API contract for `DotNetToolbox.Algorithms`.
The library has zero external dependencies and must compile on all .NET 8 platforms.

---

## Component: `TopologicalSorter<T>`

**Namespace:** `DotNetToolbox.Algorithms.Sorting`
**Type:** `public static class TopologicalSorter<T>`

### Purpose

Performs a topological sort over a directed acyclic graph (DAG) of typed nodes.
Primary uses:

- **FK delete ordering**: order tables by foreign-key dependency so deletes succeed without constraint violations
- **FK insert ordering**: reverse order so referenced tables are populated before referencing tables
- **Task sequencing**: order export/import tasks declared with `DependsOn` references

### Method Signature

```csharp
/// <summary>
/// Returns <paramref name="nodes"/> ordered so that for every dependency edge (A → B),
/// A appears before B in the result. When a cycle is detected the original node order
/// is returned and a warning is emitted.
/// </summary>
/// <param name="nodes">All nodes in the graph.</param>
/// <param name="dependencies">
///   Directed edges as (from, to) tuples meaning "from must come before to".
/// </param>
/// <param name="comparer">
///   Equality comparer for <typeparamref name="T"/>. Defaults to
///   <see cref="EqualityComparer{T}.Default"/> when null.
/// </param>
/// <returns>
///   A new list with nodes ordered by dependency. Nodes not mentioned in any edge
///   retain their original relative order among themselves.
/// </returns>
public static IReadOnlyList<T> Sort(
    IEnumerable<T> nodes,
    IEnumerable<(T From, T To)> dependencies,
    IEqualityComparer<T>? comparer = null)
```

### Algorithm

Kahn's algorithm (BFS-based):

1. Build an adjacency list and in-degree map from `dependencies`
2. Seed the queue with all nodes whose in-degree is zero
3. Process nodes BFS-order: emit each node, decrement neighbour in-degrees, enqueue any that reach zero
4. If the number of emitted nodes < total nodes → a cycle exists; fall back to original order

### Cycle Handling

| Situation | Behaviour |
|---|---|
| No cycle | Returns correctly sorted `IReadOnlyList<T>` |
| Cycle detected | Returns original node order unchanged; emits a diagnostic via `System.Diagnostics.Trace.TraceWarning` |

**Rationale:** The caller (FK ordering, task scheduling) should not crash when a schema has a
circular reference. Returning the original order is a safe degraded mode.
The warning allows diagnosis without forcing the caller to handle an exception.

### Behaviour Details

| Case | Behaviour |
|---|---|
| Empty `nodes` | Returns empty list |
| Single node, no edges | Returns `[node]` |
| Node in `nodes` not referenced in `dependencies` | Included in output in original relative position |
| Edge references a node not in `nodes` | That node is silently ignored (edges only constrain known nodes) |
| Duplicate nodes | Treated as one node (deduplication by comparer) |
| `null` node values in collection | Undefined — callers must not pass null elements |

### Stability

Nodes with the same in-degree at any point in the BFS queue are emitted in the order they
first appeared in the original `nodes` enumerable. This makes output deterministic for a
given input order.

### Thread Safety

`Sort` is a pure static method with no shared state. Thread-safe by construction.

### Performance Expectations

- Time complexity: O(V + E) where V = node count, E = edge count
- Space complexity: O(V + E)
- Expected inputs: V ≤ 200 (typical FK graph), V ≤ 5000 (large schema import)

### Usage Examples

```csharp
// --- FK delete order (delete dependents before referenced tables) ---
string[] tables = ["Customers", "Orders", "OrderItems", "Payments"];
(string, string)[] fks =
[
    ("Orders",     "Customers"),   // Orders.CustomerId → Customers
    ("OrderItems", "Orders"),      // OrderItems.OrderId → Orders
    ("Payments",   "Orders"),      // Payments.OrderId → Orders
];

// insertOrder: Customers → Orders → OrderItems, Payments
var insertOrder = TopologicalSorter<string>.Sort(tables, fks);

// deleteOrder: reverse insertOrder
var deleteOrder = insertOrder.Reverse().ToList();

// --- String case-insensitive table names ---
var sorted = TopologicalSorter<string>.Sort(
    tables, fks,
    comparer: StringComparer.OrdinalIgnoreCase);

// --- Custom type ---
record Table(string Name);

var tableMeta = new[] { new Table("Customers"), new Table("Orders") };
var edges     = new[] { (new Table("Orders"), new Table("Customers")) };
var comparer  = EqualityComparer<Table>.Default;  // record default equality (by value)

var result = TopologicalSorter<Table>.Sort(tableMeta, edges, comparer);
```

### Validation Checks (post-implementation)

After implementation verify:

1. Single-element list with no edges → `[element]`
2. Linear chain A→B→C → `[A, B, C]`
3. Diamond A→B, A→C, B→D, C→D → A before B and C, both before D
4. Cycle A→B→A → returns original order, `Trace.TraceWarning` called once
5. Empty input → empty output, no exception
6. Nodes not in any edge appear in original relative order among sorted nodes
