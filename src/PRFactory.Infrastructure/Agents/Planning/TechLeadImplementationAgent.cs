using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Claude;

namespace PRFactory.Infrastructure.Agents.Planning;

/// <summary>
/// Tech Lead agent that generates detailed implementation steps
/// with file-level guidance based on all previous planning artifacts.
/// </summary>
public class TechLeadImplementationAgent : BaseAgent
{
    private readonly ICliAgent _cliAgent;
    private readonly IContextBuilder _contextBuilder;

    public override string Name => "Tech Lead Implementation Agent";
    public override string Description => "Generates detailed implementation steps using Tech Lead persona";

    public TechLeadImplementationAgent(
        ILogger<TechLeadImplementationAgent> logger,
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
        // Get all previous artifacts
        var userStories = GetRequiredStateValue<string>(context, "UserStories");
        var apiDesign = GetRequiredStateValue<string>(context, "ApiDesign");
        var dbSchema = GetRequiredStateValue<string>(context, "DatabaseSchema");
        var testCases = GetRequiredStateValue<string>(context, "TestCases");

        Logger.LogInformation("Generating implementation steps for ticket {TicketKey}", context.Ticket.TicketKey);

        try
        {
            // Build comprehensive codebase context
            var codebaseContext = await _contextBuilder.BuildImplementationContextAsync(
                context.Ticket,
                context.RepositoryPath!);

            // Build prompt
            var prompt = BuildImplementationStepsPrompt(
                context, userStories, apiDesign, dbSchema, testCases, codebaseContext);

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
                    Error = $"Implementation steps generation failed: {cliResponse.ErrorMessage}"
                };
            }

            // Extract and validate implementation steps
            var implementationSteps = ExtractImplementationSteps(cliResponse.Content);
            ValidateImplementationStepsFormat(implementationSteps);

            // Store in context
            context.State["ImplementationSteps"] = implementationSteps;

            Logger.LogInformation(
                "Implementation steps generated successfully for ticket {TicketKey}. Step count: {StepCount}",
                context.Ticket.TicketKey,
                CountSteps(implementationSteps));

            return new AgentResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object>
                {
                    ["ImplementationSteps"] = implementationSteps,
                    ["StepCount"] = CountSteps(implementationSteps),
                    ["FileCount"] = CountMentionedFiles(implementationSteps),
                    ["TokensUsed"] = cliResponse.Metadata.GetValueOrDefault("tokens_used", 0)
                }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to generate implementation steps for ticket {TicketKey}", context.Ticket.TicketKey);
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

