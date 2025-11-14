using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.LLM;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Interfaces;

namespace PRFactory.Infrastructure.Application;

/// <summary>
/// Implementation of plan validation service using AI
/// </summary>
public class PlanValidationService : IPlanValidationService
{
    private readonly IPlanService _planService;
    private readonly IAgentPromptTemplateRepository _promptTemplateRepo;
    private readonly ILlmProviderFactory _llmProviderFactory;
    private readonly ILogger<PlanValidationService> _logger;

    // Prompt template names (stored in database via AgentPromptTemplates)
    private const string SecurityCheckPrompt = "plan-security-check";
    private const string CompletenessCheckPrompt = "plan-completeness-check";
    private const string PerformanceCheckPrompt = "plan-performance-check";
    private const string CodeValidationPrompt = "code-plan-validation";

    public PlanValidationService(
        IPlanService planService,
        IAgentPromptTemplateRepository promptTemplateRepo,
        ILlmProviderFactory llmProviderFactory,
        ILogger<PlanValidationService> logger)
    {
        _planService = planService;
        _promptTemplateRepo = promptTemplateRepo;
        _llmProviderFactory = llmProviderFactory;
        _logger = logger;
    }

    public async Task<PlanValidationResult> ValidatePlanAsync(
        Guid ticketId,
        string checkType,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating plan for ticket {TicketId} with check type {CheckType}",
            ticketId, checkType);

        // Get plan content
        var plan = await _planService.GetPlanAsync(ticketId, cancellationToken);
        if (plan == null)
        {
            throw new InvalidOperationException($"No plan found for ticket {ticketId}");
        }

        // Select prompt template name based on check type
        var promptTemplateName = checkType.ToLowerInvariant() switch
        {
            "security" => SecurityCheckPrompt,
            "completeness" => CompletenessCheckPrompt,
            "performance" => PerformanceCheckPrompt,
            _ => throw new ArgumentException($"Unknown check type: {checkType}", nameof(checkType))
        };

        // Load prompt template from database
        var promptTemplate = await _promptTemplateRepo.GetByNameAsync(promptTemplateName, null, cancellationToken);
        if (promptTemplate == null)
        {
            throw new InvalidOperationException($"Prompt template '{promptTemplateName}' not found in database");
        }

        var prompt = promptTemplate.PromptContent.Replace("{plan_content}", plan.Content);

        // Get LLM provider
        var llmProvider = _llmProviderFactory.GetDefaultProvider();

        // Call LLM
        var response = await llmProvider.SendMessageAsync(prompt, ct: cancellationToken);

        if (!response.Success)
        {
            throw new InvalidOperationException($"LLM provider failed: {response.ErrorMessage}");
        }

        // Parse response
        var result = ParseValidationResponse(ticketId, checkType, response.Content);

        _logger.LogInformation(
            "Plan validation completed for ticket {TicketId}. Score: {Score}, Findings: {FindingCount}",
            ticketId, result.Score, result.Findings.Count);

