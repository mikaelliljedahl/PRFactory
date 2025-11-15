# Phase 6: Testing & Integration

**Status**: Not Started
**Estimated Effort**: 4-6 hours
**Dependencies**: All previous phases
**Risk Level**: Low

## Objective

Comprehensive testing of the entire diff viewer workflow and integration validation.

## Testing Strategy

### 1. Unit Tests

#### WorkspaceService Tests

**File**: `/tests/PRFactory.Infrastructure.Tests/Workspace/WorkspaceServiceTests.cs`

```csharp
public class WorkspaceServiceTests
{
    [Theory]
    [InlineData("/var/prfactory/workspace")]
    [InlineData("C:\\temp\\prfactory")]
    public void GetWorkspaceDirectory_ReturnsCorrectPath(string basePath)
    {
        var ticketId = Guid.NewGuid();
        var service = CreateService(basePath);

        var path = service.GetWorkspaceDirectory(ticketId);

        Assert.Equal(Path.Combine(basePath, ticketId.ToString()), path);
    }

    [Fact]
    public async Task WriteDiffAsync_CreatesDiffFile()
    {
        var ticketId = Guid.NewGuid();
        var diffContent = "diff --git a/file.txt b/file.txt\n+added line";
        var service = CreateService(useTempDirectory: true);

        await service.WriteDiffAsync(ticketId, diffContent);

        var exists = await service.DiffExistsAsync(ticketId);
        Assert.True(exists);

        var readContent = await service.ReadDiffAsync(ticketId);
        Assert.Equal(diffContent, readContent);
    }

    [Fact]
    public async Task DeleteDiffAsync_RemovesDiffFile()
    {
        var ticketId = Guid.NewGuid();
        var service = CreateService(useTempDirectory: true);

        await service.WriteDiffAsync(ticketId, "test diff");
        await service.DeleteDiffAsync(ticketId);

        var exists = await service.DiffExistsAsync(ticketId);
        Assert.False(exists);
    }
}
```

**Coverage Target**: 90%+

#### DiffRenderService Tests

**File**: `/tests/PRFactory.Infrastructure.Tests/CodeDiff/DiffRenderServiceTests.cs`

```csharp
public class DiffRenderServiceTests
{
    [Fact]
    public void RenderDiffAsHtml_EmptyPatch_ReturnsMessage()
    {
        var service = CreateService();

        var html = service.RenderDiffAsHtml(string.Empty);

        Assert.Contains("No changes to display", html);
    }

    [Fact]
    public void RenderDiffAsHtml_SimplePatch_RendersCorrectly()
    {
        var service = CreateService();
        var patch = @"diff --git a/test.txt b/test.txt
--- a/test.txt
+++ b/test.txt
@@ -1,3 +1,4 @@
 line 1
+added line
 line 2";

        var html = service.RenderDiffAsHtml(patch, DiffViewMode.Unified);

        Assert.Contains("test.txt", html);
        Assert.Contains("added line", html);
        Assert.Contains("diff-line-added", html);
    }

    [Fact]
    public void ParseFileChanges_CountsCorrectly()
    {
        var service = CreateService();
        var patch = @"diff --git a/file1.txt b/file1.txt
+++ b/file1.txt
@@ -1 +1,2 @@
 line 1
+line 2
diff --git a/file2.txt b/file2.txt
--- a/file2.txt
@@ -1,2 +1 @@
 line 1
-line 2";

        var files = service.ParseFileChanges(patch);

        Assert.Equal(2, files.Count);
        Assert.Equal(1, files[0].LinesAdded);
        Assert.Equal(0, files[0].LinesDeleted);
        Assert.Equal(0, files[1].LinesAdded);
        Assert.Equal(1, files[1].LinesDeleted);
    }
}
```

**Coverage Target**: 85%+

#### TicketApplicationService Tests

**File**: `/tests/PRFactory.Infrastructure.Tests/Application/TicketApplicationServiceTests.cs`

```csharp
public class TicketApplicationServiceTests
{
    [Fact]
    public async Task GetDiffContentAsync_ReturnsNull_WhenNoDiff()
    {
        var ticketId = Guid.NewGuid();
        _workspaceServiceMock.Setup(x => x.ReadDiffAsync(ticketId))
            .ReturnsAsync((string?)null);

        var result = await _service.GetDiffContentAsync(ticketId);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreatePullRequestAsync_FailsForInvalidState()
    {
        var ticket = CreateTicket(state: WorkflowState.Analyzing);
        _ticketRepoMock.Setup(x => x.GetByIdAsync(ticket.Id))
            .ReturnsAsync(ticket);

        var result = await _service.CreatePullRequestAsync(ticket.Id);

        Assert.False(result.Success);
        Assert.Contains("not in Implementing state", result.ErrorMessage);
    }

    [Fact]
    public async Task CreatePullRequestAsync_CreatesSuccessfully()
    {
        var ticket = CreateTicket(state: WorkflowState.Implementing);
        SetupMocksForSuccessfulPRCreation(ticket);

        var result = await _service.CreatePullRequestAsync(ticket.Id, "test-user");

        Assert.True(result.Success);
        Assert.NotNull(result.PullRequestUrl);
        Assert.True(result.PullRequestNumber > 0);

        // Verify state transition
        _ticketRepoMock.Verify(x => x.UpdateAsync(
            It.Is<Ticket>(t => t.State == WorkflowState.PRCreated)), Times.Once);
    }
}
```

