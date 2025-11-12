using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.LLM;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Agents.Messages;
using PRFactory.Infrastructure.Git;

namespace PRFactory.Infrastructure.Agents.Specialized;

/// <summary>
/// Specialized agent that performs AI code reviews on pull requests.
/// Fetches PR details, analyzes code changes, and provides structured feedback.
/// </summary>
public class CodeReviewAgent : BaseAgent
{
    private readonly ILlmProviderFactory _providerFactory;
    private readonly IPromptLoaderService _promptService;
    private readonly ICodeReviewResultRepository _reviewResultRepo;
    private readonly ITicketRepository _ticketRepo;
    private readonly IGitPlatformService _gitPlatformService;
    private readonly Guid? _llmProviderId;

    public CodeReviewAgent(
        ILogger<CodeReviewAgent> logger,
        ILlmProviderFactory providerFactory,
        IPromptLoaderService promptService,
        ICodeReviewResultRepository reviewResultRepo,
        ITicketRepository ticketRepo,
        IGitPlatformService gitPlatformService,
        Guid? llmProviderId = null)
        : base(logger)
    {
        _providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
        _promptService = promptService ?? throw new ArgumentNullException(nameof(promptService));
        _reviewResultRepo = reviewResultRepo ?? throw new ArgumentNullException(nameof(reviewResultRepo));
        _ticketRepo = ticketRepo ?? throw new ArgumentNullException(nameof(ticketRepo));
        _gitPlatformService = gitPlatformService ?? throw new ArgumentNullException(nameof(gitPlatformService));
        _llmProviderId = llmProviderId;
    }

    public override string Name => "code-review-agent";

    public override string Description => "Reviews pull requests for code quality, security, and best practices";

    protected override async Task<AgentResult> ExecuteAsync(
        AgentContext context,
        CancellationToken cancellationToken)
    {
        Logger.LogInformation(
            "Starting code review for ticket {TicketId}, PR #{PrNumber}",
            context.TicketId, context.PullRequestNumber);

        // Validate context
        if (context.PullRequestNumber == null)
        {
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = "PullRequestNumber is required for code review"
            };
        }

