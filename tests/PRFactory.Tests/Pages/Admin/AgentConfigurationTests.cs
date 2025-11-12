using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Tests.Blazor;
using PRFactory.Web.Models;
using PRFactory.Web.Pages.Admin;
using PRFactory.Web.Services;
using Xunit;

namespace PRFactory.Tests.Pages.Admin;

public class AgentConfigurationTests : PageTestBase
{
    private readonly Mock<IAgentConfigurationService> _mockConfigService;
    private readonly Mock<ILogger<AgentConfiguration>> _mockLogger;

    public AgentConfigurationTests()
    {
        _mockConfigService = new Mock<IAgentConfigurationService>();
        _mockLogger = new Mock<ILogger<AgentConfiguration>>();

        Services.AddSingleton(_mockConfigService.Object);
        Services.AddSingleton(_mockLogger.Object);
    }

    [Fact]
    public async Task OnInitialized_LoadsConfiguration()
    {
        // Arrange
        var config = new AgentConfigurationDto
        {
            TenantId = Guid.NewGuid(),
            EnableCodeReview = true,
            MaxCodeReviewIterations = 3,
            RequireHumanApprovalAfterReview = true
        };

        _mockConfigService.Setup(s => s.GetConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        // Act
        var cut = RenderComponent<AgentConfiguration>();
        await Task.Delay(100);

        // Assert
        _mockConfigService.Verify(s => s.GetConfigurationAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Contains("Configuration", cut.Markup);
    }

    [Fact]
    public async Task OnInitialized_DisplaysConfigurationForm()
    {
        // Arrange
        var config = new AgentConfigurationDto
        {
            TenantId = Guid.NewGuid(),
            EnableCodeReview = true,
            MaxCodeReviewIterations = 3,
            RequireHumanApprovalAfterReview = true
        };

        _mockConfigService.Setup(s => s.GetConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        // Act
        var cut = RenderComponent<AgentConfiguration>();
        await Task.Delay(100);

        // Assert
        Assert.Contains("Code Review", cut.Markup);
    }
}
