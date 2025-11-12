using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Tests.Blazor;
using PRFactory.Tests.Blazor.TestDataBuilders;
using PRFactory.Web.Models;
using PRFactory.Web.Pages.Errors;
using PRFactory.Web.Services;
using Xunit;

namespace PRFactory.Tests.Pages.Errors;

public class DetailTests : PageTestBase
{
    private readonly Mock<IErrorService> _mockErrorService;
    private readonly Mock<ILogger<Detail>> _mockLogger;

    public DetailTests()
    {
        _mockErrorService = new Mock<IErrorService>();
        _mockLogger = new Mock<ILogger<Detail>>();

        Services.AddSingleton(_mockErrorService.Object);
        Services.AddSingleton(_mockLogger.Object);
    }

    [Fact]
    public async Task OnInitialized_WithValidId_LoadsError()
    {
        // Arrange
        var errorId = Guid.NewGuid();
        var error = new ErrorDtoBuilder().WithId(errorId).Build();

        _mockErrorService.Setup(s => s.GetErrorByIdAsync(errorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(error);

        // Act
        var cut = RenderComponent<Detail>(parameters => parameters
            .Add(p => p.ErrorId, errorId));
        await Task.Delay(100);

        // Assert
        _mockErrorService.Verify(s => s.GetErrorByIdAsync(errorId, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Contains(error.Message, cut.Markup);
    }

    [Fact]
    public async Task OnInitialized_WithInvalidId_DisplaysError()
    {
        // Arrange
        var errorId = Guid.NewGuid();
        _mockErrorService.Setup(s => s.GetErrorByIdAsync(errorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ErrorDto?)null);

        // Act
        var cut = RenderComponent<Detail>(parameters => parameters
            .Add(p => p.ErrorId, errorId));
        await Task.Delay(100);

        // Assert
        Assert.Contains("Error not found", cut.Markup);
    }
}
