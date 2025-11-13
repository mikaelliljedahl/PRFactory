using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.DTOs;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Domain.ValueObjects;
using PRFactory.Infrastructure.Persistence.Encryption;

namespace PRFactory.Infrastructure.Application;

/// <summary>
/// Service for managing tenant LLM provider configurations
/// </summary>
public class TenantLlmProviderService(
    ITenantLlmProviderRepository repository,
    ITenantRepository tenantRepository,
    ICurrentUserService currentUserService,
    IEncryptionService encryptionService,
    ILogger<TenantLlmProviderService> logger) : ITenantLlmProviderService
{
    /// <summary>
    /// Gets all providers for the current tenant
    /// </summary>
    public async Task<List<TenantLlmProviderDto>> GetProvidersForTenantAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = await GetCurrentTenantIdAsync(cancellationToken);
        var providers = await repository.GetByTenantIdAsync(tenantId, cancellationToken);

        logger.LogInformation("Retrieved {Count} providers for tenant {TenantId}", providers.Count, tenantId);

        return providers.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Gets a specific provider by ID (with tenant isolation)
    /// </summary>
    public async Task<TenantLlmProviderDto?> GetProviderByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = await GetCurrentTenantIdAsync(cancellationToken);
        var provider = await repository.GetByIdAsync(id, cancellationToken);

        if (provider == null)
        {
            logger.LogWarning("Provider {ProviderId} not found", id);
            return null;
        }

        // Enforce tenant isolation
        if (provider.TenantId != tenantId)
        {
            logger.LogWarning("Provider {ProviderId} belongs to different tenant. Current: {CurrentTenantId}, Owner: {OwnerTenantId}",
                id, tenantId, provider.TenantId);
            throw new UnauthorizedAccessException($"Provider {id} does not belong to the current tenant");
        }

        return MapToDto(provider);
    }

    /// <summary>
    /// Creates a new API key-based provider
    /// </summary>
    public async Task<TenantLlmProviderDto> CreateApiKeyProviderAsync(CreateApiKeyProviderDto dto, CancellationToken cancellationToken = default)
    {
        var tenantId = await GetCurrentTenantIdAsync(cancellationToken);

        // Parse provider type
        if (!Enum.TryParse<LlmProviderType>(dto.ProviderType, ignoreCase: true, out var providerType))
        {
            throw new ArgumentException($"Invalid provider type: {dto.ProviderType}");
        }

        // Encrypt API key
        var encryptedApiKey = encryptionService.Encrypt(dto.ApiKey);

        // Create provider configuration
        var config = new ApiKeyProviderConfiguration(
            tenantId: tenantId,
            name: dto.Name,
            providerType: providerType,
            apiBaseUrl: dto.ApiBaseUrl,
            encryptedApiToken: encryptedApiKey,
            defaultModel: dto.DefaultModel,
            timeoutMs: dto.TimeoutMs,
            disableNonEssentialTraffic: dto.DisableNonEssentialTraffic,
            modelOverrides: dto.ModelOverrides
        );

        // Create entity
        var provider = TenantLlmProvider.CreateApiKeyProvider(config);

        // Save to repository
        await repository.AddAsync(provider, cancellationToken);

        logger.LogInformation("Created API key provider {ProviderId} for tenant {TenantId}", provider.Id, tenantId);

        return MapToDto(provider);
    }

    /// <summary>
    /// Creates a new OAuth-based provider (Native Anthropic)
    /// </summary>
    public async Task<TenantLlmProviderDto> CreateOAuthProviderAsync(CreateOAuthProviderDto dto, CancellationToken cancellationToken = default)
    {
        var tenantId = await GetCurrentTenantIdAsync(cancellationToken);

        // Create OAuth provider
        var provider = TenantLlmProvider.CreateOAuthProvider(
            tenantId: tenantId,
            name: dto.Name,
            defaultModel: dto.DefaultModel
        );

        // Save to repository
        await repository.AddAsync(provider, cancellationToken);

        logger.LogInformation("Created OAuth provider {ProviderId} for tenant {TenantId}", provider.Id, tenantId);

        return MapToDto(provider);
    }

    /// <summary>
    /// Updates an existing provider configuration
    /// </summary>
    public async Task<TenantLlmProviderDto> UpdateProviderAsync(Guid id, UpdateProviderDto dto, CancellationToken cancellationToken = default)
    {
        var tenantId = await GetCurrentTenantIdAsync(cancellationToken);
        var provider = await repository.GetByIdAsync(id, cancellationToken);

        if (provider == null)
        {
            throw new InvalidOperationException($"Provider {id} not found");
        }

        // Enforce tenant isolation
        if (provider.TenantId != tenantId)
        {
            logger.LogWarning("Attempted to update provider {ProviderId} from different tenant. Current: {CurrentTenantId}, Owner: {OwnerTenantId}",
                id, tenantId, provider.TenantId);
            throw new UnauthorizedAccessException($"Provider {id} does not belong to the current tenant");
        }

        // Update configuration
        provider.UpdateConfiguration(
            name: dto.Name,
            apiBaseUrl: dto.ApiBaseUrl,
            defaultModel: dto.DefaultModel,
            timeoutMs: dto.TimeoutMs,
            disableNonEssentialTraffic: dto.DisableNonEssentialTraffic,
            modelOverrides: dto.ModelOverrides
        );

        // Update active status
        if (dto.IsActive)
        {
            provider.Activate();
        }
        else
        {
            provider.Deactivate();
        }

        // Save changes
        await repository.UpdateAsync(provider, cancellationToken);

        logger.LogInformation("Updated provider {ProviderId} for tenant {TenantId}", id, tenantId);

        return MapToDto(provider);
    }

    /// <summary>
    /// Deletes a provider configuration (soft delete via deactivation)
    /// </summary>
    public async Task DeleteProviderAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = await GetCurrentTenantIdAsync(cancellationToken);
        var provider = await repository.GetByIdAsync(id, cancellationToken);

        if (provider == null)
        {
            throw new InvalidOperationException($"Provider {id} not found");
        }

        // Enforce tenant isolation
        if (provider.TenantId != tenantId)
        {
            logger.LogWarning("Attempted to delete provider {ProviderId} from different tenant. Current: {CurrentTenantId}, Owner: {OwnerTenantId}",
                id, tenantId, provider.TenantId);
            throw new UnauthorizedAccessException($"Provider {id} does not belong to the current tenant");
        }

        // Soft delete: deactivate the provider
        provider.Deactivate();
        await repository.UpdateAsync(provider, cancellationToken);

        logger.LogInformation("Deactivated (soft deleted) provider {ProviderId} for tenant {TenantId}", id, tenantId);
    }

    /// <summary>
    /// Sets a provider as the default for the tenant (clears other defaults)
    /// </summary>
    public async Task<TenantLlmProviderDto> SetDefaultProviderAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = await GetCurrentTenantIdAsync(cancellationToken);

        // Load tenant with providers
        var tenant = await tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
        {
            throw new InvalidOperationException($"Tenant {tenantId} not found");
        }

        var provider = await repository.GetByIdAsync(id, cancellationToken);
        if (provider == null)
        {
            throw new InvalidOperationException($"Provider {id} not found");
        }

        // Enforce tenant isolation
        if (provider.TenantId != tenantId)
        {
            logger.LogWarning("Attempted to set default provider {ProviderId} from different tenant. Current: {CurrentTenantId}, Owner: {OwnerTenantId}",
                id, tenantId, provider.TenantId);
            throw new UnauthorizedAccessException($"Provider {id} does not belong to the current tenant");
        }

        // Get all providers for this tenant to update their status
        var allProviders = await repository.GetByTenantIdAsync(tenantId, cancellationToken);

        // Clear default status from all providers
        foreach (var p in allProviders.Where(p => p.IsDefault))
        {
            p.RemoveAsDefault();
            await repository.UpdateAsync(p, cancellationToken);
        }

        // Set new default
        provider.SetAsDefault();
        await repository.UpdateAsync(provider, cancellationToken);

        // Also update tenant entity
        await tenantRepository.UpdateAsync(tenant, cancellationToken);

        logger.LogInformation("Set provider {ProviderId} as default for tenant {TenantId}", id, tenantId);

        return MapToDto(provider);
    }

    /// <summary>
    /// Tests connection to an LLM provider
    /// </summary>
    public async Task<ConnectionTestResult> TestProviderConnectionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = await GetCurrentTenantIdAsync(cancellationToken);
        var provider = await repository.GetByIdAsync(id, cancellationToken);

        if (provider == null)
        {
            throw new InvalidOperationException($"Provider {id} not found");
        }

        // Enforce tenant isolation
        if (provider.TenantId != tenantId)
        {
            logger.LogWarning("Attempted to test provider {ProviderId} from different tenant. Current: {CurrentTenantId}, Owner: {OwnerTenantId}",
                id, tenantId, provider.TenantId);
            throw new UnauthorizedAccessException($"Provider {id} does not belong to the current tenant");
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Validate provider has API token
            if (string.IsNullOrWhiteSpace(provider.EncryptedApiToken))
            {
                return new ConnectionTestResult
                {
                    Success = false,
                    Message = "Provider has no API token configured",
                    ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds
                };
            }

            // Decrypt API token to validate it can be decrypted
            var decryptedToken = encryptionService.Decrypt(provider.EncryptedApiToken);

            if (string.IsNullOrWhiteSpace(decryptedToken))
            {
                return new ConnectionTestResult
                {
                    Success = false,
                    Message = "Decrypted API token is empty",
                    ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds
                };
            }

            stopwatch.Stop();

            // For now, just validate decryption worked
            // In the future, this could make an actual API call to the provider
            logger.LogInformation("Successfully tested provider {ProviderId} connection in {ElapsedMs}ms", id, stopwatch.ElapsedMilliseconds);

            return new ConnectionTestResult
            {
                Success = true,
                Message = "Provider configuration is valid",
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            logger.LogError(ex, "Failed to test provider {ProviderId} connection", id);

            return new ConnectionTestResult
            {
                Success = false,
                Message = $"Connection test failed: {ex.Message}",
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
    }

    /// <summary>
    /// Gets the current tenant ID (throws if not authenticated)
    /// </summary>
    private async Task<Guid> GetCurrentTenantIdAsync(CancellationToken cancellationToken)
    {
        var tenantId = await currentUserService.GetCurrentTenantIdAsync(cancellationToken);

        if (!tenantId.HasValue)
        {
            logger.LogError("No current tenant ID available - user not authenticated");
            throw new UnauthorizedAccessException("User is not authenticated or does not belong to a tenant");
        }

        return tenantId.Value;
    }

    /// <summary>
    /// Maps a TenantLlmProvider entity to a DTO
    /// </summary>
    private static TenantLlmProviderDto MapToDto(TenantLlmProvider provider)
    {
        return new TenantLlmProviderDto
        {
            Id = provider.Id,
            TenantId = provider.TenantId,
            Name = provider.Name,
            ProviderType = provider.ProviderType.ToString(),
            UsesOAuth = provider.UsesOAuth,
            ApiBaseUrl = provider.ApiBaseUrl,
            DefaultModel = provider.DefaultModel,
            TimeoutMs = provider.TimeoutMs,
            IsDefault = provider.IsDefault,
            IsActive = provider.IsActive,
            CreatedAt = provider.CreatedAt
        };
    }
}
