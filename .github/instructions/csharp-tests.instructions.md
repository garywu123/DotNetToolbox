---
applyTo: "src/DotNetToolbox.Tests/**/*.cs"
---

## Test Project Rules

### Frameworks

- **xUnit** for test runner
- **FluentAssertions** for assertions — use `.Should()` exclusively, never `Assert.*`
- **NSubstitute** for mocking interfaces

### Test Structure

```csharp
// One class per system-under-test, one file per class
public class TopologicalSorterTests
{
    // Arrange shared state in constructor or IClassFixture
    // One [Fact] or [Theory] per distinct scenario
}
```

### Naming Convention

```
MethodName_Scenario_ExpectedResult

Examples:
  Coerce_EmptyString_ReturnsDbNull
  Coerce_VarcharColumn_PassesThroughWithoutConversion
  GetColumnMapAsync_UnknownTable_ReturnsEmptyDict
  Sort_CycleDetected_ReturnsOriginalOrder
```

### Assertions Style

```csharp
// Preferred
result.Should().Be(expected);
result.Should().BeNull();
result.Should().BeOfType<DateTime>();
result.Should().HaveCount(3);
action.Should().Throw<ArgumentNullException>().WithMessage("*columnName*");

// Forbidden
Assert.Equal(expected, result);
Assert.Null(result);
```

### Parameterised Tests

```csharp
[Theory]
[InlineData("",     true)]
[InlineData("NULL", true)]
[InlineData("  ",   true)]
public void Coerce_NullLikeValues_ReturnsDbNull(string raw, bool expectDbNull)
{
    var result = DbValueCoercer.Coerce("AnyCol", raw, _schema);
    result.Should().Be(DBNull.Value);
}
```

### Integration Test Marker

```csharp
// All tests that require a real SQL Server connection
[Fact, Trait("Category", "Integration")]
public async Task GetColumnMapAsync_RealTable_ReturnsCorrectTypes()
```

Run unit tests only: `dotnet test --filter "Category!=Integration"`
Run integration tests only: `dotnet test --filter "Category=Integration"`

### Test Isolation

- Each integration test must clean up its own data (use `IAsyncLifetime` or `try/finally`)
- Do not rely on test execution order
- Use `LocalDbFixture` or `SqlServerFixture` (from `TestHelpers/`) — never create ad-hoc connections inline
- Never use production connection strings in tests — always read from `TOOLBOX_TEST_CONN`

### What Not To Do

- No `Thread.Sleep` — if timing is needed use `Task.Delay` with very short durations
- No `Console.Write*` in tests
- No magic strings — use `nameof()` or constants
- No partial test coverage — if a public method exists, it has at least one test