**Coverage Target**: 80%+

### 2. Integration Tests

#### End-to-End Workflow Test

**File**: `/tests/PRFactory.Integration.Tests/DiffViewerWorkflowTests.cs`

```csharp
public class DiffViewerWorkflowTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task E2E_DiffViewer_Workflow()
    {
        // Arrange: Create ticket and workspace with diff
        var ticket = await CreateTestTicketAsync(WorkflowState.Implementing);
        await SeedDiffFileAsync(ticket.Id);

        // Act 1: Load ticket detail page
        var response = await _client.GetAsync($"/tickets/{ticket.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();

        // Assert: Diff viewer present
        Assert.Contains("diff-viewer", html);
        Assert.Contains("Code Changes", html);

        // Act 2: Approve and create PR
        var prResponse = await _ticketService.CreatePullRequestAsync(ticket.Id, "test-user");

        // Assert: PR created successfully
        Assert.True(prResponse.Success);
        Assert.NotEmpty(prResponse.PullRequestUrl);

        // Assert: Ticket state updated
        var updatedTicket = await _ticketRepo.GetByIdAsync(ticket.Id);
        Assert.Equal(WorkflowState.PRCreated, updatedTicket.State);

        // Assert: Diff file cleaned up
        var diffExists = await _workspaceService.DiffExistsAsync(ticket.Id);
        Assert.False(diffExists);
    }
}
```

### 3. Component Tests (Blazor)

#### GitDiffViewer Component Tests

**File**: `/tests/PRFactory.Web.Tests/Components/GitDiffViewerTests.cs`

```csharp
using Bunit;

public class GitDiffViewerTests : TestContext
{
    [Fact]
    public void GitDiffViewer_RendersEmptyState()
    {
        // Arrange
        var diffRendererMock = new Mock<IDiffRenderService>();
        Services.AddSingleton(diffRendererMock.Object);

        // Act
        var component = RenderComponent<GitDiffViewer>(parameters => parameters
            .Add(p => p.DiffContent, string.Empty));

        // Assert
        Assert.Contains("No changes to display", component.Markup);
    }

    [Fact]
    public void GitDiffViewer_RendersDiffContent()
    {
        // Arrange
        var diffRendererMock = new Mock<IDiffRenderService>();
        diffRendererMock.Setup(x => x.RenderDiffAsHtml(It.IsAny<string>(), It.IsAny<DiffViewMode>()))
            .Returns("<div class='diff-output'>test diff</div>");
        diffRendererMock.Setup(x => x.ParseFileChanges(It.IsAny<string>()))
            .Returns(new List<FileChangeInfo>
            {
                new FileChangeInfo
                {
                    FilePath = "test.txt",
                    ChangeType = FileChangeType.Modified,
                    LinesAdded = 5,
                    LinesDeleted = 2
                }
            });

        Services.AddSingleton(diffRendererMock.Object);

        // Act
        var component = RenderComponent<GitDiffViewer>(parameters => parameters
            .Add(p => p.DiffContent, "diff content here"));

        // Assert
        Assert.Contains("test diff", component.Markup);
        Assert.Contains("1 file(s)", component.Markup);
    }

    [Fact]
    public void GitDiffViewer_ViewModeToggle_UpdatesRendering()
    {
        // Arrange
        var diffRendererMock = new Mock<IDiffRenderService>();
        Services.AddSingleton(diffRendererMock.Object);

        var component = RenderComponent<GitDiffViewer>(parameters => parameters
            .Add(p => p.DiffContent, "diff content"));

        // Act: Click Side-by-Side button
        var sideBySideButton = component.Find("button:contains('Side by Side')");
        sideBySideButton.Click();

        // Assert: Render called with SideBySide mode
        diffRendererMock.Verify(x => x.RenderDiffAsHtml(
            It.IsAny<string>(), DiffViewMode.SideBySide), Times.Once);
    }
}
```

### 4. Manual Testing Checklist

#### Functional Testing

- [ ] **Diff Generation**
  - [ ] ImplementationAgent generates diff after code implementation
  - [ ] Diff file saved to correct workspace path
  - [ ] Empty diffs handled gracefully

- [ ] **Diff Viewer UI**
  - [ ] Diff renders correctly in Unified view
  - [ ] Diff renders correctly in Side-by-Side view
  - [ ] View mode toggle works
  - [ ] File stats display correctly (files, +lines, -lines)
  - [ ] Syntax highlighting works (if implemented)
  - [ ] Large diffs (100+ files) render without performance issues

