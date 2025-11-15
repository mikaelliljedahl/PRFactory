using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;
using PRFactory.Infrastructure.Agents.Base;

namespace PRFactory.Infrastructure.Agents.Planning;

/// <summary>
/// QA Engineer agent that generates comprehensive test cases
/// covering happy path, edge cases, and error scenarios.
/// </summary>
public class QaTestCasesAgent : BaseAgent
{
    private readonly ICliAgent _cliAgent;

    public override string Name => "QA Test Cases Agent";
    public override string Description => "Generates comprehensive test cases using QA Engineer persona";

    public QaTestCasesAgent(
        ILogger<QaTestCasesAgent> logger,
        ICliAgent cliAgent)
        : base(logger)
    {
        _cliAgent = cliAgent ?? throw new ArgumentNullException(nameof(cliAgent));
    }

    protected override async Task<AgentResult> ExecuteAsync(
        AgentContext context,
        CancellationToken cancellationToken)
    {
        // Get all previous artifacts
        var userStories = GetRequiredStateValue<string>(context, "UserStories");
        var apiDesign = GetRequiredStateValue<string>(context, "ApiDesign");
        var dbSchema = GetRequiredStateValue<string>(context, "DatabaseSchema");

        Logger.LogInformation("Generating test cases for ticket {TicketKey}", context.Ticket.TicketKey);

        try
        {
            // Build prompt
            var prompt = BuildTestCasesPrompt(userStories, apiDesign, dbSchema);

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
                    Error = $"Test case generation failed: {cliResponse.ErrorMessage}"
                };
            }

            // Extract and validate test cases
            var testCases = ExtractTestCases(cliResponse.Content);
            ValidateTestCasesFormat(testCases);

            // Store in context
            context.State["TestCases"] = testCases;

            Logger.LogInformation(
                "Test cases generated successfully for ticket {TicketKey}. Test case count: {TestCaseCount}",
                context.Ticket.TicketKey,
                CountTestCases(testCases));

            return new AgentResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object>
                {
                    ["TestCases"] = testCases,
                    ["TestCaseCount"] = CountTestCases(testCases),
                    ["TokensUsed"] = cliResponse.Metadata.GetValueOrDefault("tokens_used", 0)
                }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to generate test cases for ticket {TicketKey}", context.Ticket.TicketKey);
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = $"Failed to generate test cases: {ex.Message}"
            };
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

    private string BuildTestCasesPrompt(string userStories, string apiDesign, string dbSchema)
    {
        var promptBuilder = new StringBuilder();

        promptBuilder.AppendLine("You are a QA Engineer designing comprehensive test cases.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("<role>");
        promptBuilder.AppendLine("Your role is to:");
        promptBuilder.AppendLine("1. Generate comprehensive test cases covering all scenarios");
        promptBuilder.AppendLine("2. Cover happy path, edge cases, and error handling");
        promptBuilder.AppendLine("3. Reference API endpoints and database schema");
        promptBuilder.AppendLine("4. Include unit tests, integration tests, and end-to-end tests");
        promptBuilder.AppendLine("5. Define test data requirements and expected outcomes");
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
        promptBuilder.AppendLine("Generate test cases in the following format:");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("# Test Cases");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("## Unit Tests");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("### Test Case 1: [Test Name]");
        promptBuilder.AppendLine("**Category**: Unit Test");
        promptBuilder.AppendLine("**Priority**: High/Medium/Low");
        promptBuilder.AppendLine("**Description**: Brief description of what is being tested");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("**Preconditions**:");
        promptBuilder.AppendLine("- Precondition 1");
        promptBuilder.AppendLine("- Precondition 2");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("**Test Steps**:");
        promptBuilder.AppendLine("1. Step 1");
        promptBuilder.AppendLine("2. Step 2");
        promptBuilder.AppendLine("3. Step 3");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("**Expected Result**:");
        promptBuilder.AppendLine("- Expected outcome 1");
        promptBuilder.AppendLine("- Expected outcome 2");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("**Test Data**:");
        promptBuilder.AppendLine("- Input data required");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("## Integration Tests");
        promptBuilder.AppendLine("[Similar format for integration tests]");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("## Edge Cases and Error Handling");
        promptBuilder.AppendLine("[Test cases for edge cases, validation errors, security scenarios]");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("## Performance Tests");
        promptBuilder.AppendLine("[Test cases for load testing, stress testing, response time verification]");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Output ONLY the markdown content (no preamble or explanation).");

        return promptBuilder.ToString();
    }

    private string ExtractTestCases(string cliResponse)
    {
        // Strategy 1: Look for markdown heading
        var lines = cliResponse.Split('\n');
        var inContent = false;
        var contentLines = new List<string>();

        foreach (var line in lines)
        {
            if (line.TrimStart().StartsWith("# Test Cases", StringComparison.OrdinalIgnoreCase))
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

    private void ValidateTestCasesFormat(string testCases)
    {
        // Basic validation
        if (string.IsNullOrWhiteSpace(testCases))
        {
            throw new InvalidOperationException("Generated test cases are empty");
        }

        // Check for required sections
        if (!testCases.Contains("# Test Cases", StringComparison.OrdinalIgnoreCase))
        {
            Logger.LogWarning("Test cases missing main heading");
        }

        // Check for at least one test case
        var hasTestCases =
            testCases.Contains("### Test Case", StringComparison.OrdinalIgnoreCase) ||
            testCases.Contains("**Description**", StringComparison.OrdinalIgnoreCase);

        if (!hasTestCases)
        {
            throw new InvalidOperationException("No test cases found in expected format");
        }
    }

    private int CountTestCases(string testCases)
    {
        return Regex.Matches(testCases, @"### Test Case", RegexOptions.IgnoreCase).Count;
    }
}
