using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRFactory.Domain.Entities;

namespace PRFactory.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the InlineCommentAnchor entity.
/// </summary>
public class InlineCommentAnchorConfiguration : IEntityTypeConfiguration<InlineCommentAnchor>
{
    public void Configure(EntityTypeBuilder<InlineCommentAnchor> builder)
    {
        builder.ToTable("InlineCommentAnchors");

        // Primary Key
        builder.HasKey(a => a.Id);

        // Properties
        builder.Property(a => a.ReviewCommentId)
            .IsRequired();

        builder.Property(a => a.StartLine)
            .IsRequired();

        builder.Property(a => a.EndLine)
            .IsRequired();

        builder.Property(a => a.TextSnippet)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(a => a.ReviewCommentId)
            .IsUnique() // One anchor per comment
            .HasDatabaseName("IX_InlineCommentAnchors_ReviewCommentId");

        builder.HasIndex(a => new { a.StartLine, a.EndLine })
            .HasDatabaseName("IX_InlineCommentAnchors_LineRange");

        // Relationships
        builder.HasOne(a => a.ReviewComment)
            .WithOne()
            .HasForeignKey<InlineCommentAnchor>(a => a.ReviewCommentId)
            .OnDelete(DeleteBehavior.Cascade); // Delete anchor when comment is deleted
    }
}
