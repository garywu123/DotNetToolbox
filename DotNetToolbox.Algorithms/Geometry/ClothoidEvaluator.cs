namespace DotNetToolbox.Algorithms.Geometry;

/// <summary>
/// Evaluates and samples a clothoid with known length and curvature parameters.
/// </summary>
public static class ClothoidEvaluator
{
    /// <summary>
    /// Calculates curvature at a distance along a clothoid.
    /// </summary>
    /// <param name="distance">Arc length from the start in metres.</param>
    /// <param name="initialCurvature">Curvature at the start in inverse metres.</param>
    /// <param name="curvatureRate">Curvature change per metre of arc length, in inverse square metres.</param>
    /// <returns>The curvature at <paramref name="distance"/>, in inverse metres.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// An argument is not finite, or <paramref name="distance"/> is negative.
    /// </exception>
    public static double CurvatureAt(
        double distance,
        double initialCurvature,
        double curvatureRate)
    {
        EnsureFinite(distance, nameof(distance));
        EnsureFinite(initialCurvature, nameof(initialCurvature));
        EnsureFinite(curvatureRate, nameof(curvatureRate));

        if (distance < 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(distance),
                distance,
                "Distance cannot be negative.");
        }

        return initialCurvature + (curvatureRate * distance);
    }

    /// <summary>
    /// Calculates the directed travel tangent at a distance along a clothoid.
    /// </summary>
    /// <param name="distance">Arc length from the start in metres.</param>
    /// <param name="initialTangent">Directed travel tangent at the start in radians.</param>
    /// <param name="initialCurvature">Curvature at the start in inverse metres.</param>
    /// <param name="curvatureRate">Curvature change per metre of arc length, in inverse square metres.</param>
    /// <returns>The directed travel tangent at <paramref name="distance"/>, in radians.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// An argument is not finite, or <paramref name="distance"/> is negative.
    /// </exception>
    public static double TangentAt(
        double distance,
        double initialTangent,
        double initialCurvature,
        double curvatureRate)
    {
        EnsureFinite(distance, nameof(distance));
        EnsureFinite(initialTangent, nameof(initialTangent));
        EnsureFinite(initialCurvature, nameof(initialCurvature));
        EnsureFinite(curvatureRate, nameof(curvatureRate));

        if (distance < 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(distance),
                distance,
                "Distance cannot be negative.");
        }

        return initialTangent
            + (initialCurvature * distance)
            + (0.5d * curvatureRate * distance * distance);
    }

    /// <summary>
    /// Samples position, tangent, and curvature along a clothoid using midpoint integration.
    /// </summary>
    /// <param name="parameters">Known clothoid parameters.</param>
    /// <param name="stepLength">Maximum distance between adjacent samples, in metres.</param>
    /// <returns>
    /// Samples ordered by distance, including the start and a sample at the exact requested
    /// terminal arc length. Returned X and Y coordinates are midpoint-integration approximations.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A numeric input is not finite, length is negative, step length is not positive,
    /// or step length is too small to advance the calculation.
    /// </exception>
    /// <remarks>
    /// A smaller <paramref name="stepLength"/> generally improves position accuracy but creates
    /// more samples. This method does not alter the last position to match an external endpoint.
    /// </remarks>
    public static IReadOnlyList<PathSample> Sample(
        ClothoidParameters parameters,
        double stepLength)
    {
        EnsureFinite(parameters.StartPose.X, "StartPose.X");
        EnsureFinite(parameters.StartPose.Y, "StartPose.Y");
        EnsureFinite(parameters.StartPose.TangentAngle, "StartPose.TangentAngle");
        EnsureFinite(parameters.Length, nameof(parameters.Length));
        EnsureFinite(parameters.InitialCurvature, nameof(parameters.InitialCurvature));
        EnsureFinite(parameters.CurvatureRate, nameof(parameters.CurvatureRate));
        EnsureFinite(stepLength, nameof(stepLength));

        if (parameters.Length < 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(parameters.Length),
                parameters.Length,
                "Length cannot be negative.");
        }

        if (stepLength <= 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(stepLength),
                stepLength,
                "Step length must be greater than zero.");
        }

        var initialTangent = Angles.NormalizeRadians(parameters.StartPose.TangentAngle);

        var samples = new List<PathSample>();
        var currentDistance = 0d;
        var x = parameters.StartPose.X;
        var y = parameters.StartPose.Y;

        samples.Add(new PathSample(
            Distance: 0d,
            X: x,
            Y: y,
            TangentAngle: initialTangent,
            Curvature: parameters.InitialCurvature));

        while (currentDistance < parameters.Length)
        {
            var nextDistance = Math.Min(
                currentDistance + stepLength,
                parameters.Length);

            if (nextDistance <= currentDistance)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(stepLength),
                    stepLength,
                    "Step length is too small to advance the calculation.");
            }

            var segmentLength = nextDistance - currentDistance;
            var middleDistance = currentDistance + (segmentLength / 2d);
            var middleTangent = TangentAt(
                middleDistance,
                initialTangent,
                parameters.InitialCurvature,
                parameters.CurvatureRate);

            x += Math.Cos(middleTangent) * segmentLength;
            y += Math.Sin(middleTangent) * segmentLength;
            currentDistance = nextDistance;

            samples.Add(new PathSample(
                Distance: currentDistance,
                X: x,
                Y: y,
                TangentAngle: TangentAt(
                    currentDistance,
                    initialTangent,
                    parameters.InitialCurvature,
                    parameters.CurvatureRate),
                Curvature: CurvatureAt(
                    currentDistance,
                    parameters.InitialCurvature,
                    parameters.CurvatureRate)));
        }

        return samples;
    }

    private static void EnsureFinite(double value, string parameterName)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "Value must be a finite number.");
        }
    }
}
