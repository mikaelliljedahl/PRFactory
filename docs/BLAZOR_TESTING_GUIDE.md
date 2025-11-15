# Blazor Component Testing Guide

**Status:** ✅ Implemented
**Framework:** bUnit + xUnit + Moq
**Coverage:** 100+ active components tested with 793 total test methods

---

## Overview

This guide explains how to write and run tests for Blazor components in PRFactory using bUnit, xUnit, and Moq.

## Test Infrastructure

### Test Framework Stack

- **bUnit 1.32.7** - Blazor component testing framework
- **xUnit** - Primary testing framework (required by project standards)
- **Moq** - Mocking framework for service dependencies
- **AngleSharp** - HTML parsing for assertions

### Project Structure

```
/tests/PRFactory.Web.Tests/                # Main Blazor component test project
├── Components/                     # Business component tests (32 files)
│   ├── Agents/
│   ├── Code/
│   ├── Repositories/
│   ├── Settings/
│   ├── Tenants/
│   └── Tickets/
├── Pages/                          # Page component tests (2 files)
│   └── Settings/
│       └── LlmProviders/
├── UI/                             # Pure UI component tests (38 files)
│   ├── Alerts/
│   ├── Buttons/
│   ├── Cards/
│   ├── Checklists/
│   ├── Comments/
│   ├── Dialogs/
│   ├── Display/
│   ├── Editors/
│   ├── Forms/
│   ├── Help/
│   ├── Layout/
│   ├── Navigation/
│   └── Notifications/
├── Services/                       # Service facade tests
├── Controllers/                    # API controller tests
└── Properties/                     # Test project properties

Test Infrastructure (/tests/PRFactory.Tests/Blazor/):
├── TestContextBase.cs             # Base class for all Blazor tests
├── ComponentTestBase.cs           # Base for business component tests
├── PageTestBase.cs                # Base for page tests
└── BlazorMockHelpers.cs           # Common mock helpers

Test Data Builders (/tests/PRFactory.Tests/Builders/):
├── TicketBuilder.cs
├── TicketUpdateBuilder.cs
├── RepositoryBuilder.cs
├── TenantBuilder.cs
└── UserBuilder.cs
```

---

## Writing Tests

### Base Classes

All test classes should inherit from one of these base classes:

#### **TestContextBase**

Base class for ALL Blazor component tests. Provides:
- Automatic service mocking (ITicketService, IToastService, etc.)
- bUnit TestContext access
- FakeNavigationManager for navigation testing

#### **ComponentTestBase**

For business/domain components. Extends `TestContextBase` with:
- Helper methods for rendering components
- DOM assertion helpers

#### **PageTestBase**

For page components. Extends `TestContextBase` with:
- Page-specific setup (navigation, etc.)

---

### Test Pattern 1: Pure UI Components

**Example:** Testing `StatusBadge.razor`

```csharp
using Bunit;
using PRFactory.Tests.Blazor;
using PRFactory.Web.UI.Display;
using PRFactory.Domain.ValueObjects;
using Xunit;

namespace PRFactory.Web.Tests.UI.Display;

public class StatusBadgeTests : ComponentTestBase
{
    [Fact]
    public void Render_WithCompletedState_ShowsSuccessBadge()
    {
        // Arrange & Act
        var cut = RenderComponent<StatusBadge>(parameters => parameters
            .Add(p => p.State, WorkflowState.Completed));

        // Assert
        Assert.Contains("bg-success", cut.Markup);
        Assert.Contains("Completed", cut.Markup);
    }

    [Theory]
    [InlineData(WorkflowState.Failed, "bg-danger")]
    [InlineData(WorkflowState.Completed, "bg-success")]
    [InlineData(WorkflowState.AwaitingAnswers, "bg-warning")]
    public void Render_WithState_ShowsCorrectBadgeColor(
        WorkflowState state,
        string expectedClass)
    {
        // Arrange & Act
        var cut = RenderComponent<StatusBadge>(parameters => parameters
            .Add(p => p.State, state));

        // Assert
        Assert.Contains(expectedClass, cut.Markup);
    }
}
```

