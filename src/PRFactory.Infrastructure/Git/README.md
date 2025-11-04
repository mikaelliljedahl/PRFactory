# Git Platform Integration

This module provides a unified abstraction for working with multiple Git platforms (GitHub, Bitbucket, Azure DevOps) while keeping the core application logic platform-agnostic.

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│          IGitPlatformService (Facade)                       │
│  • CloneRepositoryAsync()                                   │
│  • CreateBranchAsync()                                      │
│  • CommitFilesAsync()                                       │
│  • PushBranchAsync()                                        │
│  • CreatePullRequestAsync()                                 │
│  • AddPullRequestCommentAsync()                             │
└────────────────────┬────────────────────────────────────────┘
                     │
         ┌───────────┼───────────┐
         │           │           │
         ▼           ▼           ▼
┌─────────────┐ ┌─────────┐ ┌──────────────┐
│   GitHub    │ │Bitbucket│ │ Azure DevOps │
│  Provider   │ │Provider │ │  Provider    │
│  (Octokit)  │ │(HttpClt)│ │  (ADO SDK)   │
└──────┬──────┘ └────┬────┘ └──────┬───────┘
       │             │             │
       │  All use    │             │
       └─────────────┼─────────────┘
                     ▼
         ┌───────────────────────┐
         │  LocalGitService      │
         │  (LibGit2Sharp)       │
         │  • Clone, Branch      │
         │  • Commit, Push       │
         └───────────────────────┘
```

## Components

### 1. LocalGitService (LibGit2Sharp Wrapper)

Handles all local Git operations using LibGit2Sharp:

```csharp
public interface ILocalGitService
{
    Task<string> CloneAsync(string repoUrl, string accessToken, CancellationToken ct = default);
    Task<string> CreateBranchAsync(string repoPath, string branchName, string fromBranch);
    Task CommitAsync(string repoPath, Dictionary<string, string> files, string message, string author);
    Task PushAsync(string repoPath, string branchName, string accessToken);
    Task<bool> BranchExistsAsync(string repoPath, string branchName);
    string GetDefaultBranch(string repoPath);
}
```

### 2. IGitPlatformProvider (Strategy Interface)

Platform-specific operations (PR creation, comments):

```csharp
public interface IGitPlatformProvider
{
    string PlatformName { get; }
    Task<PullRequestInfo> CreatePullRequestAsync(Guid repositoryId, CreatePullRequestRequest request, CancellationToken ct = default);
    Task AddPullRequestCommentAsync(Guid repositoryId, int pullRequestNumber, string comment, CancellationToken ct = default);
    Task<RepositoryInfo> GetRepositoryInfoAsync(Guid repositoryId, CancellationToken ct = default);
}
```

**Implementations:**
- **GitHubProvider** - Uses Octokit .NET library
- **BitbucketProvider** - Uses HttpClient with Bitbucket REST API
- **AzureDevOpsProvider** - Uses Microsoft.TeamFoundation.SourceControl.WebApi

### 3. GitPlatformService (Facade)

Unified interface that coordinates local operations and platform APIs:

```csharp
public interface IGitPlatformService
{
    Task<string> CloneRepositoryAsync(Guid repositoryId, CancellationToken ct = default);
    Task<string> CreateBranchAsync(Guid repositoryId, string branchName, string? fromBranch = null, CancellationToken ct = default);
    Task CommitFilesAsync(Guid repositoryId, string branchName, Dictionary<string, string> files, string commitMessage, CancellationToken ct = default);
    Task PushBranchAsync(Guid repositoryId, string branchName, CancellationToken ct = default);
    Task<PullRequestInfo> CreatePullRequestAsync(Guid repositoryId, CreatePullRequestRequest request, CancellationToken ct = default);
    Task AddPullRequestCommentAsync(Guid repositoryId, int pullRequestNumber, string comment, CancellationToken ct = default);
    Task<RepositoryInfo> GetRepositoryInfoAsync(Guid repositoryId, CancellationToken ct = default);
}
```

## Setup & Configuration

### 1. Install NuGet Packages

```bash
# LibGit2Sharp for local git operations
dotnet add package LibGit2Sharp

# GitHub integration
dotnet add package Octokit

# Azure DevOps integration
dotnet add package Microsoft.TeamFoundationServer.Client
dotnet add package Microsoft.VisualStudio.Services.Client

# Polly for retry policies
dotnet add package Polly
dotnet add package Microsoft.Extensions.Http.Polly

# Memory cache
dotnet add package Microsoft.Extensions.Caching.Memory
```

### 2. Register Services in Program.cs

```csharp
using PRFactory.Infrastructure.Git;

// Option 1: Basic registration with stub repository getter
services.AddGitPlatformIntegration();

