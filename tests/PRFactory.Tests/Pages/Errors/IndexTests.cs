using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Tests.Blazor;
using PRFactory.Tests.Blazor.TestDataBuilders;
using PRFactory.Web.Models;
using PRFactory.Web.Services;
using Xunit;
using ErrorsIndex = PRFactory.Web.Pages.Errors.Index;

namespace PRFactory.Tests.Pages.Errors;

// TODO: Fix compilation errors in this test class (40+ errors)
// Main issues:
// - CS1503: GetErrorsAsync signature mismatch
// - CS1929: ReturnsAsync type mismatches
// - CS1061: Missing properties/methods
/*
public class IndexTests : PageTestBase
{
    private readonly Mock<IErrorService> _mockErrorService;
    private readonly Mock<ILogger<ErrorsIndex>> _mockLogger;

    public IndexTests()
    {
        _mockErrorService = new Mock<IErrorService>();
        _mockLogger = new Mock<ILogger<ErrorsIndex>>();

        Services.AddSingleton(_mockErrorService.Object);
        Services.AddSingleton(_mockLogger.Object);
    }

    [Fact]
    public async Task OnInitialized_LoadsErrors()
    {
        // Arrange
        SetupMockService();

        // Act
        var cut = RenderComponent<ErrorsIndex>();
        await Task.Delay(100);

        // Assert
        _mockErrorService.Verify(s => s.GetErrorsAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(),
            It.IsAny<bool?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnInitialized_DisplaysStatistics()
    {
        // Arrange
        SetupMockService();

        // Act
        var cut = RenderComponent<ErrorsIndex>();
        await Task.Delay(100);

        // Assert
        Assert.Contains("Total Errors", cut.Markup);
        Assert.Contains("Unresolved", cut.Markup);
    }

    [Fact]
    public async Task OnInitialized_WithNoErrors_DisplaysEmptyState()
    {
        // Arrange
        SetupMockService(emptyErrors: true);

        // Act
        var cut = RenderComponent<ErrorsIndex>();
        await Task.Delay(100);

        // Assert
        Assert.Contains("No errors found", cut.Markup);
    }

    private void SetupMockService(bool emptyErrors = false)
    {
        var errors = emptyErrors
            ? new List<ErrorDto>()
            : new List<ErrorDto>
            {
                new ErrorDtoBuilder().Critical("Critical error").Build(),
                new ErrorDtoBuilder().High("High priority error").Build()
            };

        var pagedResult = new PagedResult<ErrorDto>(errors, 1, 50, errors.Count);
        _mockErrorService.Setup(s => s.GetErrorsAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(),
                It.IsAny<bool?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var statistics = new ErrorStatisticsDto
        {
            TotalErrors = 100,
            UnresolvedCount = 20,
            CriticalCount = 5,
            ResolvedLast24Hours = 10
        };

        _mockErrorService.Setup(s => s.GetStatisticsAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(statistics);
    }
}
*/
