using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;
using PRFactory.Infrastructure.Claude.Models;
using CoreModels = PRFactory.Core.Application.Services;

namespace PRFactory.Infrastructure.Claude;

/// <summary>
/// Main service for Claude AI integration
/// Orchestrates codebase analysis, question generation, planning, and implementation
/// </summary>
public class ClaudeService : IClaudeService
{
    private readonly IClaudeClient _client;
    private readonly IContextBuilder _contextBuilder;
    private readonly IConversationHistoryRepository _historyRepo;
    private readonly ILogger<ClaudeService> _logger;

    public ClaudeService(
        IClaudeClient client,
        IContextBuilder contextBuilder,
        IConversationHistoryRepository historyRepo,
        ILogger<ClaudeService> logger)
    {
        _client = client;
        _contextBuilder = contextBuilder;
        _historyRepo = historyRepo;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<CoreModels.CodebaseAnalysis> AnalyzeCodebaseAsync(
        dynamic ticket,
        string repositoryPath,
        CancellationToken ct = default)
    {
        var ticketId = (string)ticket.Id;
        _logger.LogInformation("Starting codebase analysis for ticket {TicketId}", ticketId);

        var context = await _contextBuilder.BuildAnalysisContextAsync(ticket, repositoryPath);

        var messages = new List<Models.Message>
        {
            new("user", context)
        };

        var response = await _client.SendMessageAsync(
            PromptTemplates.ANALYSIS_SYSTEM_PROMPT,
            messages,
            maxTokens: 4000,
            ct
        );

        // Save conversation
        await _historyRepo.AddMessageAsync(
            ticket.Id.ToString(),
            "analysis",
            messages[0],
            response);

        // Parse response into CodebaseAnalysis
        var analysis = ParseAnalysisResponse(response);

        // Read relevant file contents
        var fileContents = new Dictionary<string, string>();
        foreach (var file in analysis.RelevantFiles.Take(20))
        {
            var fullPath = Path.Combine(repositoryPath, file);
            if (File.Exists(fullPath))
            {
                try
                {
                    fileContents[file] = await File.ReadAllTextAsync(fullPath, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read file {FilePath}", fullPath);
                }
            }
        }

        _logger.LogInformation(
            "Codebase analysis completed. Found {FileCount} relevant files",
            fileContents.Count);

        return analysis with { FileContents = fileContents };
    }

    /// <inheritdoc/>
    public async Task<List<CoreModels.Question>> GenerateQuestionsAsync(
        dynamic ticket,
        CoreModels.CodebaseAnalysis analysis,
        CancellationToken ct = default)
    {
        var ticketId = (string)ticket.Id;
        _logger.LogInformation("Generating questions for ticket {TicketId}", ticketId);

        var context = new StringBuilder();
        context.AppendLine("## Ticket");
        context.AppendLine($"**Title**: {ticket.Title}");
        context.AppendLine($"**Description**: {ticket.Description}");
        context.AppendLine();
        context.AppendLine("## Codebase Analysis Summary");
        context.AppendLine($"**Architecture**: {analysis.Architecture}");
        context.AppendLine($"**Relevant files**: {string.Join(", ", analysis.RelevantFiles.Take(5))}");

        if (analysis.Patterns.Any())
        {
            context.AppendLine($"**Patterns**: {string.Join(", ", analysis.Patterns.Take(3))}");
        }

        var messages = new List<Models.Message>
        {
            new("user", context.ToString())
        };

        var response = await _client.SendMessageAsync(
            PromptTemplates.QUESTIONS_SYSTEM_PROMPT,
            messages,
            maxTokens: 2000,
            ct
        );

        await _historyRepo.AddMessageAsync(
            ticket.Id.ToString(),
            "questions",
            messages[0],
            response);

        // Parse JSON response
        var questionsJson = ExtractJsonFromResponse(response);
        var questionDtos = JsonSerializer.Deserialize<List<QuestionDto>>(
            questionsJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var questions = questionDtos?.Select(q => new CoreModels.Question(
            Id: Guid.NewGuid().ToString(),
            Category: q.Category ?? "General",
            Text: q.Text ?? string.Empty,
            CreatedAt: DateTime.UtcNow
        )).ToList() ?? new List<CoreModels.Question>();

        _logger.LogInformation("Generated {QuestionCount} questions", questions.Count);

        return questions;
    }

    /// <inheritdoc/>
    public async Task<CoreModels.ImplementationPlan> GenerateImplementationPlanAsync(
        dynamic ticket,
        CancellationToken ct = default)
    {
        var ticketId = (string)ticket.Id;
        _logger.LogInformation("Generating implementation plan for ticket {TicketId}", ticketId);

        // Get conversation history
        var history = await _historyRepo.GetConversationAsync(ticket.Id.ToString());

        // Get the analysis from history
        var analysisResponse = await _historyRepo.GetLastResponseAsync(
            ticket.Id.ToString(),
            "analysis");

        CoreModels.CodebaseAnalysis analysis;
        if (analysisResponse != null)
        {
            analysis = ParseAnalysisResponse(analysisResponse);
        }
        else
        {
            // Fallback to empty analysis
            analysis = new CoreModels.CodebaseAnalysis(
                new List<string>(),
                new Dictionary<string, string>(),
                "Unknown",
                new List<string>(),
                new List<string>()
            );
        }

        // Build planning context
        var context = await _contextBuilder.BuildPlanningContextAsync(ticket, analysis);

        var messages = history.ToList();
        messages.Add(new Models.Message(
            "user",
            $"Now create a detailed implementation plan.\n\n{context}"));

        var response = await _client.SendMessageAsync(
            PromptTemplates.PLANNING_SYSTEM_PROMPT,
            messages,
            8000,
            ct
        );

        await _historyRepo.AddMessageAsync(
            ticket.Id.ToString(),
            "planning",
            messages.Last(),
            response);

        // Split response into sections
        var sections = SplitPlanSections(response);

        var plan = new CoreModels.ImplementationPlan(
            MainPlan: response,
            AffectedFiles: sections.GetValueOrDefault("Files to Modify", "")
                + "\n" + sections.GetValueOrDefault("Files to Create", ""),
            TestStrategy: sections.GetValueOrDefault("Testing Strategy", ""),
            EstimatedComplexity: ExtractComplexity(
                sections.GetValueOrDefault("Estimated Complexity", "3"))
        );

        _logger.LogInformation(
            "Implementation plan generated. Complexity: {Complexity}",
            plan.EstimatedComplexity);

        return plan;
    }

    /// <inheritdoc/>
    public async Task<CoreModels.CodeImplementation> ImplementCodeAsync(
        dynamic ticket,
        string repositoryPath,
        CancellationToken ct = default)
    {
        var ticketId = (string)ticket.Id;
        _logger.LogInformation("Implementing code for ticket {TicketId}", ticketId);

        var context = await _contextBuilder.BuildImplementationContextAsync(ticket, repositoryPath);
        var history = await _historyRepo.GetConversationAsync(ticket.Id.ToString());

        var messages = history.ToList();
        messages.Add(new Models.Message(
            "user",
            $"Implement the approved plan.\n\n{context}"));

        var response = await _client.SendMessageAsync(
            PromptTemplates.IMPLEMENTATION_SYSTEM_PROMPT,
            messages,
            16000,
            ct
        );

        await _historyRepo.AddMessageAsync(
            ticket.Id.ToString(),
            "implementation",
            messages.Last(),
            response);

        // Parse JSON response with file changes
        var fileChangesJson = ExtractJsonFromResponse(response);
        var fileChanges = JsonSerializer.Deserialize<List<FileChangeDto>>(
            fileChangesJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var modifiedFiles = fileChanges
            ?.ToDictionary(fc => fc.Path ?? "", fc => fc.Content ?? "")
            ?? new Dictionary<string, string>();

        var createdFiles = fileChanges
            ?.Where(fc => fc.Action?.ToLower() == "create")
            .Select(fc => fc.Path ?? "")
            .ToList() ?? new List<string>();

        _logger.LogInformation(
            "Code implementation completed. Modified {ModifiedCount} files, created {CreatedCount} files",
            modifiedFiles.Count, createdFiles.Count);

        return new CoreModels.CodeImplementation(
            ModifiedFiles: modifiedFiles,
            CreatedFiles: createdFiles,
            Summary: $"Implementation completed: {modifiedFiles.Count} files modified, {createdFiles.Count} files created"
        );
    }

    #region Response Parsing Helpers

    /// <summary>
    /// Parse analysis response to extract relevant information
    /// </summary>
    private CoreModels.CodebaseAnalysis ParseAnalysisResponse(string response)
    {
        var relevantFiles = new List<string>();
        var patterns = new List<string>();
        var dependencies = new List<string>();
        var architecture = "Unknown";

        // Extract file paths (look for common file extensions)
        var fileMatches = Regex.Matches(
            response,
            @"[a-zA-Z0-9_\-/\.]+\.(cs|js|ts|tsx|jsx|py|java|go|rs|cpp|h|yml|yaml|json|xml)",
            RegexOptions.IgnoreCase);

        relevantFiles.AddRange(
            fileMatches.Select(m => m.Value)
                .Distinct()
                .Where(f => !f.Contains("node_modules") && !f.Contains("bin/"))
                .Take(50));

        // Try to extract architecture style
        var archPatterns = new[]
        {
            "Clean Architecture", "MVC", "MVVM", "Microservices",
            "Layered Architecture", "Hexagonal", "Onion Architecture"
        };

        foreach (var pattern in archPatterns)
        {
            if (response.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                architecture = pattern;
                break;
            }
        }

        // Extract patterns mentioned
        var patternKeywords = new[]
        {
            "Repository Pattern", "Dependency Injection", "Factory Pattern",
            "Singleton", "Observer Pattern", "Strategy Pattern"
        };

        patterns.AddRange(
            patternKeywords.Where(p =>
                response.Contains(p, StringComparison.OrdinalIgnoreCase)));

        return new CoreModels.CodebaseAnalysis(
            RelevantFiles: relevantFiles,
            FileContents: new Dictionary<string, string>(),
            Architecture: architecture,
            Patterns: patterns,
            Dependencies: dependencies
        );
    }

    /// <summary>
    /// Extract JSON from response (handles markdown code blocks)
    /// </summary>
    private string ExtractJsonFromResponse(string response)
    {
        // Try to extract JSON from markdown code block
        var match = Regex.Match(
            response,
            @"```json\s*([\s\S]*?)\s*```",
            RegexOptions.Singleline);

        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }

        // Try to find JSON array or object directly
        match = Regex.Match(
            response,
            @"(\[[\s\S]*\]|\{[\s\S]*\})",
            RegexOptions.Singleline);

        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }

        // Fallback: return the whole response
        return response;
    }

    /// <summary>
    /// Split plan into sections based on markdown headers
    /// </summary>
    private Dictionary<string, string> SplitPlanSections(string plan)
    {
        var sections = new Dictionary<string, string>();
        var matches = Regex.Matches(
            plan,
            @"##\s*(\d+\.\s*)?(.+?)\s*\n(.*?)(?=##|\z)",
            RegexOptions.Singleline);

        foreach (Match match in matches)
        {
            var title = match.Groups[2].Value.Trim();
            var content = match.Groups[3].Value.Trim();
            sections[title] = content;
        }

        return sections;
    }

    /// <summary>
    /// Extract complexity rating from text
    /// </summary>
    private int ExtractComplexity(string complexitySection)
    {
        var match = Regex.Match(complexitySection, @"(\d)");
        if (match.Success && int.TryParse(match.Groups[1].Value, out var complexity))
        {
            return Math.Clamp(complexity, 1, 5);
        }
        return 3; // Default to medium complexity
    }

    #endregion
}

#region DTOs for JSON Parsing

/// <summary>
/// DTO for parsing question JSON
/// </summary>
internal record QuestionDto(string? Category, string? Text);

/// <summary>
/// DTO for parsing file change JSON
/// </summary>
internal record FileChangeDto(string? Action, string? Path, string? Content);

#endregion
