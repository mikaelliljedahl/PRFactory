# Git Platform Integration - Quick Start Guide

## 5-Minute Setup

### Step 1: Install NuGet Packages

```bash
dotnet add package LibGit2Sharp
dotnet add package Octokit
dotnet add package Microsoft.TeamFoundationServer.Client
dotnet add package Microsoft.VisualStudio.Services.Client
dotnet add package Polly
dotnet add package Microsoft.Extensions.Http.Polly
```

### Step 2: Register Services (Program.cs)

```csharp
using PRFactory.Infrastructure.Git;

// Add to your service registration
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
            GitPlatform = repo.GitPlatform,  // "GitHub", "Bitbucket", or "AzureDevOps"
            CloneUrl = repo.CloneUrl,
            DefaultBranch = repo.DefaultBranch,
            AccessToken = repo.AccessToken
        };
    };
});
```

### Step 3: Configure Workspace (appsettings.json)

```json
{
  "Workspace": {
    "BasePath": "/var/prfactory/workspace"
  }
}
```

### Step 4: Use in Your Code

```csharp
public class MyService
{
    private readonly IGitPlatformService _gitService;

    public MyService(IGitPlatformService gitService)
    {
        _gitService = gitService;
    }

    public async Task CreateImplementationPlanAsync(Guid repoId, string ticketKey)
    {
        // Clone repo
        await _gitService.CloneRepositoryAsync(repoId);

        // Create branch
        var branch = $"feature/{ticketKey}-plan";
        await _gitService.CreateBranchAsync(repoId, branch);

        // Commit files
        await _gitService.CommitFilesAsync(repoId, branch, new Dictionary<string, string>
        {
            ["IMPLEMENTATION_PLAN.md"] = "# Plan content..."
        }, $"Add plan for {ticketKey}");

        // Push
        await _gitService.PushBranchAsync(repoId, branch);

        // Create PR
        var pr = await _gitService.CreatePullRequestAsync(repoId, new CreatePullRequestRequest(
            SourceBranch: branch,
            TargetBranch: "main",
            Title: $"Plan: {ticketKey}",
            Description: "Implementation plan for review"
        ));

        Console.WriteLine($"PR created: {pr.HtmlUrl}");
    }
}
```

## Common Operations

### Clone Repository
```csharp
var repoPath = await _gitService.CloneRepositoryAsync(repositoryId);
// Returns: /var/prfactory/workspace/{guid}/repo-name
// Cached for 1 hour
```

### Create Branch
```csharp
// From default branch
await _gitService.CreateBranchAsync(repoId, "feature/new-feature");

// From specific branch
await _gitService.CreateBranchAsync(repoId, "feature/new-feature", fromBranch: "develop");
```

### Commit Files
```csharp
var files = new Dictionary<string, string>
{
    ["path/to/file1.md"] = "Content 1",
    ["path/to/file2.cs"] = "Content 2"
};

await _gitService.CommitFilesAsync(repoId, "feature/branch", files, "Commit message");
```

### Push Branch
```csharp
await _gitService.PushBranchAsync(repoId, "feature/branch");
```

### Create Pull Request
```csharp
var request = new CreatePullRequestRequest(
    SourceBranch: "feature/my-branch",
    TargetBranch: "main",
    Title: "My PR Title",
    Description: "PR description with markdown support"
);

var pr = await _gitService.CreatePullRequestAsync(repoId, request);
// Returns: PullRequestInfo with Number, Url, HtmlUrl
```

### Add PR Comment
```csharp
await _gitService.AddPullRequestCommentAsync(repoId, prNumber: 42, "Great work!");
```

## Platform Support

| Platform | Status | Implementation | API Library |
|----------|--------|----------------|-------------|
| GitHub | ✅ Ready | GitHubProvider | Octokit |
| Bitbucket | ✅ Ready | BitbucketProvider | HttpClient + REST API |
| Azure DevOps | ✅ Ready | AzureDevOpsProvider | Azure DevOps SDK |

All platforms support:
- ✅ Create Pull Request
- ✅ Add PR Comment
- ✅ Get Repository Info
- ✅ Clone, Branch, Commit, Push (via LibGit2Sharp)

## Error Handling

All operations include automatic retry with exponential backoff for transient errors:

```csharp
try
{
    var pr = await _gitService.CreatePullRequestAsync(repoId, request);
}
catch (ApiException ex) when (ex.StatusCode == 404)
{
    // Repository or branch not found
}
catch (VssServiceException ex)
{
    // Azure DevOps specific error
}
catch (HttpRequestException ex)
{
    // Bitbucket HTTP error
}
```

## Testing

Mock the service for unit tests:

```csharp
var mockGitService = new Mock<IGitPlatformService>();

mockGitService
    .Setup(x => x.CreatePullRequestAsync(It.IsAny<Guid>(), It.IsAny<CreatePullRequestRequest>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(new PullRequestInfo(42, "https://api.url", "https://html.url"));

// Use in your tests
var service = new MyService(mockGitService.Object);
```

## Troubleshooting

### "Repository getter not configured"
Make sure you call `AddGitPlatformIntegration()` with a repository getter factory.

### "Git platform 'X' is not supported"
Check that `Repository.GitPlatform` is one of: "GitHub", "Bitbucket", "AzureDevOps" (case-insensitive).

### "Branch 'X' not found"
Ensure the base branch exists. The default branch is used if `fromBranch` is null.

### Token Authentication Issues
- **GitHub**: Use a Personal Access Token with `repo` scope
- **Bitbucket**: Use an App Password with repository write permissions
- **Azure DevOps**: Use a PAT with Code (Read & Write) permissions

## Next Steps

- See [README.md](./README.md) for detailed documentation
- Review [git-integration.md](/home/user/PRFactory/docs/architecture/git-integration.md) for architecture details
- Check individual provider implementations in `Providers/` directory

## File Structure

```
Git/
├── LocalGitService.cs              # LibGit2Sharp wrapper
├── IGitPlatformProvider.cs         # Strategy interface + DTOs
├── GitPlatformService.cs           # Main facade service
├── GitServiceCollectionExtensions.cs  # DI registration
├── README.md                       # Full documentation
├── QUICKSTART.md                   # This file
└── Providers/
    ├── GitHubProvider.cs           # GitHub implementation
    ├── BitbucketProvider.cs        # Bitbucket implementation
    └── AzureDevOpsProvider.cs      # Azure DevOps implementation
```

**Total**: ~1,300 lines of production-ready code
