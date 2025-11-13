using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using PRFactory.Infrastructure.Git.Providers;

namespace PRFactory.Infrastructure.Git;

/// <summary>
/// Extension methods for registering Git platform services
/// </summary>
public static class GitServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Git platform integration services including:
    /// - LocalGitService (LibGit2Sharp wrapper)
    /// - Platform providers (GitHub, Bitbucket, Azure DevOps)
    /// - GitPlatformService facade
    /// - Polly retry policies
    /// </summary>
    public static IServiceCollection AddGitPlatformIntegration(
        this IServiceCollection services,
        Func<IServiceProvider, Func<Guid, CancellationToken, Task<RepositoryEntity>>>? repositoryGetterFactory = null)
    {
        // Register local git service
        services.AddScoped<ILocalGitService, LocalGitService>();

        // Register platform providers
        services.AddScoped<IGitPlatformProvider, GitHubProvider>();
        services.AddScoped<IGitPlatformProvider, AzureDevOpsProvider>();

        // Register Bitbucket provider with HttpClient and retry policy
        // TODO: AddHttpClient requires Microsoft.Extensions.Http package
        // services.AddHttpClient<BitbucketProvider>()
        //     .AddPolicyHandler(GetHttpRetryPolicy());

        // Register BitbucketProvider as IGitPlatformProvider
        services.AddScoped<BitbucketProvider>();
        services.AddScoped<IGitPlatformProvider>(sp => sp.GetRequiredService<BitbucketProvider>());

        // Register facade service
        if (repositoryGetterFactory != null)
        {
            services.AddScoped<IGitPlatformService>(sp =>
            {
                var localGitService = sp.GetRequiredService<ILocalGitService>();
                var providers = sp.GetRequiredService<IEnumerable<IGitPlatformProvider>>();
                var cache = sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<GitPlatformService>>();
                var repositoryGetter = repositoryGetterFactory(sp);

                return new GitPlatformService(
                    localGitService,
                    providers,
                    cache,
                    logger,
                    repositoryGetter
                );
            });
        }
        else
        {
            // For testing or when repository getter is not available
            services.AddScoped<IGitPlatformService>(sp =>
            {
                var localGitService = sp.GetRequiredService<ILocalGitService>();
                var providers = sp.GetRequiredService<IEnumerable<IGitPlatformProvider>>();
                var cache = sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<GitPlatformService>>();

                // Default stub repository getter
                Task<RepositoryEntity> DefaultRepositoryGetter(Guid id, CancellationToken ct)
                {
                    throw new InvalidOperationException(
                        "Repository getter not configured. Please provide a repositoryGetterFactory when calling AddGitPlatformIntegration()");
                }

                return new GitPlatformService(
                    localGitService,
                    providers,
                    cache,
                    logger,
                    DefaultRepositoryGetter
                );
            });
        }

        // Ensure memory cache is registered
        services.AddMemoryCache();

        return services;
    }

    /// <summary>
    /// Polly retry policy for HTTP operations (Bitbucket provider)
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetHttpRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
            );
    }

    /// <summary>
    /// Example: Register with a custom repository getter
    /// </summary>
    /// <example>
    /// services.AddGitPlatformIntegration(sp =>
    /// {
    ///     var repoRepository = sp.GetRequiredService&lt;IRepositoryRepository&gt;();
    ///     return async (Guid id, CancellationToken ct) =>
    ///     {
    ///         var repo = await repoRepository.GetByIdAsync(id, ct);
    ///         return new RepositoryEntity
    ///         {
    ///             Id = repo.Id,
    ///             Name = repo.Name,
    ///             GitPlatform = repo.GitPlatform,
    ///             CloneUrl = repo.CloneUrl,
    ///             DefaultBranch = repo.DefaultBranch,
    ///             AccessToken = repo.AccessToken
    ///         };
    ///     };
    /// });
    /// </example>
    public static IServiceCollection AddGitPlatformIntegrationWithRepository<TRepository>(
        this IServiceCollection services)
        where TRepository : class
    {
        return services.AddGitPlatformIntegration(sp =>
        {
            var repository = sp.GetRequiredService<TRepository>();

            // Use reflection to call GetByIdAsync
            return async (Guid id, CancellationToken ct) =>
            {
                var method = typeof(TRepository).GetMethod("GetByIdAsync");
                if (method == null)
                {
                    throw new InvalidOperationException(
                        $"Repository type {typeof(TRepository).Name} does not have a GetByIdAsync method");
                }

                object[] args = [id, ct];
                var task = method.Invoke(repository, args) as Task<object>;
                if (task == null)
                {
                    throw new InvalidOperationException("GetByIdAsync did not return a Task");
                }

                var repo = await task;
                if (repo == null)
                {
                    throw new InvalidOperationException($"Repository with ID {id} not found");
                }

                // Map to RepositoryEntity
                return MapToRepositoryEntity(repo);
            };
        });
    }

    private static RepositoryEntity MapToRepositoryEntity(object repo)
    {
        var type = repo.GetType();

        return new RepositoryEntity
        {
            Id = (Guid)type.GetProperty("Id")?.GetValue(repo)!,
            Name = (string)type.GetProperty("Name")?.GetValue(repo)!,
            GitPlatform = (string)type.GetProperty("GitPlatform")?.GetValue(repo)!,
            CloneUrl = (string)type.GetProperty("CloneUrl")?.GetValue(repo)!,
            DefaultBranch = (string)type.GetProperty("DefaultBranch")?.GetValue(repo)! ?? "main",
            AccessToken = (string)type.GetProperty("AccessToken")?.GetValue(repo)!
        };
    }
}