        if (string.IsNullOrEmpty(context.PullRequestUrl))
        {
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = "PullRequestUrl is required for code review"
            };
        }

        try
        {
            // 1. Get LLM provider (tenant-specific or default)
            var provider = await GetLlmProviderAsync(context);
            Logger.LogInformation("Using LLM provider: {Provider}", provider.ProviderName);

            // 2. Get implementation plan
            var planContent = await LoadImplementationPlanAsync(context, cancellationToken);

            // 3. Build template variables
            var templateVariables = await BuildTemplateVariablesAsync(context, planContent, cancellationToken);

            // 4. Render prompts
            var systemPrompt = _promptService.LoadPrompt(
                agentName: "code-review",
                providerName: provider.ProviderName.ToLowerInvariant(),
                promptType: "system");

            var userPrompt = _promptService.RenderTemplate(
                agentName: "code-review",
                providerName: provider.ProviderName.ToLowerInvariant(),
                promptType: "user_template",
                templateVariables: templateVariables);

            Logger.LogDebug("Prompts rendered successfully. System prompt length: {SystemLength}, User prompt length: {UserLength}",
                systemPrompt.Length, userPrompt.Length);

            // 5. Execute LLM review
            var response = await provider.SendMessageAsync(
                prompt: userPrompt,
                systemPrompt: systemPrompt,
                options: new LlmOptions
                {
                    Model = provider.SupportedModels.FirstOrDefault(),
                    MaxTokens = 8000,
                    Temperature = 0.3  // Lower temperature for more consistent reviews
                },
                ct: cancellationToken);

            if (!response.Success)
            {
                Logger.LogError("LLM review failed: {Error}", response.ErrorMessage);
                return new AgentResult
                {
                    Status = AgentStatus.Failed,
                    Error = $"LLM review failed: {response.ErrorMessage}"
                };
            }

            Logger.LogInformation("LLM review completed successfully. Response length: {Length} chars", response.Content.Length);

            // 6. Parse review response
            var (criticalIssues, suggestions, praise) = ParseReviewResponse(response.Content);

            Logger.LogInformation(
                "Review parsed: {CriticalCount} critical issues, {SuggestionCount} suggestions, {PraiseCount} praise items",
                criticalIssues.Count, suggestions.Count, praise.Count);

            // 7. Get retry count
            var retryAttempt = await _reviewResultRepo.GetRetryCountAsync(
                Guid.Parse(context.TicketId), cancellationToken);

            // 8. Store review results
            var reviewResult = new CodeReviewResult(
                ticketId: Guid.Parse(context.TicketId),
                pullRequestNumber: context.PullRequestNumber.Value,
                pullRequestUrl: context.PullRequestUrl,
                llmProviderName: provider.ProviderName,
                modelName: provider.SupportedModels.FirstOrDefault() ?? "unknown",
                criticalIssues: criticalIssues,
                suggestions: suggestions,
                praise: praise,
                fullReviewContent: response.Content,
                retryAttempt: retryAttempt + 1
            );

            await SaveReviewResultsAsync(reviewResult, cancellationToken);

            Logger.LogInformation(
                "Code review result saved with ID {ReviewId}. Passed: {Passed}",
                reviewResult.Id, reviewResult.Passed);

            // 9. Return result
            return new AgentResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object>
                {
                    ["ReviewId"] = reviewResult.Id,
                    ["HasCriticalIssues"] = !reviewResult.Passed,
                    ["CriticalIssues"] = criticalIssues,
                    ["Suggestions"] = suggestions,
                    ["Praise"] = praise,
                    ["ReviewContent"] = response.Content,
                    ["RetryAttempt"] = reviewResult.RetryAttempt
                }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Code review failed for ticket {TicketId}", context.TicketId);
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = ex.Message,
                ErrorDetails = ex.ToString()
            };
        }
    }

    /// <summary>
    /// Gets the appropriate LLM provider for this review
    /// </summary>
    private async Task<ILlmProvider> GetLlmProviderAsync(AgentContext context)
    {
        // If a specific provider ID was set for this agent, use it
        if (_llmProviderId.HasValue)
        {
            // TODO: Implement GetProviderByIdAsync when LlmProviderFactory is enhanced
            // For now, get default provider
            Logger.LogWarning("Specific provider ID requested but not yet implemented. Using default provider.");
        }

        // Use default provider from factory
        return await Task.FromResult(_providerFactory.CreateProvider("anthropic"));
    }

    /// <summary>
    /// Loads the implementation plan for context
    /// </summary>
    private async Task<string> LoadImplementationPlanAsync(AgentContext context, CancellationToken cancellationToken)
    {
        // For now, return from context if available
        if (!string.IsNullOrEmpty(context.ImplementationPlan))
        {
            return context.ImplementationPlan;
        }

        // TODO: Load from file system or database
        Logger.LogWarning("Implementation plan not found in context. Returning empty plan.");
        return "Implementation plan not available.";
    }

    /// <summary>
    /// Builds template variables for the code review prompt
    /// </summary>
    private async Task<object> BuildTemplateVariablesAsync(
        AgentContext context,
        string planContent,
        CancellationToken cancellationToken)
    {
        // Get ticket info
        var ticket = await _ticketRepo.GetByIdAsync(Guid.Parse(context.TicketId), cancellationToken);
        if (ticket == null)
        {
            throw new InvalidOperationException($"Ticket {context.TicketId} not found");
        }

        // Fetch real PR details from git platform
        PullRequestDetails? prDetails = null;
        if (context.PullRequestNumber.HasValue && !string.IsNullOrEmpty(context.RepositoryId))
        {
            try
            {
                if (Guid.TryParse(context.RepositoryId, out var repositoryGuid))
                {
                    prDetails = await _gitPlatformService.GetPullRequestDetailsAsync(
                        repositoryGuid,
                        context.PullRequestNumber.Value,
                        cancellationToken);

                    Logger.LogInformation(
                        "Fetched PR details: {FileCount} files, {Additions}+ {Deletions}-, {CommitCount} commits",
                        prDetails.FilesChangedCount, prDetails.LinesAdded, prDetails.LinesDeleted, prDetails.CommitsCount);
                }
                else
                {
                    Logger.LogWarning("Invalid repository ID format: {RepositoryId}", context.RepositoryId);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to fetch PR details for PR #{Number}. Using placeholder values.",
                    context.PullRequestNumber);
            }
        }

        // Map file changes to template format
        List<object> fileChanges;
        List<object> testFiles;

        if (prDetails?.FilesChanged != null)
        {
            var fileChangesList = prDetails.FilesChanged.Select(f => new
            {
                path = f.Path,
                status = f.Status,
                additions = f.Additions,
                deletions = f.Deletions,
                changes = f.Changes,
                language = DetectLanguage(f.Path),
                is_test = IsTestFile(f.Path)
            }).ToList();

            fileChanges = fileChangesList.Cast<object>().ToList();
            testFiles = fileChangesList.Where(f => f.is_test).Cast<object>().ToList();
        }
        else
        {
            fileChanges = new List<object>();
            testFiles = new List<object>();
        }

        // Build comprehensive template variables
        return new
        {
            // Ticket information
            ticket_number = ticket.TicketKey,
            ticket_title = ticket.Title,
            ticket_description = ticket.Description,
            ticket_url = ticket.ExternalTicketId ?? "",

            // Plan information
            plan_path = "", // TODO: Add plan path tracking to Ticket entity or workflow context
            plan_summary = ExtractPlanSummary(planContent),
            plan_content = planContent,

            // Pull request details
            pull_request_url = context.PullRequestUrl ?? "",
            pull_request_number = context.PullRequestNumber ?? 0,
            branch_name = context.ImplementationBranchName ?? "",
            target_branch = context.Repository?.DefaultBranch ?? "main",
            files_changed_count = prDetails?.FilesChangedCount ?? 0,
            lines_added = prDetails?.LinesAdded ?? 0,
            lines_deleted = prDetails?.LinesDeleted ?? 0,
            commits_count = prDetails?.CommitsCount ?? 0,

            // Code changes
            file_changes = fileChanges,

            // Codebase context
            codebase_structure = await GetCodebaseStructureAsync(context, cancellationToken),
            related_files = await GetRelatedFilesAsync(context, cancellationToken),

            // Testing information
            tests_added = testFiles.Count,
            test_coverage_percentage = 0.0,  // TODO: Calculate coverage from test execution
            test_files = testFiles,

            // Metadata
            repository_name = context.Repository?.Name ?? "",
            repository_url = context.Repository?.CloneUrl ?? "",
            author_name = "PRFactory Agent",
            created_at = DateTime.UtcNow.ToString("o")
        };
    }

    /// <summary>
    /// Extracts a summary from the implementation plan
    /// </summary>
    private string ExtractPlanSummary(string planContent)
    {
        if (string.IsNullOrEmpty(planContent))
            return "No plan available";

        // Extract first few lines as summary
        var lines = planContent.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Take(5)
            .ToList();

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Gets the codebase structure as a tree
    /// </summary>
    private async Task<string> GetCodebaseStructureAsync(AgentContext context, CancellationToken cancellationToken)
    {
        // TODO: Generate actual directory tree from repository
        await Task.CompletedTask;
        return "src/\n  Controllers/\n  Services/\n  Domain/\n  Infrastructure/";
    }

    /// <summary>
    /// Gets files related to the changes
    /// </summary>
    private async Task<List<object>> GetRelatedFilesAsync(AgentContext context, CancellationToken cancellationToken)
    {
        // TODO: Analyze changed files and find related files
        await Task.CompletedTask;
        return new List<object>();
    }

    /// <summary>
    /// Parses the LLM review response into structured format
    /// </summary>
    private (List<string> criticalIssues, List<string> suggestions, List<string> praise) ParseReviewResponse(string reviewContent)
    {
        var criticalIssues = new List<string>();
        var suggestions = new List<string>();
        var praise = new List<string>();

        // Parse the review content looking for sections
        // Expected format:
        // ## Critical Issues
        // - Issue 1
        // - Issue 2
        //
        // ## Suggestions
        // - Suggestion 1
        //
        // ## Praise
        // - Good thing 1

        var sections = SplitIntoSections(reviewContent);

        foreach (var (sectionName, sectionContent) in sections)
        {
            var items = ExtractListItems(sectionContent);

            if (sectionName.Contains("critical", StringComparison.OrdinalIgnoreCase) ||
                sectionName.Contains("issue", StringComparison.OrdinalIgnoreCase) ||
                sectionName.Contains("must fix", StringComparison.OrdinalIgnoreCase))
            {
                criticalIssues.AddRange(items);
            }
            else if (sectionName.Contains("suggestion", StringComparison.OrdinalIgnoreCase) ||
                     sectionName.Contains("recommendation", StringComparison.OrdinalIgnoreCase) ||
                     sectionName.Contains("improvement", StringComparison.OrdinalIgnoreCase))
            {
                suggestions.AddRange(items);
            }
            else if (sectionName.Contains("praise", StringComparison.OrdinalIgnoreCase) ||
                     sectionName.Contains("good", StringComparison.OrdinalIgnoreCase) ||
                     sectionName.Contains("well done", StringComparison.OrdinalIgnoreCase))
            {
                praise.AddRange(items);
            }
        }

        return (criticalIssues, suggestions, praise);
    }

    /// <summary>
    /// Splits review content into sections based on markdown headers
    /// </summary>
    private List<(string SectionName, string Content)> SplitIntoSections(string content)
    {
        var sections = new List<(string, string)>();
        var lines = content.Split('\n');

        string? currentSection = null;
        var currentContent = new List<string>();

        foreach (var line in lines)
        {
            // Check if this is a section header (## Header or **Header**)
            if (line.StartsWith("##") || (line.StartsWith("**") && line.EndsWith("**")))
            {
                // Save previous section
                if (currentSection != null)
                {
                    sections.Add((currentSection, string.Join("\n", currentContent)));
                }

                // Start new section
                currentSection = line.Replace("##", "").Replace("**", "").Trim();
                currentContent = new List<string>();
            }
            else if (currentSection != null)
            {
                currentContent.Add(line);
            }
        }

        // Add last section
        if (currentSection != null)
        {
            sections.Add((currentSection, string.Join("\n", currentContent)));
        }

        return sections;
    }

    /// <summary>
    /// Extracts list items from section content
    /// </summary>
    private List<string> ExtractListItems(string content)
    {
        var items = new List<string>();
        var lines = content.Split('\n');

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // Check for markdown list items (-, *, or numbered)
            if (trimmed.StartsWith("- ") || trimmed.StartsWith("* "))
            {
                items.Add(trimmed.Substring(2).Trim());
            }
            else if (Regex.IsMatch(trimmed, @"^\d+\.\s"))
            {
                // Numbered list item
                var match = Regex.Match(trimmed, @"^\d+\.\s(.+)$");
                if (match.Success)
                {
                    items.Add(match.Groups[1].Value.Trim());
                }
            }
        }

        return items;
    }

    /// <summary>
    /// Saves the review results to the database
    /// </summary>
    private async Task SaveReviewResultsAsync(CodeReviewResult reviewResult, CancellationToken cancellationToken)
    {
        await _reviewResultRepo.AddAsync(reviewResult, cancellationToken);
    }

    /// <summary>
    /// Detects the programming language from a file path
    /// </summary>
    private string DetectLanguage(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".cs" => "csharp",
            ".ts" => "typescript",
            ".tsx" => "tsx",
            ".js" => "javascript",
            ".jsx" => "jsx",
            ".py" => "python",
            ".java" => "java",
            ".go" => "go",
            ".rs" => "rust",
            ".rb" => "ruby",
            ".php" => "php",
            ".cpp" or ".cc" or ".cxx" => "cpp",
            ".c" => "c",
            ".h" or ".hpp" => "cpp",
            ".md" => "markdown",
            ".json" => "json",
            ".xml" => "xml",
            ".yaml" or ".yml" => "yaml",
            ".sql" => "sql",
            _ => "text"
        };
    }

    /// <summary>
    /// Checks if a file is a test file
    /// </summary>
    private bool IsTestFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath).ToLowerInvariant();
        var dirName = Path.GetDirectoryName(filePath)?.ToLowerInvariant() ?? "";

        return fileName.Contains("test") ||
               fileName.Contains("spec") ||
               dirName.Contains("test") ||
               dirName.Contains("tests") ||
               dirName.Contains("__tests__") ||
               dirName.Contains("spec");
    }
}
