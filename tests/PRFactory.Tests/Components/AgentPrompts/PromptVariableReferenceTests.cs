using Bunit;
using PRFactory.Tests.Blazor;
using PRFactory.Web.Components.AgentPrompts;
using Xunit;

namespace PRFactory.Tests.Components.AgentPrompts;

public class PromptVariableReferenceTests : ComponentTestBase
{
    [Fact]
    public void Render_DisplaysTicketVariables()
    {
        // Act
        var cut = RenderComponent<PromptVariableReference>();

        // Assert - Component shows predefined ticket variables
        Assert.Contains("TicketKey", cut.Markup);
        Assert.Contains("TicketTitle", cut.Markup);
        Assert.Contains("TicketDescription", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysRepositoryVariables()
    {
        // Act
        var cut = RenderComponent<PromptVariableReference>();

        // Assert - Component shows predefined repository variables
        Assert.Contains("RepositoryName", cut.Markup);
        Assert.Contains("RepositoryUrl", cut.Markup);
        Assert.Contains("BranchName", cut.Markup);
    }
}
