namespace DotNetToolbox.Algorithms.Geometry;

/// <summary>
/// Provides angle conversion and normalization operations.
/// </summary>
public static class Angles
{
    /// <summary>
    /// Converts an angle from degrees to radians.
    /// </summary>
    /// <param name="degrees">Angle in degrees.</param>
    /// <returns>The equivalent angle in radians.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="degrees"/> is not finite.
    /// </exception>
    public static double DegreesToRadians(double degrees)
    {
        EnsureFinite(degrees, nameof(degrees));
        return degrees * (Math.PI / 180d);
    }

    /// <summary>
    /// Converts an angle from radians to degrees.
    /// </summary>
    /// <param name="radians">Angle in radians.</param>
    /// <returns>The equivalent angle in degrees.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="radians"/> is not finite.
    /// </exception>
    public static double RadiansToDegrees(double radians)
    {
        EnsureFinite(radians, nameof(radians));
        return radians * (180d / Math.PI);
    }

    /// <summary>
    /// Normalizes an angle to the half-open interval [-pi, pi).
    /// </summary>
    /// <param name="angle">Angle in radians.</param>
    /// <returns>The equivalent angle in the half-open interval [-pi, pi).</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="angle"/> is not finite.
    /// </exception>
    public static double NormalizeRadians(double angle)
    {
        EnsureFinite(angle, nameof(angle));

        var normalized = angle % Math.Tau;

        if (normalized < -Math.PI)
        {
            normalized += Math.Tau;
        }
        else if (normalized >= Math.PI)
        {
            normalized -= Math.Tau;
        }

        return normalized == 0d ? 0d : normalized;
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
