# Phase 1: Workspace & Diff Generation (Backend)

**Status**: Not Started
**Estimated Effort**: 4-6 hours
**Dependencies**: None
**Risk Level**: Low

## Objective

Create workspace service abstraction and enhance implementation agent to generate git diffs after code generation.

## Architecture Overview

```
ImplementationAgent
  → Executes ICliAgent (Claude Code, builtin, or external CLI)
  → ILocalGitService.GetDiffAsync(repoPath)
  → IWorkspaceService.WriteDiffAsync(ticketId, diffContent)
  → Ticket state → Implementing (diff ready for review)
```

## Tasks

### Task 1.1: Create `IWorkspaceService` Interface

**File**: `/src/PRFactory.Core/Application/Services/IWorkspaceService.cs`

```csharp
namespace PRFactory.Core.Application.Services;

/// <summary>
/// Service for managing workspace directories and files for tickets.
/// Centralizes workspace path logic and file operations.
/// </summary>
public interface IWorkspaceService
{
    /// <summary>
    /// Gets the root workspace directory for a ticket.
    /// Example: /var/prfactory/workspace/{guid}/
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    /// <returns>Absolute path to workspace directory</returns>
    string GetWorkspaceDirectory(Guid ticketId);

    /// <summary>
    /// Gets the repository path within the workspace.
    /// Example: /var/prfactory/workspace/{guid}/repo/
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    /// <returns>Absolute path to repository directory</returns>
    string GetRepositoryPath(Guid ticketId);

    /// <summary>
    /// Gets the diff.patch file path for a ticket.
    /// Example: /var/prfactory/workspace/{guid}/diff.patch
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    /// <returns>Absolute path to diff.patch file</returns>
    string GetDiffPath(Guid ticketId);

    /// <summary>
    /// Reads the diff content for a ticket if it exists.
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    /// <returns>Diff content, or null if file doesn't exist</returns>
    Task<string?> ReadDiffAsync(Guid ticketId);

    /// <summary>
    /// Writes diff content to the workspace.
    /// Creates the workspace directory if it doesn't exist.
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    /// <param name="diffContent">The diff content to write</param>
    Task WriteDiffAsync(Guid ticketId, string diffContent);

    /// <summary>
    /// Checks if a diff file exists for a ticket.
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    /// <returns>True if diff.patch exists</returns>
    Task<bool> DiffExistsAsync(Guid ticketId);

    /// <summary>
    /// Deletes the diff file for a ticket (e.g., after PR created).
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    Task DeleteDiffAsync(Guid ticketId);

    /// <summary>
    /// Creates workspace directory for a ticket if it doesn't exist.
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    /// <returns>Workspace directory path</returns>
    Task<string> EnsureWorkspaceExistsAsync(Guid ticketId);
}
```

**Key Design Decisions**:
- Returns absolute paths (easier to debug, explicit)
- Async methods for I/O operations (file reads/writes)
- Centralizes workspace logic (currently scattered in agents)
- Nullable return for `ReadDiffAsync` (diff may not exist yet)

### Task 1.2: Implement `WorkspaceService`

**File**: `/src/PRFactory.Infrastructure/Workspace/WorkspaceService.cs`

