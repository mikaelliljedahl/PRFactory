# Phase 2: Service Layer & DTOs

**Status**: Not Started
**Estimated Effort**: 3-4 hours
**Dependencies**: Phase 1
**Risk Level**: Low

## Objective

Create application service methods and DTOs for diff retrieval. Enable Blazor components to load diff content via direct service injection (NO HTTP calls per CLAUDE.md).

## Architecture

```
Blazor Component (Detail.razor.cs)
  → @inject ITicketService  (Web facade)
    → ITicketApplicationService  (Application service)
      → IWorkspaceService.ReadDiffAsync(ticketId)
      → Returns DiffContentDto
```

## Tasks

### Task 2.1: Create DTOs

**File**: `/src/PRFactory.Web/Models/DiffContentDto.cs`

```csharp
namespace PRFactory.Web.Models;

/// <summary>
/// DTO for diff content returned to Blazor components.
/// </summary>
public class DiffContentDto
{
    /// <summary>
    /// The ticket ID this diff belongs to
    /// </summary>
    public required Guid TicketId { get; init; }

    /// <summary>
    /// Raw diff content in unified diff format (git patch)
    /// </summary>
    public required string DiffContent { get; init; }

    /// <summary>
    /// Size of diff in bytes
    /// </summary>
    public int SizeBytes { get; init; }

    /// <summary>
    /// Number of files changed (parsed from diff)
    /// </summary>
    public int FilesChanged { get; init; }

    /// <summary>
    /// Indicates if diff exists and is available
    /// </summary>
    public bool Available { get; init; }
}
```

**File**: `/src/PRFactory.Web/Models/CreatePRResponse.cs`

```csharp
namespace PRFactory.Web.Models;

/// <summary>
/// Response DTO after pull request creation.
/// </summary>
public class CreatePRResponse
{
    /// <summary>
    /// URL to the created pull request
    /// </summary>
    public required string PullRequestUrl { get; init; }

    /// <summary>
    /// Pull request number (e.g., #123)
    /// </summary>
    public required int PullRequestNumber { get; init; }

    /// <summary>
    /// Indicates if PR was created successfully
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if creation failed
    /// </summary>
    public string? ErrorMessage { get; init; }
}
```

### Task 2.2: Extend Application Service Interface

**File**: `/src/PRFactory.Core/Application/Services/ITicketApplicationService.cs` (existing file)

**Add new methods**:

```csharp
/// <summary>
/// Gets the diff content for a ticket if available.
/// </summary>
/// <param name="ticketId">The ticket ID</param>
/// <returns>Diff content, or null if not available</returns>
Task<string?> GetDiffContentAsync(Guid ticketId);

/// <summary>
/// Creates a pull request for a ticket with approved code changes.
/// </summary>
/// <param name="ticketId">The ticket ID</param>
/// <param name="approvedBy">User who approved the changes</param>
/// <returns>PR creation result with URL and number</returns>
Task<PullRequestCreationResult> CreatePullRequestAsync(Guid ticketId, string? approvedBy = null);
```

**Note**: `PullRequestCreationResult` is a domain object, not a DTO.

**File**: `/src/PRFactory.Domain/Results/PullRequestCreationResult.cs` (new file)

```csharp
namespace PRFactory.Domain.Results;

public class PullRequestCreationResult
{
    public bool Success { get; init; }
    public string? PullRequestUrl { get; init; }
    public int? PullRequestNumber { get; init; }
    public string? ErrorMessage { get; init; }

    public static PullRequestCreationResult Successful(string url, int number) => new()
    {
        Success = true,
        PullRequestUrl = url,
        PullRequestNumber = number
    };

    public static PullRequestCreationResult Failed(string errorMessage) => new()
    {
        Success = false,
        ErrorMessage = errorMessage
    };
}
```

### Task 2.3: Implement Application Service Methods

**File**: `/src/PRFactory.Infrastructure/Application/TicketApplicationService.cs` (existing file)

**Add dependencies** (constructor injection):

```csharp
private readonly IWorkspaceService _workspaceService;
private readonly ILocalGitService _localGitService;
private readonly IGitPlatformProvider _gitPlatformProvider;

// Add to constructor parameters
```

**Implement new methods**:

