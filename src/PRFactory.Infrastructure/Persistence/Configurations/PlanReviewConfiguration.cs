using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRFactory.Domain.Entities;

namespace PRFactory.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the PlanReview entity.
/// </summary>
public class PlanReviewConfiguration : IEntityTypeConfiguration<PlanReview>
{
    public void Configure(EntityTypeBuilder<PlanReview> builder)
    {
        builder.ToTable("PlanReviews");

        // Primary Key
        builder.HasKey(pr => pr.Id);

        // Properties
        builder.Property(pr => pr.TicketId)
            .IsRequired();

        builder.Property(pr => pr.ReviewerId)
            .IsRequired();

        builder.Property(pr => pr.Status)
            .IsRequired()
            .HasConversion<int>(); // Store enum as int

        builder.Property(pr => pr.IsRequired)
            .IsRequired();

        builder.Property(pr => pr.AssignedAt)
            .IsRequired();

        builder.Property(pr => pr.ReviewedAt);

        builder.Property(pr => pr.Decision)
            .HasMaxLength(2000);

        // Indexes
        builder.HasIndex(pr => pr.TicketId)
            .HasDatabaseName("IX_PlanReviews_TicketId");

        builder.HasIndex(pr => pr.ReviewerId)
            .HasDatabaseName("IX_PlanReviews_ReviewerId");

        builder.HasIndex(pr => pr.Status)
            .HasDatabaseName("IX_PlanReviews_Status");

        // Unique constraint: One review per ticket per reviewer
        builder.HasIndex(pr => new { pr.TicketId, pr.ReviewerId })
            .IsUnique()
            .HasDatabaseName("IX_PlanReviews_TicketId_ReviewerId");

        // Relationships
        builder.HasOne(pr => pr.Ticket)
            .WithMany(t => t.PlanReviews)
            .HasForeignKey(pr => pr.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pr => pr.Reviewer)
            .WithMany(u => u.PlanReviews)
            .HasForeignKey(pr => pr.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict); // Don't delete reviews if user is deleted
    }
}