```csharp
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;

namespace PRFactory.Infrastructure.Workspace;

/// <summary>
/// Implementation of workspace service for managing ticket workspaces.
/// </summary>
public class WorkspaceService : IWorkspaceService
{
    private readonly ILogger<WorkspaceService> _logger;
    private readonly string _workspaceBasePath;

    public WorkspaceService(
        IConfiguration configuration,
        ILogger<WorkspaceService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Read from configuration, default to /var/prfactory/workspace
        _workspaceBasePath = configuration["Workspace:BasePath"] ?? "/var/prfactory/workspace";

        _logger.LogInformation("WorkspaceService initialized with base path: {BasePath}", _workspaceBasePath);
    }

    public string GetWorkspaceDirectory(Guid ticketId)
    {
        if (ticketId == Guid.Empty)
            throw new ArgumentException("Ticket ID cannot be empty", nameof(ticketId));

        // Use ticket ID as subdirectory (consistent, predictable)
        return Path.Combine(_workspaceBasePath, ticketId.ToString());
    }

    public string GetRepositoryPath(Guid ticketId)
    {
        var workspaceDir = GetWorkspaceDirectory(ticketId);
        return Path.Combine(workspaceDir, "repo");
    }

    public string GetDiffPath(Guid ticketId)
    {
        var workspaceDir = GetWorkspaceDirectory(ticketId);
        return Path.Combine(workspaceDir, "diff.patch");
    }

    public async Task<string?> ReadDiffAsync(Guid ticketId)
    {
        var diffPath = GetDiffPath(ticketId);

        if (!File.Exists(diffPath))
        {
            _logger.LogDebug("Diff file not found for ticket {TicketId} at {DiffPath}", ticketId, diffPath);
            return null;
        }

        _logger.LogDebug("Reading diff file for ticket {TicketId} from {DiffPath}", ticketId, diffPath);

        try
        {
            return await File.ReadAllTextAsync(diffPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading diff file for ticket {TicketId} from {DiffPath}", ticketId, diffPath);
            throw new InvalidOperationException($"Failed to read diff file for ticket {ticketId}", ex);
        }
    }

    public async Task WriteDiffAsync(Guid ticketId, string diffContent)
    {
        if (string.IsNullOrEmpty(diffContent))
        {
            _logger.LogWarning("Attempted to write empty diff for ticket {TicketId}", ticketId);
            throw new ArgumentException("Diff content cannot be empty", nameof(diffContent));
        }

        var diffPath = GetDiffPath(ticketId);
        var workspaceDir = Path.GetDirectoryName(diffPath)!;

        // Ensure workspace directory exists
        if (!Directory.Exists(workspaceDir))
        {
            _logger.LogInformation("Creating workspace directory: {WorkspaceDir}", workspaceDir);
            Directory.CreateDirectory(workspaceDir);
        }

        _logger.LogInformation("Writing diff file for ticket {TicketId} to {DiffPath} ({Size} bytes)",
            ticketId, diffPath, diffContent.Length);

        try
        {
            await File.WriteAllTextAsync(diffPath, diffContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing diff file for ticket {TicketId} to {DiffPath}", ticketId, diffPath);
            throw new InvalidOperationException($"Failed to write diff file for ticket {ticketId}", ex);
        }
    }

    public Task<bool> DiffExistsAsync(Guid ticketId)
    {
        var diffPath = GetDiffPath(ticketId);
        return Task.FromResult(File.Exists(diffPath));
    }

    public Task DeleteDiffAsync(Guid ticketId)
    {
        var diffPath = GetDiffPath(ticketId);

        if (File.Exists(diffPath))
        {
            _logger.LogInformation("Deleting diff file for ticket {TicketId} at {DiffPath}", ticketId, diffPath);
            File.Delete(diffPath);
        }
        else
        {
            _logger.LogDebug("Diff file not found for deletion: {DiffPath}", diffPath);
        }

        return Task.CompletedTask;
    }

    public Task<string> EnsureWorkspaceExistsAsync(Guid ticketId)
    {
        var workspaceDir = GetWorkspaceDirectory(ticketId);

        if (!Directory.Exists(workspaceDir))
        {
            _logger.LogInformation("Creating workspace directory: {WorkspaceDir}", workspaceDir);
            Directory.CreateDirectory(workspaceDir);
        }

        return Task.FromResult(workspaceDir);
    }
}
```

**Key Implementation Details**:
- Workspace path: `{BasePath}/{TicketId}/` (e.g., `/var/prfactory/workspace/a1b2c3.../`)
- Repository path: `{WorkspaceDir}/repo/`
- Diff path: `{WorkspaceDir}/diff.patch`
- Creates directories on demand (no manual setup needed)
- Comprehensive logging for debugging
- Validates inputs (non-empty ticket IDs, non-empty diff content)

### Task 1.3: Register `IWorkspaceService` in DI Container

**File**: `/src/PRFactory.Infrastructure/DependencyInjection.cs`

**Add to existing registration method**:

```csharp
using PRFactory.Infrastructure.Workspace;

// In AddInfrastructure() method:
services.AddScoped<IWorkspaceService, WorkspaceService>();
```

**Location**: Around line 80-100 (after other service registrations)

### Task 1.4: Enhance `ImplementationAgent` to Generate Diffs

