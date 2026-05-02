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
        // WorkflowDefinition — store Steps as JSONB, not a separate table
        modelBuilder.Entity<WorkflowDefinition>(b =>
        {
            b.ToTable("workflow_definitions");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            b.Property(x => x.Description).HasColumnName("description").HasMaxLength(1000);
            b.Property(x => x.Version).HasColumnName("version");

            // Store the entire Steps list as JSONB — avoids mapping StepDefinition as entity
            b.Property(x => x.Steps)
             .HasColumnName("steps")
             .HasColumnType("jsonb");
        });

        // WorkflowInstance
        modelBuilder.Entity<WorkflowInstance>(b =>
        {
            b.ToTable("workflow_instances");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.DefinitionId).HasColumnName("definition_id").IsRequired();
            b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired();
            b.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            b.Property(x => x.ExecutionData).HasColumnName("execution_data").HasColumnType("jsonb");
            b.HasMany(x => x.StepStateEntries)
             .WithOne(x => x.Instance)
             .HasForeignKey(x => x.InstanceId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // StepStateEntry
        modelBuilder.Entity<StepStateEntry>(b =>
        {
            b.ToTable("step_state_entries");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.InstanceId).HasColumnName("instance_id").IsRequired();
            b.Property(x => x.StepId).HasColumnName("step_id").HasMaxLength(200).IsRequired();
            b.Property(x => x.Status).HasColumnName("status").HasMaxLength(50).IsRequired();
            b.Property(x => x.ErrorMessage).HasColumnName("error_message").HasMaxLength(2000);
            b.Property(x => x.RecordedAt).HasColumnName("recorded_at").IsRequired();
            b.HasIndex(x => x.InstanceId).HasDatabaseName("ix_step_state_entries_instance_id");
            b.HasIndex(x => x.RecordedAt).HasDatabaseName("ix_step_state_entries_recorded_at");
        });

        base.OnModelCreating(modelBuilder);
    }
}
