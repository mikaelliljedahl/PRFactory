# Epic 09: Blazor Component Test Coverage - Overview

## Summary

**Goal**: Achieve 80% test coverage for PRFactory.Web Blazor components using bUnit.

## ✅ CURRENT STATUS (As of 2025-01-15)

### Progress Summary
- **Total Tests**: 747 passing, 10 skipped, 0 failing ✅
- **Pass Rate**: 100% (of runnable tests)
- **Components Tested**: 47 out of 125 (~38% coverage)
- **Target Coverage**: 80% (100 components)
- **Remaining Needed**: ~53 more components

### What's Completed

✅ **Batch 1 - UI Simple Components** (22 test files, ~270 tests)
- Forms: FormTextField, FormPasswordField, FormTextArea, FormSelect, FormCheckbox, FormCodeEditor
- Buttons: LoadingButton, IconButton
- Display: EmptyState, ErrorCard, LoadingSpinner, ProgressBar, StatusBadge, RelativeTime, ReviewerAvatar, StackTraceViewer, EventTimeline
- Alerts: AlertMessage, DemoModeBanner
- Cards: Card
- Help: ContextualHelp
- Navigation: Breadcrumbs

✅ **Batch 2 - UI Complex Components** (9 test files, 85 tests)
- Checklists: ChecklistItemRow, ReviewChecklistPanel
- Comments: CommentAnchorIndicator, InlineCommentPanel
- Dialogs: Modal
- Editors: MarkdownPreview, MarkdownToolbar
- Notifications: Toast, ToastContainer

✅ **Batch 3 - Ticket Components** (9 test files, 100 tests)
- TicketHeader, PlanReviewStatus, TicketDiffViewer, SuccessCriteriaEditor
- WorkflowTimeline, QuestionAnswerForm (10 tests skipped), TicketUpdateEditor
- TicketUpdatePreview, ReviewerAssignment

✅ **Batch 4 - Partial** (7 test files, 96 tests)
- Repositories: RepositoryListItem, BranchSelector, RepositoryConnectionTest, RepositoryForm, RepositoryStatistics
- Tenants: TenantListItem, TenantForm

### ⏭️ NEXT SESSION: Complete Batch 4 to Reach 80%

**Remaining components needed** (~32 components for 80% target):

1. **TenantConfigEditor** (1 component)
2. **Settings Components** (13 components):
   - LlmProviderListItem, LlmProviderStatistics
   - ApiKeyProviderForm, OAuthProviderForm, ProviderTypeSelector
   - ModelOverridesEditor, AllowedRepositoriesEditor
   - LlmProviderAssignmentPanel, CodeReviewSettingsPanel
   - TenantInfoPanel, WorkflowSettingsPanel
   - UserListItem, UserRoleEditor, UserStatistics
3. **Workflows Components** (2 components):
   - EventLogFilter, EventDetail
4. **Errors Components** (3 components):
   - ErrorListFilter, ErrorResolutionForm, ErrorDetail
5. **Auth Components** (2 components):
   - UserProfileDropdown, RedirectToLogin
6. **Agent Prompts Components** (4 components):
   - PromptTemplateListItem, PromptTemplateForm, PromptPreview, PromptVariableReference
7. **Misc Components** (3 components):
   - Pagination, TicketFilters, TicketListItem
8. **Layout Components** (2 components):
   - NavMenu, MainLayout

**Estimated effort**: 10-15 hours with parallel agents

### Known Issues
- **QuestionAnswerForm**: 10 tests skipped due to Blazor ValidationMessage limitation with dictionary indexers (component design issue, not test issue)

---

## Initial State (For Reference)

- **Total Blazor Components**: 125 (.razor files)
- **Components with Code-Behind**: 88 (.razor.cs files)
- **Initial Tests**: 8 test files
- **Initial Coverage**: ~6% (8 tests / 125 components)

## Test Coverage Strategy

To achieve 80% coverage efficiently, we prioritize:

1. **Simple UI Components** (Batch 1) - ~30 components - **HIGH PRIORITY**
   - Pure UI with minimal logic
   - Easy to test, high ROI
   - Forms, Buttons, Alerts, Display, Cards

2. **Business Components - Tickets** (Batch 3) - ~9 components - **HIGH PRIORITY**
   - Core business logic
   - Critical user workflows
   - Already have 2 tested

3. **Business Components - Other** (Batch 4) - ~40 components - **MEDIUM PRIORITY**
   - Settings, Repositories, Tenants, Workflows, etc.
   - Important but less critical than tickets

4. **Medium UI Components** (Batch 2) - ~10 components - **MEDIUM PRIORITY**
   - More complex UI (Dialogs, Editors, Checklists)
   - Some state management

5. **Pages** (Batch 5) - ~36 pages - **LOWER PRIORITY**
   - Complex integration tests
   - Can defer to achieve 80% faster

## Test Plan Organization

The test plan is organized into 5 batches that can be implemented in parallel:

### Batch 1: Simple UI Components
- **Components**: ~30
- **Complexity**: Simple
- **Estimated Time**: 6-8 hours
- **Priority**: HIGH
- **Details**: `01-batch-ui-simple.md`

### Batch 2: Complex UI Components
- **Components**: ~10
- **Complexity**: Medium
- **Estimated Time**: 4-6 hours
- **Priority**: MEDIUM
- **Details**: `02-batch-ui-complex.md`