---

### Test Pattern 2: Business Components with Services

**Example:** Testing `TicketUpdatePreview.razor`

```csharp
using Bunit;
using Moq;
using PRFactory.Tests.Blazor;
using PRFactory.Tests.Builders;
using PRFactory.Web.Components.Tickets;
using Xunit;

namespace PRFactory.Web.Tests.Components.Tickets;

public class TicketUpdatePreviewTests : ComponentTestBase
{
    [Fact]
    public void OnInitialized_LoadsTicketUpdate()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var expectedUpdate = new TicketUpdateBuilder()
            .WithTicketId(ticketId)
            .Build();

        MockTicketService
            .Setup(x => x.GetLatestTicketUpdateAsync(ticketId))
            .ReturnsAsync(expectedUpdate);

        var originalTicket = new TicketBuilder()
            .WithId(ticketId)
            .Build();

        // Act
        var cut = RenderComponent<TicketUpdatePreview>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.OriginalTicket, originalTicket));

        // Wait for async initialization
        cut.WaitForState(() => cut.Markup.Contains(expectedUpdate.ProposedTitle));

        // Assert
        Assert.Contains(expectedUpdate.ProposedTitle, cut.Markup);
        MockTicketService.Verify(
            x => x.GetLatestTicketUpdateAsync(ticketId),
            Times.Once);
    }

    [Fact]
    public async Task ApproveButton_Clicked_CallsServiceAndInvokesCallback()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var updateId = Guid.NewGuid();
        var update = new TicketUpdateBuilder()
            .WithId(updateId)
            .WithTicketId(ticketId)
            .Build();

        MockTicketService
            .Setup(x => x.GetLatestTicketUpdateAsync(ticketId))
            .ReturnsAsync(update);
        MockTicketService
            .Setup(x => x.ApproveTicketUpdateAsync(updateId))
            .Returns(Task.CompletedTask);

        var callbackInvoked = false;
        var originalTicket = new TicketBuilder().WithId(ticketId).Build();

        var cut = RenderComponent<TicketUpdatePreview>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.OriginalTicket, originalTicket)
            .Add(p => p.OnUpdateApproved, () => { callbackInvoked = true; }));

        cut.WaitForState(() => cut.Markup.Contains(update.ProposedTitle));

        // Act
        var approveButton = cut.Find("button:contains('Approve')");
        await approveButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        MockTicketService.Verify(
            x => x.ApproveTicketUpdateAsync(updateId),
            Times.Once);
        Assert.True(callbackInvoked);
    }
}
```

---

### Test Pattern 3: Pages with Navigation

**Example:** Testing `Tickets/Create.razor`

```csharp
using Bunit;
using PRFactory.Tests.Blazor;
using PRFactory.Tests.Builders;
using PRFactory.Web.Pages.Tickets;
using Xunit;

namespace PRFactory.Web.Tests.Pages.Tickets;

public class CreateTests : PageTestBase
{
    [Fact]
    public void OnInitialized_LoadsQuestions()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var questions = new List<QuestionDto>
        {
            new QuestionDtoBuilder().RequirementsQuestion().Build(),
            new QuestionDtoBuilder().TechnicalQuestion().Build()
        };

        MockTicketService
            .Setup(x => x.GetQuestionsByTicketIdAsync(ticketId))
            .ReturnsAsync(questions);

        // Act
        var cut = RenderComponent<Create>(parameters => parameters
            .Add(p => p.TicketId, ticketId.ToString()));

        // Assert
        Assert.Contains(questions[0].Text, cut.Markup);
        Assert.Contains(questions[1].Text, cut.Markup);
    }
}
```

---

## Service Mocking

### Available Mocks (from TestContextBase)

All test classes have access to these mocked services:

