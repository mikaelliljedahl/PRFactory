using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;
using PRFactory.Infrastructure.Agents.Base;

namespace PRFactory.Infrastructure.Agents.Planning;

/// <summary>
/// Database Architect agent that generates SQL DDL statements
/// for schema changes based on user stories.
/// </summary>
public class ArchitectDbSchemaAgent : BaseAgent
{
    private readonly ICliAgent _cliAgent;
    private readonly IArchitectureContextService _architectureContextService;

    public override string Name => "Architect DB Schema Agent";
    public override string Description => "Generates SQL DDL statements using Database Architect persona";

    public ArchitectDbSchemaAgent(
        ILogger<ArchitectDbSchemaAgent> logger,
        ICliAgent cliAgent,
        IArchitectureContextService architectureContextService)
        : base(logger)
    {
        _cliAgent = cliAgent ?? throw new ArgumentNullException(nameof(cliAgent));
        _architectureContextService = architectureContextService ?? throw new ArgumentNullException(nameof(architectureContextService));
    }

    protected override async Task<AgentResult> ExecuteAsync(
        AgentContext context,
        CancellationToken cancellationToken)
    {
        // Get user stories from previous agent
        var userStories = GetRequiredStateValue<string>(context, "UserStories");

        Logger.LogInformation("Generating database schema for ticket {TicketKey}", context.Ticket.TicketKey);

        try
        {
            // Build existing schema context using Epic 07 service
            var architecturePatterns = await _architectureContextService.GetArchitecturePatternsAsync(
                context.RepositoryPath!,
                cancellationToken);
            var techStack = _architectureContextService.GetTechnologyStack();
            var codeStyle = _architectureContextService.GetCodeStyleGuidelines();
            var codeSnippets = await _architectureContextService.GetRelevantCodeSnippetsAsync(
                context.RepositoryPath!,
                context.Ticket.Description,
                maxSnippets: 3,
                cancellationToken);

            var existingSchema = BuildDatabaseSchemaContext(architecturePatterns, techStack, codeStyle, codeSnippets);

            // Build prompt
            var prompt = BuildDatabaseSchemaPrompt(userStories, existingSchema);

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
                    Error = $"Database schema generation failed: {cliResponse.ErrorMessage}"
                };
            }

            // Extract and validate SQL
            var dbSchema = ExtractSqlContent(cliResponse.Content);
            ValidateSqlSyntax(dbSchema);

            // Store in context
            context.State["DatabaseSchema"] = dbSchema;

            Logger.LogInformation(
                "Database schema generated successfully for ticket {TicketKey}. Table count: {TableCount}",
                context.Ticket.TicketKey,
                CountTables(dbSchema));

            return new AgentResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object>
                {
                    ["DatabaseSchema"] = dbSchema,
                    ["TableCount"] = CountTables(dbSchema),
                    ["TokensUsed"] = cliResponse.Metadata.GetValueOrDefault("tokens_used", 0)
                }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to generate database schema for ticket {TicketKey}", context.Ticket.TicketKey);
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = $"Failed to generate database schema: {ex.Message}"
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

    private string BuildDatabaseSchemaContext(
        string architecturePatterns,
        string techStack,
        string codeStyle,
        List<CodeSnippet> codeSnippets)
    {
        var snippetText = string.Join("\n\n", codeSnippets.Select(s =>
            $"File: {s.FilePath}\n```{s.Language}\n{s.Code}\n```\n{s.Description}"));

        return $@"
<architecture_patterns>
{architecturePatterns}
</architecture_patterns>

<technology_stack>
{techStack}
</technology_stack>

<code_style_guidelines>
{codeStyle}
</code_style_guidelines>

<existing_schema_examples>
{snippetText}
</existing_schema_examples>
";
    }

