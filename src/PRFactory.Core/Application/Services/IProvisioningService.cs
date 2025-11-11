using PRFactory.Domain.Entities;

namespace PRFactory.Core.Application.Services;

/// <summary>
/// Service for auto-provisioning tenants and users from external identity providers.
/// Handles user onboarding by creating tenants and users when they first authenticate.
/// </summary>
public interface IProvisioningService
{
    /// <summary>
    /// Provisions a user and their tenant from external identity provider information.
    /// If the tenant already exists (matched by identityProvider + externalTenantId),
    /// creates/updates the user in that tenant. If the tenant doesn't exist, creates a new tenant.
    /// </summary>
    /// <param name="externalUserId">The user ID from the identity provider (e.g., Auth0 user ID)</param>
    /// <param name="identityProvider">The identity provider name (e.g., "Auth0", "AzureAD")</param>
    /// <param name="externalTenantId">Optional tenant ID from the identity provider (e.g., Auth0 organization ID)</param>
    /// <param name="email">User's email address</param>
    /// <param name="displayName">User's display name</param>
    /// <param name="avatarUrl">Optional URL to user's avatar image</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple containing the provisioned Tenant, User, and a boolean indicating if this is a new tenant</returns>
    Task<(Tenant Tenant, User User, bool IsNewTenant)> ProvisionUserAsync(
        string externalUserId,
        string identityProvider,
        string? externalTenantId,
        string email,
        string displayName,
        string? avatarUrl = null,
        CancellationToken cancellationToken = default);
}
