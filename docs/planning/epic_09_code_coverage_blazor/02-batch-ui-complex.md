# Batch 2: Complex UI Components

## Summary
- **Total Components**: 10 (1 already tested)
- **Estimated Complexity**: Medium
- **Estimated Time**: 4-6 hours
- **Priority**: MEDIUM
- **Coverage Contribution**: +7% (9/125 components)

## Overview

This batch focuses on UI components with more complexity - state management, dialogs, editors, and interactive components. These components may have code-behind files and more sophisticated logic.

## Components to Test

### UI/Checklists (2 components)

#### ChecklistItemRow
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/UI/Checklists/ChecklistItemRow.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/UI/Checklists/ChecklistItemRowTests.cs`
- **Complexity**: Medium
- **Priority**: MEDIUM
- **Dependencies**: None
- **Test Scenarios**:
  1. Renders unchecked item
  2. Renders checked item with checkmark
  3. OnToggle callback invoked when clicked
  4. Disabled state prevents interaction
  5. Shows item text correctly
  6. Applies correct CSS classes for checked/unchecked states
- **Mocking Requirements**: None

#### ReviewChecklistPanel
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/UI/Checklists/ReviewChecklistPanel.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/UI/Checklists/ReviewChecklistPanelTests.cs`
- **Complexity**: Medium
- **Priority**: MEDIUM
- **Dependencies**: ChecklistItemRow component
- **Code-Behind**: Yes (`ReviewChecklistPanel.razor.cs`)
- **Test Scenarios**:
  1. Renders panel with title
  2. Renders list of checklist items
  3. Shows progress bar with percentage completion
  4. Updates when items toggled
  5. All items checked shows 100% completion
  6. Empty checklist shows appropriate state
  7. OnAllItemsCompleted callback invoked when all checked
- **Mocking Requirements**: None

---

### UI/Comments (2 components)

#### CommentAnchorIndicator
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/UI/Comments/CommentAnchorIndicator.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/UI/Comments/CommentAnchorIndicatorTests.cs`
- **Complexity**: Simple
- **Priority**: MEDIUM
- **Dependencies**: None
- **Test Scenarios**:
  1. Renders anchor indicator icon
  2. Shows comment count badge
  3. OnClick callback invoked when clicked
  4. Highlights when active
  5. Position parameter affects placement
- **Mocking Requirements**: None

#### InlineCommentPanel
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/UI/Comments/InlineCommentPanel.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/UI/Comments/InlineCommentPanelTests.cs`
- **Complexity**: Medium
- **Priority**: MEDIUM
- **Dependencies**: MarkdownEditor, ReviewerAvatar
- **Code-Behind**: Yes (`InlineCommentPanel.razor.cs`)
- **Test Scenarios**:
  1. Renders comment thread
  2. Shows existing comments with author and timestamp
  3. Allows adding new comment when enabled
  4. OnCommentAdded callback invoked when comment submitted
  5. Resolves comment thread when resolved
  6. Shows resolved state with strikethrough
  7. Empty state when no comments
- **Mocking Requirements**: None (uses comment DTO models)

---

### UI/Dialogs (2 components)

#### ConfirmDialog
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/UI/Dialogs/ConfirmDialog.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/UI/Dialogs/ConfirmDialogTests.cs`
- **Complexity**: Medium
- **Priority**: HIGH
- **Dependencies**: Modal component
- **Test Scenarios**:
  1. Renders modal when IsVisible=true
  2. Does not render when IsVisible=false
  3. Shows title and message
  4. Displays confirm and cancel buttons
  5. OnConfirm callback invoked when confirmed
  6. OnCancel callback invoked when cancelled
  7. Closes after confirm
  8. Closes after cancel
  9. Applies danger variant for destructive actions
- **Mocking Requirements**: None

#### Modal
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/UI/Dialogs/Modal.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/UI/Dialogs/ModalTests.cs`
- **Complexity**: Medium
- **Priority**: HIGH
- **Dependencies**: None
- **Test Scenarios**:
  1. Renders modal when IsVisible=true
  2. Does not render when IsVisible=false
  3. Shows title with icon
  4. Renders body content
  5. Renders footer content
  6. Shows close button when ShowCloseButton=true
  7. OnClose callback invoked when closed
  8. Applies size classes (Small, Medium, Large, ExtraLarge)
  9. Applies variant classes (Default, Primary, Success, Danger, Warning, Info)
  10. Centers modal when Centered=true
  11. Shows default buttons when ShowDefaultButtons=true
  12. Confirm button invokes OnConfirm and closes
  13. Cancel button invokes OnCancel and closes
  14. Renders modal backdrop
- **Mocking Requirements**: None

---

### UI/Editors (3 components, 1 already tested)

#### MarkdownEditor ✅ (Already Tested)
- Existing test: `/tests/PRFactory.Web.Tests/UI/Editors/MarkdownEditorTests.cs`

