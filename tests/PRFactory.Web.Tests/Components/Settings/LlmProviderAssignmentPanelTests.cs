using Bunit;
using Xunit;
using PRFactory.Web.Components.Settings;
using PRFactory.Core.Application.DTOs;

namespace PRFactory.Web.Tests.Components.Settings;

/// <summary>
/// Tests for the LlmProviderAssignmentPanel component.
/// Verifies provider loading, dropdown rendering, and assignment functionality.
/// </summary>
public class LlmProviderAssignmentPanelTests : TestContext
{
    private TenantConfigurationDto CreateTestConfiguration()
    {
        return new TenantConfigurationDto
        {
            AnalysisLlmProviderId = null,
            PlanningLlmProviderId = null,
            ImplementationLlmProviderId = null,
            CodeReviewLlmProviderId = null,
            EnableAutoCodeReview = false,
            MaxCodeReviewIterations = 3,
            AutoApproveIfNoIssues = false
        };
    }

    private List<TenantLlmProviderDto> CreateTestProviders()
    {
        return new List<TenantLlmProviderDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                Name = "OpenAI Production",
                ProviderType = "OpenAI",
                DefaultModel = "gpt-4o",
                IsActive = true,
                IsDefault = true,
                UsesOAuth = false,
                ApiBaseUrl = "https://api.openai.com/v1"
            },
            new()
            {
                Id = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                Name = "Claude Production",
                ProviderType = "Anthropic",
                DefaultModel = "claude-3-5-sonnet",
                IsActive = true,
                IsDefault = false,
                UsesOAuth = false,
                ApiBaseUrl = "https://api.anthropic.com"
            }
        };
    }

    [Fact]
    public void Render_WithNoProviders_DisplaysWarningAlert()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var providers = new List<TenantLlmProviderDto>();

        // Act
        var cut = RenderComponent<LlmProviderAssignmentPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.Providers, providers));

        // Assert
        var warningAlert = cut.Find(".alert-warning");
        Assert.NotNull(warningAlert);
        Assert.Contains("No LLM providers configured", warningAlert.TextContent);
        Assert.Contains("Add a provider", warningAlert.TextContent);
    }

    [Fact]
    public void Render_WithProviders_HidesWarningAlert()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var providers = CreateTestProviders();

        // Act
        var cut = RenderComponent<LlmProviderAssignmentPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.Providers, providers));

        // Assert
        var warningAlert = cut.FindAll(".alert-warning").FirstOrDefault();
        Assert.Null(warningAlert);
    }

    [Fact]
    public void Render_WithProviders_DisplaysAnalysisProviderSelect()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var providers = CreateTestProviders();

        // Act
        var cut = RenderComponent<LlmProviderAssignmentPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.Providers, providers));

        // Assert
        var select = cut.Find("select[id='analysisProvider']");
        Assert.NotNull(select);
    }

    [Fact]
    public void Render_WithProviders_DisplaysPlanningProviderSelect()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var providers = CreateTestProviders();

        // Act
        var cut = RenderComponent<LlmProviderAssignmentPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.Providers, providers));

        // Assert
        var select = cut.Find("select[id='planningProvider']");
        Assert.NotNull(select);
    }

    [Fact]
    public void Render_WithProviders_DisplaysImplementationProviderSelect()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var providers = CreateTestProviders();

        // Act
        var cut = RenderComponent<LlmProviderAssignmentPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.Providers, providers));

        // Assert
        var select = cut.Find("select[id='implementationProvider']");
        Assert.NotNull(select);
    }

    [Fact]
    public void Render_WithProviders_DisplaysCodeReviewProviderSelect()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var providers = CreateTestProviders();

        // Act
        var cut = RenderComponent<LlmProviderAssignmentPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.Providers, providers));

        // Assert
        var select = cut.Find("select[id='codeReviewProvider']");
        Assert.NotNull(select);
    }

    [Fact]
    public void Render_WithProviders_DisplaysAllProviderOptions()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var providers = CreateTestProviders();

        // Act
        var cut = RenderComponent<LlmProviderAssignmentPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.Providers, providers));

        // Assert
        var analysisSelect = cut.Find("select[id='analysisProvider']");
        var options = analysisSelect.FindAll("option");

        Assert.Contains("Use Tenant Default", options.Select(o => o.TextContent).ToList());
        Assert.Contains("OpenAI Production (OpenAI) ⭐", options.Select(o => o.TextContent).FirstOrDefault(t => t.Contains("OpenAI Production")));
    }

    [Fact]
    public void Render_OnlyDisplaysActiveProviders()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var providers = new List<TenantLlmProviderDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                Name = "Active Provider",
                ProviderType = "OpenAI",
                DefaultModel = "gpt-4o",
                IsActive = true,
                IsDefault = true,
                UsesOAuth = false,
                ApiBaseUrl = "https://api.openai.com/v1"
            },
            new()
            {
                Id = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                Name = "Inactive Provider",
                ProviderType = "Anthropic",
                DefaultModel = "claude-3-5-sonnet",
                IsActive = false, // Inactive
                IsDefault = false,
                UsesOAuth = false,
                ApiBaseUrl = "https://api.anthropic.com"
            }
        };

        // Act
        var cut = RenderComponent<LlmProviderAssignmentPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.Providers, providers));

        // Assert
        var analysisSelect = cut.Find("select[id='analysisProvider']");
        var options = cut.FindAll("select[id='analysisProvider'] option");
        var optionsText = options.Select(o => o.TextContent).ToList();

        Assert.Contains("Active Provider", string.Join(" ", optionsText));
        Assert.DoesNotContain("Inactive Provider", string.Join(" ", optionsText));
    }

    [Fact]
    public void Render_DisplaysDefaultProviderStar()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var providers = CreateTestProviders();

        // Act
        var cut = RenderComponent<LlmProviderAssignmentPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.Providers, providers));

        // Assert
        var options = cut.FindAll("select[id='analysisProvider'] option");
        var defaultOption = options.FirstOrDefault(o => o.TextContent.Contains("⭐"));

        Assert.NotNull(defaultOption);
        Assert.Contains("OpenAI Production", defaultOption.TextContent);
    }

    [Fact]
    public void Render_DisplaysProviderTypeInOptions()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var providers = CreateTestProviders();

        // Act
        var cut = RenderComponent<LlmProviderAssignmentPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.Providers, providers));

        // Assert
        var options = cut.FindAll("select[id='analysisProvider'] option");
        var optionsText = string.Join(" ", options.Select(o => o.TextContent));

        Assert.Contains("OpenAI", optionsText);
        Assert.Contains("Anthropic", optionsText);
    }

    [Fact]
    public void Render_WhenCanEditFalse_DisablesAllSelects()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var providers = CreateTestProviders();

        // Act
        var cut = RenderComponent<LlmProviderAssignmentPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.Providers, providers)
            .Add(p => p.CanEdit, false));

        // Assert
        var analysisSelect = cut.Find("select[id='analysisProvider']");
        var planningSelect = cut.Find("select[id='planningProvider']");
        var implementationSelect = cut.Find("select[id='implementationProvider']");
        var codeReviewSelect = cut.Find("select[id='codeReviewProvider']");

        Assert.True(analysisSelect.HasAttribute("disabled"));
        Assert.True(planningSelect.HasAttribute("disabled"));
        Assert.True(implementationSelect.HasAttribute("disabled"));
        Assert.True(codeReviewSelect.HasAttribute("disabled"));
    }

    [Fact]
    public void Render_WhenCanEditTrue_EnablesAllSelects()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var providers = CreateTestProviders();

        // Act
        var cut = RenderComponent<LlmProviderAssignmentPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.Providers, providers)
            .Add(p => p.CanEdit, true));

        // Assert
        var analysisSelect = cut.Find("select[id='analysisProvider']");
        var planningSelect = cut.Find("select[id='planningProvider']");
        var implementationSelect = cut.Find("select[id='implementationProvider']");
        var codeReviewSelect = cut.Find("select[id='codeReviewProvider']");

        Assert.False(analysisSelect.HasAttribute("disabled"));
        Assert.False(planningSelect.HasAttribute("disabled"));
        Assert.False(implementationSelect.HasAttribute("disabled"));
        Assert.False(codeReviewSelect.HasAttribute("disabled"));
    }

    [Fact]
    public void Render_DisplaysHelpText()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var providers = CreateTestProviders();

        // Act
        var cut = RenderComponent<LlmProviderAssignmentPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.Providers, providers));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Analyzes codebase and generates clarifying questions", markup);
        Assert.Contains("Creates detailed implementation plans for review", markup);
        Assert.Contains("Generates code based on approved plans", markup);
        Assert.Contains("Reviews generated code for issues and improvements", markup);
    }

    [Fact]
    public void Render_DisplaysTipAlert()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var providers = CreateTestProviders();

        // Act
        var cut = RenderComponent<LlmProviderAssignmentPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.Providers, providers));

        // Assert
        var tipAlert = cut.Find(".alert-info");
        Assert.NotNull(tipAlert);
        Assert.Contains("Tip", tipAlert.TextContent);
        Assert.Contains("Assign different providers", tipAlert.TextContent);
    }

    [Fact]
    public void Render_BothColumns_DisplaysProperlyStructured()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var providers = CreateTestProviders();

        // Act
        var cut = RenderComponent<LlmProviderAssignmentPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.Providers, providers));

        // Assert
        var columns = cut.FindAll(".col-md-6");
        Assert.Equal(2, columns.Count);
    }

    [Fact]
    public void Select_AnalysisProvider_UpdatesConfiguration()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var providers = CreateTestProviders();
        var providerId = providers[0].Id;

        var cut = RenderComponent<LlmProviderAssignmentPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.Providers, providers)
            .Add(p => p.CanEdit, true));

        var select = cut.Find("select[id='analysisProvider']");

        // Act
        select.Change(providerId.ToString());

        // Assert
        Assert.Equal(providerId, config.AnalysisLlmProviderId);
    }

    [Fact]
    public void Select_PlanningProvider_UpdatesConfiguration()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var providers = CreateTestProviders();
        var providerId = providers[0].Id;

        var cut = RenderComponent<LlmProviderAssignmentPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.Providers, providers)
            .Add(p => p.CanEdit, true));

        var select = cut.Find("select[id='planningProvider']");

        // Act
        select.Change(providerId.ToString());

        // Assert
        Assert.Equal(providerId, config.PlanningLlmProviderId);
    }

    [Fact]
    public void Select_ImplementationProvider_UpdatesConfiguration()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var providers = CreateTestProviders();
        var providerId = providers[0].Id;

        var cut = RenderComponent<LlmProviderAssignmentPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.Providers, providers)
            .Add(p => p.CanEdit, true));

        var select = cut.Find("select[id='implementationProvider']");

        // Act
        select.Change(providerId.ToString());

        // Assert
        Assert.Equal(providerId, config.ImplementationLlmProviderId);
    }

    [Fact]
    public void Select_CodeReviewProvider_UpdatesConfiguration()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var providers = CreateTestProviders();
        var providerId = providers[0].Id;

        var cut = RenderComponent<LlmProviderAssignmentPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.Providers, providers)
            .Add(p => p.CanEdit, true));

        var select = cut.Find("select[id='codeReviewProvider']");

        // Act
        select.Change(providerId.ToString());

        // Assert
        Assert.Equal(providerId, config.CodeReviewLlmProviderId);
    }
}
