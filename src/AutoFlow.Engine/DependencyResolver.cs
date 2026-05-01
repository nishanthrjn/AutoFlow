using AutoFlow.Domain.Entities;

namespace AutoFlow.Engine;

public class DependencyResolver : IDependencyResolver
{
    public IReadOnlyList<IReadOnlyList<StepDefinition>> GetExecutionBatches(
        WorkflowDefinition workflow)
    {
        var steps    = workflow.Steps;
        var inDegree = steps.ToDictionary(s => s.Id, s => s.DependsOn.Count);
        var batches  = new List<IReadOnlyList<StepDefinition>>();

        while (true)
        {
            // All steps with no remaining dependencies form the next parallel batch
            var batch = steps
                .Where(s => inDegree.ContainsKey(s.Id) && inDegree[s.Id] == 0)
                .ToList();

            if (batch.Count == 0) break;

            batches.Add(batch);

            foreach (var step in batch)
            {
                inDegree.Remove(step.Id);

                // Reduce in-degree for steps that depended on this one
                foreach (var dependent in steps.Where(s => s.DependsOn.Contains(step.Id)))
                    if (inDegree.ContainsKey(dependent.Id))
                        inDegree[dependent.Id]--;
            }
        }

        if (inDegree.Count > 0)
            throw new InvalidOperationException(
                "Circular dependency detected in workflow DAG.");

        return batches;
    }
}
