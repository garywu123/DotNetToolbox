namespace DotNetToolbox.Algorithms.Geometry;

/// <summary>
/// Fits the relevant Bertolazzi-Frego G1 clothoid between two directed tangent poses.
/// </summary>
/// <remarks>
/// The fitter operates on generic geometry. Both pose angles are directed path tangents;
/// vehicle body-heading and travel-direction conversion belongs in the calling application.
/// </remarks>
public static class ClothoidFitter
{
    private const int IntegrationSubintervals = 2048;
    private const int MaxIterations = 12;
    private const double CoincidentPositionTolerance = 1e-9;
    private const double RootTolerance = 1e-12;
    private const double DerivativeTolerance = 1e-14;
    private const double RelativePositionTolerance = 1e-8;
    private const double TangentTolerance = 1e-10;

    private static readonly double[] InitialGuessCoefficients =
    [
        2.989696028701907d,
        0.716228953608281d,
        -0.458969738821509d,
        -0.502821153340377d,
        0.261062141752652d,
        -0.045854475238709d,
    ];

    /// <summary>
    /// Attempts to fit the canonical single-clothoid candidate joining two positions
    /// with their specified directed path tangents.
    /// </summary>
    /// <param name="startPose">Start position and directed tangent in radians.</param>
    /// <param name="endPose">End position and directed tangent in radians.</param>
    /// <param name="candidate">
    /// The fitted candidate when this method returns <see langword="true"/>;
    /// otherwise <see langword="default"/>.
    /// </param>
    /// <param name="failure">
    /// <see cref="G1FitFailure.None"/> on success; otherwise the reason no candidate was returned.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when a finite positive-length candidate satisfies the endpoint tolerances;
    /// otherwise <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A coordinate or tangent in <paramref name="startPose"/> or <paramref name="endPose"/> is not finite.
    /// </exception>
    /// <remarks>
    /// This dependency-free implementation evaluates generalized Fresnel moments over
    /// the normalized interval [0, 1] with fixed 2048-subinterval composite Simpson quadrature.
    /// The rule is not adaptive and does not calculate an integration error bound. The reported
    /// residuals therefore validate the computed fixed-quadrature candidate, not the exact
    /// Fresnel integrals. This implementation targets deterministic offline geometry work;
    /// it is not the asymptotic Fresnel implementation from the reference paper.
    /// </remarks>
    public static bool TryFitG1(
        Pose2D startPose,
        Pose2D endPose,
        out G1FitCandidate candidate,
        out G1FitFailure failure)
    {
        EnsureFinite(startPose, nameof(startPose));
        EnsureFinite(endPose, nameof(endPose));

        candidate = default;

        var deltaX = endPose.X - startPose.X;
        var deltaY = endPose.Y - startPose.Y;
        var chordLength = Magnitude(deltaX, deltaY);

        if (chordLength <= CoincidentPositionTolerance)
        {
            failure = G1FitFailure.CoincidentEndpoints;
            return false;
        }

        var chordAngle = Math.Atan2(deltaY, deltaX);
        var phi0 = Angles.NormalizeRadians(startPose.TangentAngle - chordAngle);
        var phi1 = Angles.NormalizeRadians(endPose.TangentAngle - chordAngle);

        // Do not normalize delta: the difference preserves the selected angle lift
        // and lies strictly between -2*pi and 2*pi.
        var delta = phi1 - phi0;
        var root = InitialGuess(phi0, phi1);
        var updates = 0;
        var moments = default(FresnelMoments);

        for (; updates < MaxIterations; updates++)
        {
            moments = EvaluateMoments(2d * root, delta - root, phi0);

            if (Math.Abs(moments.Y0) <= RootTolerance)
            {
                break;
            }

            var derivative = moments.X2 - moments.X1;
            if (!double.IsFinite(derivative) || Math.Abs(derivative) < DerivativeTolerance)
            {
                failure = G1FitFailure.DegenerateDerivative;
                return false;
            }

            root -= moments.Y0 / derivative;
            if (!double.IsFinite(root))
            {
                failure = G1FitFailure.DidNotConverge;
                return false;
            }
        }

        moments = EvaluateMoments(2d * root, delta - root, phi0);
        var rootResidual = Math.Abs(moments.Y0);
        if (rootResidual > RootTolerance)
        {
            failure = G1FitFailure.DidNotConverge;
            return false;
        }

        var length = chordLength / moments.X0;
        if (!double.IsFinite(length) || length <= 0d)
        {
            failure = G1FitFailure.InvalidLength;
            return false;
        }

        var initialCurvature = (delta - root) / length;
        var curvatureRate = 2d * root / (length * length);
        var normalizedStartTangent = Angles.NormalizeRadians(startPose.TangentAngle);
        var parameters = new ClothoidParameters(
            new Pose2D(startPose.X, startPose.Y, normalizedStartTangent),
            length,
            initialCurvature,
            curvatureRate);

        var localX = length * moments.X0;
        var localY = length * moments.Y0;
        var chordCosine = Math.Cos(chordAngle);
        var chordSine = Math.Sin(chordAngle);
        var fittedEndX = startPose.X + (localX * chordCosine) - (localY * chordSine);
        var fittedEndY = startPose.Y + (localX * chordSine) + (localY * chordCosine);
        var positionResidual = Magnitude(fittedEndX - endPose.X, fittedEndY - endPose.Y);
        var fittedEndTangent = ClothoidEvaluator.TangentAt(
            length,
            normalizedStartTangent,
            initialCurvature,
            curvatureRate);
        var tangentResidual = Math.Abs(Angles.NormalizeRadians(
            fittedEndTangent - endPose.TangentAngle));

        if (!double.IsFinite(initialCurvature)
            || !double.IsFinite(curvatureRate)
            || !double.IsFinite(positionResidual)
            || !double.IsFinite(tangentResidual)
            || positionResidual > RelativePositionTolerance * Math.Max(1d, chordLength)
            || tangentResidual > TangentTolerance)
        {
            failure = G1FitFailure.ResidualExceeded;
            return false;
        }

        candidate = new G1FitCandidate(
            parameters,
            updates,
            rootResidual,
            positionResidual,
            tangentResidual);
        failure = G1FitFailure.None;
        return true;
    }

