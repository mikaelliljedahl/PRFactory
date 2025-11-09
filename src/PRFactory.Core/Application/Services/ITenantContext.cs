namespace PRFactory.Core.Application.Services;

/// <summary>
/// Provides the current tenant context for the application.
/// Used to determine which tenant's data should be accessed.
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// Gets the current tenant ID
    /// </summary>
    /// <returns>The current tenant ID</returns>
    Guid GetCurrentTenantId();
}