```csharp
protected Mock<ITicketService> MockTicketService
protected Mock<IToastService> MockToastService
protected Mock<IAgentPromptService> MockAgentPromptService
protected Mock<IErrorService> MockErrorService
protected Mock<IWorkflowEventService> MockWorkflowEventService
protected Mock<IRepositoryService> MockRepositoryService
protected Mock<ITenantService> MockTenantService
protected FakeNavigationManager NavigationManager
```

### Using Mock Helpers

```csharp
// Setup ticket service
BlazorMockHelpers.SetupGetTicketById(
    MockTicketService,
    ticketId,
    ticketDto);

// Setup toast service
BlazorMockHelpers.SetupToastService(MockToastService);

// Verify navigation
VerifyNavigatedTo("/tickets/123");

// Verify toast
BlazorMockHelpers.VerifySuccessToast(MockToastService);
```

---

## Test Data Builders

Use fluent builders for consistent test data:

```csharp
// Ticket
var ticket = new TicketBuilder()
    .WithTitle("Test Ticket")
    .InPlanReviewState()
    .Build();

// Ticket Update
var update = new TicketUpdateBuilder()
    .WithTicketId(ticketId)
    .WithSampleCriteria()
    .Build();

// Repository
var repo = new RepositoryBuilder()
    .GitHub()
    .WithActivity()
    .Build();

// Tenant
var tenant = new TenantBuilder()
    .FullyConfigured()
    .WithAutoImplementation()
    .Build();

// User
var user = new UserBuilder()
    .WithEmail("test@example.com")
    .AsAdmin()
    .Build();
```

---

## Running Tests

### Run All Blazor Tests

```bash
source /tmp/dotnet-proxy-setup.sh && \
  dotnet test PRFactory.Web.Tests
```

### Run Tests by Category

```bash
# UI components only
dotnet test --filter "FullyQualifiedName~PRFactory.Web.Tests.UI"

# Business components only
dotnet test --filter "FullyQualifiedName~PRFactory.Web.Tests.Components"

# Pages only
dotnet test --filter "FullyQualifiedName~PRFactory.Web.Tests.Pages"

# Specific component
dotnet test --filter "FullyQualifiedName~PRFactory.Web.Tests.Components.Tickets"
```

### Run with Coverage

```bash
source /tmp/dotnet-proxy-setup.sh && \
  dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

---

## Best Practices

### 1. **Test Naming**

Use the pattern: `{MethodOrAction}_{Scenario}_{ExpectedOutcome}`

```csharp
✅ Render_WithCompletedState_ShowsSuccessBadge
✅ ApproveButton_Clicked_CallsServiceAndInvokesCallback
✅ OnInitialized_WhenServiceThrows_ShowsErrorMessage
```

### 2. **Arrange-Act-Assert Pattern**

```csharp
[Fact]
public void ExampleTest()
{
    // Arrange - Set up test data and mocks
    var data = CreateTestData();
    MockService.Setup(x => x.Method()).ReturnsAsync(data);

    // Act - Perform the action being tested
    var result = PerformAction();

    // Assert - Verify the outcome
    Assert.Equal(expected, result);
}
```

### 3. **Use xUnit Assert (NOT FluentAssertions)**

```csharp
✅ Assert.Equal(expected, actual);
✅ Assert.Contains("text", markup);
✅ Assert.True(condition);

❌ result.Should().Be(expected);  // FluentAssertions NOT allowed
```

### 4. **Async Testing with WaitForState**

```csharp
// Wait for component to finish loading
cut.WaitForState(() => cut.Markup.Contains("Expected Text"));

// Or wait for specific element
cut.WaitForState(() => cut.FindAll("button").Count > 0);
```

### 5. **Testing Event Callbacks**

```csharp
var callbackInvoked = false;

var cut = RenderComponent<MyComponent>(parameters => parameters
    .Add(p => p.OnSave, () => { callbackInvoked = true; }));

// Trigger the event
var button = cut.Find("button");
await button.ClickAsync(new MouseEventArgs());

