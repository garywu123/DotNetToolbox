using System.Diagnostics;

using DotNetToolbox.Algorithms.Sorting;

using FluentAssertions;

namespace DotNetToolbox.Tests.Algorithms;

public class TopologicalSorterTests
{
    [Fact]
    public void Sort_EmptyNodes_ReturnsEmpty()
    {
        IReadOnlyList<string> result = TopologicalSorter<string>.Sort([], []);
        result.Should().BeEmpty();
    }

    [Fact]
    public void Sort_SingleNode_NoEdges_ReturnsThatNode()
    {
        IReadOnlyList<string> result = TopologicalSorter<string>.Sort(["A"], []);
        result.Should().Equal(["A"]);
    }

    [Fact]
    public void Sort_LinearChain_ReturnsDepsFirst()
    {
        var nodes = new[] { "A", "B", "C" };
        var edges = new[] { (From: "A", To: "B"), (From: "B", To: "C") };

        var result = TopologicalSorter<string>.Sort(nodes, edges);

        result.Should().ContainInOrder("A", "B", "C");
    }

    [Fact]
    public void Sort_DiamondGraph_RootFirstLeafLast()
    {
        var nodes = new[] { "A", "B", "C", "D" };
        var edges = new[]
        {
            (From: "A", To: "B"),
            (From: "A", To: "C"),
            (From: "B", To: "D"),
            (From: "C", To: "D"),
        };

        var result = TopologicalSorter<string>.Sort(nodes, edges);

        result[0].Should().Be("A");
        result[^1].Should().Be("D");
    }

    [Fact]
    public void Sort_NoEdges_PreservesOriginalOrder()
    {
        var nodes = new[] { "C", "A", "B" };

        var result = TopologicalSorter<string>.Sort(nodes, []);

        result.Should().Equal(nodes);
    }

    [Fact]
    public void Sort_CycleDetected_ReturnsOriginalOrder()
    {
        var nodes = new[] { "A", "B", "C" };
        var edges = new[] { (From: "A", To: "B"), (From: "B", To: "A") };

        var result = TopologicalSorter<string>.Sort(nodes, edges);

        result.Should().Equal(nodes);
    }

    [Fact]
    public void Sort_CycleDetected_TracesWarning()
    {
        var nodes = new[] { "A", "B" };
        var edges = new[] { (From: "A", To: "B"), (From: "B", To: "A") };

        using var listener = new RecordingTraceListener();
        Trace.Listeners.Add(listener);
        try
        {
            _ = TopologicalSorter<string>.Sort(nodes, edges);
        }
        finally
        {
            Trace.Listeners.Remove(listener);
        }

        listener.Messages.Should().Contain(m => m.Contains("cycle", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Sort_OrdinalIgnoreCaseComparer_MatchesByCase()
    {
        var nodes = new[] { "a", "B" };
        var edges = new[] { (From: "A", To: "b") };

        var result = TopologicalSorter<string>.Sort(nodes, edges, StringComparer.OrdinalIgnoreCase);

        result.Should().ContainInOrder("a", "B");
    }

    [Fact]
    public void Sort_EdgeToUnknownNode_Ignored()
    {
        var result = TopologicalSorter<string>.Sort(["A"], [(From: "A", To: "Z")]);
        result.Should().Equal(["A"]);
    }

    private sealed class RecordingTraceListener : TraceListener
    {
        public List<string> Messages { get; } = [];

        public override void Write(string? message)
        {
            if (message is not null)
            {
                Messages.Add(message);
            }
        }

        public override void WriteLine(string? message)
        {
            if (message is not null)
            {
                Messages.Add(message);
            }
        }
    }
}
