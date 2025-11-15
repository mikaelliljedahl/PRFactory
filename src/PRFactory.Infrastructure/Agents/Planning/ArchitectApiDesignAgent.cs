using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Claude;
using YamlDotNet.Serialization;

namespace PRFactory.Infrastructure.Agents.Planning;

/// <summary>
/// Software Architect agent that generates OpenAPI 3.0 specifications
/// based on user stories and existing API patterns.
/// </summary>
public class ArchitectApiDesignAgent : BaseAgent
{
    private readonly ICliAgent _cliAgent;
    private readonly IContextBuilder _contextBuilder;

    public override string Name => "Architect API Design Agent";
    public override string Description => "Generates OpenAPI specification using Software Architect persona";

    public ArchitectApiDesignAgent(
        ILogger<ArchitectApiDesignAgent> logger,
        ICliAgent cliAgent,
        IContextBuilder contextBuilder)
        : base(logger)
    {
        _cliAgent = cliAgent ?? throw new ArgumentNullException(nameof(cliAgent));
        _contextBuilder = contextBuilder ?? throw new ArgumentNullException(nameof(contextBuilder));
    }

    protected override async Task<AgentResult> ExecuteAsync(
        AgentContext context,
        CancellationToken cancellationToken)
    {
        // Get user stories from previous agent
        var userStories = GetRequiredStateValue<string>(context, "UserStories");

        Logger.LogInformation("Generating API design for ticket {TicketKey}", context.Ticket.TicketKey);

        try
        {
            // Build codebase context (existing API patterns)
            var codebaseContext = await _contextBuilder.BuildApiDesignContextAsync(
                context.Repository,
                context.RepositoryPath!,
                cancellationToken);

            // Build prompt
            var prompt = BuildApiDesignPrompt(context, userStories, codebaseContext);

            // Call LLM
            var cliResponse = await _cliAgent.ExecuteWithProjectContextAsync(
                prompt,
                context.RepositoryPath!,
                cancellationToken);

            if (!cliResponse.Success)
            {
                return new AgentResult
                {
                    Status = AgentStatus.Failed,
                    Error = $"API design generation failed: {cliResponse.ErrorMessage}"
                };
            }

            // Extract and validate YAML
            var apiDesign = ExtractYamlContent(cliResponse.Content);
            ValidateOpenApiYaml(apiDesign);

            // Store in context
            context.State["ApiDesign"] = apiDesign;

            Logger.LogInformation(
                "API design generated successfully for ticket {TicketKey}. Endpoint count: {EndpointCount}",
                context.Ticket.TicketKey,
                CountEndpoints(apiDesign));

            return new AgentResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object>
                {
                    ["ApiDesign"] = apiDesign,
                    ["EndpointCount"] = CountEndpoints(apiDesign),
                    ["TokensUsed"] = cliResponse.Metadata.GetValueOrDefault("tokens_used", 0)
                }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to generate API design for ticket {TicketKey}", context.Ticket.TicketKey);
            throw;
        }
    }

    private T GetRequiredStateValue<T>(AgentContext context, string key)
    {
        if (!context.State.TryGetValue(key, out var value))
        {
            throw new InvalidOperationException($"{key} not found in context. Ensure previous agents executed successfully.");
        }

        if (value is not T typedValue)
        {
            throw new InvalidOperationException($"{key} is not of expected type {typeof(T).Name}");
        }

        return typedValue;
    }

    private string BuildApiDesignPrompt(AgentContext context, string userStories, string codebaseContext)
    {
        var promptBuilder = new StringBuilder();

        promptBuilder.AppendLine("You are a Software Architect designing RESTful API endpoints.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("<role>");
        promptBuilder.AppendLine("Your role is to:");
        promptBuilder.AppendLine("1. Design RESTful API endpoints based on user stories");
        promptBuilder.AppendLine("2. Follow existing API patterns in the codebase");
        promptBuilder.AppendLine("3. Generate OpenAPI 3.0 specification in YAML format");
        promptBuilder.AppendLine("4. Define request/response schemas with validation rules");
        promptBuilder.AppendLine("5. Include error handling and appropriate status codes");
        promptBuilder.AppendLine("</role>");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("<user_stories>");
        promptBuilder.AppendLine(userStories);
        promptBuilder.AppendLine("</user_stories>");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("<existing_api_patterns>");
        promptBuilder.AppendLine(codebaseContext);
        promptBuilder.AppendLine("</existing_api_patterns>");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Generate an OpenAPI 3.0 specification in YAML format that:");
        promptBuilder.AppendLine("- Follows RESTful best practices");
        promptBuilder.AppendLine("- Uses appropriate HTTP methods (GET, POST, PUT, DELETE, PATCH)");
        promptBuilder.AppendLine("- Includes detailed request and response schemas");
        promptBuilder.AppendLine("- Defines error responses (400, 401, 404, 500, etc.)");
        promptBuilder.AppendLine("- Matches existing API naming conventions");
        promptBuilder.AppendLine("- Includes descriptions for all endpoints and parameters");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Output ONLY the YAML content (no preamble or explanation).");

        return promptBuilder.ToString();
    }

    private string ExtractYamlContent(string cliResponse)
    {
        // Strategy 1: Look for YAML code block
        var yamlCodeBlockPattern = @"```yaml\s*(.*?)\s*```";
        var match = Regex.Match(cliResponse, yamlCodeBlockPattern,
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }

        // Strategy 2: Look for generic code block
        var codeBlockPattern = @"```\s*(.*?)\s*```";
        match = Regex.Match(cliResponse, codeBlockPattern,
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }

        // Strategy 3: Look for content starting with "openapi:"
        var lines = cliResponse.Split('\n');
        var yamlLines = new List<string>();
        var foundStart = false;

        foreach (var line in lines)
        {
            if (line.TrimStart().StartsWith("openapi:", StringComparison.OrdinalIgnoreCase))
            {
                foundStart = true;
            }

            if (foundStart)
            {
                yamlLines.Add(line);
            }
        }

        if (yamlLines.Count > 0)
        {
            return string.Join("\n", yamlLines).Trim();
        }

        // Fallback: Use full response
        return cliResponse.Trim();
    }

    private void ValidateOpenApiYaml(string yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml))
        {
            throw new InvalidOperationException("Generated API design is empty");
        }

        try
        {
            var deserializer = new DeserializerBuilder().Build();
            var yamlObject = deserializer.Deserialize<Dictionary<string, object>>(yaml);

            // Validate required OpenAPI 3.0 fields
            if (!yamlObject.ContainsKey("openapi"))
                throw new InvalidOperationException("Missing 'openapi' version field");

            if (!yamlObject.ContainsKey("info"))
                throw new InvalidOperationException("Missing 'info' field");

            if (!yamlObject.ContainsKey("paths"))
                throw new InvalidOperationException("Missing 'paths' field");

            // Validate version is 3.x
            var version = yamlObject["openapi"]?.ToString() ?? "";
            if (!version.StartsWith("3."))
                throw new InvalidOperationException($"Expected OpenAPI 3.x, got {version}");

            Logger.LogInformation("OpenAPI YAML validation passed");
        }
        catch (YamlDotNet.Core.YamlException ex)
        {
            throw new InvalidOperationException($"Invalid YAML syntax: {ex.Message}", ex);
        }
    }

    private int CountEndpoints(string apiDesign)
    {
        // Count path definitions (lines starting with "  /api/" or similar)
        var pathPattern = @"^\s{2,4}/[a-zA-Z]";
        return Regex.Matches(apiDesign, pathPattern, RegexOptions.Multiline).Count;
    }
}
