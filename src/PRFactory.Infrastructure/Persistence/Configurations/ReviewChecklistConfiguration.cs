using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRFactory.Domain.Entities;

namespace PRFactory.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the ReviewChecklist entity
/// </summary>
public class ReviewChecklistConfiguration : IEntityTypeConfiguration<ReviewChecklist>
{
    public void Configure(EntityTypeBuilder<ReviewChecklist> builder)
    {
        builder.ToTable("ReviewChecklists");

        // Primary Key
        builder.HasKey(rc => rc.Id);

        // Properties
        builder.Property(rc => rc.PlanReviewId)
            .IsRequired();

        builder.Property(rc => rc.TemplateName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(rc => rc.CreatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(rc => rc.PlanReviewId)
            .HasDatabaseName("IX_ReviewChecklists_PlanReviewId");

        // Relationships
        builder.HasOne(rc => rc.PlanReview)
            .WithOne(pr => pr.Checklist)
            .HasForeignKey<ReviewChecklist>(rc => rc.PlanReviewId)
            .OnDelete(DeleteBehavior.Cascade); // Delete checklist when review is deleted

        builder.HasMany(rc => rc.Items)
            .WithOne(ci => ci.ReviewChecklist)
            .HasForeignKey(ci => ci.ReviewChecklistId)
            .OnDelete(DeleteBehavior.Cascade); // Delete items when checklist is deleted
    }
}