        return result;
    }

    public async Task<PlanValidationResult> ValidatePlanWithPromptAsync(
        Guid ticketId,
        string customPrompt,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating plan for ticket {TicketId} with custom prompt", ticketId);

        // Get plan content
        var plan = await _planService.GetPlanAsync(ticketId, cancellationToken);
        if (plan == null)
        {
            throw new InvalidOperationException($"No plan found for ticket {ticketId}");
        }

        // Replace placeholder in custom prompt
        var prompt = customPrompt.Replace("{plan_content}", plan.Content);

        // Get LLM provider
        var llmProvider = _llmProviderFactory.GetDefaultProvider();

        // Call LLM
        var response = await llmProvider.SendMessageAsync(prompt, ct: cancellationToken);

        if (!response.Success)
        {
            throw new InvalidOperationException($"LLM provider failed: {response.ErrorMessage}");
        }

        // Parse response (generic parsing)
        var result = ParseValidationResponse(ticketId, "custom", response.Content);

        _logger.LogInformation(
            "Custom plan validation completed for ticket {TicketId}. Score: {Score}",
            ticketId, result.Score);

        return result;
    }

    public async Task<CodePlanAlignmentResult> ValidateCodeVsPlanAsync(
        Guid ticketId,
        string diffContent,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating code alignment for ticket {TicketId}", ticketId);

        // Get plan content
        var plan = await _planService.GetPlanAsync(ticketId, cancellationToken);
        if (plan == null)
        {
            throw new InvalidOperationException($"No plan found for ticket {ticketId}");
        }

        if (!plan.IsApproved)
        {
            throw new InvalidOperationException(
                $"Cannot validate code against unapproved plan for ticket {ticketId}");
        }

        // Load validation prompt template from database
        var promptTemplate = await _promptTemplateRepo.GetByNameAsync(CodeValidationPrompt, null, cancellationToken);
        if (promptTemplate == null)
        {
            throw new InvalidOperationException($"Prompt template '{CodeValidationPrompt}' not found in database");
        }

        var prompt = promptTemplate.PromptContent
            .Replace("{plan_artifacts}", plan.Content)
            .Replace("{code_diff}", diffContent);

        // Get LLM provider
        var llmProvider = _llmProviderFactory.GetDefaultProvider();

        // Call LLM
        var response = await llmProvider.SendMessageAsync(prompt, ct: cancellationToken);

        if (!response.Success)
        {
            throw new InvalidOperationException($"LLM provider failed: {response.ErrorMessage}");
        }

        // Parse response
        var result = ParseAlignmentResponse(ticketId, response.Content);

        _logger.LogInformation(
            "Code-plan alignment validation completed. Alignment score: {Score}",
            result.AlignmentScore);

        return result;
    }

    /// <summary>
    /// Parse validation response from LLM
    /// </summary>
    private PlanValidationResult ParseValidationResponse(
        Guid ticketId,
        string checkType,
        string response)
    {
        var result = new PlanValidationResult
        {
            TicketId = ticketId,
            CheckType = checkType,
            RawResponse = response,
            ValidatedAt = DateTime.UtcNow
        };

        // Extract score (look for patterns like "Score: 85" or "85/100")
        var scoreMatch = Regex.Match(response, @"(?:score|rating):\s*(\d+)", RegexOptions.IgnoreCase);
        if (scoreMatch.Success && int.TryParse(scoreMatch.Groups[1].Value, out var score))
        {
            result.Score = score;
        }
        else
        {
            // Default score based on findings count
            result.Score = 70; // Conservative default
        }

        // Extract risk level (for security checks)
        var riskMatch = Regex.Match(response,
            @"risk\s+level:\s*(Low|Medium|High|Critical)",
            RegexOptions.IgnoreCase);
        if (riskMatch.Success)
        {
            result.RiskLevel = riskMatch.Groups[1].Value;
        }

        // Extract findings (lines starting with - or bullets)
        var findingMatches = Regex.Matches(response, @"^[\s]*[-•]\s*(.+)$", RegexOptions.Multiline);
        foreach (Match match in findingMatches)
        {
            result.Findings.Add(match.Groups[1].Value.Trim());
        }

        // Extract recommendations (section after "Recommendations:" or "Mitigations:")
        var recommendationSection = Regex.Match(response,
            @"(?:Recommendations|Mitigations):\s*\n((?:[\s]*[-•]\s*.+\n?)+)",
            RegexOptions.IgnoreCase);
        if (recommendationSection.Success)
        {
            var recMatches = Regex.Matches(recommendationSection.Groups[1].Value,
                @"^[\s]*[-•]\s*(.+)$",
                RegexOptions.Multiline);
            foreach (Match match in recMatches)
            {
                result.Recommendations.Add(match.Groups[1].Value.Trim());
            }
        }

        return result;
    }

    /// <summary>
    /// Parse code-plan alignment response from LLM
    /// </summary>
    private CodePlanAlignmentResult ParseAlignmentResponse(Guid ticketId, string response)
    {
        var result = new CodePlanAlignmentResult
        {
            TicketId = ticketId,
            RawResponse = response,
            ValidatedAt = DateTime.UtcNow
        };

        // Extract alignment score
        var scoreMatch = Regex.Match(response, @"(?:score|alignment):\s*(\d+)", RegexOptions.IgnoreCase);
        if (scoreMatch.Success && int.TryParse(scoreMatch.Groups[1].Value, out var score))
        {
            result.AlignmentScore = score;
        }

        // Extract implemented requirements (✅ lines)
        var implementedMatches = Regex.Matches(response, @"✅\s*(.+)$", RegexOptions.Multiline);
        foreach (Match match in implementedMatches)
        {
            result.ImplementedRequirements.Add(match.Groups[1].Value.Trim());
        }

        // Extract missing requirements (❌ lines)
        var missingMatches = Regex.Matches(response, @"❌\s*(.+)$", RegexOptions.Multiline);
        foreach (Match match in missingMatches)
        {
            result.MissingRequirements.Add(match.Groups[1].Value.Trim());
        }

        // Extract deviations (⚠️ lines)
        var deviationMatches = Regex.Matches(response, @"⚠️\s*(.+)$", RegexOptions.Multiline);
        foreach (Match match in deviationMatches)
        {
            result.Deviations.Add(match.Groups[1].Value.Trim());
        }

        // Extract unplanned code (❓ lines)
        var unplannedMatches = Regex.Matches(response, @"❓\s*(.+)$", RegexOptions.Multiline);
        foreach (Match match in unplannedMatches)
        {
            result.UnplannedCode.Add(match.Groups[1].Value.Trim());
        }

        return result;
    }
}
