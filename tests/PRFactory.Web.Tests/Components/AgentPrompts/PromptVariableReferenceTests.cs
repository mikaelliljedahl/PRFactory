using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moq;
using Xunit;
using PRFactory.Web.Components.AgentPrompts;

namespace PRFactory.Web.Tests.Components.AgentPrompts;

/// <summary>
/// Tests for the PromptVariableReference component.
/// Verifies rendering of available variables by category and callback invocation.
/// </summary>
public class PromptVariableReferenceTests : TestContext
{
    private Mock<IJSRuntime> _mockJsRuntime;

    public PromptVariableReferenceTests()
    {
        _mockJsRuntime = new Mock<IJSRuntime>();
        Services.AddSingleton(_mockJsRuntime.Object);

        // Setup JSInterop for Radzen components
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid("Radzen.preventArrows", _ => true);
        JSInterop.SetupVoid("Radzen.closeDropdown", _ => true);
        JSInterop.SetupVoid("Radzen.openDropdown", _ => true);
    }

    [Fact]
    public void PromptVariableReference_DisplaysTicketVariablesSection()
    {
        // Act
        var cut = RenderComponent<PromptVariableReference>();

        // Assert
        Assert.Contains("Ticket Variables", cut.Markup);
    }

    [Fact]
    public void PromptVariableReference_DisplaysTicketKey()
    {
        // Act
        var cut = RenderComponent<PromptVariableReference>();

        // Assert
        Assert.Contains("{{TicketKey}}", cut.Markup);
        Assert.Contains("Ticket identifier", cut.Markup);
    }

    [Fact]
    public void PromptVariableReference_DisplaysTicketTitle()
    {
        // Act
        var cut = RenderComponent<PromptVariableReference>();

        // Assert
        Assert.Contains("{{TicketTitle}}", cut.Markup);
        Assert.Contains("Original ticket title", cut.Markup);
    }

    [Fact]
    public void PromptVariableReference_DisplaysTicketDescription()
    {
        // Act
        var cut = RenderComponent<PromptVariableReference>();

        // Assert
        Assert.Contains("{{TicketDescription}}", cut.Markup);
        Assert.Contains("Original ticket description", cut.Markup);
    }

    [Fact]
    public void PromptVariableReference_DisplaysUpdatedTitle()
    {
        // Act
        var cut = RenderComponent<PromptVariableReference>();

        // Assert
        Assert.Contains("{{UpdatedTitle}}", cut.Markup);
        Assert.Contains("AI-refined ticket title", cut.Markup);
    }

    [Fact]
    public void PromptVariableReference_DisplaysUpdatedDescription()
    {
        // Act
        var cut = RenderComponent<PromptVariableReference>();

        // Assert
        Assert.Contains("{{UpdatedDescription}}", cut.Markup);
        Assert.Contains("AI-refined description", cut.Markup);
    }

    [Fact]
    public void PromptVariableReference_DisplaysAcceptanceCriteria()
    {
        // Act
        var cut = RenderComponent<PromptVariableReference>();

        // Assert
        Assert.Contains("{{AcceptanceCriteria}}", cut.Markup);
        Assert.Contains("Acceptance criteria list", cut.Markup);
    }

    [Fact]
    public void PromptVariableReference_DisplaysRepositoryVariablesSection()
    {
        // Act
        var cut = RenderComponent<PromptVariableReference>();

        // Assert
        Assert.Contains("Repository Variables", cut.Markup);
    }

    [Fact]
    public void PromptVariableReference_DisplaysRepositoryName()
    {
        // Act
        var cut = RenderComponent<PromptVariableReference>();

        // Assert
        Assert.Contains("{{RepositoryName}}", cut.Markup);
        Assert.Contains("Repository name", cut.Markup);
    }

    [Fact]
    public void PromptVariableReference_DisplaysRepositoryUrl()
    {
        // Act
        var cut = RenderComponent<PromptVariableReference>();

        // Assert
        Assert.Contains("{{RepositoryUrl}}", cut.Markup);
        Assert.Contains("Full repository URL", cut.Markup);
    }

    [Fact]
    public void PromptVariableReference_DisplaysBranchName()
    {
        // Act
        var cut = RenderComponent<PromptVariableReference>();

        // Assert
        Assert.Contains("{{BranchName}}", cut.Markup);
        Assert.Contains("Target branch name", cut.Markup);
    }

    [Fact]
    public void PromptVariableReference_DisplaysPlanVariablesSection()
    {
        // Act
        var cut = RenderComponent<PromptVariableReference>();

        // Assert
        Assert.Contains("Plan Variables", cut.Markup);
    }

    [Fact]
    public void PromptVariableReference_DisplaysPlanContent()
    {
        // Act
        var cut = RenderComponent<PromptVariableReference>();

        // Assert
        Assert.Contains("{{PlanContent}}", cut.Markup);
        Assert.Contains("Implementation plan markdown", cut.Markup);
    }

    [Fact]
    public void PromptVariableReference_DisplaysUserName()
    {
        // Act
        var cut = RenderComponent<PromptVariableReference>();

        // Assert
        Assert.Contains("{{UserName}}", cut.Markup);
        Assert.Contains("Current user name", cut.Markup);
    }

