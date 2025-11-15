# Batch 1: Simple UI Components

## Summary
- **Total Components**: 30
- **Estimated Complexity**: Simple
- **Estimated Time**: 6-8 hours
- **Priority**: HIGH
- **Coverage Contribution**: +24% (30/125 components)

## Overview

This batch focuses on pure UI components with minimal logic and no service dependencies. These components are the easiest to test and provide high ROI for coverage.

## Components to Test

### UI/Alerts (2 components, 1 already tested)

#### AlertMessage
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/UI/Alerts/AlertMessage.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/UI/Alerts/AlertMessageTests.cs`
- **Complexity**: Simple
- **Priority**: HIGH
- **Dependencies**: None
- **Test Scenarios**:
  1. Renders with default info type
  2. Renders with different alert types (Success, Warning, Danger, Info)
  3. Displays message correctly
  4. Shows icon when Icon parameter provided
  5. Renders dismissible button when Dismissible=true
  6. Does not render when Message is null/empty
  7. OnDismiss callback invoked when dismiss button clicked
- **Mocking Requirements**: None

#### DemoModeBanner
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/UI/Alerts/DemoModeBanner.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/UI/Alerts/DemoModeBannerTests.cs`
- **Complexity**: Simple
- **Priority**: HIGH
- **Dependencies**: None
- **Test Scenarios**:
  1. Renders banner with demo mode message
  2. Shows correct icon
  3. Displays expected warning text
- **Mocking Requirements**: None

#### InfoBox ✅ (Already Tested)
- Existing test: `/home/user/PRFactory/tests/PRFactory.Web.Tests/UI/Alerts/InfoBoxTests.cs`

---

### UI/Buttons (2 components)

#### IconButton
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/UI/Buttons/IconButton.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/UI/Buttons/IconButtonTests.cs`
- **Complexity**: Simple
- **Priority**: HIGH
- **Dependencies**: None
- **Test Scenarios**:
  1. Renders button with icon class
  2. Applies variant classes correctly (Primary, Secondary, Danger, etc.)
  3. Applies size classes correctly (Small, Normal, Large)
  4. Disabled state prevents click
  5. OnClick callback invoked when clicked
  6. Tooltip displayed when provided
  7. AdditionalClass parameter applied correctly
- **Mocking Requirements**: None

#### LoadingButton
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/UI/Buttons/LoadingButton.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/UI/Buttons/LoadingButtonTests.cs`
- **Complexity**: Simple
- **Priority**: HIGH
- **Dependencies**: None
- **Test Scenarios**:
  1. Renders button with text
  2. Shows loading spinner when IsLoading=true
  3. Displays LoadingText when loading
  4. Shows icon when not loading and Icon provided
  5. Disabled when IsLoading=true
  6. Disabled when Disabled=true
  7. OnClick not invoked when disabled or loading
  8. OnClick invoked when enabled and not loading
  9. Variant classes applied correctly
  10. Size classes applied correctly
  11. ChildContent rendered when not loading
- **Mocking Requirements**: None

---

### UI/Cards (1 component)

#### Card
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/UI/Cards/Card.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/UI/Cards/CardTests.cs`
- **Complexity**: Simple
- **Priority**: HIGH
- **Dependencies**: None
- **Test Scenarios**:
  1. Renders card with title
  2. Shows icon when Icon parameter provided
  3. Applies variant classes correctly (Default, Primary, Success, Danger, Warning, Info)
  4. Renders ChildContent in card body
  5. Does not render header when Title is null
  6. AdditionalClass parameter applied correctly
- **Mocking Requirements**: None

---

### UI/Display (9 components)

#### EmptyState
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/UI/Display/EmptyState.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/UI/Display/EmptyStateTests.cs`
- **Complexity**: Simple
- **Priority**: HIGH
- **Dependencies**: None
- **Test Scenarios**:
  1. Renders message correctly
  2. Shows icon when Icon parameter provided
  3. Renders ActionButton when provided
  4. Shows appropriate empty state styling
- **Mocking Requirements**: None

#### ErrorCard
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/UI/Display/ErrorCard.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/UI/Display/ErrorCardTests.cs`
- **Complexity**: Simple
- **Priority**: HIGH
- **Dependencies**: None
- **Test Scenarios**:
  1. Renders error message
  2. Shows error icon
  3. Applies danger styling
  4. Displays detailed error when provided
- **Mocking Requirements**: None

#### LoadingSpinner
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/UI/Display/LoadingSpinner.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/UI/Display/LoadingSpinnerTests.cs`
- **Complexity**: Simple
- **Priority**: HIGH
- **Dependencies**: None
- **Test Scenarios**:
  1. Renders spinner when IsLoading=true
  2. Does not render when IsLoading=false
  3. Displays loading message when provided
  4. Shows centered spinner
- **Mocking Requirements**: None

