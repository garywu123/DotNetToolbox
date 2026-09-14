namespace DotNetToolbox.Algorithms.Geometry;

/// <summary>
/// Describes a position and directed path tangent in a two-dimensional plane.
/// </summary>
/// <param name="X">X coordinate in metres.</param>
/// <param name="Y">Y coordinate in metres.</param>
/// <param name="TangentAngle">Directed path tangent in radians.</param>
/// <remarks>
/// This generic geometry type stores a path tangent, not a vehicle body heading.
/// Convert vehicle heading and forward/reverse travel semantics before constructing it.
/// The value type itself does not validate that its components are finite.
/// </remarks>
public readonly record struct Pose2D(
    double X,
    double Y,
    double TangentAngle);

/// <summary>
/// Describes a clothoid whose curvature changes linearly with arc length.
/// </summary>
/// <param name="StartPose">Starting position and directed path tangent.</param>
/// <param name="Length">Total arc length in metres.</param>
/// <param name="InitialCurvature">Curvature at the start in inverse metres.</param>
/// <param name="CurvatureRate">Curvature change per metre of arc length, in inverse square metres.</param>
/// <remarks>
/// The value type itself does not validate the length, coordinates, tangent, or curvature values.
/// <see cref="ClothoidEvaluator"/> validates them when evaluating a path.
/// </remarks>
public readonly record struct ClothoidParameters(
    Pose2D StartPose,
    double Length,
    double InitialCurvature,
    double CurvatureRate);

/// <summary>
/// Describes one evaluated point along a clothoid path.
/// </summary>
/// <param name="Distance">Arc length from the path start in metres.</param>
/// <param name="X">X coordinate in metres.</param>
/// <param name="Y">Y coordinate in metres.</param>
/// <param name="TangentAngle">Directed travel tangent in radians.</param>
/// <param name="Curvature">Curvature in inverse metres.</param>
/// <remarks>
/// Position values produced by <see cref="ClothoidEvaluator.Sample"/> are numerical
/// midpoint-integration approximations. Tangent and curvature values are evaluated analytically.
/// </remarks>
public readonly record struct PathSample(
    double Distance,
    double X,
    double Y,
    double TangentAngle,
    double Curvature);
