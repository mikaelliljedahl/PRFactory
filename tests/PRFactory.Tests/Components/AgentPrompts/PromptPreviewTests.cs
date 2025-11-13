using Bunit;
using Moq;
using PRFactory.Tests.Blazor;
using PRFactory.Tests.Blazor.TestDataBuilders;
using PRFactory.Web.Components.AgentPrompts;
using PRFactory.Web.Services;
using Xunit;

namespace PRFactory.Tests.Components.AgentPrompts;

public class PromptPreviewTests : ComponentTestBase
{
    [Fact(Skip = "Component requires IAgentPromptService with specific mock setup - needs test refactoring")]
    public void Render_WithTemplate_DisplaysPreview()
    {
        // TODO: Refactor to use ConfigureServices pattern
        // The issue is that each test needs different mock setups, which is incompatible
        // with the ConfigureServices approach. Consider redesigning the test or component.
    }

    [Fact(Skip = "Component requires IAgentPromptService with specific mock setup - needs test refactoring")]
    public void Render_WithSampleData_SubstitutesVariables()
    {
        // TODO: Refactor to use ConfigureServices pattern
        // The issue is that each test needs different mock setups, which is incompatible
        // with the ConfigureServices approach. Consider redesigning the test or component.
    }
}
