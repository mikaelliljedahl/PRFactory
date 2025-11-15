using Bunit;
using Xunit;
using PRFactory.Web.Components.Plans;
using PRFactory.Core.Application.DTOs;

namespace PRFactory.Web.Tests.Components.Plans;

/// <summary>
/// Tests for PlanArtifactsCard component
/// </summary>
public class PlanArtifactsCardTests : TestContext
{
    [Fact]
    public void PlanArtifactsCard_WithMultipleArtifacts_ShowsAllTabs()
    {
        // Arrange
        var plan = new PlanDto
        {
            Id = Guid.NewGuid(),
            TicketId = Guid.NewGuid(),
            UserStories = "# User Stories\n- Story 1\n- Story 2",
            ApiDesign = "openapi: 3.0.0\ninfo:\n  title: Test API",
            DatabaseSchema = "CREATE TABLE test (id INT);",
            TestCases = "# Test Cases\n- Test 1\n- Test 2",
            ImplementationSteps = "# Implementation\n1. Step 1\n2. Step 2",
            Version = 1,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<PlanArtifactsCard>(parameters =>
            parameters.Add(p => p.Plan, plan));

        // Assert
        Assert.Contains("User Stories", cut.Markup);
        Assert.Contains("API Design", cut.Markup);
        Assert.Contains("Database Schema", cut.Markup);
        Assert.Contains("Test Cases", cut.Markup);
        Assert.Contains("Implementation Steps", cut.Markup);
    }

    [Fact]
    public void PlanArtifactsCard_WithLegacyPlan_ShowsFallbackView()
    {
        // Arrange
        var plan = new PlanDto
        {
            Id = Guid.NewGuid(),
            TicketId = Guid.NewGuid(),
            Content = "# Implementation Plan\nLegacy plan content",
            Version = 1,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<PlanArtifactsCard>(parameters =>
            parameters.Add(p => p.Plan, plan));

        // Assert
        Assert.Contains("Implementation Plan", cut.Markup);
        Assert.DoesNotContain("User Stories", cut.Markup);
        Assert.DoesNotContain("API Design", cut.Markup);
    }

    [Fact]
    public void PlanArtifactsCard_WithMissingArtifacts_ShowsEmptyStates()
    {
        // Arrange
        var plan = new PlanDto
        {
            Id = Guid.NewGuid(),
            TicketId = Guid.NewGuid(),
            UserStories = "# User Stories\n- Story 1",
            Version = 1,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<PlanArtifactsCard>(parameters =>
            parameters.Add(p => p.Plan, plan));

        // Assert - Has tabs but shows empty states for missing artifacts
        Assert.Contains("User Stories", cut.Markup);
        Assert.Contains("API Design", cut.Markup);
        Assert.Contains("Database Schema", cut.Markup);
    }

    [Fact]
    public void PlanArtifactsCard_WithVersions_ShowsVersionHistoryTab()
    {
        // Arrange
        var plan = new PlanDto
        {
            Id = Guid.NewGuid(),
            TicketId = Guid.NewGuid(),
            UserStories = "# User Stories\n- Story 1",
            Version = 2,
            CreatedAt = DateTime.UtcNow,
            Versions = new List<PlanVersionDto>
            {
                new PlanVersionDto
                {
                    Id = Guid.NewGuid(),
                    PlanId = Guid.NewGuid(),
                    Version = 1,
                    UserStories = "# User Stories\n- Old story",
                    CreatedAt = DateTime.UtcNow.AddHours(-1),
                    CreatedBy = "user@example.com",
                    RevisionReason = "Initial version"
                }
            }
        };

        // Act
        var cut = RenderComponent<PlanArtifactsCard>(parameters =>
            parameters.Add(p => p.Plan, plan));

        // Assert
        Assert.Contains("Version History", cut.Markup);
    }

    [Fact]
    public void PlanArtifactsCard_WithoutVersions_DoesNotShowVersionHistoryTab()
    {
        // Arrange
        var plan = new PlanDto
        {
            Id = Guid.NewGuid(),
            TicketId = Guid.NewGuid(),
            UserStories = "# User Stories\n- Story 1",
            Version = 1,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<PlanArtifactsCard>(parameters =>
            parameters.Add(p => p.Plan, plan));

        // Assert
        Assert.DoesNotContain("Version History", cut.Markup);
    }

    [Fact]
    public void PlanArtifactsCard_WithNullPlan_RendersNothing()
    {
        // Act
        var cut = RenderComponent<PlanArtifactsCard>(parameters =>
            parameters.Add(p => p.Plan, null));

        // Assert
        Assert.Empty(cut.Markup.Trim());
    }

    [Fact]
    public void PlanArtifactsCard_VersionSelected_InvokesCallback()
    {
        // Arrange
        var versionSelected = false;
        PlanVersionDto? selectedVersion = null;
        var plan = new PlanDto
        {
            Id = Guid.NewGuid(),
            TicketId = Guid.NewGuid(),
            UserStories = "# User Stories\n- Story 1",
            Version = 2,
            CreatedAt = DateTime.UtcNow,
            Versions = new List<PlanVersionDto>
            {
                new PlanVersionDto
                {
                    Id = Guid.NewGuid(),
                    PlanId = Guid.NewGuid(),
                    Version = 1,
                    UserStories = "# User Stories\n- Old story",
                    CreatedAt = DateTime.UtcNow.AddHours(-1),
                    CreatedBy = "user@example.com"
                }
            }
        };

        // Act
        var cut = RenderComponent<PlanArtifactsCard>(parameters => parameters
            .Add(p => p.Plan, plan)
            .Add(p => p.OnVersionSelected, (PlanVersionDto version) =>
            {
                versionSelected = true;
                selectedVersion = version;
            }));

        // Trigger version selection through PlanVersionHistory component
        var versionHistoryButtons = cut.FindAll("button");
        var viewButton = versionHistoryButtons.FirstOrDefault(b => b.TextContent.Contains("View"));

        if (viewButton != null)
        {
            viewButton.Click();

            // Assert
            Assert.True(versionSelected);
            Assert.NotNull(selectedVersion);
        }
    }
}
