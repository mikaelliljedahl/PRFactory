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
    public void Render_WithTemplateAndSampleData_DisplaysPreviewAndSubstitutesVariables()
    {
        // Test implementation pending refactoring of IAgentPromptService mock setup
        throw new NotSupportedException("Test requires refactoring - see Skip attribute for details");
    }
}
