using AutoFlow.Domain.Entities;
using AutoFlow.Engine;
using Xunit;

namespace AutoFlow.Engine.Tests;

public class DependencyResolverTests
{
    [Fact]
    public void GetExecutionBatches_ShouldReturnCorrectOrder_ForValidDAG()
    {
        // Arrange
        var resolver = new DependencyResolver();
        var workflow = new WorkflowDefinition
        {
            Steps = new List<StepDefinition>
            {
                new() { Id = "Step_B", DependsOn = new List<string> { "Step_A" } },
                new() { Id = "Step_A", DependsOn = new List<string>() }
            }
        };

        // Act — returns batches: [[Step_A], [Step_B]]
        var batches = resolver.GetExecutionBatches(workflow);

        // Assert
        Assert.Equal(2, batches.Count);
        Assert.Equal("Step_A", batches[0][0].Id);  // first batch
        Assert.Equal("Step_B", batches[1][0].Id);  // second batch
    }

    [Fact]
    public void GetExecutionBatches_ShouldThrow_WhenCycleDetected()
    {
        // Arrange
        var resolver = new DependencyResolver();
        var workflow = new WorkflowDefinition
        {
            Steps = new List<StepDefinition>
            {
                new() { Id = "A", DependsOn = new List<string> { "B" } },
                new() { Id = "B", DependsOn = new List<string> { "A" } }
            }
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => resolver.GetExecutionBatches(workflow));
    }

    [Fact]
    public void GetExecutionBatches_ShouldReturnParallelBatch_ForDiamondDAG()
    {
        // Arrange: A → (B, C) → D
        var resolver = new DependencyResolver();
        var workflow = new WorkflowDefinition
        {
            Steps = new List<StepDefinition>
            {
                new() { Id = "A", DependsOn = new List<string>() },
                new() { Id = "B", DependsOn = new List<string> { "A" } },
                new() { Id = "C", DependsOn = new List<string> { "A" } },
                new() { Id = "D", DependsOn = new List<string> { "B", "C" } }
            }
        };

        // Act
        var batches = resolver.GetExecutionBatches(workflow);

        // Assert — 3 batches: [A], [B,C], [D]
        Assert.Equal(3, batches.Count);
        Assert.Single(batches[0]);
        Assert.Equal("A", batches[0][0].Id);
        Assert.Equal(2, batches[1].Count);  // B and C in parallel
        Assert.Single(batches[2]);
        Assert.Equal("D", batches[2][0].Id);
    }
}
