using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRFactory.Domain.Entities;
using PRFactory.Infrastructure.Persistence.Encryption;

namespace PRFactory.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the Repository entity.
/// </summary>
public class RepositoryConfiguration : IEntityTypeConfiguration<Repository>
{
    private readonly IEncryptionService _encryptionService;

    public RepositoryConfiguration(IEncryptionService encryptionService)
    {
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
    }

    public void Configure(EntityTypeBuilder<Repository> builder)
    {
        builder.ToTable("Repositories");

        // Primary Key
        builder.HasKey(r => r.Id);

        // Properties
        builder.Property(r => r.TenantId)
            .IsRequired();

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.GitPlatform)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.CloneUrl)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(r => r.DefaultBranch)
            .IsRequired()
            .HasMaxLength(100);

        // Encrypted field - AccessToken
        builder.Property(r => r.AccessToken)
            .IsRequired()
            .HasMaxLength(1000)
            .HasConversion(
                v => _encryptionService.Encrypt(v),
                v => _encryptionService.Decrypt(v)
            );

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.Property(r => r.UpdatedAt);

        builder.Property(r => r.LastAccessedAt);

        builder.Property(r => r.LocalPath)
            .HasMaxLength(1000);

        builder.Property(r => r.IsActive)
            .IsRequired();

        // Relationships
        builder.HasOne(r => r.Tenant)
            .WithMany(t => t.Repositories)
            .HasForeignKey(r => r.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.Tickets)
            .WithOne(t => t.Repository)
            .HasForeignKey(t => t.RepositoryId)
            .OnDelete(DeleteBehavior.Restrict); // Don't cascade delete tickets
    }
}
