namespace DotNetToolbox.Algorithms.Geometry;

/// <summary>
/// Identifies why a valid finite G1 fitting request did not produce a candidate.
/// </summary>
public enum G1FitFailure
{
    /// <summary>No failure occurred.</summary>
    None,

    /// <summary>The endpoint separation is too small for ordinary G1 fitting.</summary>
    CoincidentEndpoints,

    /// <summary>The Newton derivative is too small for a safe update.</summary>
    DegenerateDerivative,

    /// <summary>The canonical-root iteration did not converge.</summary>
    DidNotConverge,

    /// <summary>The recovered curve length is not finite and positive.</summary>
    InvalidLength,

    /// <summary>The recovered candidate exceeds the position or tangent residual tolerance.</summary>
    ResidualExceeded,
}

/// <summary>
/// Describes a fitted canonical G1 clothoid and its numerical diagnostics.
/// </summary>
/// <param name="Parameters">Recovered clothoid parameters.</param>
/// <param name="Iterations">Number of Newton updates used to find the canonical root.</param>
/// <param name="RootResidual">Absolute normalized transverse residual.</param>
/// <param name="PositionResidual">Endpoint position residual in metres.</param>
/// <param name="TangentResidual">Absolute endpoint tangent residual in radians.</param>
/// <remarks>
/// <paramref name="RootResidual"/> is dimensionless because the root equation is evaluated
/// in normalized chord coordinates. The diagnostics describe the fixed-quadrature numerical
/// solution and are not independent error bounds for the exact Fresnel integrals.
/// </remarks>
public readonly record struct G1FitCandidate(
    ClothoidParameters Parameters,
    int Iterations,
    double RootResidual,
    double PositionResidual,
    double TangentResidual);
