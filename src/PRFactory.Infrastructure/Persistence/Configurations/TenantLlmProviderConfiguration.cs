using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRFactory.Domain.Entities;
using PRFactory.Infrastructure.Persistence.Encryption;
using System.Text.Json;

namespace PRFactory.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the TenantLlmProvider entity.
/// </summary>
public class TenantLlmProviderConfiguration : IEntityTypeConfiguration<TenantLlmProvider>
{
    private readonly IEncryptionService _encryptionService;

    public TenantLlmProviderConfiguration(IEncryptionService encryptionService)
    {
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
    }

    public void Configure(EntityTypeBuilder<TenantLlmProvider> builder)
    {
        builder.ToTable("TenantLlmProviders");

        // Primary Key
        builder.HasKey(p => p.Id);

        // Properties
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.ProviderType)
            .IsRequired()
            .HasConversion<int>(); // Store enum as integer

        builder.Property(p => p.UsesOAuth)
            .IsRequired();

        // Encrypted API token field
        builder.Property(p => p.EncryptedApiToken)
            .HasMaxLength(2000) // Longer for encrypted tokens
            .HasConversion(
                v => v == null ? null : _encryptionService.Encrypt(v),
                v => v == null ? null : _encryptionService.Decrypt(v)
            );

        builder.Property(p => p.ApiBaseUrl)
            .HasMaxLength(500);

        builder.Property(p => p.TimeoutMs)
            .IsRequired()
            .HasDefaultValue(300000);

        builder.Property(p => p.DefaultModel)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.DisableNonEssentialTraffic)
            .IsRequired()
            .HasDefaultValue(false);

        // Store ModelOverrides as JSON
        builder.Property(p => p.ModelOverrides)
            .HasColumnType("jsonb") // PostgreSQL JSONB
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => v == null ? null : JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null)
            );

        builder.Property(p => p.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(p => p.IsDefault)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.UpdatedAt);

        builder.Property(p => p.OAuthTokenRefreshedAt);

        // Indexes
        builder.HasIndex(p => p.TenantId)
            .HasDatabaseName("IX_TenantLlmProviders_TenantId");

        builder.HasIndex(p => p.ProviderType)
            .HasDatabaseName("IX_TenantLlmProviders_ProviderType");

        builder.HasIndex(p => new { p.TenantId, p.IsDefault })
            .HasDatabaseName("IX_TenantLlmProviders_TenantId_IsDefault");

        builder.HasIndex(p => new { p.TenantId, p.IsActive })
            .HasDatabaseName("IX_TenantLlmProviders_TenantId_IsActive");

        // Relationships
        builder.HasOne(p => p.Tenant)
            .WithMany() // No navigation property on Tenant yet
            .HasForeignKey(p => p.TenantId)
            .OnDelete(DeleteBehavior.Cascade); // If tenant deleted, delete all provider configs
    }
}
