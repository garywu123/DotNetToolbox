using DotNetToolbox.Algorithms.Geometry;
using FluentAssertions;

namespace DotNetToolbox.Tests.Algorithms.Geometry;

public sealed class ClothoidFailurePathTests
{
    public static TheoryData<Action> NonFiniteAngleCalls => new()
    {
        () => Angles.NormalizeRadians(double.NaN),
        () => Angles.DegreesToRadians(double.PositiveInfinity),
        () => Angles.RadiansToDegrees(double.NegativeInfinity),
    };

    [Theory]
    [MemberData(nameof(NonFiniteAngleCalls))]
    public void AngleOperation_NonFiniteAngle_Throws(Action operation)
    {
        operation.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CurvatureAt_NegativeDistance_Throws()
    {
        var operation = () => ClothoidEvaluator.CurvatureAt(-0.01d, 0d, 0.1d);

        operation.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void TangentAt_NegativeDistance_Throws()
    {
        var operation = () => ClothoidEvaluator.TangentAt(-0.01d, 0d, 0d, 0.1d);

        operation.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [MemberData(nameof(NonFiniteEvaluatorCalls))]
    public void Evaluator_NonFiniteInput_Throws(Action operation)
    {
        operation.Should().Throw<ArgumentOutOfRangeException>();
    }

    public static TheoryData<Action> NonFiniteEvaluatorCalls => new()
    {
        () => ClothoidEvaluator.CurvatureAt(double.NaN, 0d, 0d),
        () => ClothoidEvaluator.CurvatureAt(0d, double.NaN, 0d),
        () => ClothoidEvaluator.CurvatureAt(0d, 0d, double.NaN),
        () => ClothoidEvaluator.TangentAt(double.NaN, 0d, 0d, 0d),
        () => ClothoidEvaluator.TangentAt(0d, double.NaN, 0d, 0d),
        () => ClothoidEvaluator.TangentAt(0d, 0d, double.NaN, 0d),
        () => ClothoidEvaluator.TangentAt(0d, 0d, 0d, double.NaN),
    };

    [Theory]
    [MemberData(nameof(InvalidSamples))]
    public void Sample_InvalidInput_ThrowsWithoutReturningSamples(
        ClothoidParameters parameters,
        double stepLength)
    {
        var operation = () => ClothoidEvaluator.Sample(parameters, stepLength);

        operation.Should().Throw<ArgumentOutOfRangeException>();
    }

    public static TheoryData<ClothoidParameters, double> InvalidSamples => new()
    {
        { ValidParameters() with { StartPose = new Pose2D(double.NaN, 0d, 0d) }, 0.1d },
        { ValidParameters() with { StartPose = new Pose2D(0d, double.NaN, 0d) }, 0.1d },
        { ValidParameters() with { StartPose = new Pose2D(0d, 0d, double.NaN) }, 0.1d },
        { ValidParameters() with { Length = double.PositiveInfinity }, 0.1d },
        { ValidParameters() with { InitialCurvature = double.NaN }, 0.1d },
        { ValidParameters() with { CurvatureRate = double.NaN }, 0.1d },
        { ValidParameters() with { Length = -1d }, 0.1d },
        { ValidParameters(), 0d },
        { ValidParameters(), -0.1d },
        { ValidParameters(), double.PositiveInfinity },
    };

    private static ClothoidParameters ValidParameters() => new(
        StartPose: new Pose2D(0d, 0d, 0d),
        Length: 1d,
        InitialCurvature: 0d,
        CurvatureRate: 0d);
}
