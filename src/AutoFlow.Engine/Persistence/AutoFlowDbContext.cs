using System.Text.Json;
using AutoFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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
        var jsonOptions = new JsonSerializerOptions();

        // ── ExecutionData converter + comparer ───────────────────────────
        var executionDataConverter = new ValueConverter<Dictionary<string, object>, string>(
            v => JsonSerializer.Serialize(v, jsonOptions),
            v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, jsonOptions)
                 ?? new Dictionary<string, object>());

        var executionDataComparer = new ValueComparer<Dictionary<string, object>>(
            (c1, c2) => JsonSerializer.Serialize(c1, jsonOptions) ==
                        JsonSerializer.Serialize(c2, jsonOptions),
            c => JsonSerializer.Serialize(c, jsonOptions).GetHashCode(),
            c => JsonSerializer.Deserialize<Dictionary<string, object>>(
                     JsonSerializer.Serialize(c, jsonOptions), jsonOptions)
                 ?? new Dictionary<string, object>());

        // ── Steps converter + comparer ───────────────────────────────────
        var stepsConverter = new ValueConverter<List<StepDefinition>, string>(
            v => JsonSerializer.Serialize(v, jsonOptions),
            v => JsonSerializer.Deserialize<List<StepDefinition>>(v, jsonOptions)
                 ?? new List<StepDefinition>());

        var stepsComparer = new ValueComparer<List<StepDefinition>>(
            (c1, c2) => JsonSerializer.Serialize(c1, jsonOptions) ==
                        JsonSerializer.Serialize(c2, jsonOptions),
            c => JsonSerializer.Serialize(c, jsonOptions).GetHashCode(),
            c => JsonSerializer.Deserialize<List<StepDefinition>>(
                     JsonSerializer.Serialize(c, jsonOptions), jsonOptions)
                 ?? new List<StepDefinition>());

        // ── WorkflowDefinition ───────────────────────────────────────────
        modelBuilder.Entity<WorkflowDefinition>(b =>
        {
            b.ToTable("workflow_definitions");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            b.Property(x => x.Description).HasColumnName("description").HasMaxLength(1000);
            b.Property(x => x.Version).HasColumnName("version");
            b.Property(x => x.Steps)
             .HasColumnName("steps")
             .HasColumnType("jsonb")
             .HasConversion(stepsConverter, stepsComparer);
        });

        // ── WorkflowInstance ─────────────────────────────────────────────
        modelBuilder.Entity<WorkflowInstance>(b =>
        {
            b.ToTable("workflow_instances");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.DefinitionId).HasColumnName("definition_id").IsRequired();
            b.Property(x => x.Status)
             .HasColumnName("status")
             .HasConversion<string>()
             .IsRequired();
            b.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            b.Property(x => x.ExecutionData)
             .HasColumnName("execution_data")
             .HasColumnType("jsonb")
             .HasConversion(executionDataConverter, executionDataComparer);
            b.HasMany(x => x.StepStateEntries)
             .WithOne(x => x.Instance)
             .HasForeignKey(x => x.InstanceId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── StepStateEntry ───────────────────────────────────────────────
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
            b.HasIndex(x => x.InstanceId)
             .HasDatabaseName("ix_step_state_entries_instance_id");
            b.HasIndex(x => x.RecordedAt)
             .HasDatabaseName("ix_step_state_entries_recorded_at");
        });

        base.OnModelCreating(modelBuilder);
    }
}