### Batch 3: Business Components - Tickets
- **Components**: ~9 (2 already tested)
- **Complexity**: Medium-Complex
- **Estimated Time**: 6-8 hours
- **Priority**: HIGH
- **Details**: `03-batch-components-tickets.md`

### Batch 4: Business Components - Other
- **Components**: ~40
- **Complexity**: Medium-Complex
- **Estimated Time**: 15-20 hours
- **Priority**: MEDIUM
- **Details**: `04-batch-components-other.md`

### Batch 5: Pages
- **Components**: ~36
- **Complexity**: Complex
- **Estimated Time**: 15-20 hours
- **Priority**: LOWER (optional for 80%)
- **Details**: `05-batch-pages.md`

## Estimated Coverage After Each Batch

| Batch | Components Tested | Cumulative Total | Coverage % |
|-------|-------------------|------------------|------------|
| Current | 8 | 8 | 6% |
| Batch 1 (UI Simple) | 30 | 38 | 30% |
| Batch 2 (UI Complex) | 10 | 48 | 38% |
| Batch 3 (Tickets) | 9 | 57 | 46% |
| Batch 4 (Other) | 40 | 97 | **78%** ✅ |
| Batch 5 (Pages) | 36 | 133 | 106% (over-target) |

**Recommendation**: Execute Batches 1-4 to achieve 78% coverage, approaching the 80% target efficiently.

## Testing Standards

### Testing Framework
- **xUnit** - Primary testing framework (use `Assert` class only)
- **bUnit** - Blazor component testing
- **Moq** - Mocking framework

### Forbidden Libraries
❌ **DO NOT use FluentAssertions** - Use xUnit `Assert` only

### Test File Naming Convention
- Component: `/src/PRFactory.Web/UI/Buttons/LoadingButton.razor`
- Test: `/tests/PRFactory.Web.Tests/UI/Buttons/LoadingButtonTests.cs`

### Test Structure
```csharp
using Bunit;
using Xunit;
using PRFactory.Web.UI.Buttons;

namespace PRFactory.Web.Tests.UI.Buttons;

public class LoadingButtonTests : TestContext
{
    [Fact]
    public void LoadingButton_WithText_RendersCorrectly()
    {
        // Arrange
        var expectedText = "Click Me";

        // Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.Text, expectedText));

        // Assert
        Assert.Contains(expectedText, cut.Markup);
    }
}
```

### Required Test Scenarios

For each component, test:
1. **Rendering** - Component renders without errors
2. **Parameters** - All required parameters work correctly
3. **User Interactions** - Button clicks, form inputs, etc.
4. **Conditional Rendering** - Different UI states (loading, error, empty, etc.)
5. **Event Callbacks** - Event handlers are invoked correctly

### Mocking Services

Use Moq to mock injected services:

```csharp
[Fact]
public void Component_WithServiceDependency_Works()
{
    // Arrange
    var mockService = new Mock<ITicketService>();
    mockService.Setup(s => s.GetTicketDtoByIdAsync(It.IsAny<Guid>()))
        .ReturnsAsync(new TicketDto { Title = "Test" });

    Services.AddSingleton(mockService.Object);

    // Act
    var cut = RenderComponent<TicketHeader>(parameters => parameters
        .Add(p => p.TicketId, Guid.NewGuid()));

    // Assert
    Assert.Contains("Test", cut.Markup);
}
```

## Parallel Execution Strategy

To maximize efficiency, batches can be executed in parallel by different agents:

### Phase 1 (Parallel) - Achieve ~46% Coverage
- **Agent 1**: Batch 1 (UI Simple) - 30 components
- **Agent 2**: Batch 3 (Tickets) - 9 components

### Phase 2 (Parallel) - Achieve ~78% Coverage
- **Agent 1**: Batch 2 (UI Complex) - 10 components
- **Agent 2**: Batch 4a (Repositories, Tenants) - ~15 components
- **Agent 3**: Batch 4b (Settings) - ~15 components
- **Agent 4**: Batch 4c (Workflows, Errors, Auth) - ~10 components

### Phase 3 (Optional) - Exceed 80% Coverage
- Multiple agents tackle Batch 5 (Pages) if needed

## Success Criteria

✅ **Minimum Success**: 80% coverage (100 components tested)
✅ **All tests pass**: `dotnet test` returns 0 failures
✅ **Code compiles**: `dotnet build` succeeds
✅ **Format checks pass**: `dotnet format --verify-no-changes` succeeds
✅ **No FluentAssertions**: Only xUnit `Assert` used
✅ **Proper mocking**: Services mocked with Moq, not HttpClient

## Next Steps

1. Review this overview and batch plans
2. Assign batches to agents or execute sequentially
3. Start with **Batch 1** and **Batch 3** (highest priority)
4. After Batch 1-3, assess coverage and continue with Batch 4
5. Defer Batch 5 (Pages) unless needed to hit 80%

## Files in This Planning Directory

- `00-overview.md` (this file) - Overall summary and strategy
- `01-batch-ui-simple.md` - Simple UI components plan
- `02-batch-ui-complex.md` - Complex UI components plan
- `03-batch-components-tickets.md` - Ticket business components plan
- `04-batch-components-other.md` - Other business components plan
- `05-batch-pages.md` - Page components plan (optional)