    private string BuildImplementationStepsPrompt(
        AgentContext context,
        string userStories,
        string apiDesign,
        string dbSchema,
        string testCases,
        string codebaseContext)
    {
        var promptBuilder = new StringBuilder();

        promptBuilder.AppendLine("You are a Tech Lead creating a detailed implementation plan.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("<role>");
        promptBuilder.AppendLine("Your role is to:");
        promptBuilder.AppendLine("1. Create step-by-step implementation guidance");
        promptBuilder.AppendLine("2. Specify which files to create, modify, or delete");
        promptBuilder.AppendLine("3. Follow existing codebase patterns and conventions");
        promptBuilder.AppendLine("4. Include database migration steps");
        promptBuilder.AppendLine("5. Reference the API design and test cases");
        promptBuilder.AppendLine("6. Provide code snippets for critical sections");
        promptBuilder.AppendLine("</role>");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("<user_stories>");
        promptBuilder.AppendLine(userStories);
        promptBuilder.AppendLine("</user_stories>");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("<api_design>");
        promptBuilder.AppendLine(apiDesign);
        promptBuilder.AppendLine("</api_design>");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("<database_schema>");
        promptBuilder.AppendLine(dbSchema);
        promptBuilder.AppendLine("</database_schema>");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("<test_cases>");
        promptBuilder.AppendLine(testCases);
        promptBuilder.AppendLine("</test_cases>");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("<codebase_context>");
        promptBuilder.AppendLine(codebaseContext);
        promptBuilder.AppendLine("</codebase_context>");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Generate implementation steps in the following format:");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("# Implementation Plan");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("## Overview");
        promptBuilder.AppendLine("Brief summary of the implementation approach");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("## Step 1: Database Migration");
        promptBuilder.AppendLine("**Files to modify/create**:");
        promptBuilder.AppendLine("- `src/Infrastructure/Migrations/YYYYMMDDHHMMSS_AddFeature.cs`");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("**Implementation details**:");
        promptBuilder.AppendLine("1. Create EF Core migration for new tables/columns");
        promptBuilder.AppendLine("2. Apply migration to update database schema");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("## Step 2: Domain Entities");
        promptBuilder.AppendLine("**Files to modify/create**:");
        promptBuilder.AppendLine("- `src/Domain/Entities/NewEntity.cs` (create)");
        promptBuilder.AppendLine("- `src/Domain/Entities/ExistingEntity.cs` (modify)");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("**Implementation details**:");
        promptBuilder.AppendLine("[Specific guidance on entity properties, relationships, validation]");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("## Step 3: Repository Layer");
        promptBuilder.AppendLine("[Repository interface and implementation]");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("## Step 4: Application Service Layer");
        promptBuilder.AppendLine("[Business logic implementation]");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("## Step 5: API Controllers");
        promptBuilder.AppendLine("[Controller endpoints matching the API design]");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("## Step 6: Unit Tests");
        promptBuilder.AppendLine("[Test files to create based on test cases]");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("## Step 7: Integration Tests");
        promptBuilder.AppendLine("[End-to-end test scenarios]");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("## Configuration Changes");
        promptBuilder.AppendLine("- Dependency injection registrations");
        promptBuilder.AppendLine("- appsettings.json updates (if needed)");
        promptBuilder.AppendLine("- Environment variable additions (if needed)");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Output ONLY the markdown content (no preamble or explanation).");

        return promptBuilder.ToString();
    }

    private string ExtractImplementationSteps(string cliResponse)
    {
        // Strategy 1: Look for markdown heading
        var lines = cliResponse.Split('\n');
        var inContent = false;
        var contentLines = new List<string>();

        foreach (var line in lines)
        {
            if (line.TrimStart().StartsWith("# Implementation Plan", StringComparison.OrdinalIgnoreCase) ||
                line.TrimStart().StartsWith("# Implementation Steps", StringComparison.OrdinalIgnoreCase))
            {
                inContent = true;
            }

            if (inContent)
            {
                contentLines.Add(line);
            }
        }

        if (contentLines.Count > 0)
        {
            return string.Join("\n", contentLines).Trim();
        }

        // Strategy 2: Look for markdown code block
        var codeBlockPattern = @"```markdown\s*(.*?)\s*```";
        var match = Regex.Match(cliResponse, codeBlockPattern,
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }

        // Fallback: Use full response
        return cliResponse.Trim();
    }

    private void ValidateImplementationStepsFormat(string implementationSteps)
    {
        // Basic validation
        if (string.IsNullOrWhiteSpace(implementationSteps))
        {
            throw new InvalidOperationException("Generated implementation steps are empty");
        }

        // Check for required sections
        var hasHeading =
            implementationSteps.Contains("# Implementation Plan", StringComparison.OrdinalIgnoreCase) ||
            implementationSteps.Contains("# Implementation Steps", StringComparison.OrdinalIgnoreCase);

        if (!hasHeading)
        {
            Logger.LogWarning("Implementation steps missing main heading");
        }

        // Check for at least one step
        var hasSteps =
            implementationSteps.Contains("## Step", StringComparison.OrdinalIgnoreCase) ||
            implementationSteps.Contains("## Overview", StringComparison.OrdinalIgnoreCase);

        if (!hasSteps)
        {
            throw new InvalidOperationException("No implementation steps found in expected format");
        }
    }

    private int CountSteps(string implementationSteps)
    {
        return Regex.Matches(implementationSteps, @"## Step \d+", RegexOptions.IgnoreCase).Count;
    }

    private int CountMentionedFiles(string implementationSteps)
    {
        // Count file paths mentioned in backticks
        var filePattern = @"`([^`]+\.(cs|sql|json|yaml|yml|md))`";
        return Regex.Matches(implementationSteps, filePattern, RegexOptions.IgnoreCase).Count;
    }
}