    private string BuildDatabaseSchemaPrompt(string userStories, string existingSchema)
    {
        var promptBuilder = new StringBuilder();

        promptBuilder.AppendLine("You are a Database Architect designing database schema changes.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("<role>");
        promptBuilder.AppendLine("Your role is to:");
        promptBuilder.AppendLine("1. Analyze user stories to determine database requirements");
        promptBuilder.AppendLine("2. Generate SQL DDL statements (CREATE TABLE, ALTER TABLE, CREATE INDEX)");
        promptBuilder.AppendLine("3. Follow existing database schema patterns");
        promptBuilder.AppendLine("4. Define appropriate data types, constraints, and indexes");
        promptBuilder.AppendLine("5. Include foreign key relationships and cascading rules");
        promptBuilder.AppendLine("</role>");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("<user_stories>");
        promptBuilder.AppendLine(userStories);
        promptBuilder.AppendLine("</user_stories>");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("<existing_schema>");
        promptBuilder.AppendLine(existingSchema);
        promptBuilder.AppendLine("</existing_schema>");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Generate SQL DDL statements that:");
        promptBuilder.AppendLine("- Create new tables or alter existing tables as needed");
        promptBuilder.AppendLine("- Define appropriate primary keys, foreign keys, and indexes");
        promptBuilder.AppendLine("- Use proper data types (VARCHAR, INT, DATETIME, BOOLEAN, etc.)");
        promptBuilder.AppendLine("- Include NOT NULL constraints where appropriate");
        promptBuilder.AppendLine("- Add comments explaining the purpose of tables and columns");
        promptBuilder.AppendLine("- Follow existing naming conventions");
        promptBuilder.AppendLine("- Include migration considerations (e.g., default values for new columns)");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("IMPORTANT: Do NOT include DROP DATABASE statements. Only CREATE and ALTER statements.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Output ONLY the SQL DDL statements (no preamble or explanation).");

        return promptBuilder.ToString();
    }

    private string ExtractSqlContent(string cliResponse)
    {
        // Strategy 1: Look for SQL code block
        var sqlCodeBlockPattern = @"```sql\s*(.*?)\s*```";
        var match = Regex.Match(cliResponse, sqlCodeBlockPattern,
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

        // Strategy 3: Look for content starting with CREATE or ALTER
        var lines = cliResponse.Split('\n');
        var sqlLines = new List<string>();
        var foundStart = false;

        foreach (var line in lines)
        {
            var trimmedLine = line.TrimStart();
            if (trimmedLine.StartsWith("CREATE ", StringComparison.OrdinalIgnoreCase) ||
                trimmedLine.StartsWith("ALTER ", StringComparison.OrdinalIgnoreCase))
            {
                foundStart = true;
            }

            if (foundStart)
            {
                sqlLines.Add(line);
            }
        }

        if (sqlLines.Count > 0)
        {
            return string.Join("\n", sqlLines).Trim();
        }

        // Fallback: Use full response
        return cliResponse.Trim();
    }

    private void ValidateSqlSyntax(string sql)
    {
        // Basic validation
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new InvalidOperationException("Generated SQL schema is empty");
        }

        // Check for dangerous operations
        if (sql.Contains("DROP DATABASE", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("SQL contains dangerous DROP DATABASE statement");
        }

        if (sql.Contains("TRUNCATE", StringComparison.OrdinalIgnoreCase))
        {
            Logger.LogWarning("SQL contains TRUNCATE statement. This may be risky.");
        }

        // Ensure it contains CREATE or ALTER statements
        var hasValidStatements =
            sql.Contains("CREATE TABLE", StringComparison.OrdinalIgnoreCase) ||
            sql.Contains("ALTER TABLE", StringComparison.OrdinalIgnoreCase) ||
            sql.Contains("CREATE INDEX", StringComparison.OrdinalIgnoreCase);

        if (!hasValidStatements)
        {
            throw new InvalidOperationException("SQL schema missing CREATE/ALTER statements");
        }

        Logger.LogInformation("SQL DDL validation passed");
    }

    private int CountTables(string sql)
    {
        return Regex.Matches(sql, @"CREATE TABLE", RegexOptions.IgnoreCase).Count;
    }
}
