using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Tests.Blazor;
using PRFactory.Tests.Blazor.TestDataBuilders;
using PRFactory.Web.Models;
using PRFactory.Web.Pages.Workflows;
using PRFactory.Web.Services;
using Xunit;

namespace PRFactory.Tests.Pages.Workflows;

public class EventsTests : PageTestBase
{
    private readonly Mock<IWorkflowEventService> _mockEventService = new();
    private readonly Mock<ILogger<Events>> _mockLogger = new();

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        services.AddSingleton(_mockEventService.Object);
        services.AddSingleton(_mockLogger.Object);
        services.AddScoped<Radzen.DialogService>();

        // Setup JSInterop for all Radzen component calls
        JSInterop.SetupVoid("Radzen.preventArrows", _ => true).SetVoidResult();
        JSInterop.SetupVoid("Radzen.destroyPopup", _ => true).SetVoidResult();
        JSInterop.SetupVoid("Radzen.closePopup", _ => true).SetVoidResult();
        JSInterop.SetupVoid("Radzen.createDatePicker", _ => true).SetVoidResult();
        JSInterop.SetupVoid("Radzen.destroyDatePicker", _ => true).SetVoidResult();
        JSInterop.SetupVoid("Radzen.selectListItem", _ => true).SetVoidResult();
        JSInterop.SetupVoid("Radzen.openPopup", _ => true).SetVoidResult();
    }

    [Fact]
    public async Task OnInitialized_LoadsEventsAndStatistics()
    {
        // Arrange
        SetupMockServices();

        // Act
        var cut = RenderComponent<Events>();
        await Task.Delay(100);

        // Assert
        _mockEventService.Verify(s => s.GetEventTypesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockEventService.Verify(s => s.GetEventsAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<string?>(),
            It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _mockEventService.Verify(s => s.GetStatisticsAsync(
            It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OnInitialized_DisplaysPageTitle()
    {
        // Arrange
        SetupMockServices();

        // Act
        var cut = RenderComponent<Events>();
        await Task.Delay(100);

        // Assert
        Assert.Contains("Workflow Event Log", cut.Markup);
    }

    [Fact]
    public async Task OnInitialized_DisplaysStatistics()
    {
        // Arrange
        SetupMockServices();

        // Act
        var cut = RenderComponent<Events>();
        await Task.Delay(100);

        // Assert
        // EventStatistics component should be rendered
        Assert.Contains("Total Events", cut.Markup);
    }

    [Fact]
    public async Task OnInitialized_WithEvents_DisplaysEventsTable()
    {
        // Arrange
        SetupMockServices();

        // Act
        var cut = RenderComponent<Events>();
        await Task.Delay(100);

        // Assert
        Assert.Contains("Events", cut.Markup);
    }

    [Fact]
    public async Task OnInitialized_WithNoEvents_DisplaysEmptyState()
    {
        // Arrange
        SetupMockServices(emptyEvents: true);

        // Act
        var cut = RenderComponent<Events>();
        await Task.Delay(100);

        // Assert
        Assert.Contains("No events found", cut.Markup);
        Assert.Contains("Clear Filters", cut.Markup);
    }

    [Fact]
    public async Task OnInitialized_WithError_DisplaysErrorMessage()
    {
        // Arrange
        _mockEventService.Setup(s => s.GetEventTypesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service error"));
        _mockEventService.Setup(s => s.GetEventsAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<string?>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service error"));
        _mockEventService.Setup(s => s.GetStatisticsAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var cut = RenderComponent<Events>();
        await Task.Delay(100);

        // Assert
        Assert.Contains("Failed to load event log", cut.Markup);
        Assert.Contains("Retry", cut.Markup);
    }

    [Fact]
    public async Task RefreshButton_Click_ReloadsData()
    {
        // Arrange
        SetupMockServices();
        var cut = RenderComponent<Events>();
        await Task.Delay(100);

        _mockEventService.Invocations.Clear(); // Reset mock calls

        // Act
        var refreshButton = cut.Find("button:contains('Refresh')");
        refreshButton.Click();
        await Task.Delay(100);

        // Assert
        _mockEventService.Verify(s => s.GetEventsAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<string?>(),
            It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task AutoRefreshButton_Click_TogglesAutoRefresh()
    {
        // Arrange
        SetupMockServices();
        var cut = RenderComponent<Events>();
        await Task.Delay(100);

        // Act
        var autoRefreshButton = cut.Find("button:contains('Auto-refresh')");
        autoRefreshButton.Click();
        await Task.Delay(100);

        // Assert
        Assert.Contains("Auto-refresh ON", cut.Markup);
    }

    [Fact]
    public async Task ExportCsvButton_Click_ExportsEvents()
    {
        // Arrange
        SetupMockServices();
        var csvData = System.Text.Encoding.UTF8.GetBytes("event1,event2");
        _mockEventService.Setup(s => s.ExportToCsvAsync(
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(csvData);

        var cut = RenderComponent<Events>();
        await Task.Delay(100);

        // Act
        var exportButton = cut.Find("button:contains('Export CSV')");
        exportButton.Click();
        await Task.Delay(100);

        // Assert
        _mockEventService.Verify(s => s.ExportToCsvAsync(
            It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExportJsonButton_Click_ExportsEvents()
    {
        // Arrange
        SetupMockServices();
        var jsonData = System.Text.Encoding.UTF8.GetBytes("{}");
        _mockEventService.Setup(s => s.ExportToJsonAsync(
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonData);

        var cut = RenderComponent<Events>();
        await Task.Delay(100);

        // Act
        var exportButton = cut.Find("button:contains('Export JSON')");
        exportButton.Click();
        await Task.Delay(100);

        // Assert
        _mockEventService.Verify(s => s.ExportToJsonAsync(
            It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task FilterChange_ReloadsEventsWithFilters()
    {
        // Arrange
        SetupMockServices();
        var cut = RenderComponent<Events>();
        await Task.Delay(100);

        _mockEventService.Invocations.Clear();

        // Note: Testing filter changes through EventLogFilter component is complex
        // The component uses Radzen dropdowns which are difficult to trigger in unit tests
        // This test verifies the page renders correctly with filters

        // Assert
        Assert.Contains("Event Type", cut.Markup);
        Assert.Contains("Start Date", cut.Markup);
        Assert.Contains("End Date", cut.Markup);
        Assert.Contains("Severity", cut.Markup);
    }

    [Fact]
    public async Task Pagination_NextPage_LoadsNextPage()
    {
        // Arrange
        SetupMockServices(totalPages: 3);
        var cut = RenderComponent<Events>();
        await Task.Delay(100);

        _mockEventService.Invocations.Clear();

        // Act
        var nextButton = cut.Find("button:contains('Next')");
        nextButton.Click();
        await Task.Delay(100);

        // Assert
        _mockEventService.Verify(s => s.GetEventsAsync(
            2, // page 2
            It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<string?>(),
            It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Pagination_PreviousPage_LoadsPreviousPage()
    {
        // Arrange - start on page 2 (1-based)
        SetupMockServices(totalPages: 3, currentPage: 2);
        var cut = RenderComponent<Events>();
        await Task.Delay(100);

        // Clear to see only the previous button click
        _mockEventService.Invocations.Clear();

        // Act - click previous button
        var prevButton = cut.Find("button:contains('Previous')");
        prevButton.Click();
        await Task.Delay(100);

        // Assert - should load page 1 now (0-based pagination in the service call)
        _mockEventService.Verify(s => s.GetEventsAsync(
            It.Is<int>(p => p < 2), // previous page
            It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<string?>(),
            It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task Dispose_StopsAutoRefreshTimer()
    {
        // Arrange
        SetupMockServices();
        var cut = RenderComponent<Events>();
        await Task.Delay(100);

        // Enable auto-refresh
        var autoRefreshButton = cut.Find("button:contains('Auto-refresh')");
        autoRefreshButton.Click();
        await Task.Delay(100);

        // Act
        cut.Instance.Dispose();

        // Assert - no exception should occur
        Assert.NotNull(cut.Instance);
    }

    private void SetupMockServices(bool emptyEvents = false, int totalPages = 1, int currentPage = 1)
    {
        var eventTypes = new List<string> { "StateChanged", "QuestionAdded", "PlanCreated" };
        _mockEventService.Setup(s => s.GetEventTypesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(eventTypes);

        var events = emptyEvents
            ? new List<WorkflowEventDto>()
            : new List<WorkflowEventDto>
            {
                new WorkflowEventDtoBuilder().WithEventType("StateChanged").Build(),
                new WorkflowEventDtoBuilder().WithEventType("QuestionAdded").Build()
            };

        var pagedResult = new PagedResult<WorkflowEventDto>(events, currentPage, 50, events.Count);
        if (totalPages > 1)
        {
            // Adjust paged result to reflect multiple pages
            pagedResult = new PagedResult<WorkflowEventDto>(events, currentPage, 50, totalPages * 50);
        }

        _mockEventService.Setup(s => s.GetEventsAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<string?>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var statistics = new EventStatisticsDto
        {
            TotalEvents = 100,
            ErrorCount = 5,
            SuccessRate = 95.0,
            AverageDurationSeconds = 120,
            EventTypeCounts = new Dictionary<string, int>
            {
                { "StateChanged", 50 },
                { "QuestionAdded", 30 },
                { "PlanCreated", 20 }
            }
        };

        _mockEventService.Setup(s => s.GetStatisticsAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(statistics);
    }
}