#### ProgressBar
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/UI/Display/ProgressBar.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/UI/Display/ProgressBarTests.cs`
- **Complexity**: Simple
- **Priority**: HIGH
- **Dependencies**: None
- **Test Scenarios**:
  1. Renders progress bar with percentage
  2. Applies correct width style based on percentage
  3. Shows label when ShowLabel=true
  4. Applies variant classes (Success, Info, Warning, Danger)
  5. Clamps percentage between 0-100
  6. Shows striped animation when Striped=true
- **Mocking Requirements**: None

#### StatusBadge
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/UI/Display/StatusBadge.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/UI/Display/StatusBadgeTests.cs`
- **Complexity**: Simple
- **Priority**: HIGH
- **Dependencies**: None (may reference WorkflowState enum)
- **Test Scenarios**:
  1. Renders badge with status text
  2. Applies correct variant class based on status
  3. Shows icon when provided
  4. Different status states render different colors
- **Mocking Requirements**: None

#### RelativeTime
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/UI/Display/RelativeTime.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/UI/Display/RelativeTimeTests.cs`
- **Complexity**: Simple
- **Priority**: HIGH
- **Dependencies**: None
- **Test Scenarios**:
  1. Renders "just now" for very recent times
  2. Renders "X minutes ago" for recent times
  3. Renders "X hours ago" for hour-old times
  4. Renders "X days ago" for day-old times
  5. Shows full date for old times
  6. Tooltip shows full datetime
- **Mocking Requirements**: None

#### ReviewerAvatar
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/UI/Display/ReviewerAvatar.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/UI/Display/ReviewerAvatarTests.cs`
- **Complexity**: Simple
- **Priority**: HIGH
- **Dependencies**: None
- **Test Scenarios**:
  1. Renders avatar with initials from name
  2. Shows tooltip with full name
  3. Applies different colors based on name hash
  4. Handles null/empty names gracefully
- **Mocking Requirements**: None

#### StackTraceViewer
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/UI/Display/StackTraceViewer.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/UI/Display/StackTraceViewerTests.cs`
- **Complexity**: Simple
- **Priority**: HIGH
- **Dependencies**: None
- **Test Scenarios**:
  1. Renders stack trace text
  2. Applies monospace font styling
  3. Preserves whitespace and line breaks
  4. Handles null/empty stack trace
- **Mocking Requirements**: None

#### EventTimeline
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/UI/Display/EventTimeline.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/UI/Display/EventTimelineTests.cs`
- **Complexity**: Simple
- **Priority**: MEDIUM
- **Dependencies**: May have code-behind with minimal logic
- **Test Scenarios**:
  1. Renders empty state when no events
  2. Renders timeline with events
  3. Shows event icons correctly
  4. Displays event timestamps
  5. Formats event descriptions
- **Mocking Requirements**: None (uses event DTO models)

---

### UI/Forms (6 components)

#### FormCheckboxField
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/UI/Forms/FormCheckboxField.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/UI/Forms/FormCheckboxFieldTests.cs`
- **Complexity**: Simple
- **Priority**: HIGH
- **Dependencies**: None
- **Test Scenarios**:
  1. Renders checkbox with label
  2. Checked state reflected in UI
  3. ValueChanged callback invoked on change
  4. Disabled state prevents interaction
  5. Validation message shown when invalid
  6. Help text displayed when provided
- **Mocking Requirements**: None

#### FormPasswordField
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/UI/Forms/FormPasswordField.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/UI/Forms/FormPasswordFieldTests.cs`
- **Complexity**: Simple
- **Priority**: HIGH
- **Dependencies**: None
- **Test Scenarios**:
  1. Renders password input with label
  2. Input type is "password"
  3. ValueChanged callback invoked on input
  4. Required indicator shown when Required=true
  5. Validation message shown when invalid
  6. Help text and tooltip displayed when provided
  7. Disabled state applied correctly
  8. Placeholder text shown
- **Mocking Requirements**: None

#### FormSelectField
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/UI/Forms/FormSelectField.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/UI/Forms/FormSelectFieldTests.cs`
- **Complexity**: Simple
- **Priority**: HIGH
- **Dependencies**: None
- **Test Scenarios**:
  1. Renders select with label
  2. Options rendered from ChildContent
  3. ValueChanged callback invoked on change
  4. Required indicator shown when Required=true
  5. Validation message shown when invalid
  6. Help text displayed when provided
  7. Disabled state applied correctly
- **Mocking Requirements**: None

#### FormTextAreaField
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/UI/Forms/FormTextAreaField.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/UI/Forms/FormTextAreaFieldTests.cs`
- **Complexity**: Simple
- **Priority**: HIGH
- **Dependencies**: None
- **Test Scenarios**:
  1. Renders textarea with label
  2. ValueChanged callback invoked on input
  3. Required indicator shown when Required=true
  4. Validation message shown when invalid
  5. Help text and tooltip displayed when provided
  6. Disabled state applied correctly
  7. Placeholder text shown
  8. Rows parameter sets textarea height
- **Mocking Requirements**: None

