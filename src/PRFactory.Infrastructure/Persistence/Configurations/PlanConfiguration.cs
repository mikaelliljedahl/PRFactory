using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRFactory.Domain.Entities;

namespace PRFactory.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the Plan entity.
/// </summary>
public class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("Plans");

        // Primary Key
        builder.HasKey(p => p.Id);

        // Properties
        builder.Property(p => p.TicketId)
            .IsRequired();

        // Legacy field
        builder.Property(p => p.Content)
            .HasColumnType("nvarchar(max)");

        // Multi-artifact fields
        builder.Property(p => p.UserStories)
            .HasColumnType("nvarchar(max)");

        builder.Property(p => p.ApiDesign)
            .HasColumnType("nvarchar(max)");

        builder.Property(p => p.DatabaseSchema)
            .HasColumnType("nvarchar(max)");

        builder.Property(p => p.TestCases)
            .HasColumnType("nvarchar(max)");

        builder.Property(p => p.ImplementationSteps)
            .HasColumnType("nvarchar(max)");

        builder.Property(p => p.Version)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.UpdatedAt);

        // Relationships
        builder.HasOne(p => p.Ticket)
            .WithOne()
            .HasForeignKey<Plan>(p => p.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Versions)
            .WithOne(v => v.Plan)
            .HasForeignKey(v => v.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(p => p.TicketId)
            .IsUnique();

        // Ignore computed property
        builder.Ignore(p => p.HasMultipleArtifacts);
    }
}
