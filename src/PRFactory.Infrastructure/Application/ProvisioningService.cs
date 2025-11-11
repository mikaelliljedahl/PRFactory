using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;

namespace PRFactory.Infrastructure.Application;

/// <summary>
/// Service implementation for provisioning users and tenants from external OAuth providers
/// </summary>
public class ProvisioningService : IProvisioningService
{
    private const string PlaceholderTicketPlatformUrl = "https://placeholder.example.com";
    private const string PlaceholderApiToken = "PLACEHOLDER_TOKEN";
    private const string PlaceholderClaudeApiKey = "PLACEHOLDER_API_KEY";

    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<ProvisioningService> _logger;

    public ProvisioningService(
        ITenantRepository tenantRepository,
        IUserRepository userRepository,
        ILogger<ProvisioningService> logger)
    {
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<(Tenant Tenant, User User, bool IsNewTenant)> ProvisionUserAsync(
        string externalUserId,
        string identityProvider,
        string? externalTenantId,
        string email,
        string displayName,
        string? avatarUrl = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalUserId))
            throw new ArgumentException("External user ID cannot be empty", nameof(externalUserId));

        if (string.IsNullOrWhiteSpace(identityProvider))
            throw new ArgumentException("Identity provider cannot be empty", nameof(identityProvider));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name cannot be empty", nameof(displayName));

        _logger.LogInformation(
            "Provisioning user: ExternalUserId={ExternalUserId}, IdentityProvider={IdentityProvider}, ExternalTenantId={ExternalTenantId}, Email={Email}",
            externalUserId, identityProvider, externalTenantId, email);

        // 1. Find or create tenant
        Tenant? tenant = null;
        bool isNewTenant = false;

        if (!string.IsNullOrWhiteSpace(externalTenantId))
        {
            // Try to find existing tenant by external tenant ID
            tenant = await _tenantRepository.GetByExternalTenantAsync(identityProvider, externalTenantId, cancellationToken);
        }

        if (tenant == null)
        {
            // Create new tenant
            var tenantName = DetermineTenantName(identityProvider, externalTenantId, email);
            var effectiveExternalTenantId = externalTenantId ?? email; // Use email if no external tenant ID

            tenant = Tenant.Create(
                name: tenantName,
                identityProvider: identityProvider,
                externalTenantId: effectiveExternalTenantId,
                ticketPlatformUrl: PlaceholderTicketPlatformUrl, // Placeholder - user will configure later
                ticketPlatformApiToken: PlaceholderApiToken, // Placeholder - user will configure later
                claudeApiKey: GetClaudeApiKeyFromEnvironment() ?? PlaceholderClaudeApiKey, // Try environment variable or placeholder
                ticketPlatform: "Jira");

            // Mark tenant as inactive - requires configuration before use
            tenant.Deactivate();

            await _tenantRepository.AddAsync(tenant, cancellationToken);
            isNewTenant = true;

            _logger.LogInformation(
                "Created new tenant: TenantId={TenantId}, Name={TenantName}, IdentityProvider={IdentityProvider}, ExternalTenantId={ExternalTenantId}",
                tenant.Id, tenant.Name, identityProvider, effectiveExternalTenantId);
        }
        else
        {
            _logger.LogInformation(
                "Found existing tenant: TenantId={TenantId}, Name={TenantName}",
                tenant.Id, tenant.Name);
        }

        // 2. Find or create user (check by email in tenant first)
        var user = await _userRepository.GetByEmailAsync(tenant.Id, email, cancellationToken);

        if (user == null)
        {
            // Determine role: Owner if first user in new tenant, Member otherwise
            var role = isNewTenant ? UserRole.Owner : UserRole.Member;

            // Create new user with all properties
            user = User.Create(
                tenantId: tenant.Id,
                email: email,
                displayName: displayName,
                avatarUrl: avatarUrl,
                externalAuthId: externalUserId,
                identityProvider: identityProvider,
                role: role);

            await _userRepository.AddAsync(user, cancellationToken);

            _logger.LogInformation(
                "Created new user: UserId={UserId}, Email={Email}, TenantId={TenantId}, Role={Role}",
                user.Id, user.Email, tenant.Id, user.Role);
        }
        else
        {
            // User exists - check if we need to link external auth
            if (string.IsNullOrWhiteSpace(user.ExternalAuthId))
            {
                _logger.LogInformation(
                    "Linking external auth for existing user: UserId={UserId}, IdentityProvider={IdentityProvider}",
                    user.Id, identityProvider);

                user.LinkExternalAuth(externalUserId, identityProvider);
            }

            // Update existing user's profile if needed
            if (user.DisplayName != displayName || user.AvatarUrl != avatarUrl)
            {
                user.UpdateProfile(displayName, avatarUrl);
            }

            // Update last seen
            user.UpdateLastSeen();
            await _userRepository.UpdateAsync(user, cancellationToken);

            _logger.LogInformation(
                "Found existing user: UserId={UserId}, Email={Email}, TenantId={TenantId}, Role={Role}",
                user.Id, user.Email, user.TenantId, user.Role);
        }

        return (tenant, user, isNewTenant);
    }

    /// <summary>
    /// Determines a tenant name based on identity provider and external tenant ID
    /// </summary>
    private static string DetermineTenantName(string identityProvider, string? externalTenantId, string email)
    {
        // For Google Workspace, use the domain
        if (identityProvider == "GoogleWorkspace" && !string.IsNullOrWhiteSpace(externalTenantId))
        {
            return externalTenantId; // e.g., "company.com"
        }

        // For Azure AD, try to extract organization name from email domain
        if (identityProvider == "AzureAD")
        {
            var domain = email.Split('@').LastOrDefault();
            if (!string.IsNullOrWhiteSpace(domain))
            {
                // Remove common TLDs and return company name
                var companyName = domain.Split('.').FirstOrDefault() ?? "Unknown";
                return char.ToUpper(companyName[0]) + companyName.Substring(1);
            }
        }

        // Fallback to email domain
        var emailDomain = email.Split('@').LastOrDefault() ?? "Unknown";
        return emailDomain;
    }

    /// <summary>
    /// Tries to get Claude API key from environment variable
    /// </summary>
    private static string? GetClaudeApiKeyFromEnvironment()
    {
        // Try to get from environment variable (useful for development)
        return Environment.GetEnvironmentVariable("CLAUDE_API_KEY")
            ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
    }
}
