using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRFactory.Domain.Entities;

namespace PRFactory.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the PlanVersion entity.
/// </summary>
public class PlanVersionConfiguration : IEntityTypeConfiguration<PlanVersion>
{
    public void Configure(EntityTypeBuilder<PlanVersion> builder)
    {
        builder.ToTable("PlanVersions");

        // Primary Key
        builder.HasKey(v => v.Id);

        // Properties
        builder.Property(v => v.PlanId)
            .IsRequired();

        builder.Property(v => v.Version)
            .IsRequired();

        builder.Property(v => v.UserStories)
            .HasColumnType("nvarchar(max)");

        builder.Property(v => v.ApiDesign)
            .HasColumnType("nvarchar(max)");

        builder.Property(v => v.DatabaseSchema)
            .HasColumnType("nvarchar(max)");

        builder.Property(v => v.TestCases)
            .HasColumnType("nvarchar(max)");

        builder.Property(v => v.ImplementationSteps)
            .HasColumnType("nvarchar(max)");

        builder.Property(v => v.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(v => v.CreatedBy)
            .HasMaxLength(256);

        builder.Property(v => v.RevisionReason)
            .HasMaxLength(1000);

        // Relationships
        builder.HasOne(v => v.Plan)
            .WithMany(p => p.Versions)
            .HasForeignKey(v => v.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(v => v.PlanId);

        builder.HasIndex(v => new { v.PlanId, v.Version })
            .IsUnique()
            .HasDatabaseName("UQ_PlanVersions_PlanId_Version");
    }
}
