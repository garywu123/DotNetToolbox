using DotNetToolbox.Algorithms.Geometry;
using FluentAssertions;

namespace DotNetToolbox.Tests.Algorithms.Geometry;

public sealed class ClothoidHappyPathTests
{
    [Theory]
    [InlineData(5d * Math.PI, -Math.PI)]
    [InlineData(-5d * Math.PI, -Math.PI)]
    [InlineData(2d * Math.PI + 0.25d, 0.25d)]
    public void NormalizeRadians_EquivalentAngle_ReturnsHalfOpenRange(
        double angle,
        double expected)
    {
        var actual = Angles.NormalizeRadians(angle);

        actual.Should().BeApproximately(expected, 1e-12);
        actual.Should().BeGreaterThanOrEqualTo(-Math.PI);
        actual.Should().BeLessThan(Math.PI);
    }

    [Fact]
    public void DegreesToRadians_RightAngle_ReturnsPiOverTwo()
    {
        Angles.DegreesToRadians(90d).Should().BeApproximately(Math.PI / 2d, 1e-12);
    }

    [Fact]
    public void RadiansToDegrees_PiOverTwo_ReturnsRightAngle()
    {
        Angles.RadiansToDegrees(Math.PI / 2d).Should().BeApproximately(90d, 1e-12);
    }

    [Fact]
    public void Sample_StraightSegment_ReachesAnalyticEndpoint()
    {
        var parameters = new ClothoidParameters(
            StartPose: new Pose2D(2d, -3d, Math.PI / 2d),
            Length: 6d,
            InitialCurvature: 0d,
            CurvatureRate: 0d);

        var samples = ClothoidEvaluator.Sample(parameters, stepLength: 2d);
        var end = samples[^1];

        samples.Should().HaveCount(4);
        end.Distance.Should().BeApproximately(6d, 1e-12);
        end.X.Should().BeApproximately(2d, 1e-12);
        end.Y.Should().BeApproximately(3d, 1e-12);
        end.TangentAngle.Should().BeApproximately(Math.PI / 2d, 1e-12);
        end.Curvature.Should().BeApproximately(0d, 1e-12);
    }

    [Fact]
    public void Sample_ZeroLength_ReturnsOnlyStartPose()
    {
        var start = new Pose2D(2d, -3d, Math.PI / 4d);
        var parameters = new ClothoidParameters(
            StartPose: start,
            Length: 0d,
            InitialCurvature: 0.1d,
            CurvatureRate: 0.02d);

        var sample = ClothoidEvaluator.Sample(parameters, stepLength: 0.5d)
            .Should().ContainSingle().Which;

        sample.Should().Be(new PathSample(0d, start.X, start.Y, start.TangentAngle, 0.1d));
    }

    [Fact]
    public void Sample_ConstantCurvatureQuarterCircle_AgreesWithAnalyticCircle()
    {
        const double radius = 2d;
        var parameters = new ClothoidParameters(
            StartPose: new Pose2D(0d, 0d, 0d),
            Length: Math.PI * radius / 2d,
            InitialCurvature: 1d / radius,
            CurvatureRate: 0d);

        var end = ClothoidEvaluator.Sample(parameters, stepLength: 0.001d)[^1];

        end.X.Should().BeApproximately(radius, 1e-6);
        end.Y.Should().BeApproximately(radius, 1e-6);
        end.TangentAngle.Should().BeApproximately(Math.PI / 2d, 1e-12);
        end.Curvature.Should().BeApproximately(1d / radius, 1e-12);
    }

    [Fact]
    public void Evaluate_ChangingCurvature_FollowsDocumentedFormulas()
    {
        var curvature = ClothoidEvaluator.CurvatureAt(
            distance: 5d,
            initialCurvature: 0.1d,
            curvatureRate: 0.02d);

        var tangent = ClothoidEvaluator.TangentAt(
            distance: 5d,
            initialTangent: 0d,
            initialCurvature: 0.1d,
            curvatureRate: 0.02d);

        curvature.Should().BeApproximately(0.2d, 1e-12);
        tangent.Should().BeApproximately(0.75d, 1e-12);
    }

    [Fact]
    public void Sample_PoseTangent_DefinesStraightPathDirection()
    {
        var parameters = new ClothoidParameters(
            StartPose: new Pose2D(0d, 0d, -Math.PI),
            Length: 2d,
            InitialCurvature: 0d,
            CurvatureRate: 0d);

        var samples = ClothoidEvaluator.Sample(parameters, stepLength: 0.5d);
        var start = samples[0];
        var end = samples[^1];

        start.TangentAngle.Should().BeApproximately(-Math.PI, 1e-12);
        end.X.Should().BeApproximately(-2d, 1e-12);
        end.Y.Should().BeApproximately(0d, 1e-12);
        end.TangentAngle.Should().BeApproximately(-Math.PI, 1e-12);
    }
}