// Verify callback was invoked
Assert.True(callbackInvoked);
```

### 6. **Component Parameter Setup**

```csharp
// Add required parameters
var cut = RenderComponent<MyComponent>(parameters => parameters
    .Add(p => p.RequiredParam, value)
    .Add(p => p.OptionalParam, otherValue));

// For child content (RenderFragment)
var cut = RenderComponent<Card>(parameters => parameters
    .Add(p => p.Title, "Card Title")
    .AddChildContent("<p>Child Content</p>"));
```

---

## Troubleshooting

### Common Issues

#### **Issue: "New services cannot be registered after first service retrieved"**

**Solution:** Register all services in `ConfigureServices()` override, not in constructor:

```csharp
protected override void ConfigureServices(IServiceCollection services)
{
    base.ConfigureServices(services); // Call base first!
    services.AddSingleton(...);        // Then add your services
}
```

#### **Issue: "Cannot mock NavigationManager"**

**Solution:** Use `FakeNavigationManager` from bUnit (already provided in TestContextBase):

```csharp
// Already available as protected property
NavigationManager.NavigateTo("/some/url");

// Verify navigation
Assert.Equal("/some/url", NavigationManager.Uri);
```

#### **Issue: "WaitForState timeout"**

**Solution:** Ensure component actually renders the expected content, or increase timeout:

```csharp
// Default timeout (1 second)
cut.WaitForState(() => condition);

// Custom timeout
cut.WaitForState(() => condition, TimeSpan.FromSeconds(5));
```

#### **Issue: "Component not found in markup"**

**Solution:** Component may not be rendering. Check:
1. Are all required parameters provided?
2. Are service mocks set up correctly?
3. Is component conditionally rendered based on state?

---

## Test Coverage Summary

### Active Tests

| Category | Test Files | Test Methods | Details |
|----------|------------|--------------|---------|
| **UI Components** | 38 | ~380 | Pure UI components in `/UI/*` |
| **Business Components** | 32 | ~370 | Domain logic in `/Components/*` |
| **Pages** | 2 | ~40 | Page navigation in `/Pages/*` |
| **Services** | 1 | ~3 | Service facades |
| **Controllers** | 1 | ~0 | API controllers |
| **Total** | **74** | **793** | Comprehensive coverage |

### Test Infrastructure

- Test infrastructure classes (TestContextBase, ComponentTestBase, PageTestBase, BlazorMockHelpers) are located in `/tests/PRFactory.Tests/Blazor/`
- Test data builders (TicketBuilder, TicketUpdateBuilder, etc.) are located in `/tests/PRFactory.Tests/Builders/`
- All tests reference these shared components via using statements

---

## CI/CD Integration

### Pre-Commit Checks

```bash
# Run before committing
source /tmp/dotnet-proxy-setup.sh && \
  dotnet build && \
  dotnet test PRFactory.Web.Tests && \
  dotnet format --verify-no-changes
```

### Coverage Requirements

- **Minimum:** 80% code coverage for new components
- **Target:** 90%+ coverage for critical components

---

## Contributing

When adding new Blazor components:

1. **Create corresponding test file** in `PRFactory.Web.Tests/{UI|Components|Pages}/`
2. **Inherit from correct base class** (ComponentTestBase or PageTestBase)
3. **Use test data builders** from `PRFactory.Tests/Builders/` for consistent test data
4. **Test all parameters** and event callbacks
5. **Test error scenarios** (service errors, invalid input)
6. **Run tests locally** before committing

**Test infrastructure classes are shared:**
- Base classes (TestContextBase, ComponentTestBase, PageTestBase) are in `PRFactory.Tests/Blazor/`
- These are referenced as `using PRFactory.Tests.Blazor;`
- Data builders are in `PRFactory.Tests/Builders/`

---

## References

- **bUnit Documentation:** https://bunit.dev/
- **xUnit Documentation:** https://xunit.net/
- **Moq Documentation:** https://github.com/moq/moq4
- **PRFactory Architecture:** `/docs/ARCHITECTURE.md`
- **PRFactory Guidelines:** `/CLAUDE.md`
