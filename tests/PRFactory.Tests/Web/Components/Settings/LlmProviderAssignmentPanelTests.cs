using Bunit;
using PRFactory.Core.Application.DTOs;
using PRFactory.Web.Components.Settings;
using Xunit;

namespace PRFactory.Tests.Web.Components.Settings;

public class LlmProviderAssignmentPanelTests : TestContext
{
    [Fact]
    public void LlmProviderAssignmentPanel_ShowsWarning_WhenNoProviders()
    {
        // Arrange
        var config = new TenantConfigurationDto();
        var providers = new List<TenantLlmProviderDto>();

        // Act
        var cut = RenderComponent<LlmProviderAssignmentPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.Providers, providers)
            .Add(p => p.CanEdit, true));

        // Assert
        Assert.Contains("No LLM providers configured", cut.Markup);
        Assert.Contains("Add a provider", cut.Markup);
    }

    [Fact]
    public void LlmProviderAssignmentPanel_DisplaysAllRoleDropdowns_WhenProvidersExist()
    {
        // Arrange
        var config = new TenantConfigurationDto();
        var providers = new List<TenantLlmProviderDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Test Provider",
                ProviderType = "ZAi",
                IsActive = true,
                IsDefault = false
            }
        };

        // Act
        var cut = RenderComponent<LlmProviderAssignmentPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.Providers, providers)
            .Add(p => p.CanEdit, true));

        // Assert
        Assert.Contains("Analysis LLM Provider", cut.Markup);
        Assert.Contains("Planning LLM Provider", cut.Markup);
        Assert.Contains("Implementation LLM Provider", cut.Markup);
        Assert.Contains("Code Review LLM Provider", cut.Markup);
    }

    [Fact]
    public void LlmProviderAssignmentPanel_DisplaysProviderInDropdown()
    {
        // Arrange
        var config = new TenantConfigurationDto();
        var providers = new List<TenantLlmProviderDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Production Claude",
                ProviderType = "ZAi",
                IsActive = true,
                IsDefault = false
            }
        };

        // Act
        var cut = RenderComponent<LlmProviderAssignmentPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.Providers, providers)
            .Add(p => p.CanEdit, true));

        // Assert
        Assert.Contains("Production Claude", cut.Markup);
        Assert.Contains("ZAi", cut.Markup);
    }

    [Fact]
    public void LlmProviderAssignmentPanel_ShowsDefaultStar_ForDefaultProvider()
    {
        // Arrange
        var config = new TenantConfigurationDto();
        var providers = new List<TenantLlmProviderDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Default Provider",
                ProviderType = "ZAi",
                IsActive = true,
                IsDefault = true
            }
        };

        // Act
        var cut = RenderComponent<LlmProviderAssignmentPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.Providers, providers)
            .Add(p => p.CanEdit, true));

        // Assert
        Assert.Contains("⭐", cut.Markup);
    }

    [Fact]
    public void LlmProviderAssignmentPanel_FiltersInactiveProviders()
    {
        // Arrange
        var config = new TenantConfigurationDto();
        var providers = new List<TenantLlmProviderDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Active Provider",
                ProviderType = "ZAi",
                IsActive = true,
                IsDefault = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Inactive Provider",
                ProviderType = "MinimaxM2",
                IsActive = false,
                IsDefault = false
            }
        };

        // Act
        var cut = RenderComponent<LlmProviderAssignmentPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.Providers, providers)
            .Add(p => p.CanEdit, true));

        // Assert
        Assert.Contains("Active Provider", cut.Markup);
        Assert.DoesNotContain("Inactive Provider", cut.Markup);
    }

    [Fact]
    public void LlmProviderAssignmentPanel_DisablesDropdowns_WhenCannotEdit()
    {
        // Arrange
        var config = new TenantConfigurationDto();
        var providers = new List<TenantLlmProviderDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Test Provider",
                ProviderType = "ZAi",
                IsActive = true,
                IsDefault = false
            }
        };

        // Act
        var cut = RenderComponent<LlmProviderAssignmentPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.Providers, providers)
            .Add(p => p.CanEdit, false));

        // Assert
        Assert.Contains("disabled", cut.Markup);
    }

    [Fact]
    public void LlmProviderAssignmentPanel_ShowsTip()
    {
        // Arrange
        var config = new TenantConfigurationDto();
        var providers = new List<TenantLlmProviderDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Test Provider",
                ProviderType = "ZAi",
                IsActive = true,
                IsDefault = false
            }
        };

        // Act
        var cut = RenderComponent<LlmProviderAssignmentPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.Providers, providers)
            .Add(p => p.CanEdit, true));

        // Assert
        Assert.Contains("Assign different providers to different roles", cut.Markup);
    }
}
