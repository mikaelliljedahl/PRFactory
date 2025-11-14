using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRFactory.Domain.Entities;

namespace PRFactory.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the ChecklistItem entity
/// </summary>
public class ChecklistItemConfiguration : IEntityTypeConfiguration<ChecklistItem>
{
    public void Configure(EntityTypeBuilder<ChecklistItem> builder)
    {
        builder.ToTable("ChecklistItems");

        // Primary Key
        builder.HasKey(ci => ci.Id);

        // Properties
        builder.Property(ci => ci.ReviewChecklistId)
            .IsRequired();

        builder.Property(ci => ci.Category)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(ci => ci.Title)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(ci => ci.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(ci => ci.Severity)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(ci => ci.IsChecked)
            .IsRequired();

        builder.Property(ci => ci.CheckedAt);

        builder.Property(ci => ci.SortOrder)
            .IsRequired();

        // Indexes
        builder.HasIndex(ci => ci.ReviewChecklistId)
            .HasDatabaseName("IX_ChecklistItems_ReviewChecklistId");

        builder.HasIndex(ci => new { ci.ReviewChecklistId, ci.SortOrder })
            .HasDatabaseName("IX_ChecklistItems_ReviewChecklistId_SortOrder");

        // Relationships configured in ReviewChecklistConfiguration
    }
}
