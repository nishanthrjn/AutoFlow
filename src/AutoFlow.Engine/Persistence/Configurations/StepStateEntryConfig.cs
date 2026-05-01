using AutoFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoFlow.Engine.Persistence.Configurations;

public class StepStateEntryConfig : IEntityTypeConfiguration<StepStateEntry>
{
    public void Configure(EntityTypeBuilder<StepStateEntry> builder)
    {
        builder.ToTable("step_state_entries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.InstanceId).HasColumnName("instance_id").IsRequired();
        builder.Property(x => x.StepId).HasColumnName("step_id").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(50).IsRequired();
        builder.Property(x => x.ErrorMessage).HasColumnName("error_message").HasMaxLength(2000);
        builder.Property(x => x.RecordedAt).HasColumnName("recorded_at").IsRequired();
        builder.HasIndex(x => x.InstanceId).HasDatabaseName("ix_step_state_entries_instance_id");
        builder.HasIndex(x => x.RecordedAt).HasDatabaseName("ix_step_state_entries_recorded_at");
    }
}