#### MarkdownPreview
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/UI/Editors/MarkdownPreview.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/UI/Editors/MarkdownPreviewTests.cs`
- **Complexity**: Simple
- **Priority**: MEDIUM
- **Dependencies**: Markdown rendering library
- **Test Scenarios**:
  1. Renders markdown content as HTML
  2. Handles null/empty markdown gracefully
  3. Converts headings correctly
  4. Converts lists correctly
  5. Converts code blocks correctly
  6. Converts links correctly
  7. Sanitizes HTML to prevent XSS
- **Mocking Requirements**: None (uses Markdig or similar library)

#### MarkdownToolbar
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/UI/Editors/MarkdownToolbar.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/UI/Editors/MarkdownToolbarTests.cs`
- **Complexity**: Medium
- **Priority**: MEDIUM
- **Dependencies**: None
- **Test Scenarios**:
  1. Renders toolbar buttons
  2. OnBoldClick callback invoked
  3. OnItalicClick callback invoked
  4. OnLinkClick callback invoked
  5. OnCodeClick callback invoked
  6. OnListClick callback invoked
  7. Shows tooltips on buttons
  8. Disabled state affects all buttons
- **Mocking Requirements**: None

---

### UI/Notifications (2 components)

#### Toast
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/UI/Notifications/Toast.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/UI/Notifications/ToastTests.cs`
- **Complexity**: Medium
- **Priority**: MEDIUM
- **Dependencies**: None
- **Test Scenarios**:
  1. Renders toast with message
  2. Applies type classes (Success, Info, Warning, Danger)
  3. Shows icon based on type
  4. Auto-dismisses after timeout
  5. OnDismiss callback invoked when dismissed
  6. Shows close button
  7. Does not auto-dismiss when AutoDismiss=false
- **Mocking Requirements**: May need to mock timers

#### ToastContainer
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/UI/Notifications/ToastContainer.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/UI/Notifications/ToastContainerTests.cs`
- **Complexity**: Medium
- **Priority**: MEDIUM
- **Dependencies**: Toast component
- **Test Scenarios**:
  1. Renders container in correct position
  2. Renders multiple toasts
  3. Stacks toasts vertically
  4. Removes toast when dismissed
  5. Position parameter affects placement (TopRight, BottomRight, etc.)
- **Mocking Requirements**: None

---

## Testing Priority Order

### Phase 1 (Critical, ~2 hours)
1. Dialogs (2 components) - Modal, ConfirmDialog - Highly reusable
2. Editors (2 components) - MarkdownPreview, MarkdownToolbar

### Phase 2 (Important, ~2 hours)
3. Notifications (2 components) - Toast, ToastContainer
4. Comments (2 components) - CommentAnchorIndicator, InlineCommentPanel

### Phase 3 (Nice to have, ~2 hours)
5. Checklists (2 components) - ChecklistItemRow, ReviewChecklistPanel

## Expected Test File Template

```csharp
using Bunit;
using Xunit;
using PRFactory.Web.UI.Dialogs;

namespace PRFactory.Web.Tests.UI.Dialogs;

public class ModalTests : TestContext
{
    [Fact]
    public void Modal_WhenVisible_RendersModal()
    {
        // Arrange & Act
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test Modal")
            .Add(p => p.Message, "Test message"));

        // Assert
        Assert.Contains("modal fade show", cut.Markup);
        Assert.Contains("Test Modal", cut.Markup);
        Assert.Contains("Test message", cut.Markup);
    }

    [Fact]
    public void Modal_WhenNotVisible_DoesNotRender()
    {
        // Arrange & Act
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, false));

        // Assert
        Assert.DoesNotContain("modal fade show", cut.Markup);
    }

    [Fact]
    public void Modal_WhenCloseButtonClicked_InvokesOnClose()
    {
        // Arrange
        var closeInvoked = false;
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test")
            .Add(p => p.ShowCloseButton, true)
            .Add(p => p.OnClose, () => closeInvoked = true));

        // Act
        cut.Find(".btn-close").Click();

        // Assert
        Assert.True(closeInvoked);
    }
}
```

## Success Criteria

- ✅ All 10 components have test files (9 new + 1 existing)
- ✅ Each component has 5-10 test scenarios covered
- ✅ Tests use xUnit Assert (not FluentAssertions)
- ✅ Tests use bUnit RenderComponent pattern
- ✅ All tests pass: `dotnet test`
- ✅ Code compiles: `dotnet build`
- ✅ Format checks pass: `dotnet format --verify-no-changes`

## Notes

- These components have more complex interactions than Batch 1
- May require testing event callbacks and state changes
- Some components compose other components (test composition)
- Focus on user interactions and state transitions
- Test edge cases (null/empty data, disabled states)
