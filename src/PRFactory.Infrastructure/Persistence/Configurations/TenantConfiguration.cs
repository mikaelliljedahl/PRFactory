using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRFactory.Domain.Entities;
using PRFactory.Infrastructure.Persistence.Encryption;
using System.Text.Json;

namespace PRFactory.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the Tenant entity.
/// </summary>
public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    private readonly IEncryptionService _encryptionService;

    public TenantConfiguration(IEncryptionService encryptionService)
    {
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
    }

    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");

        // Primary Key
        builder.HasKey(t => t.Id);

        // Properties
        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.TicketPlatform)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Jira");

        builder.Property(t => t.TicketPlatformUrl)
            .IsRequired()
            .HasMaxLength(500);

        // Encrypted fields - store as encrypted strings
        builder.Property(t => t.TicketPlatformApiToken)
            .IsRequired()
            .HasMaxLength(1000)
            .HasConversion(
                v => _encryptionService.Encrypt(v),
                v => _encryptionService.Decrypt(v)
            );

        builder.Property(t => t.ClaudeApiKey)
            .IsRequired()
            .HasMaxLength(1000)
            .HasConversion(
                v => _encryptionService.Encrypt(v),
                v => _encryptionService.Decrypt(v)
            );

        builder.Property(t => t.IsActive)
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.UpdatedAt);

        // Complex type - TenantConfiguration stored as JSON
        builder.OwnsOne(t => t.Configuration, config =>
        {
            config.ToJson();
            config.Property(c => c.AutoImplementAfterPlanApproval);
            config.Property(c => c.MaxRetries);
            config.Property(c => c.ClaudeModel).HasMaxLength(100);
            config.Property(c => c.MaxTokensPerRequest);
            config.Property(c => c.ApiTimeoutSeconds);
            config.Property(c => c.EnableVerboseLogging);
            config.Property(c => c.EnableCodeReview);
            config.Property(c => c.AllowedRepositories);
            config.OwnsOne(c => c.CustomPromptTemplates);
        });

        // Indexes
        builder.HasIndex(t => t.TicketPlatform)
            .HasDatabaseName("IX_Tenants_TicketPlatform");

        // Relationships
        builder.HasMany(t => t.Repositories)
            .WithOne(r => r.Tenant)
            .HasForeignKey(r => r.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Tickets)
            .WithOne(ticket => ticket.Tenant)
            .HasForeignKey(ticket => ticket.TenantId)
            .OnDelete(DeleteBehavior.Restrict); // Don't cascade delete tickets
    }
}
