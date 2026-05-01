using AutoFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoFlow.Engine.Persistence;

public class AutoFlowDbContext : DbContext
{
    public AutoFlowDbContext(DbContextOptions<AutoFlowDbContext> options)
        : base(options) { }

    public DbSet<WorkflowInstance>   WorkflowInstances   { get; set; }
    public DbSet<StepStateEntry>     StepStateEntries    { get; set; }
    public DbSet<WorkflowDefinition> WorkflowDefinitions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AutoFlowDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
