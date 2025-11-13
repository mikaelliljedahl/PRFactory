using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PRFactory.Web.Services;
using Xunit;

namespace PRFactory.Tests.Blazor;

/// <summary>
/// Base class for all Blazor component tests providing common setup and mocks
/// Inherits from Bunit.TestContext to support bUnit extension methods
/// </summary>
public abstract class TestContextBase : Bunit.TestContext
{
    protected Mock<ITicketService> MockTicketService { get; private set; }
    protected Mock<IToastService> MockToastService { get; private set; }
    protected Mock<IAgentPromptService> MockAgentPromptService { get; private set; }
    protected Mock<IErrorService> MockErrorService { get; private set; }
    protected Mock<IWorkflowEventService> MockWorkflowEventService { get; private set; }
    protected Mock<IRepositoryService> MockRepositoryService { get; private set; }
    protected Mock<ITenantService> MockTenantService { get; private set; }
    protected FakeNavigationManager NavigationManager { get; private set; }

    protected TestContextBase()
    {
        // Create mocks for commonly used services
        MockTicketService = new Mock<ITicketService>();
        MockToastService = new Mock<IToastService>();
        MockAgentPromptService = new Mock<IAgentPromptService>();
        MockErrorService = new Mock<IErrorService>();
        MockWorkflowEventService = new Mock<IWorkflowEventService>();
        MockRepositoryService = new Mock<IRepositoryService>();
        MockTenantService = new Mock<ITenantService>();

        // Register services BEFORE getting anything from the service provider
        Services.AddSingleton(MockTicketService.Object);
        Services.AddSingleton(MockToastService.Object);
        Services.AddSingleton(MockAgentPromptService.Object);
        Services.AddSingleton(MockErrorService.Object);
        Services.AddSingleton(MockWorkflowEventService.Object);
        Services.AddSingleton(MockRepositoryService.Object);
        Services.AddSingleton(MockTenantService.Object);

        // Add additional common services
        ConfigureServices(Services);

        // Now get FakeNavigationManager (bUnit registers it automatically)
        NavigationManager = Services.GetRequiredService<FakeNavigationManager>();
    }

    /// <summary>
    /// Override this method to configure additional services for specific test classes
    /// </summary>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
        // Override in derived classes to add more services
    }

    /// <summary>
    /// Verify that navigation occurred to the specified URL
    /// </summary>
    protected void VerifyNavigatedTo(string expectedUrl)
    {
        // FakeNavigationManager tracks navigation history
        Assert.Equal(expectedUrl, NavigationManager.Uri);
    }
}