#### FormTextField
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/UI/Forms/FormTextField.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/UI/Forms/FormTextFieldTests.cs`
- **Complexity**: Simple
- **Priority**: HIGH
- **Dependencies**: ContextualHelp component
- **Test Scenarios**:
  1. Renders input with label
  2. Input type applied correctly (text, email, number, etc.)
  3. ValueChanged callback invoked on input
  4. Required indicator shown when Required=true
  5. Validation message shown when invalid
  6. Help text displayed when provided
  7. Help tooltip shown when HelpTooltipText provided
  8. Disabled state applied correctly
  9. Placeholder text shown
  10. Field ID generated or uses custom Id
- **Mocking Requirements**: None

#### FormCodeEditor
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/UI/Forms/FormCodeEditor.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/UI/Forms/FormCodeEditorTests.cs`
- **Complexity**: Simple
- **Priority**: MEDIUM
- **Dependencies**: None
- **Test Scenarios**:
  1. Renders code editor with label
  2. Applies monospace font
  3. ValueChanged callback invoked on input
  4. Language/syntax highlighting class applied
  5. Validation message shown when invalid
- **Mocking Requirements**: None

---

### UI/Layout (4 components, 4 already tested ✅)

All Layout components are already tested:
- ✅ GridColumn - `/tests/PRFactory.Web.Tests/UI/Layout/GridColumnTests.cs`
- ✅ GridLayout - `/tests/PRFactory.Web.Tests/UI/Layout/GridLayoutTests.cs`
- ✅ PageHeader - `/tests/PRFactory.Web.Tests/UI/Layout/PageHeaderTests.cs`
- ✅ Section - `/tests/PRFactory.Web.Tests/UI/Layout/SectionTests.cs`

---

### UI/Help (1 component)

#### ContextualHelp
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/UI/Help/ContextualHelp.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/UI/Help/ContextualHelpTests.cs`
- **Complexity**: Simple
- **Priority**: MEDIUM
- **Dependencies**: None
- **Test Scenarios**:
  1. Renders help icon
  2. Shows tooltip on hover with HelpText
  3. Displays title when provided
  4. Renders "Learn More" link when LearnMoreUrl provided
  5. Position parameter affects tooltip placement
- **Mocking Requirements**: None

---

### UI/Navigation (1 component)

#### Breadcrumbs
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/UI/Navigation/Breadcrumbs.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/UI/Navigation/BreadcrumbsTests.cs`
- **Complexity**: Simple
- **Priority**: MEDIUM
- **Dependencies**: None
- **Test Scenarios**:
  1. Renders breadcrumb items
  2. Shows home icon for first item
  3. Last item is not a link (active)
  4. Non-last items are links
  5. Icons displayed when provided
  6. Handles empty Items list
- **Mocking Requirements**: None

---

## Testing Priority Order

### Phase 1 (Critical, ~4 hours)
1. Forms (6 components) - Most reusable, core UI
2. Buttons (2 components) - Highly reusable
3. Display (6 components) - LoadingSpinner, StatusBadge, EmptyState, ErrorCard, ProgressBar, RelativeTime

### Phase 2 (Important, ~2 hours)
4. Alerts (2 components) - AlertMessage, DemoModeBanner
5. Cards (1 component)
6. Display (3 remaining) - ReviewerAvatar, StackTraceViewer, EventTimeline

### Phase 3 (Nice to have, ~2 hours)
7. Help (1 component) - ContextualHelp
8. Navigation (1 component) - Breadcrumbs

## Expected Test File Template

```csharp
using Bunit;
using Xunit;
using PRFactory.Web.UI.Buttons;

namespace PRFactory.Web.Tests.UI.Buttons;

public class LoadingButtonTests : TestContext
{
    [Fact]
    public void LoadingButton_WithText_RendersText()
    {
        // Arrange
        var expectedText = "Click Me";

        // Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.Text, expectedText));

        // Assert
        Assert.Contains(expectedText, cut.Markup);
    }

    [Fact]
    public void LoadingButton_WhenLoading_ShowsSpinner()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.IsLoading, true)
            .Add(p => p.LoadingText, "Processing..."));

        // Assert
        Assert.Contains("spinner-border", cut.Markup);
        Assert.Contains("Processing...", cut.Markup);
    }

    [Fact]
    public void LoadingButton_WhenDisabled_DoesNotInvokeOnClick()
    {
        // Arrange
        var clickInvoked = false;
        var cut = RenderComponent<LoadingButton>(parameters => parameters
            .Add(p => p.Disabled, true)
            .Add(p => p.OnClick, () => clickInvoked = true));

        // Act
        cut.Find("button").Click();

        // Assert
        Assert.False(clickInvoked);
    }
}
```

## Success Criteria

- ✅ All 30 components have test files
- ✅ Each component has 3-10 test scenarios covered
- ✅ Tests use xUnit Assert (not FluentAssertions)
- ✅ Tests use bUnit RenderComponent pattern
- ✅ All tests pass: `dotnet test`
- ✅ Code compiles: `dotnet build`
- ✅ Format checks pass: `dotnet format --verify-no-changes`

## Notes

- These are pure UI components with minimal or no dependencies
- No service mocking required (except for components that compose other components)
- Focus on parameter variations and conditional rendering
- Test user interactions (clicks, inputs) where applicable
- Verify CSS classes applied correctly for variants and states