- [ ] **PR Creation**
  - [ ] Approve button triggers PR creation
  - [ ] PR description includes plan artifacts
  - [ ] PR created on correct platform (GitHub/Bitbucket/Azure DevOps)
  - [ ] Ticket state transitions to PRCreated
  - [ ] Diff file deleted after PR creation
  - [ ] Success message displayed with PR link

- [ ] **Error Handling**
  - [ ] Missing diff file shows appropriate message
  - [ ] Git push failures show user-friendly error
  - [ ] Platform API failures show user-friendly error
  - [ ] Invalid state transitions prevented

#### Non-Functional Testing

- [ ] **Performance**
  - [ ] Diff rendering <1 second for typical diffs (10-20 files)
  - [ ] Large diffs (100+ files) render <5 seconds
  - [ ] No UI blocking during PR creation

- [ ] **Security**
  - [ ] Diff content HTML-encoded (no XSS)
  - [ ] Access tokens not exposed in logs
  - [ ] Workspace isolation enforced (multi-tenant)

- [ ] **Accessibility**
  - [ ] Diff viewer keyboard navigable
  - [ ] Color contrast meets WCAG 2.1 AA
  - [ ] Screen reader friendly

### 5. Test Coverage Requirements

| Component | Coverage Target | Current | Status |
|-----------|-----------------|---------|--------|
| WorkspaceService | 90% | TBD | ⚠️ |
| DiffRenderService | 85% | TBD | ⚠️ |
| TicketApplicationService | 80% | TBD | ⚠️ |
| GitDiffViewer Component | 75% | TBD | ⚠️ |
| **Overall Epic 04** | **80%** | **TBD** | **⚠️** |

**Note**: All new code MUST meet 80% coverage minimum per CLAUDE.md.

### 6. Regression Testing

Run full test suite to ensure Epic 04 doesn't break existing functionality:

```bash
dotnet test --configuration Release --verbosity normal
```

Expected: All existing tests pass + new Epic 04 tests pass.

## Integration Validation

### Pre-Deployment Checklist

- [ ] All unit tests pass (>80% coverage)
- [ ] All integration tests pass
- [ ] All component tests pass (bUnit)
- [ ] Manual testing checklist completed
- [ ] No new warnings from `dotnet build`
- [ ] Code formatting passes (`dotnet format --verify-no-changes`)
- [ ] Security scan passes (if configured)
- [ ] Documentation updated (IMPLEMENTATION_STATUS.md, ROADMAP.md)

### Deployment Steps

1. **Merge to main branch**
   ```bash
   git checkout main
   git merge feature/epic-04-diff-viewer
   ```

2. **Run database migrations** (if any new entities/properties):
   ```bash
   dotnet ef database update --project src/PRFactory.Infrastructure
   ```

3. **Deploy to staging**
   - Test end-to-end workflow in staging
   - Verify multi-platform PR creation

4. **Deploy to production**
   - Monitor logs for errors
   - Validate first few PRs created successfully

## Acceptance Criteria (Epic-Level)

- [ ] All 6 phases completed
- [ ] Unit test coverage ≥80%
- [ ] Integration tests pass
- [ ] Manual testing completed
- [ ] No regressions in existing functionality
- [ ] Documentation updated
- [ ] Code review approved
- [ ] Deployed to production successfully

## Post-Deployment Monitoring

### Metrics to Track

- **Diff Generation Success Rate**: % of tickets with successfully generated diffs
- **PR Creation Success Rate**: % of approved diffs that create PRs
- **Error Rate**: Count of errors in logs related to diff viewer
- **Performance**: p50, p95, p99 diff rendering times
- **User Engagement**: % of users who approve code via diff viewer

### Known Limitations (Document for Users)

1. **Side-by-Side View**: MVP uses same rendering as Unified (full alignment algorithm in future)
2. **Syntax Highlighting**: Basic HTML encoding only (future: language-specific highlighting)
3. **File Tree Navigation**: Not implemented in MVP (future enhancement)
4. **Inline Comments**: Not implemented in MVP (future enhancement)

## Next Steps After Epic 04

1. **Update Documentation**
   - IMPLEMENTATION_STATUS.md: Mark Epic 04 complete
   - ROADMAP.md: Add Epic 04 to "Recently Completed"
   - Epic 04 planning docs: Archive to `/docs/archive/`

2. **Gather User Feedback**
   - Monitor usage metrics
   - Collect user feedback on diff viewer UX
   - Identify pain points for future improvements

3. **Plan Enhancements** (Future Epics)
   - Epic 04.1: Advanced Side-by-Side View
   - Epic 04.2: Syntax Highlighting
   - Epic 04.3: File Tree Navigation
   - Epic 04.4: Inline Comment Support
