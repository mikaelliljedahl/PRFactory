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
    private static readonly string[] FileSizes = { "B", "KB", "MB", "GB" };

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
    private static void RegisterCustomHelpers(IHandlebars handlebars)
    {
        RegisterCodeHelper(handlebars);
        RegisterTruncateHelper(handlebars);
        RegisterFileSizeHelper(handlebars);
    }

    /// <summary>
    /// Register custom code formatting helper
    /// </summary>
    /// <param name="handlebars">Handlebars instance</param>
    private static void RegisterCodeHelper(IHandlebars handlebars)
    {
        // Custom helper: Format code with syntax highlighting markers
        // Usage: {{code "csharp" codeContent}}
        handlebars.RegisterHelper("code", (writer, context, parameters) =>
        {
            var language = parameters.Length > 0 ? parameters[0]?.ToString() ?? "text" : "text";
            var code = parameters.Length > 1 ? parameters[1]?.ToString() ?? "" : "";
            writer.WriteSafeString($"```{language}\n{code}\n```");
        });
    }

    /// <summary>
    /// Register custom text truncation helper
    /// </summary>
    /// <param name="handlebars">Handlebars instance</param>
    private static void RegisterTruncateHelper(IHandlebars handlebars)
    {
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
                var truncated = text.AsSpan(0, maxLength);
                writer.WriteSafeString($"{truncated}...");
            }
        });
    }

    /// <summary>
    /// Register custom file size formatting helper
    /// </summary>
    /// <param name="handlebars">Handlebars instance</param>
    private static void RegisterFileSizeHelper(IHandlebars handlebars)
    {
        // Custom helper: Format file size
        // Usage: {{filesize bytesCount}}
        handlebars.RegisterHelper("filesize", (writer, context, parameters) =>
        {
            var bytes = parameters.Length > 0 ? long.Parse(parameters[0]?.ToString() ?? "0") : 0;
            var formatted = FormatFileSize(bytes);
            writer.WriteSafeString(formatted);
        });
    }

    /// <summary>
    /// Format bytes into human-readable file size
    /// </summary>
    /// <param name="bytes">Number of bytes</param>
    /// <returns>Formatted file size string (e.g., "1.5 MB")</returns>
    private static string FormatFileSize(long bytes)
    {
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < FileSizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {FileSizes[order]}";
    }
}