**File**: `/src/PRFactory.Infrastructure/Agents/ImplementationAgent.cs` (existing file)

**Required Changes**:

1. **Add dependencies** (constructor injection):
```csharp
private readonly ILocalGitService _localGitService;
private readonly IWorkspaceService _workspaceService;

public ImplementationAgent(
    ITicketRepository ticketRepo,
    ICheckpointRepository checkpointRepo,
    ILogger<ImplementationAgent> logger,
    ICliAgent cliAgent,
    ILocalGitService localGitService,
    IWorkspaceService workspaceService)
{
    // ... existing constructor code ...
    _localGitService = localGitService ?? throw new ArgumentNullException(nameof(localGitService));
    _workspaceService = workspaceService ?? throw new ArgumentNullException(nameof(workspaceService));
}
```

2. **Add diff generation after code implementation**:
```csharp
public async Task<AgentResult> ExecuteAsync(AgentContext context)
{
    // ... existing implementation logic ...

    // After code is generated by ICliAgent
    var ticket = await _ticketRepo.GetByIdAsync(context.TicketId);

    // Generate diff using LibGit2Sharp
    var repoPath = _workspaceService.GetRepositoryPath(ticket.Id);
    _logger.LogInformation("Generating diff for ticket {TicketId} from repository {RepoPath}",
        ticket.Id, repoPath);

    try
    {
        // Get diff from HEAD (includes uncommitted changes + last commit)
        var diffContent = await _localGitService.GetDiffAsync(
            repoPath: repoPath,
            filePath: null,  // All files
            baseBranch: "main",  // Or ticket.Repository.DefaultBranch
            compareBranch: null  // HEAD (current branch)
        );

        if (string.IsNullOrEmpty(diffContent))
        {
            _logger.LogWarning("Generated diff is empty for ticket {TicketId}", ticket.Id);
            // Continue anyway - might be no changes, or issue with diff generation
        }
        else
        {
            _logger.LogInformation("Generated diff for ticket {TicketId}: {Size} bytes",
                ticket.Id, diffContent.Length);

            // Save diff to workspace
            await _workspaceService.WriteDiffAsync(ticket.Id, diffContent);
            _logger.LogInformation("Saved diff to workspace for ticket {TicketId}", ticket.Id);
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error generating diff for ticket {TicketId}", ticket.Id);
        // Don't fail the entire agent - diff generation is secondary to code generation
        // Continue with workflow, user can still create PR without diff preview
    }

    // ... existing checkpoint/completion logic ...

    return new AgentResult
    {
        Success = true,
        NextAgent = "HumanWaitAgent",  // Wait for user approval in UI
        Metadata = new Dictionary<string, object>
        {
            { "diff_generated", !string.IsNullOrEmpty(diffContent) },
            { "diff_size_bytes", diffContent?.Length ?? 0 }
        }
    };
}
```

**Key Implementation Notes**:
- Diff generated AFTER code implementation (agent or CLI finishes)
- Uses `baseBranch: "main"` (or read from repository configuration)
- `compareBranch: null` means HEAD (current branch with uncommitted changes)
- Error handling: don't fail workflow if diff generation fails (log and continue)
- Metadata tracks diff generation success for debugging

### Task 1.5: Update Configuration (appsettings.json)

**File**: `/src/PRFactory.Web/appsettings.json`

**Add workspace configuration** (if not already present):

```json
{
  "Workspace": {
    "BasePath": "/var/prfactory/workspace"
  }
}
```

**For local development** (appsettings.Development.json):

```json
{
  "Workspace": {
    "BasePath": "C:\\Temp\\prfactory-workspace"  // Windows
    // OR
    "BasePath": "/tmp/prfactory-workspace"       // Linux/Mac
  }
}
```

## Acceptance Criteria

### Functional Requirements

- [ ] `IWorkspaceService` interface created with all required methods
- [ ] `WorkspaceService` implementation handles file I/O correctly
- [ ] `WorkspaceService` registered in DI container
- [ ] `ImplementationAgent` generates diffs after code implementation
- [ ] Diff content saved to `{workspace}/{ticketId}/diff.patch`
- [ ] Diff generation errors logged but don't fail workflow
- [ ] Configuration supports custom workspace base paths

### Non-Functional Requirements

