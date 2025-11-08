using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRFactory.Domain.Entities;

namespace PRFactory.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the AgentPromptTemplate entity.
/// </summary>
public class AgentPromptTemplateConfiguration : IEntityTypeConfiguration<AgentPromptTemplate>
{
    public void Configure(EntityTypeBuilder<AgentPromptTemplate> builder)
    {
        builder.ToTable("AgentPromptTemplates");

        // Primary Key
        builder.HasKey(t => t.Id);

        // Properties
        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(t => t.PromptContent)
            .IsRequired();

        builder.Property(t => t.RecommendedModel)
            .HasMaxLength(50);

        builder.Property(t => t.Color)
            .HasMaxLength(50);

        builder.Property(t => t.Category)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.IsSystemTemplate)
            .IsRequired();

        builder.Property(t => t.TenantId);

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.UpdatedAt);

        // Relationships
        builder.HasOne(t => t.Tenant)
            .WithMany()
            .HasForeignKey(t => t.TenantId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        // Indexes
        builder.HasIndex(t => t.Name);

        builder.HasIndex(t => t.Category);

        builder.HasIndex(t => t.IsSystemTemplate);

        builder.HasIndex(t => t.TenantId);

        // Composite index for efficient lookups
        builder.HasIndex(t => new { t.Category, t.TenantId });

        builder.HasIndex(t => new { t.Name, t.TenantId });
    }
}
