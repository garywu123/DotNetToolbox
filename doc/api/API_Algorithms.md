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

## Namespace: `DotNetToolbox.Algorithms.Geometry`

The geometry API uses metres, radians, inverse metres for curvature, and inverse
square metres for curvature rate. It has no vehicle-specific forward/reverse model:
every `Pose2D.TangentAngle` is already a directed path tangent.

### Value types

| Type | Meaning |
|---|---|
| `Pose2D` | X/Y position and directed path tangent |
| `ClothoidParameters` | Start pose, arc length, initial curvature, and curvature rate |
| `PathSample` | One evaluated distance, position, tangent, and curvature |
| `G1FitCandidate` | Fitted parameters plus Newton and endpoint residual diagnostics |
| `G1FitFailure` | Named failure reason returned by `TryFitG1` |

These record structs do not validate themselves. `ClothoidEvaluator` and
`ClothoidFitter` validate inputs at their public operation boundaries.

### `Angles`

```csharp
public static double DegreesToRadians(double degrees)
public static double RadiansToDegrees(double radians)
public static double NormalizeRadians(double angle)
```

`NormalizeRadians` returns an equivalent angle in the half-open interval
`[-Math.PI, Math.PI)`. All three methods reject non-finite input.

### `ClothoidEvaluator`

```csharp
public static double CurvatureAt(
    double distance,
    double initialCurvature,
    double curvatureRate)

public static double TangentAt(
    double distance,
    double initialTangent,
    double initialCurvature,
    double curvatureRate)

public static IReadOnlyList<PathSample> Sample(
    ClothoidParameters parameters,
    double stepLength)
```

`CurvatureAt` and `TangentAt` are analytical. `Sample` uses midpoint integration
for X/Y, includes the start and terminal arc length, and does not force its last
position onto an external endpoint. A smaller `stepLength` generally improves
position accuracy at the cost of more samples.

### `ClothoidFitter.TryFitG1`

```csharp
public static bool TryFitG1(
    Pose2D startPose,
    Pose2D endPose,
    out G1FitCandidate candidate,
    out G1FitFailure failure)
```

This fits the canonical single-clothoid candidate between two positions and
their directed tangents. Invalid non-finite input throws
`ArgumentOutOfRangeException`. A valid request that cannot produce an accepted
candidate returns `false`, leaves `candidate` at `default`, and supplies a
`G1FitFailure` value.

```csharp
using DotNetToolbox.Algorithms.Geometry;

var start = new Pose2D(0d, 0d, 0d);
var end = new Pose2D(2d, 2d, Math.PI / 2d);

if (ClothoidFitter.TryFitG1(start, end, out var fit, out var failure))
{
    Console.WriteLine($"Length: {fit.Parameters.Length}");
    Console.WriteLine($"Initial curvature: {fit.Parameters.InitialCurvature}");
}
else
{
    Console.WriteLine($"Fit failed: {failure}");
}
```

The fitter evaluates generalized Fresnel moments with fixed
2048-subinterval composite Simpson quadrature. It is dependency-free and
deterministic, but it is not adaptive and reports no integration error bound.
Its residuals assess the computed fixed-quadrature candidate, not the exact
Fresnel integrals.

The tangent-only contract is intentional. An experiment that starts with a
vehicle body heading must first convert it to a path tangent; for ideal
no-slip travel this is normally the same heading when moving forward and the
heading plus `Math.PI` when reversing. That conversion is outside this generic
library.

