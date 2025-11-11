namespace PRFactory.Core.Application.LLM;

/// <summary>
/// Service for loading and rendering prompt templates.
/// Supports different prompts for different LLM providers and agent types.
/// </summary>
public interface IPromptLoaderService
{
    /// <summary>
    /// Load a prompt template file
    /// </summary>
    /// <param name="agentName">Agent name (e.g., "plan", "code-review", "implementation")</param>
    /// <param name="providerName">Provider name (e.g., "anthropic", "openai", "google")</param>
    /// <param name="promptType">Prompt type (e.g., "system", "user_template")</param>
    /// <returns>Prompt template content</returns>
    string LoadPrompt(string agentName, string providerName, string promptType);

    /// <summary>
    /// Render a Handlebars template with variables
    /// </summary>
    /// <param name="agentName">Agent name (e.g., "plan", "code-review", "implementation")</param>
    /// <param name="providerName">Provider name (e.g., "anthropic", "openai", "google")</param>
    /// <param name="promptType">Prompt type (e.g., "system", "user_template")</param>
    /// <param name="templateVariables">Variables to inject into template</param>
    /// <returns>Rendered prompt with variables substituted</returns>
    string RenderTemplate(
        string agentName,
        string providerName,
        string promptType,
        object templateVariables);
}
