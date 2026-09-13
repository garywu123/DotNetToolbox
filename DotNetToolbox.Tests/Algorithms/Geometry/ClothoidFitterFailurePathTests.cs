using DotNetToolbox.Algorithms.Geometry;
using FluentAssertions;

namespace DotNetToolbox.Tests.Algorithms.Geometry;

public sealed class ClothoidFitterFailurePathTests
{
    public static TheoryData<Pose2D, Pose2D> NonFinitePoses => new()
    {
        { new Pose2D(double.NaN, 0d, 0d), new Pose2D(1d, 0d, 0d) },
        { new Pose2D(0d, double.PositiveInfinity, 0d), new Pose2D(1d, 0d, 0d) },
        { new Pose2D(0d, 0d, double.NegativeInfinity), new Pose2D(1d, 0d, 0d) },
        { new Pose2D(0d, 0d, 0d), new Pose2D(double.NaN, 0d, 0d) },
        { new Pose2D(0d, 0d, 0d), new Pose2D(1d, double.PositiveInfinity, 0d) },
        { new Pose2D(0d, 0d, 0d), new Pose2D(1d, 0d, double.NegativeInfinity) },
    };

    [Theory]
    [MemberData(nameof(NonFinitePoses))]
    public void TryFitG1_NonFinitePose_Throws(
        Pose2D startPose,
        Pose2D endPose)
    {
        var operation = () => ClothoidFitter.TryFitG1(
            startPose,
            endPose,
            out _,
            out _);

        operation.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(5e-10)]
    public void TryFitG1_CoincidentEndpoints_ReturnsNamedFailure(
        double endX)
    {
        var success = ClothoidFitter.TryFitG1(
            new Pose2D(0d, 0d, 0d),
            new Pose2D(endX, 0d, Math.PI / 2d),
            out var candidate,
            out var failure);

        success.Should().BeFalse();
        candidate.Should().Be(default(G1FitCandidate));
        failure.Should().Be(G1FitFailure.CoincidentEndpoints);
    }

    [Fact]
    public void TryFitG1_OverflowingFiniteChord_ReturnsInvalidLength()
    {
        var success = ClothoidFitter.TryFitG1(
            new Pose2D(-double.MaxValue, 0d, 0d),
            new Pose2D(double.MaxValue, 0d, 0d),
            out var candidate,
            out var failure);

        success.Should().BeFalse();
        candidate.Should().Be(default(G1FitCandidate));
        failure.Should().Be(G1FitFailure.InvalidLength);
    }
}
