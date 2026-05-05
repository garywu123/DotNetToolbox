# IP-01: DotNetToolbox.Algorithms

## Overview

| Item | Value |
|---|---|
| Target library | `DotNetToolbox.Algorithms` |
| Project path | `src/DotNetToolbox.Algorithms/DotNetToolbox.Algorithms.csproj` |
| Test project | `src/DotNetToolbox.Tests/DotNetToolbox.Tests.csproj` (shared) |
| Spec | `doc/spec/Spec_Algorithms.md` |
| NuGet deps | None (BCL only) |
| DB required | No |
| Depends on IP | None — implement first |

---

## Deliverables

### `src/DotNetToolbox.Algorithms/Sorting/TopologicalSorter.cs`

**Class:** `public static class TopologicalSorter<T>` — namespace `DotNetToolbox.Algorithms.Sorting`

**Method:**
```
IReadOnlyList<T> Sort(
    IEnumerable<T> nodes,
    IEnumerable<(T From, T To)> dependencies,
    IEqualityComparer<T>? comparer = null)
```

**Responsibilities:**
- Accepts a flat list of nodes and directed dependency edges `(From, To)` meaning "From must come before To"
- Returns nodes ordered so every dependency appears before its dependents
- Uses **Kahn's BFS algorithm**: build in-degree map, seed queue from zero-in-degree nodes, process BFS
- When cycle detected (emitted count < node count): return original input order, emit `Trace.TraceWarning`
- Preserves original relative order among nodes with equal in-degree at each BFS step

**Boundary conditions:**

| Condition | Behaviour |
|---|---|
| `nodes` is empty | Return empty `IReadOnlyList<T>` immediately |
| `nodes` has one element, no edges | Return `[node]` |
| Edge references a node not in `nodes` | Silently ignore that edge |
| Multiple edges between same pair | In-degree incremented per edge (correct, no special handling needed) |
| `comparer` is null | Fall back to `EqualityComparer<T>.Default` |
| Cycle detected | Return original order; call `Trace.TraceWarning` once |
| No edges at all | Return nodes in original order |

---

## Tests

**File:** `src/DotNetToolbox.Tests/Algorithms/TopologicalSorterTests.cs`

### Test Cases

| # | Test Name | Setup | Assertion |
|---|---|---|---|
| 1 | `Sort_EmptyNodes_ReturnsEmpty` | `nodes=[]`, `edges=[]` | result is empty |
| 2 | `Sort_SingleNode_NoEdges_ReturnsThatNode` | `nodes=["A"]`, `edges=[]` | result is `["A"]` |
| 3 | `Sort_LinearChain_ReturnsDepsFirst` | nodes `A,B,C`; edges `A→B`, `B→C` | result is `["A","B","C"]` in that order |
| 4 | `Sort_DiamondGraph_RootFirstLeafLast` | nodes `A,B,C,D`; edges `A→B`, `A→C`, `B→D`, `C→D` | `A` is first, `D` is last |
| 5 | `Sort_NoEdges_PreservesOriginalOrder` | `nodes=["C","A","B"]`, `edges=[]` | result is `["C","A","B"]` |
| 6 | `Sort_CycleDetected_ReturnsOriginalOrder` | nodes `A,B,C`; edges `A→B`, `B→A` | result equals `["A","B","C"]` |
| 7 | `Sort_CycleDetected_TracesWarning` | same cycle as above | verify `Trace.TraceWarning` was called (use `TraceListener` stub) |
| 8 | `Sort_OrdinalIgnoreCaseComparer_MatchesByCase` | nodes `"a","B"`; edge `("A","b")` with `OrdinalIgnoreCase` | `"a"` appears before `"B"` |
| 9 | `Sort_EdgeToUnknownNode_Ignored` | `nodes=["A"]`; edge `("A","Z")` | result is `["A"]`, no exception |

### Assertion guidance

- Test 3: `result.Should().ContainInOrder("A", "B", "C")`
- Test 4: `result[0].Should().Be("A")` and `result[^1].Should().Be("D")`
- Test 6: `result.Should().Equal(new[]{"A","B","C"})` (exact sequence match)
- Test 7: add a `TraceListener` stub before calling `Sort`, assert it received ≥ 1 warning message

---

## Definition of Done

- [ ] `TopologicalSorter.cs` created in correct namespace and folder
- [ ] `TopologicalSorterTests.cs` contains all 9 test cases
- [ ] All 9 tests pass
- [ ] `dotnet build DotNetToolbox.sln` — zero errors, zero warnings
- [ ] `dotnet test --filter "Category!=Integration"` — all pass
- [ ] No missing XML doc warnings (`GenerateDocumentationFile` is enabled)
- [ ] No Windows-specific APIs used

## Validation (manual smoke test)

```
tables = ["Customers", "Orders", "OrderItems"]
edges  = [("Orders", "Customers"), ("OrderItems", "Orders")]

insertOrder = Sort(tables, edges)
→ expected: ["Customers", "Orders", "OrderItems"]

deleteOrder = insertOrder.Reverse()
→ expected: ["OrderItems", "Orders", "Customers"]
```