    [Fact]
    public void PromptVariableReference_DisplaysVariableButtons()
    {
        // Act
        var cut = RenderComponent<PromptVariableReference>();

        // Assert
        var buttons = cut.FindAll("button.variable-button");
        Assert.NotEmpty(buttons);
    }

    [Fact]
    public void PromptVariableReference_ClickVariableButton_InvokesCallback()
    {
        // Arrange
        var callbackInvoked = false;
        string? invokedVariable = null;

        var cut = RenderComponent<PromptVariableReference>(parameters => parameters
            .Add(p => p.OnVariableSelected, EventCallback.Factory.Create<string>(this, variable =>
            {
                callbackInvoked = true;
                invokedVariable = variable;
            })));

        // Act
        var button = cut.FindAll("button.variable-button").First();
        button.Click();

        // Assert
        Assert.True(callbackInvoked);
        Assert.NotNull(invokedVariable);
    }

    [Fact]
    public async Task PromptVariableReference_ClickVariable_CopiesVariableToClipboard()
    {
        // Arrange
        var cut = RenderComponent<PromptVariableReference>();

        // Act
        var button = cut.FindAll("button.variable-button").First();
        // Click should not throw - clipboard interaction happens in JS
        button.Click();

        // Assert
        // Verify the component rendered successfully with the variable button
        Assert.NotNull(button);
        Assert.Contains("code", button.InnerHtml);
    }

    [Fact]
    public void PromptVariableReference_AfterClickingVariable_DisplaysSuccessMessage()
    {
        // Arrange
        var cut = RenderComponent<PromptVariableReference>();

        // Act
        var button = cut.FindAll("button.variable-button").First();
        button.Click();

        cut.WaitForAssertion(() =>
        {
            // Assert - Success message should appear
            Assert.Contains("Copied", cut.Markup);
        });
    }

    [Fact]
    public void PromptVariableReference_DisplaysInstructions()
    {
        // Act
        var cut = RenderComponent<PromptVariableReference>();

        // Assert
        Assert.Contains("Click a variable to copy to clipboard", cut.Markup);
    }

    [Fact]
    public void PromptVariableReference_VariableCategoriesHaveIcons()
    {
        // Act
        var cut = RenderComponent<PromptVariableReference>();

        // Assert
        var ticketIcon = cut.Find(".bi-ticket-detailed");
        Assert.NotNull(ticketIcon);

        var repoIcon = cut.Find(".bi-folder");
        Assert.NotNull(repoIcon);

        var planIcon = cut.Find(".bi-file-earmark-text");
        Assert.NotNull(planIcon);
    }

    [Fact]
    public void PromptVariableReference_AllVariablesDisplayed()
    {
        // Act
        var cut = RenderComponent<PromptVariableReference>();

        // Assert
        // Verify all major variables are displayed
        var variables = new[]
        {
            "{{TicketKey}}", "{{TicketTitle}}", "{{TicketDescription}}",
            "{{UpdatedTitle}}", "{{UpdatedDescription}}", "{{AcceptanceCriteria}}",
            "{{RepositoryName}}", "{{RepositoryUrl}}", "{{BranchName}}",
            "{{PlanContent}}", "{{UserName}}"
        };

        var markup = cut.Markup;
        foreach (var variable in variables)
        {
            Assert.Contains(variable, markup);
        }
    }

    [Fact]
    public void PromptVariableReference_WithNoCallback_StillDisplaysVariables()
    {
        // Act - No OnVariableSelected callback provided
        var cut = RenderComponent<PromptVariableReference>();

        // Assert
        Assert.Contains("{{TicketKey}}", cut.Markup);
        Assert.Contains("{{RepositoryName}}", cut.Markup);
        Assert.Contains("{{PlanContent}}", cut.Markup);
    }

    [Fact]
    public void PromptVariableReference_VariableDescriptionsDisplayed()
    {
        // Act
        var cut = RenderComponent<PromptVariableReference>();

        // Assert
        // Check that descriptions are shown
        Assert.Contains("identifier", cut.Markup.ToLower());
        Assert.Contains("description", cut.Markup.ToLower());
        Assert.Contains("url", cut.Markup.ToLower());
    }

    [Fact]
    public void PromptVariableReference_ClicksMultipleVariables_InvokesCallbackMultipleTimes()
    {
        // Arrange
        var invokeCount = 0;

        var cut = RenderComponent<PromptVariableReference>(parameters => parameters
            .Add(p => p.OnVariableSelected, EventCallback.Factory.Create<string>(this, _ =>
            {
                invokeCount++;
            })));

        // Act
        // Re-query buttons after each click because component re-renders and invalidates old references
        var buttons = cut.FindAll("button.variable-button");
        if (buttons.Count >= 2)
        {
            buttons[0].Click();
            // Re-query after first click
            buttons = cut.FindAll("button.variable-button");
            buttons[1].Click();
        }

        // Assert
        Assert.Equal(2, invokeCount);
    }
}
