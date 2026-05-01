using AutoFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoFlow.Engine.Persistence.Configurations;

public class WorkflowInstanceConfig : IEntityTypeConfiguration<WorkflowInstance>
{
    public void Configure(EntityTypeBuilder<WorkflowInstance> builder)
    {
        builder.ToTable("workflow_instances");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.DefinitionId).HasColumnName("definition_id").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.ExecutionData).HasColumnName("execution_data").HasColumnType("jsonb");
        builder.HasMany(x => x.StepStateEntries)
               .WithOne(x => x.Instance)
               .HasForeignKey(x => x.InstanceId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
