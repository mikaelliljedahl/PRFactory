using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRFactory.Domain.Entities;

namespace PRFactory.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the PlanRevision entity.
/// </summary>
public class PlanRevisionConfiguration : IEntityTypeConfiguration<PlanRevision>
{
    public void Configure(EntityTypeBuilder<PlanRevision> builder)
    {
        builder.ToTable("PlanRevisions");

        // Primary Key
        builder.HasKey(pr => pr.Id);

        // Properties
        builder.Property(pr => pr.TicketId)
            .IsRequired();

        builder.Property(pr => pr.RevisionNumber)
            .IsRequired();

        builder.Property(pr => pr.BranchName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(pr => pr.MarkdownPath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(pr => pr.CommitHash)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(pr => pr.Content)
            .IsRequired();

        builder.Property(pr => pr.CreatedAt)
            .IsRequired();

        builder.Property(pr => pr.CreatedByUserId);

        builder.Property(pr => pr.Reason)
            .IsRequired()
            .HasConversion<int>(); // Store enum as int

        // Indexes
        builder.HasIndex(pr => pr.TicketId)
            .HasDatabaseName("IX_PlanRevisions_TicketId");

        builder.HasIndex(pr => new { pr.TicketId, pr.RevisionNumber })
            .HasDatabaseName("IX_PlanRevisions_TicketId_RevisionNumber");

        // Relationships
        builder.HasOne(pr => pr.Ticket)
            .WithMany()
            .HasForeignKey(pr => pr.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pr => pr.CreatedBy)
            .WithMany()
            .HasForeignKey(pr => pr.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict); // Don't delete revisions if user is deleted
    }
}
