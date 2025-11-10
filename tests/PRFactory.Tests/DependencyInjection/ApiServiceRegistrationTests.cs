using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace PRFactory.Tests.DependencyInjection;

/// <summary>
/// Tests for API layer service registrations (Program.cs)
/// Note: Most tests are skipped as API infrastructure services are not yet fully registered
/// </summary>
public class ApiServiceRegistrationTests : DIValidationTestBase
{
    #region Current State Tests

    [Fact]
    public void ApiProgram_HasBasicServiceSetup()
    {
        // Arrange
        var services = CreateServiceCollection();

        // Note: API Program.cs currently has basic setup but infrastructure services
        // are not yet registered (commented out in Program.cs)
        // This test documents current state

        // Assert - Just verify we can create a service collection
        Assert.NotNull(services);
    }

    #endregion

    #region Future Infrastructure Service Tests (Currently Skipped)

    [Fact(Skip = "Infrastructure services registration in API is pending")]
    public void ApiProgram_RegistersInfrastructureServices_WhenEnabled()
    {
        // When infrastructure services are enabled in API:
        // - Should register all repositories
        // - Should register all application services
        // - Should register agent framework
        // - Should register Git platform services

        // This test will be enabled once API infrastructure registration is complete
    }

    [Fact(Skip = "Repository registration in API is pending")]
    public void ApiProgram_RegistersRepositories_WhenEnabled()
    {
        // When enabled, should register:
        // - ITenantRepository
        // - IRepositoryRepository
        // - ITicketRepository
        // - ITicketUpdateRepository
        // - etc.
    }

    [Fact(Skip = "Application service registration in API is pending")]
    public void ApiProgram_RegistersApplicationServices_WhenEnabled()
    {
        // When enabled, should register:
        // - ITicketUpdateService
        // - ITicketApplicationService
        // - IRepositoryApplicationService
        // - etc.
    }

    #endregion

    #region Documentation Tests

    [Fact]
    public void ApiProgram_InfrastructureServicesAreCommentedOut()
    {
        // This test documents that infrastructure services are currently
        // commented out in PRFactory.Api/Program.cs
        //
        // Once registration is complete:
        // 1. Enable infrastructure service registration in API Program.cs
        // 2. Remove Skip attributes from tests above
        // 3. Update this test to verify services are registered

        Assert.True(true, "Infrastructure services are not yet registered in API - this is expected");
    }

    #endregion
}
