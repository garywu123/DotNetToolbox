---
applyTo: "src/DotNetToolbox.*/**/*.cs"
---

## Library Source Rules

### Platform Targeting

- `DotNetToolbox.Algorithms` and `DotNetToolbox.Data.Csv`: **must be cross-platform**
  - No `System.Runtime.InteropServices` P/Invoke
  - No Windows registry, DPAPI, or Windows-specific paths
  - No `Environment.SpecialFolder` that returns empty on non-Windows
- `DotNetToolbox.Data.SqlServer`: may use `Microsoft.Data.SqlClient` (cross-platform client)

### Public API Design

- Keep the public surface minimal — start with `internal` and promote to `public` only when needed
- Prefer interfaces for injectable services (`ISchemaService`, not just `SchemaService`)
- Stateless utilities should be `public static class` — no interface needed
- Immutable data types should be `record` or `readonly struct`

### XML Documentation (mandatory on public members)

```csharp
/// <summary>Brief description.</summary>
/// <param name="x">What x is.</param>
/// <returns>What is returned. State DBNull.Value / empty / null cases explicitly.</returns>
/// <remarks>Performance notes, thread safety, or important edge cases.</remarks>
public static object Coerce(...) { }
```

### Error Handling in Library Code

- Validate inputs at public API boundaries — throw `ArgumentNullException` / `ArgumentException`
- Internal helpers can assume valid inputs (validated by the caller)
- Return empty collections, not null
- Never throw from a `Dispose` / `DisposeAsync` implementation

### Resource Management

- Implement `IAsyncDisposable` (not `IDisposable`) for classes that own async resources
- Use `await using` at call sites
- Database connections must not be held open across unrelated operations

### Thread Safety

- `ConcurrentDictionary` for caches shared across async operations
- Document thread-safety guarantees in XML `<remarks>` on the class
