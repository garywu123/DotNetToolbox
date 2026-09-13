using DotNetToolbox.Algorithms.Geometry;
using FluentAssertions;

namespace DotNetToolbox.Tests.Algorithms.Geometry;

public sealed class ClothoidFitterHappyPathTests
{
    [Fact]
    public void TryFitG1_ForwardStraight_RecoversLineParameters()
    {
        var success = ClothoidFitter.TryFitG1(
            new Pose2D(0d, 0d, 0d),
            new Pose2D(6d, 0d, 0d),
            out var candidate,
            out var failure);

        success.Should().BeTrue();
        failure.Should().Be(G1FitFailure.None);
        candidate.Parameters.Length.Should().BeApproximately(6d, 1e-12);
        candidate.Parameters.InitialCurvature.Should().BeApproximately(0d, 1e-12);
        candidate.Parameters.CurvatureRate.Should().BeApproximately(0d, 1e-12);
        candidate.PositionResidual.Should().BeLessThan(1e-10);
        candidate.TangentResidual.Should().BeLessThan(1e-12);
    }

    [Fact]
    public void TryFitG1_QuarterCircle_RecoversCircleParameters()
    {
        var success = ClothoidFitter.TryFitG1(
            new Pose2D(0d, 0d, 0d),
            new Pose2D(2d, 2d, Math.PI / 2d),
            out var candidate,
            out var failure);

        success.Should().BeTrue();
        failure.Should().Be(G1FitFailure.None);
        candidate.Parameters.Length.Should().BeApproximately(Math.PI, 1e-10);
        candidate.Parameters.InitialCurvature.Should().BeApproximately(0.5d, 1e-10);
        candidate.Parameters.CurvatureRate.Should().BeApproximately(0d, 1e-10);
        candidate.PositionResidual.Should().BeLessThan(1e-8);
        candidate.TangentResidual.Should().BeLessThan(1e-10);
    }

    [Fact]
    public void TryFitG1_KnownChangingCurvature_RecoversOriginalParameters()
    {
        var success = ClothoidFitter.TryFitG1(
            new Pose2D(1d, -2d, 0.3d),
            new Pose2D(4.139239281721023d, 0.3014304526629612d, 1.1d),
            out var candidate,
            out var failure);

        success.Should().BeTrue();
        failure.Should().Be(G1FitFailure.None);
        candidate.Parameters.Length.Should().BeApproximately(4d, 1e-9);
        candidate.Parameters.InitialCurvature.Should().BeApproximately(0.1d, 1e-9);
        candidate.Parameters.CurvatureRate.Should().BeApproximately(0.05d, 1e-9);
        candidate.RootResidual.Should().BeLessThan(1e-11);
        candidate.PositionResidual.Should().BeLessThan(1e-8);
        candidate.TangentResidual.Should().BeLessThan(1e-10);
    }

    [Fact]
    public void TryFitG1_MirroredBoundary_MirrorsCurvatureSigns()
    {
        var leftSuccess = ClothoidFitter.TryFitG1(
            new Pose2D(0d, 0d, 0d),
            new Pose2D(6d, 4d, Math.PI / 2d),
            out var left,
            out var leftFailure);

        var rightSuccess = ClothoidFitter.TryFitG1(
            new Pose2D(0d, 0d, 0d),
            new Pose2D(6d, -4d, -Math.PI / 2d),
            out var right,
            out var rightFailure);

        leftSuccess.Should().BeTrue();
        rightSuccess.Should().BeTrue();
        leftFailure.Should().Be(G1FitFailure.None);
        rightFailure.Should().Be(G1FitFailure.None);
        right.Parameters.Length.Should().BeApproximately(left.Parameters.Length, 1e-10);
        right.Parameters.InitialCurvature.Should().BeApproximately(
            -left.Parameters.InitialCurvature,
            1e-10);
        right.Parameters.CurvatureRate.Should().BeApproximately(
            -left.Parameters.CurvatureRate,
            1e-10);
        left.PositionResidual.Should().BeLessThan(1e-8);
        right.PositionResidual.Should().BeLessThan(1e-8);
    }

    [Fact]
    public void TryFitG1_NegativeXTravelTangent_RecoversReverseDirectedLine()
    {
        var success = ClothoidFitter.TryFitG1(
            new Pose2D(6d, 0d, Math.PI),
            new Pose2D(0d, 0d, Math.PI),
            out var candidate,
            out var failure);

        success.Should().BeTrue();
        failure.Should().Be(G1FitFailure.None);
        candidate.Parameters.Length.Should().BeApproximately(6d, 1e-12);
        candidate.Parameters.InitialCurvature.Should().BeApproximately(0d, 1e-12);
        candidate.Parameters.CurvatureRate.Should().BeApproximately(0d, 1e-12);
        candidate.PositionResidual.Should().BeLessThan(1e-10);
        candidate.TangentResidual.Should().BeLessThan(1e-12);
    }
}
