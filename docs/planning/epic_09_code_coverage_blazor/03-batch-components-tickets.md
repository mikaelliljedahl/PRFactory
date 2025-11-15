# Batch 3: Business Components - Tickets

## Summary
- **Total Components**: 11 (2 already tested)
- **Estimated Complexity**: Medium-Complex
- **Estimated Time**: 6-8 hours
- **Priority**: HIGH
- **Coverage Contribution**: +7% (9/125 components)

## Overview

This batch focuses on ticket-related business components that encapsulate core workflow logic. These components have service dependencies and complex interactions. This is a **HIGH PRIORITY** batch because tickets are the core domain of PRFactory.

## Components to Test

### Already Tested ✅

#### PlanReviewSection ✅
- Existing test: `/tests/PRFactory.Web.Tests/Components/Tickets/PlanReviewSectionTests.cs`

#### ReviewCommentThread ✅
- Existing test: `/tests/PRFactory.Web.Tests/Components/Tickets/ReviewCommentThreadTests.cs`

---

### Components to Test

#### PlanReviewStatus
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/Tickets/PlanReviewStatus.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/Tickets/PlanReviewStatusTests.cs`
- **Complexity**: Simple
- **Priority**: HIGH
- **Dependencies**: StatusBadge component
- **Code-Behind**: Yes (`PlanReviewStatus.razor.cs`)
- **Test Scenarios**:
  1. Renders status badge for plan state
  2. Shows appropriate status text
  3. Applies correct badge variant based on state
  4. Shows reviewer information when available
  5. Displays review timestamp
- **Mocking Requirements**: None (uses plan DTO model)

#### QuestionAnswerForm
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/Tickets/QuestionAnswerForm.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/Tickets/QuestionAnswerFormTests.cs`
- **Complexity**: Complex
- **Priority**: HIGH
- **Dependencies**: ITicketService, FormTextAreaField
- **Code-Behind**: Inline @code block (may need to check)
- **Test Scenarios**:
  1. Renders list of questions
  2. Shows text area for each question
  3. Validates required answers before submit
  4. OnAnswersSubmitted callback invoked with answers
  5. Shows validation errors for empty required answers
  6. Disables submit button while loading
  7. Shows loading state during submission
  8. Handles submission errors gracefully
- **Mocking Requirements**:
  - `ITicketService.SubmitAnswersAsync()`

#### ReviewerAssignment
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/Tickets/ReviewerAssignment.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/Tickets/ReviewerAssignmentTests.cs`
- **Complexity**: Medium
- **Priority**: MEDIUM
- **Dependencies**: ITicketService, ReviewerAvatar
- **Code-Behind**: Yes (`ReviewerAssignment.razor.cs`)
- **Test Scenarios**:
  1. Renders list of potential reviewers
  2. Shows selected reviewer with checkmark
  3. OnReviewerSelected callback invoked when reviewer clicked
  4. Displays reviewer avatars
  5. Shows reviewer names and roles
  6. Handles empty reviewer list
- **Mocking Requirements**:
  - `ITicketService.GetAvailableReviewersAsync()`

#### SuccessCriteriaEditor
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/Tickets/SuccessCriteriaEditor.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/Tickets/SuccessCriteriaEditorTests.cs`
- **Complexity**: Medium
- **Priority**: MEDIUM
- **Dependencies**: FormTextAreaField, ChecklistItemRow
- **Code-Behind**: Yes (`SuccessCriteriaEditor.razor.cs`)
- **Test Scenarios**:
  1. Renders existing success criteria
  2. Allows adding new criterion
  3. Allows removing criterion
  4. Allows editing criterion text
  5. OnCriteriaChanged callback invoked when modified
  6. Validates at least one criterion exists
  7. Shows empty state when no criteria
- **Mocking Requirements**: None (operates on local state)

#### TicketDiffViewer
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/Tickets/TicketDiffViewer.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/Tickets/TicketDiffViewerTests.cs`
- **Complexity**: Medium
- **Priority**: MEDIUM
- **Dependencies**: None
- **Code-Behind**: Yes (`TicketDiffViewer.razor.cs`)
- **Test Scenarios**:
  1. Renders side-by-side diff view
  2. Highlights additions in green
  3. Highlights deletions in red
  4. Highlights modifications in yellow
  5. Shows line numbers
  6. Handles empty/null diff gracefully
  7. Collapses unchanged sections
- **Mocking Requirements**: None (uses diff DTO model)

#### TicketHeader
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/Tickets/TicketHeader.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/Tickets/TicketHeaderTests.cs`
- **Complexity**: Simple
- **Priority**: HIGH
- **Dependencies**: StatusBadge, ContextualHelp
- **Code-Behind**: Yes (`TicketHeader.razor.cs`)
- **Test Scenarios**:
  1. Renders ticket title and key
  2. Shows workflow state badge
  3. Displays source badge (Jira, GitHub, etc.)
  4. Shows ticket description with formatting
  5. Displays ticket metadata (created, updated, completed)
  6. Shows repository name when available
  7. Shows contextual help for workflow state
  8. Handles missing description gracefully
- **Mocking Requirements**: None (uses ticket DTO model)

