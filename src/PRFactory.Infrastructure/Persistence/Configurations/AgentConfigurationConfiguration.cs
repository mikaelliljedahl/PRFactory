using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRFactory.Domain.Entities;

namespace PRFactory.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the AgentConfiguration entity.
/// </summary>
public class AgentConfigurationConfiguration : IEntityTypeConfiguration<AgentConfiguration>
{
    public void Configure(EntityTypeBuilder<AgentConfiguration> builder)
    {
        builder.ToTable("AgentConfigurations");

        // Primary Key
        builder.HasKey(a => a.Id);

        // Properties
        builder.Property(a => a.TenantId)
            .IsRequired();

        builder.Property(a => a.AgentName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.Instructions)
            .IsRequired()
            .HasColumnType("TEXT"); // SQLite uses TEXT for large strings

        builder.Property(a => a.EnabledTools)
            .IsRequired()
            .HasColumnType("TEXT"); // JSON array

        builder.Property(a => a.MaxTokens)
            .IsRequired()
            .HasDefaultValue(8000);

        builder.Property(a => a.Temperature)
            .IsRequired()
            .HasDefaultValue(0.3f);

        builder.Property(a => a.StreamingEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(a => a.RequiresApproval)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.Property(a => a.UpdatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(a => a.TenantId)
            .HasDatabaseName("IX_AgentConfigurations_TenantId");

        // Unique constraint: TenantId + AgentName must be unique
        builder.HasIndex(a => new { a.TenantId, a.AgentName })
            .IsUnique()
            .HasDatabaseName("IX_AgentConfigurations_TenantId_AgentName");

        // Relationships
        builder.HasOne(a => a.Tenant)
            .WithMany()
            .HasForeignKey(a => a.TenantId)
            .OnDelete(DeleteBehavior.Cascade); // Delete configs when tenant is deleted
    }
}
