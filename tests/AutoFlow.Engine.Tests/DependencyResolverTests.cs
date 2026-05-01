using AutoFlow.Domain.Entities;
using AutoFlow.Engine;
using Xunit;

namespace AutoFlow.Engine.Tests;

public class DependencyResolverTests
{
    [Fact]
    public void GetExecutionOrder_ShouldReturnCorrectOrder_ForValidDAG()
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

        // Act
        var order = resolver.GetExecutionOrder(workflow).ToList();

        // Assert
        Assert.Equal("Step_A", order[0].Id);
        Assert.Equal("Step_B", order[1].Id);
    }

    [Fact]
    public void GetExecutionOrder_ShouldThrow_WhenCycleDetected()
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
        Assert.Throws<InvalidOperationException>(() => resolver.GetExecutionOrder(workflow));
    }
}