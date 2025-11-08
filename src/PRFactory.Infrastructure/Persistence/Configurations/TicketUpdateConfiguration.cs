using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRFactory.Domain.Entities;
using PRFactory.Domain.ValueObjects;
using System.Text.Json;

namespace PRFactory.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the TicketUpdate entity.
/// </summary>
public class TicketUpdateConfiguration : IEntityTypeConfiguration<TicketUpdate>
{
    public void Configure(EntityTypeBuilder<TicketUpdate> builder)
    {
        builder.ToTable("TicketUpdates");

        // Primary Key
        builder.HasKey(tu => tu.Id);

        // Properties
        builder.Property(tu => tu.TicketId)
            .IsRequired();

        builder.Property(tu => tu.UpdatedTitle)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(tu => tu.UpdatedDescription)
            .IsRequired()
            .HasMaxLength(10000);

        // Store SuccessCriteria as JSON
        builder.Property(tu => tu.SuccessCriteria)
            .IsRequired()
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<SuccessCriterion>>(v, (JsonSerializerOptions?)null) ?? new List<SuccessCriterion>()
            )
            .HasColumnType("TEXT");

        builder.Property(tu => tu.AcceptanceCriteria)
            .IsRequired()
            .HasMaxLength(10000);

        builder.Property(tu => tu.Version)
            .IsRequired();

        builder.Property(tu => tu.IsDraft)
            .IsRequired();

        builder.Property(tu => tu.IsApproved)
            .IsRequired();

        builder.Property(tu => tu.RejectionReason)
            .HasMaxLength(2000);

        builder.Property(tu => tu.GeneratedAt)
            .IsRequired();

        builder.Property(tu => tu.ApprovedAt);

        builder.Property(tu => tu.PostedAt);

        // Relationships
        builder.HasOne(tu => tu.Ticket)
            .WithMany()
            .HasForeignKey(tu => tu.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for common queries
        builder.HasIndex(tu => tu.TicketId);

        builder.HasIndex(tu => new { tu.TicketId, tu.Version });

        builder.HasIndex(tu => new { tu.TicketId, tu.IsDraft });

        builder.HasIndex(tu => new { tu.TicketId, tu.IsApproved });

        builder.HasIndex(tu => tu.GeneratedAt);

        builder.HasIndex(tu => new { tu.IsApproved, tu.PostedAt })
            .HasFilter("PostedAt IS NULL");
    }
}