- [ ] All public methods have XML documentation comments
- [ ] Logging at appropriate levels (Debug, Info, Warning, Error)
- [ ] Input validation (non-empty GUIDs, non-null arguments)
- [ ] Error handling with descriptive exception messages
- [ ] Workspace directories created automatically on demand

## Testing Strategy

### Unit Tests

**File**: `/tests/PRFactory.Infrastructure.Tests/Workspace/WorkspaceServiceTests.cs`

```csharp
public class WorkspaceServiceTests
{
    [Fact]
    public void GetWorkspaceDirectory_ReturnsCorrectPath()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var config = CreateConfiguration("/base");
        var logger = Mock.Of<ILogger<WorkspaceService>>();
        var service = new WorkspaceService(config, logger);

        // Act
        var path = service.GetWorkspaceDirectory(ticketId);

        // Assert
        Assert.Equal($"/base/{ticketId}", path);
    }

    [Fact]
    public async Task ReadDiffAsync_ReturnsNull_WhenFileDoesNotExist()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var config = CreateConfiguration("/nonexistent");
        var logger = Mock.Of<ILogger<WorkspaceService>>();
        var service = new WorkspaceService(config, logger);

        // Act
        var content = await service.ReadDiffAsync(ticketId);

        // Assert
        Assert.Null(content);
    }

    [Fact]
    public async Task WriteDiffAsync_CreatesDiffFile()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var config = CreateConfiguration(tempDir);
        var logger = Mock.Of<ILogger<WorkspaceService>>();
        var service = new WorkspaceService(config, logger);
        var diffContent = "diff --git a/file.txt b/file.txt\n...";

        try
        {
            // Act
            await service.WriteDiffAsync(ticketId, diffContent);

            // Assert
            var diffPath = service.GetDiffPath(ticketId);
            Assert.True(File.Exists(diffPath));

            var readContent = await File.ReadAllTextAsync(diffPath);
            Assert.Equal(diffContent, readContent);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }
}
```

### Integration Tests

**File**: `/tests/PRFactory.Infrastructure.Tests/Agents/ImplementationAgentIntegrationTests.cs`

```csharp
[Fact]
public async Task ExecuteAsync_GeneratesDiff_AfterCodeImplementation()
{
    // Arrange
    var ticket = CreateTestTicket();
    var context = new AgentContext { TicketId = ticket.Id };

    // Mock CLI agent to simulate code generation
    var cliAgentMock = new Mock<ICliAgent>();
    cliAgentMock.Setup(x => x.ExecuteWithProjectContextAsync(
        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(new CliAgentResponse { Success = true });

    // Setup real services (use temp directories)
    var workspaceService = CreateWorkspaceService(useTempDirectory: true);
    var localGitService = CreateLocalGitService();

    var agent = new ImplementationAgent(
        _ticketRepo, _checkpointRepo, _logger, cliAgentMock.Object,
        localGitService, workspaceService);

    // Act
    var result = await agent.ExecuteAsync(context);

    // Assert
    Assert.True(result.Success);

    // Verify diff was generated
    var diffExists = await workspaceService.DiffExistsAsync(ticket.Id);
    Assert.True(diffExists);

    var diffContent = await workspaceService.ReadDiffAsync(ticket.Id);
    Assert.NotNull(diffContent);
    Assert.NotEmpty(diffContent);
}
```

## Risks & Mitigations

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| Workspace path conflicts (multi-tenant) | High | Low | Use ticket GUID as directory name (unique) |
| Large diff files (memory) | Medium | Medium | Stream large files in Phase 3 (DiffRenderService) |
| Diff generation fails | Low | Low | Error handling - continue workflow, log warning |
| File system permissions | Medium | Low | Document required permissions, validate on startup |

## Dependencies

- ✅ `ILocalGitService` already exists (LibGit2Sharp wrapper)
- ✅ `ICliAgent` already exists (Epic 05)
- ✅ `ImplementationAgent` already exists
- ✅ `IConfiguration` for workspace base path

## Next Steps

After Phase 1 completion:
1. **Phase 2**: Create service layer and DTOs for diff retrieval
2. **Phase 3**: Implement DiffPlex rendering (can start in parallel)
3. Merge Phase 1 PR and update IMPLEMENTATION_STATUS.md
