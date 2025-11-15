using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRFactory.Domain.Entities;
using System.Text.Json;

namespace PRFactory.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the ReviewComment entity.
/// </summary>
public class ReviewCommentConfiguration : IEntityTypeConfiguration<ReviewComment>
{
    public void Configure(EntityTypeBuilder<ReviewComment> builder)
    {
        builder.ToTable("ReviewComments");

        // Primary Key
        builder.HasKey(rc => rc.Id);

        // Properties
        builder.Property(rc => rc.TicketId)
            .IsRequired();

        builder.Property(rc => rc.AuthorId)
            .IsRequired();

        builder.Property(rc => rc.Content)
            .IsRequired()
            .HasMaxLength(10000); // Allow longer comments for detailed discussions

        // Store MentionedUserIds as JSON
        builder.Property(rc => rc.MentionedUserIds)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>())
            .HasColumnType("jsonb")
            .HasColumnName("MentionedUserIds")
            .Metadata.SetValueComparer(ValueComparerHelpers.CreateGuidListComparer());

        builder.Property(rc => rc.CreatedAt)
            .IsRequired();

        builder.Property(rc => rc.UpdatedAt);

        // Indexes
        builder.HasIndex(rc => rc.TicketId)
            .HasDatabaseName("IX_ReviewComments_TicketId");

        builder.HasIndex(rc => rc.AuthorId)
            .HasDatabaseName("IX_ReviewComments_AuthorId");

        builder.HasIndex(rc => rc.CreatedAt)
            .HasDatabaseName("IX_ReviewComments_CreatedAt")
            .IsDescending(); // Most recent comments first

        // Relationships
        builder.HasOne(rc => rc.Ticket)
            .WithMany(t => t.ReviewComments)
            .HasForeignKey(rc => rc.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rc => rc.Author)
            .WithMany(u => u.Comments)
            .HasForeignKey(rc => rc.AuthorId)
            .OnDelete(DeleteBehavior.Restrict); // Don't delete comments if user is deleted
    }
}
