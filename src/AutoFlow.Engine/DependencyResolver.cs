using AutoFlow.Domain.Entities;

namespace AutoFlow.Engine;

public class DependencyResolver
{
    /// <summary>
    /// Resolves the execution order of steps using a topological sort.
    /// Throws an exception if a circular dependency is detected.
    /// </summary>
    public List<StepDefinition> GetExecutionOrder(WorkflowDefinition workflow)
    {
        var steps = workflow.Steps;
        var result = new List<StepDefinition>();
        var inDegree = steps.ToDictionary(s => s.Id, s => s.DependsOn.Count);
        var queue = new Queue<StepDefinition>(steps.Where(s => inDegree[s.Id] == 0));

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            result.Add(current);

            // Find steps that depend on the current step
            var children = steps.Where(s => s.DependsOn.Contains(current.Id));
            foreach (var child in children)
            {
                inDegree[child.Id]--;
                if (inDegree[child.Id] == 0)
                {
                    queue.Enqueue(child);
                }
            }
        }

        if (result.Count != steps.Count)
        {
            throw new InvalidOperationException("Circular dependency detected in workflow DAG.");
        }

        return result;
    }
}