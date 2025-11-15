using Microsoft.AspNetCore.Components;
using PRFactory.Web.Models;

namespace PRFactory.Web.Components.Agents;

public partial class AgentMessage
{
    [Parameter, EditorRequired]
    public AgentChatMessage Message { get; set; } = null!;

    private string MessageClass => Message.Type switch
    {
        MessageType.UserMessage => "user",
        MessageType.AssistantMessage => "assistant",
        MessageType.ToolInvocation => "tool",
        MessageType.Reasoning => "reasoning",
        _ => "default"
    };

    private string RenderMarkdown(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
            return string.Empty;

        // Simple markdown rendering
        var result = markdown
            .Replace("\n", "<br />")
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");

        // Handle bold text (basic implementation)
        while (result.Contains("**"))
        {
            var firstIndex = result.IndexOf("**");
            var secondIndex = result.IndexOf("**", firstIndex + 2);
            if (secondIndex > firstIndex)
            {
                var before = result.Substring(0, firstIndex);
                var boldText = result.Substring(firstIndex + 2, secondIndex - firstIndex - 2);
                var after = result.Substring(secondIndex + 2);
                result = before + "<strong>" + boldText + "</strong>" + after;
            }
            else
            {
                break;
            }
        }

        // Handle code blocks (backticks)
        while (result.Contains("`"))
        {
            var firstIndex = result.IndexOf("`");
            var secondIndex = result.IndexOf("`", firstIndex + 1);
            if (secondIndex > firstIndex)
            {
                var before = result.Substring(0, firstIndex);
                var codeText = result.Substring(firstIndex + 1, secondIndex - firstIndex - 1);
                var after = result.Substring(secondIndex + 1);
                result = before + "<code>" + codeText + "</code>" + after;
            }
            else
            {
                break;
            }
        }

        return result;
    }
}