// Option 2: Register with custom repository getter
services.AddGitPlatformIntegration(sp =>
{
    var repoRepository = sp.GetRequiredService<IRepositoryRepository>();

    return async (Guid id, CancellationToken ct) =>
    {
        var repo = await repoRepository.GetByIdAsync(id, ct);

        return new RepositoryEntity
        {
            Id = repo.Id,
            Name = repo.Name,
            GitPlatform = repo.GitPlatform,  // "GitHub", "Bitbucket", "AzureDevOps"
            CloneUrl = repo.CloneUrl,
            DefaultBranch = repo.DefaultBranch,
            AccessToken = repo.AccessToken
        };
    };
});
```

### 3. Configure appsettings.json

```json
{
  "Workspace": {
    "BasePath": "/var/prfactory/workspace"
  }
}
```

## Usage Examples

### Example 1: Clone Repository and Create Branch

```csharp
public class MyService
{
    private readonly IGitPlatformService _gitService;

    public MyService(IGitPlatformService gitService)
    {
        _gitService = gitService;
    }

    public async Task PrepareFeatureBranchAsync(Guid repositoryId)
    {
        // Clone repository (cached for 1 hour)
        var repoPath = await _gitService.CloneRepositoryAsync(repositoryId);

        // Create feature branch
        var branchName = "feature/my-new-feature";
        await _gitService.CreateBranchAsync(repositoryId, branchName, fromBranch: "main");

        Console.WriteLine($"Repository cloned to: {repoPath}");
        Console.WriteLine($"Branch created: {branchName}");
    }
}
```

### Example 2: Commit and Push Changes

```csharp
public async Task CommitImplementationPlanAsync(Guid repositoryId, string branchName)
{
    var files = new Dictionary<string, string>
    {
        ["IMPLEMENTATION_PLAN.md"] = "# Implementation Plan\n...",
        ["docs/architecture.md"] = "# Architecture\n...",
        ["docs/test-plan.md"] = "# Test Plan\n..."
    };

    // Commit files
    await _gitService.CommitFilesAsync(
        repositoryId,
        branchName,
        files,
        commitMessage: "Add implementation plan for PROJ-123"
    );

    // Push to remote
    await _gitService.PushBranchAsync(repositoryId, branchName);
}
```

### Example 3: Create Pull Request (Platform-Agnostic)

```csharp
public async Task CreatePullRequestAsync(Guid repositoryId, string sourceBranch)
{
    var request = new CreatePullRequestRequest(
        SourceBranch: sourceBranch,
        TargetBranch: "main",
        Title: "Implementation plan for PROJ-123",
        Description: "This PR contains the implementation plan generated by Claude AI."
    );

    // Works with GitHub, Bitbucket, or Azure DevOps automatically
    var prInfo = await _gitService.CreatePullRequestAsync(repositoryId, request);

    Console.WriteLine($"Pull Request #{prInfo.Number} created: {prInfo.HtmlUrl}");
}
```

### Example 4: Add Comment to Pull Request

```csharp
public async Task AddPlanApprovalCommentAsync(Guid repositoryId, int prNumber)
{
    var comment = @"
## Implementation Plan Approved ✅

The implementation plan has been reviewed and approved.
You may proceed with implementation.

**Approved by:** John Doe
**Date:** 2025-11-04
    ".Trim();

    await _gitService.AddPullRequestCommentAsync(repositoryId, prNumber, comment);
}
```

### Example 5: Complete Workflow

```csharp
public async Task<string> GenerateAndCommitPlanAsync(
    Guid repositoryId,
    string ticketKey,
    string planContent)
{
    // 1. Clone repository
    var repoPath = await _gitService.CloneRepositoryAsync(repositoryId);

    // 2. Create feature branch
    var branchName = $"feature/{ticketKey}-implementation-plan";
    await _gitService.CreateBranchAsync(repositoryId, branchName);

    // 3. Commit plan
    var files = new Dictionary<string, string>
    {
        ["IMPLEMENTATION_PLAN.md"] = planContent
    };
    await _gitService.CommitFilesAsync(
        repositoryId,
        branchName,
        files,
        $"Add implementation plan for {ticketKey}"
    );

    // 4. Push branch
    await _gitService.PushBranchAsync(repositoryId, branchName);

    // 5. Create PR
    var pr = await _gitService.CreatePullRequestAsync(
        repositoryId,
        new CreatePullRequestRequest(
            branchName,
            "main",
            $"Implementation Plan: {ticketKey}",
            "Generated implementation plan for review."
        )
    );

    return pr.HtmlUrl;
}
```

## Features

### Repository Caching

Cloned repositories are cached in memory for 1 hour to avoid re-cloning:

```csharp
// First call: clones repository
var path1 = await _gitService.CloneRepositoryAsync(repoId);