```csharp
public async Task<string?> GetDiffContentAsync(Guid ticketId)
{
    _logger.LogDebug("Getting diff content for ticket {TicketId}", ticketId);

    try
    {
        var diffContent = await _workspaceService.ReadDiffAsync(ticketId);

        if (diffContent == null)
        {
            _logger.LogInformation("No diff available for ticket {TicketId}", ticketId);
            return null;
        }

        _logger.LogInformation("Retrieved diff for ticket {TicketId}: {Size} bytes",
            ticketId, diffContent.Length);

        return diffContent;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error retrieving diff for ticket {TicketId}", ticketId);
        throw;
    }
}

public async Task<PullRequestCreationResult> CreatePullRequestAsync(Guid ticketId, string? approvedBy = null)
{
    _logger.LogInformation("Creating pull request for ticket {TicketId}, approved by {ApprovedBy}",
        ticketId, approvedBy ?? "unknown");

    try
    {
        // Get ticket and validate state
        var ticket = await _ticketRepo.GetByIdAsync(ticketId);
        if (ticket == null)
        {
            return PullRequestCreationResult.Failed($"Ticket {ticketId} not found");
        }

        if (ticket.State != WorkflowState.Implementing)
        {
            return PullRequestCreationResult.Failed($"Ticket is not in Implementing state (current: {ticket.State})");
        }

        // Get repository path and push branch
        var repoPath = _workspaceService.GetRepositoryPath(ticketId);
        var branchName = $"feature/{ticket.Key}";

        await _localGitService.PushAsync(repoPath, branchName, ticket.Repository.AccessToken);

        // Build PR description with plan artifacts (Phase 5 implementation)
        var prDescription = await BuildPRDescriptionAsync(ticket);

        // Create PR via platform provider
        var createPrRequest = new CreatePullRequestRequest
        {
            SourceBranch = branchName,
            TargetBranch = ticket.Repository.DefaultBranch ?? "main",
            Title = $"{ticket.Key}: {ticket.Title}",
            Description = prDescription
        };

        var pr = await _gitPlatformProvider.CreatePullRequestAsync(ticket.Repository.Id, createPrRequest);

        // Update ticket state
        ticket.MarkPRCreated(pr.Number, pr.Url);
        await _ticketRepo.UpdateAsync(ticket);

        // Clean up diff file (no longer needed)
        await _workspaceService.DeleteDiffAsync(ticketId);

        _logger.LogInformation("Pull request created for ticket {TicketId}: {PrUrl}", ticketId, pr.Url);

        return PullRequestCreationResult.Successful(pr.Url, pr.Number);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error creating pull request for ticket {TicketId}", ticketId);
        return PullRequestCreationResult.Failed($"Error creating PR: {ex.Message}");
    }
}

private async Task<string> BuildPRDescriptionAsync(Ticket ticket)
{
    // Placeholder - full implementation in Phase 5
    return $@"
## Ticket: {ticket.Key}

{ticket.Description}

---

Generated by PRFactory
";
}
```

### Task 2.4: Extend Web Service Facade

**File**: `/src/PRFactory.Web/Services/ITicketService.cs` (existing interface)

**Add methods**:

```csharp
/// <summary>
/// Gets diff content for a ticket.
/// </summary>
Task<DiffContentDto?> GetDiffContentAsync(Guid ticketId);

/// <summary>
/// Creates a pull request for an approved ticket.
/// </summary>
Task<CreatePRResponse> CreatePullRequestAsync(Guid ticketId, string? approvedBy = null);
```

**File**: `/src/PRFactory.Web/Services/TicketService.cs` (existing implementation)

**Add dependencies and implement methods**:

```csharp
public async Task<DiffContentDto?> GetDiffContentAsync(Guid ticketId)
{
    var diffContent = await _ticketApplicationService.GetDiffContentAsync(ticketId);

    if (diffContent == null)
        return null;

    // Parse file count from diff (simple heuristic)
    var filesChanged = diffContent.Split("diff --git").Length - 1;

    return new DiffContentDto
    {
        TicketId = ticketId,
        DiffContent = diffContent,
        SizeBytes = diffContent.Length,
        FilesChanged = filesChanged,
        Available = true
    };
}

public async Task<CreatePRResponse> CreatePullRequestAsync(Guid ticketId, string? approvedBy = null)
{
    var result = await _ticketApplicationService.CreatePullRequestAsync(ticketId, approvedBy);

    return new CreatePRResponse
    {
        Success = result.Success,
        PullRequestUrl = result.PullRequestUrl ?? string.Empty,
        PullRequestNumber = result.PullRequestNumber ?? 0,
        ErrorMessage = result.ErrorMessage
    };
}
```

## Acceptance Criteria

- [ ] DTOs created (`DiffContentDto`, `CreatePRResponse`)
- [ ] Domain result object created (`PullRequestCreationResult`)
- [ ] Application service methods implemented
- [ ] Web service facade methods implemented
- [ ] All methods have XML documentation
- [ ] Error handling and logging in place
- [ ] NO HTTP calls (direct service injection)

## Testing

**Unit Tests**: `/tests/PRFactory.Infrastructure.Tests/Application/TicketApplicationServiceTests.cs`

```csharp
[Fact]
public async Task GetDiffContentAsync_ReturnsNull_WhenDiffDoesNotExist()
{
    // Arrange
    var ticketId = Guid.NewGuid();
    _workspaceServiceMock.Setup(x => x.ReadDiffAsync(ticketId))
        .ReturnsAsync((string?)null);

    // Act
    var result = await _service.GetDiffContentAsync(ticketId);

    // Assert
    Assert.Null(result);
}

[Fact]
public async Task GetDiffContentAsync_ReturnsDiff_WhenExists()
{
    // Arrange
    var ticketId = Guid.NewGuid();
    var diffContent = "diff --git a/file.txt b/file.txt\n...";
    _workspaceServiceMock.Setup(x => x.ReadDiffAsync(ticketId))
        .ReturnsAsync(diffContent);

    // Act
    var result = await _service.GetDiffContentAsync(ticketId);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(diffContent, result);
}
```

## Next Steps

After Phase 2:
- **Phase 3**: DiffPlex integration (can develop in parallel)
- **Phase 4**: Blazor UI components (depends on Phase 2 + 3)