#### TicketUpdateEditor
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/Tickets/TicketUpdateEditor.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/Tickets/TicketUpdateEditorTests.cs`
- **Complexity**: Complex
- **Priority**: HIGH
- **Dependencies**: ITicketService, MarkdownEditor, FormTextField
- **Code-Behind**: Yes (`TicketUpdateEditor.razor.cs`)
- **Test Scenarios**:
  1. Renders editor with current ticket data
  2. Shows fields for title, description, success criteria
  3. Allows editing all fields
  4. OnSaveClick callback invoked with updated data
  5. OnCancelClick callback invoked when cancelled
  6. Validates required fields
  7. Shows validation errors
  8. Disables save button while saving
  9. Shows loading state during save
- **Mocking Requirements**:
  - `ITicketService.UpdateTicketAsync()`

#### TicketUpdatePreview
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/Tickets/TicketUpdatePreview.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/Tickets/TicketUpdatePreviewTests.cs`
- **Complexity**: Complex
- **Priority**: HIGH
- **Dependencies**: ITicketService, TicketDiffViewer, LoadingButton
- **Code-Behind**: Yes (`TicketUpdatePreview.razor.cs`)
- **Test Scenarios**:
  1. Loads latest ticket update on initialization
  2. Shows diff between original and updated ticket
  3. Displays update metadata (created, updated by)
  4. Shows approve button when update pending approval
  5. Shows reject button with regenerate option
  6. OnUpdateApproved callback invoked when approved
  7. OnUpdateRejected callback invoked when rejected
  8. Shows loading state while fetching update
  9. Shows error state when update load fails
  10. Handles no update available gracefully
- **Mocking Requirements**:
  - `ITicketService.GetLatestTicketUpdateAsync()`
  - `ITicketService.ApproveUpdateAsync()`
  - `ITicketService.RejectUpdateAsync()`

#### WorkflowTimeline
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/Tickets/WorkflowTimeline.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/Tickets/WorkflowTimelineTests.cs`
- **Complexity**: Medium
- **Priority**: MEDIUM
- **Dependencies**: EventTimeline, RelativeTime
- **Code-Behind**: Yes (`WorkflowTimeline.razor.cs`)
- **Test Scenarios**:
  1. Renders timeline with workflow events
  2. Shows events in chronological order
  3. Displays event icons based on type
  4. Shows event timestamps using RelativeTime
  5. Highlights current workflow state
  6. Shows empty state when no events
  7. Groups events by date
- **Mocking Requirements**: None (uses event DTO models)

---

## Testing Priority Order

### Phase 1 (Critical Core Functionality, ~3 hours)
1. **TicketHeader** - Displays ticket information (HIGH)
2. **TicketUpdatePreview** - Approve/reject updates (HIGH)
3. **QuestionAnswerForm** - User interaction for questions (HIGH)

### Phase 2 (Important Workflow Features, ~2 hours)
4. **WorkflowTimeline** - Shows workflow progress (MEDIUM)
5. **TicketDiffViewer** - Shows changes (MEDIUM)
6. **PlanReviewStatus** - Shows plan review state (HIGH)

### Phase 3 (Supporting Features, ~2 hours)
7. **TicketUpdateEditor** - Edit tickets (HIGH)
8. **SuccessCriteriaEditor** - Edit success criteria (MEDIUM)
9. **ReviewerAssignment** - Assign reviewers (MEDIUM)

---

## Expected Test File Template

```csharp
using Bunit;
using Moq;
using Xunit;
using PRFactory.Web.Components.Tickets;
using PRFactory.Web.Services;
using PRFactory.Web.Models;

namespace PRFactory.Web.Tests.Components.Tickets;

public class TicketHeaderTests : TestContext
{
    private readonly Mock<ITicketService> _mockTicketService;

    public TicketHeaderTests()
    {
        _mockTicketService = new Mock<ITicketService>();
        Services.AddSingleton(_mockTicketService.Object);
    }

    [Fact]
    public void TicketHeader_WithTicket_RendersTitle()
    {
        // Arrange
        var ticket = new TicketDto
        {
            Id = Guid.NewGuid(),
            TicketKey = "TEST-123",
            Title = "Test Ticket",
            Description = "Test description",
            State = WorkflowState.Draft
        };

        // Act
        var cut = RenderComponent<TicketHeader>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        Assert.Contains("TEST-123", cut.Markup);
        Assert.Contains("Test Ticket", cut.Markup);
    }

    [Fact]
    public void TicketHeader_WithWorkflowState_ShowsCorrectBadge()
    {
        // Arrange
        var ticket = new TicketDto
        {
            Id = Guid.NewGuid(),
            TicketKey = "TEST-123",
            Title = "Test",
            State = WorkflowState.InReview
        };

        // Act
        var cut = RenderComponent<TicketHeader>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        // Verify StatusBadge component is rendered with correct state
        var statusBadge = cut.FindComponent<StatusBadge>();
        Assert.NotNull(statusBadge);
    }
}
```

## Success Criteria

- ✅ All 11 components have test files (9 new + 2 existing)
- ✅ Each component has 5-10 test scenarios covered
- ✅ Services properly mocked with Moq
- ✅ Tests use xUnit Assert (not FluentAssertions)
- ✅ Tests use bUnit RenderComponent pattern
- ✅ All tests pass: `dotnet test`
- ✅ Code compiles: `dotnet build`
- ✅ Format checks pass: `dotnet format --verify-no-changes`

## Notes

- These components have business logic and service dependencies
- Use Moq to mock `ITicketService` and other injected services
- Test both success and error paths
- Verify callbacks are invoked correctly
- Test loading states and error handling
- Some components may require SignalR mocking (defer if too complex)
- Focus on core scenarios first, edge cases second
