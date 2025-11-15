using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRFactory.Domain.Entities;
using System.Text.Json;

namespace PRFactory.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the CodeReviewResult entity.
/// </summary>
public class CodeReviewResultConfiguration : IEntityTypeConfiguration<CodeReviewResult>
{
    public void Configure(EntityTypeBuilder<CodeReviewResult> builder)
    {
        builder.ToTable("CodeReviewResults");

        // Primary Key
        builder.HasKey(cr => cr.Id);

        // Properties
        builder.Property(cr => cr.TicketId)
            .IsRequired();

        builder.Property(cr => cr.PullRequestNumber)
            .IsRequired();

        builder.Property(cr => cr.PullRequestUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(cr => cr.LlmProviderName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(cr => cr.ModelName)
            .IsRequired()
            .HasMaxLength(100);

        // Store lists as JSON arrays
        builder.Property(cr => cr.CriticalIssues)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            .HasColumnType("jsonb")
            .HasColumnName("CriticalIssues")
            .Metadata.SetValueComparer(ValueComparerHelpers.CreateStringListComparer());

        builder.Property(cr => cr.Suggestions)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            .HasColumnType("jsonb")
            .HasColumnName("Suggestions")
            .Metadata.SetValueComparer(ValueComparerHelpers.CreateStringListComparer());

        builder.Property(cr => cr.Praise)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            .HasColumnType("jsonb")
            .HasColumnName("Praise")
            .Metadata.SetValueComparer(ValueComparerHelpers.CreateStringListComparer());

        builder.Property(cr => cr.FullReviewContent)
            .IsRequired()
            .HasColumnType("text"); // Use text for long content

        builder.Property(cr => cr.ReviewedAt)
            .IsRequired();

        builder.Property(cr => cr.CreatedAt)
            .IsRequired();

        builder.Property(cr => cr.RetryAttempt)
            .IsRequired()
            .HasDefaultValue(0);

        // Computed property (not mapped to database)
        builder.Ignore(cr => cr.Passed);
        builder.Ignore(cr => cr.TotalIssueCount);

        // Indexes
        builder.HasIndex(cr => cr.TicketId)
            .HasDatabaseName("IX_CodeReviewResults_TicketId");

        builder.HasIndex(cr => cr.PullRequestNumber)
            .HasDatabaseName("IX_CodeReviewResults_PullRequestNumber");

        builder.HasIndex(cr => cr.ReviewedAt)
            .HasDatabaseName("IX_CodeReviewResults_ReviewedAt")
            .IsDescending(); // Most recent reviews first

        builder.HasIndex(cr => cr.LlmProviderName)
            .HasDatabaseName("IX_CodeReviewResults_LlmProviderName");

        builder.HasIndex(cr => new { cr.TicketId, cr.RetryAttempt })
            .HasDatabaseName("IX_CodeReviewResults_TicketId_RetryAttempt");

        // Relationships
        builder.HasOne(cr => cr.Ticket)
            .WithMany() // Ticket doesn't have a collection navigation property for CodeReviewResults yet
            .HasForeignKey(cr => cr.TicketId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