// Subsequent calls within 1 hour: returns cached path
var path2 = await _gitService.CloneRepositoryAsync(repoId);

Assert.Equal(path1, path2);  // Same path
```

### Retry Policies (Polly)

All platform providers include retry logic for transient errors:

- **GitHub**: Retries on rate limits, 503, and timeouts
- **Bitbucket**: Retries on 429, 503, 504, 408
- **Azure DevOps**: Retries on 503, 504, 408, 429

Exponential backoff: 2s, 4s, 8s

### Credential Handling

Access tokens are managed securely:

```csharp
// LibGit2Sharp uses OAuth2 token authentication
var cloneOptions = new CloneOptions
{
    CredentialsProvider = (url, user, cred) => new UsernamePasswordCredentials
    {
        Username = "oauth2",
        Password = accessToken
    }
};
```

### Platform Auto-Detection

The facade automatically selects the correct provider based on `Repository.GitPlatform`:

```csharp
// Repository entity has GitPlatform = "GitHub"
var pr = await _gitService.CreatePullRequestAsync(repoId, request);
// -> Uses GitHubProvider

// Repository entity has GitPlatform = "Bitbucket"
var pr = await _gitService.CreatePullRequestAsync(repoId, request);
// -> Uses BitbucketProvider
```

## Workspace Management

Repositories are cloned to isolated workspaces:

```
/var/prfactory/workspace/
├── a1b2c3d4-e5f6-7890-abcd-ef1234567890/
│   └── my-repo/
│       ├── .git/
│       ├── src/
│       └── README.md
└── f9e8d7c6-b5a4-3210-fedc-ba0987654321/
    └── another-repo/
```

Each workspace is identified by a GUID to prevent conflicts.

## Error Handling

### Invalid Platform

```csharp
try
{
    var pr = await _gitService.CreatePullRequestAsync(repoId, request);
}
catch (NotSupportedException ex)
{
    // "Git platform 'GitLab' is not supported. Available platforms: GitHub, Bitbucket, AzureDevOps"
}
```

### Branch Not Found

```csharp
try
{
    await _localGitService.CreateBranchAsync(repoPath, "feature/new", "nonexistent-branch");
}
catch (InvalidOperationException ex)
{
    // "Base branch 'nonexistent-branch' not found"
}
```

### API Errors

All platform providers include detailed logging and retry logic for transient errors.

## Testing

### Unit Tests

```csharp
[Fact]
public async Task CreateBranch_ShouldCreateFromDefaultBranch()
{
    // Arrange
    var mockLocalGit = new Mock<ILocalGitService>();
    var mockProvider = new Mock<IGitPlatformProvider>();

    mockLocalGit
        .Setup(x => x.CreateBranchAsync(It.IsAny<string>(), "feature/test", "main"))
        .ReturnsAsync("feature/test");

    // Act & Assert
    // ...
}
```

### Integration Tests

Use test repositories on each platform to verify end-to-end workflows.

## Extending with New Platforms

To add support for a new Git platform (e.g., GitLab):

1. Create `GitLabProvider.cs` implementing `IGitPlatformProvider`
2. Implement `CreatePullRequestAsync()` and `AddPullRequestCommentAsync()`
3. Register in DI: `services.AddScoped<IGitPlatformProvider, GitLabProvider>()`
4. Update repository entities to use `GitPlatform = "GitLab"`

No changes needed to core application logic!

## Dependencies

```xml
<ItemGroup>
  <PackageReference Include="LibGit2Sharp" Version="0.30.0" />
  <PackageReference Include="Octokit" Version="13.0.1" />
  <PackageReference Include="Microsoft.TeamFoundationServer.Client" Version="19.232.0" />
  <PackageReference Include="Microsoft.VisualStudio.Services.Client" Version="19.232.0" />
  <PackageReference Include="Polly" Version="8.4.1" />
  <PackageReference Include="Microsoft.Extensions.Http.Polly" Version="8.0.0" />
  <PackageReference Include="Microsoft.Extensions.Caching.Memory" Version="8.0.0" />
</ItemGroup>
```

## Related Documentation

- [Git Integration Architecture](/home/user/PRFactory/docs/architecture/git-integration.md)
- [Core Engine Architecture](/home/user/PRFactory/docs/architecture/core-engine.md)
- [LibGit2Sharp Documentation](https://github.com/libgit2/libgit2sharp/wiki)
- [Octokit.NET Documentation](https://octokitnet.readthedocs.io/)
- [Azure DevOps SDK Documentation](https://learn.microsoft.com/en-us/azure/devops/integrate/)
