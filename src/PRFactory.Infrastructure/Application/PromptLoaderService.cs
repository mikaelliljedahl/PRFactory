using System;
using System.IO;
using HandlebarsDotNet;
using Microsoft.Extensions.Configuration;
using PRFactory.Core.Application.LLM;

namespace PRFactory.Infrastructure.Application;

/// <summary>
/// Service for loading and rendering prompt templates.
/// Supports Handlebars template rendering with custom helpers.
/// </summary>
public class PromptLoaderService : IPromptLoaderService
{
    private readonly string _promptsBasePath;
    private readonly IHandlebars _handlebars;

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptLoaderService"/> class.
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    public PromptLoaderService(IConfiguration configuration)
    {
        _promptsBasePath = configuration["Prompts:BasePath"] ?? "/prompts";

        // Initialize Handlebars with custom helpers
        _handlebars = Handlebars.Create();
        RegisterCustomHelpers(_handlebars);
    }

    /// <summary>
    /// Load a prompt template file
    /// </summary>
    /// <param name="agentName">Agent name (e.g., "plan", "code-review", "implementation")</param>
    /// <param name="providerName">Provider name (e.g., "anthropic", "openai", "google")</param>
    /// <param name="promptType">Prompt type (e.g., "system", "user_template")</param>
    /// <returns>Prompt template content</returns>
    /// <exception cref="FileNotFoundException">If prompt template file is not found</exception>
    public string LoadPrompt(string agentName, string providerName, string promptType)
    {
        var path = Path.Combine(
            _promptsBasePath,
            agentName.ToLowerInvariant(),
            providerName.ToLowerInvariant(),
            $"{promptType}.txt");

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Prompt template not found: {path}");
        }

        return File.ReadAllText(path);
    }

    /// <summary>
    /// Render a Handlebars template with variables
    /// </summary>
    /// <param name="agentName">Agent name (e.g., "plan", "code-review", "implementation")</param>
    /// <param name="providerName">Provider name (e.g., "anthropic", "openai", "google")</param>
    /// <param name="promptType">Prompt type (e.g., "system", "user_template")</param>
    /// <param name="templateVariables">Variables to inject into template</param>
    /// <returns>Rendered prompt with variables substituted</returns>
    /// <exception cref="FileNotFoundException">If template file is not found</exception>
    public string RenderTemplate(
        string agentName,
        string providerName,
        string promptType,
        object templateVariables)
    {
        var templatePath = Path.Combine(
            _promptsBasePath,
            agentName.ToLowerInvariant(),
            providerName.ToLowerInvariant(),
            $"{promptType}.hbs");

        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Template not found: {templatePath}");
        }

        var templateContent = File.ReadAllText(templatePath);
        var template = _handlebars.Compile(templateContent);

        return template(templateVariables);
    }

    /// <summary>
    /// Register custom Handlebars helpers
    /// </summary>
    /// <param name="handlebars">Handlebars instance</param>
    private void RegisterCustomHelpers(IHandlebars handlebars)
    {
        // Custom helper: Format code with syntax highlighting markers
        // Usage: {{code "csharp" codeContent}}
        handlebars.RegisterHelper("code", (writer, context, parameters) =>
        {
            var language = parameters.Length > 0 ? parameters[0]?.ToString() ?? "text" : "text";
            var code = parameters.Length > 1 ? parameters[1]?.ToString() ?? "" : "";
            writer.WriteSafeString($"```{language}\n{code}\n```");
        });

        // Custom helper: Truncate long content
        // Usage: {{truncate longText 500}}
        handlebars.RegisterHelper("truncate", (writer, context, parameters) =>
        {
            var text = parameters.Length > 0 ? parameters[0]?.ToString() ?? "" : "";
            var maxLength = parameters.Length > 1 ? int.Parse(parameters[1]?.ToString() ?? "500") : 500;

            if (text.Length <= maxLength)
            {
                writer.WriteSafeString(text);
            }
            else
            {
                writer.WriteSafeString(text.Substring(0, maxLength) + "...");
            }
        });

        // Custom helper: Format file size
        // Usage: {{filesize bytesCount}}
        handlebars.RegisterHelper("filesize", (writer, context, parameters) =>
        {
            var bytes = parameters.Length > 0 ? long.Parse(parameters[0]?.ToString() ?? "0") : 0;
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            writer.WriteSafeString($"{len:0.##} {sizes[order]}");
        });
    }
}