    private static double InitialGuess(double phi0, double phi1)
    {
        var p = phi0 / Math.PI;
        var q = phi1 / Math.PI;
        var product = p * q;
        var pSquared = p * p;
        var qSquared = q * q;

        return (phi0 + phi1)
            * (InitialGuessCoefficients[0]
                + (product * (InitialGuessCoefficients[1]
                    + (product * InitialGuessCoefficients[2])))
                + ((InitialGuessCoefficients[3]
                    + (product * InitialGuessCoefficients[4]))
                    * (pSquared + qSquared))
                + (InitialGuessCoefficients[5]
                    * ((pSquared * pSquared) + (qSquared * qSquared))));
    }

    private static FresnelMoments EvaluateMoments(double a, double b, double c)
    {
        const double step = 1d / IntegrationSubintervals;
        var x0 = 0d;
        var x1 = 0d;
        var x2 = 0d;
        var y0 = 0d;

        for (var index = 0; index <= IntegrationSubintervals; index++)
        {
            var tau = index * step;
            var tauSquared = tau * tau;
            var phase = (0.5d * a * tauSquared) + (b * tau) + c;
            var (sine, cosine) = Math.SinCos(phase);
            var weight = index is 0 or IntegrationSubintervals
                ? 1d
                : index % 2 == 0 ? 2d : 4d;

            x0 += weight * cosine;
            x1 += weight * tau * cosine;
            x2 += weight * tauSquared * cosine;
            y0 += weight * sine;
        }

        var scale = step / 3d;
        return new FresnelMoments(
            x0 * scale,
            x1 * scale,
            x2 * scale,
            y0 * scale);
    }

    private static void EnsureFinite(Pose2D pose, string parameterName)
    {
        if (!double.IsFinite(pose.X)
            || !double.IsFinite(pose.Y)
            || !double.IsFinite(pose.TangentAngle))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                pose,
                "Pose coordinates and tangent must be finite numbers.");
        }
    }

    private static double Magnitude(double x, double y)
    {
        var largest = Math.Max(Math.Abs(x), Math.Abs(y));
        if (largest == 0d)
        {
            return 0d;
        }

        var scaledX = x / largest;
        var scaledY = y / largest;
        return largest * Math.Sqrt((scaledX * scaledX) + (scaledY * scaledY));
    }

    private readonly record struct FresnelMoments(
        double X0,
        double X1,
        double X2,
        double Y0);
}
