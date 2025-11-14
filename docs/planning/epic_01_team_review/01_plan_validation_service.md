# Implementation Plan: Plan Validation Service

**Feature:** AI-powered plan validation with security, completeness, and performance checks
**Priority:** P1 (Highest Value)
**Estimated Effort:** 3-5 days
**Dependencies:** Existing agent infrastructure, prompt loading system

---

## Overview

Create a service that uses AI (Claude) to validate implementation plans against various criteria:
- Security vulnerabilities and risks
- Completeness of requirements and edge cases
- Performance considerations
- Code-vs-plan alignment validation

---

## Architecture

### Service Layer

```
┌─────────────────────────────────────────────────────────────┐
│                 IPlanValidationService                       │
│         (PRFactory.Core/Application/Services/)               │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│              PlanValidationService                           │
│       (PRFactory.Infrastructure/Application/)                │
│                                                               │
│  Dependencies:                                               │
│  - IPlanService (read plan content)                          │
│  - IAgentPromptService (load validation prompts)             │
│  - ILlmProvider (call Claude API)                            │
│  - ILogger (logging)                                         │
└─────────────────────────────────────────────────────────────┘
```

---

## Files to Create

### 1. Core Interface

**File:** `/src/PRFactory.Core/Application/Services/IPlanValidationService.cs`

```csharp
namespace PRFactory.Core.Application.Services;

/// <summary>
/// Service for validating implementation plans using AI analysis
/// </summary>
public interface IPlanValidationService
{
    /// <summary>
    /// Validate a plan using a predefined check type
    /// </summary>
    /// <param name="ticketId">Ticket ID containing the plan</param>
    /// <param name="checkType">Type of validation: security, completeness, performance</param>
    /// <returns>Validation result with score and findings</returns>
    Task<PlanValidationResult> ValidatePlanAsync(Guid ticketId, string checkType);

    /// <summary>
    /// Validate a plan using a custom prompt
    /// </summary>
    /// <param name="ticketId">Ticket ID containing the plan</param>
    /// <param name="customPrompt">Custom validation prompt</param>
    /// <returns>Validation result</returns>
    Task<PlanValidationResult> ValidatePlanWithPromptAsync(Guid ticketId, string customPrompt);

    /// <summary>
    /// Validate code implementation against approved plan
    /// </summary>
    /// <param name="ticketId">Ticket ID with approved plan</param>
    /// <param name="diffContent">Git diff of code changes</param>
    /// <returns>Alignment validation result</returns>
    Task<CodePlanAlignmentResult> ValidateCodeVsPlanAsync(Guid ticketId, string diffContent);
}

/// <summary>
/// Result of plan validation
/// </summary>
public class PlanValidationResult
{
    public Guid TicketId { get; set; }
    public string CheckType { get; set; } = string.Empty;
    public int Score { get; set; } // 0-100
    public string? RiskLevel { get; set; } // Low, Medium, High, Critical (for security)
    public List<string> Findings { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public string RawResponse { get; set; } = string.Empty;
    public DateTime ValidatedAt { get; set; }
}

/// <summary>
/// Result of code-vs-plan alignment validation
/// </summary>
public class CodePlanAlignmentResult
{
    public Guid TicketId { get; set; }
    public int AlignmentScore { get; set; } // 0-100
    public List<string> ImplementedRequirements { get; set; } = new();
    public List<string> MissingRequirements { get; set; } = new();
    public List<string> Deviations { get; set; } = new();
    public List<string> UnplannedCode { get; set; } = new();
    public string RawResponse { get; set; } = string.Empty;
    public DateTime ValidatedAt { get; set; }
}
```

**Lines of Code:** ~80 lines

---

### 2. Infrastructure Implementation

**File:** `/src/PRFactory.Infrastructure/Application/PlanValidationService.cs`

```csharp
using PRFactory.Core.Application.Services;
using PRFactory.Infrastructure.Agents.Services;
using PRFactory.Infrastructure.LLM;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace PRFactory.Infrastructure.Application;

/// <summary>
/// Implementation of plan validation service using AI
/// </summary>
public class PlanValidationService : IPlanValidationService
{
    private readonly IPlanService _planService;
    private readonly IAgentPromptService _promptService;
    private readonly ILlmProvider _llmProvider;
    private readonly ILogger<PlanValidationService> _logger;

    private const string SecurityCheckPrompt = "review/anthropic/plan_security_check.txt";
    private const string CompletenessCheckPrompt = "review/anthropic/plan_completeness_check.txt";
    private const string PerformanceCheckPrompt = "review/anthropic/plan_performance_check.txt";
    private const string CodeValidationPrompt = "review/anthropic/code_plan_validation.txt";

    public PlanValidationService(
        IPlanService planService,
        IAgentPromptService promptService,
        ILlmProvider llmProvider,
        ILogger<PlanValidationService> logger)
    {
        _planService = planService;
        _promptService = promptService;
        _llmProvider = llmProvider;
        _logger = logger;
    }

    public async Task<PlanValidationResult> ValidatePlanAsync(Guid ticketId, string checkType)
    {
        _logger.LogInformation("Validating plan for ticket {TicketId} with check type {CheckType}",
            ticketId, checkType);

        // Get plan content
        var plan = await _planService.GetPlanAsync(ticketId);
        if (plan == null)
        {
            throw new InvalidOperationException($"No plan found for ticket {ticketId}");
        }

        // Select prompt based on check type
        var promptPath = checkType.ToLowerInvariant() switch
        {
            "security" => SecurityCheckPrompt,
            "completeness" => CompletenessCheckPrompt,
            "performance" => PerformanceCheckPrompt,
            _ => throw new ArgumentException($"Unknown check type: {checkType}")
        };

        // Load prompt template
        var promptTemplate = await _promptService.LoadPromptAsync(promptPath);
        var prompt = promptTemplate.Replace("{plan_content}", plan.Content);

        // Call LLM
        var response = await _llmProvider.SendMessageAsync(prompt);

        // Parse response
        var result = ParseValidationResponse(ticketId, checkType, response.Content);

        _logger.LogInformation(
            "Plan validation completed for ticket {TicketId}. Score: {Score}, Findings: {FindingCount}",
            ticketId, result.Score, result.Findings.Count);

        return result;
    }

    public async Task<PlanValidationResult> ValidatePlanWithPromptAsync(
        Guid ticketId,
        string customPrompt)
    {
        _logger.LogInformation("Validating plan for ticket {TicketId} with custom prompt", ticketId);

        // Get plan content
        var plan = await _planService.GetPlanAsync(ticketId);
        if (plan == null)
        {
            throw new InvalidOperationException($"No plan found for ticket {ticketId}");
        }

        // Replace placeholder in custom prompt
        var prompt = customPrompt.Replace("{plan_content}", plan.Content);

        // Call LLM
        var response = await _llmProvider.SendMessageAsync(prompt);

        // Parse response (generic parsing)
        var result = ParseValidationResponse(ticketId, "custom", response.Content);

        _logger.LogInformation(
            "Custom plan validation completed for ticket {TicketId}. Score: {Score}",
            ticketId, result.Score);

        return result;
    }

    public async Task<CodePlanAlignmentResult> ValidateCodeVsPlanAsync(
        Guid ticketId,
        string diffContent)
    {
        _logger.LogInformation("Validating code alignment for ticket {TicketId}", ticketId);

        // Get plan content
        var plan = await _planService.GetPlanAsync(ticketId);
        if (plan == null)
        {
            throw new InvalidOperationException($"No plan found for ticket {ticketId}");
        }

        if (!plan.IsApproved)
        {
            throw new InvalidOperationException(
                $"Cannot validate code against unapproved plan for ticket {ticketId}");
        }

        // Load validation prompt
        var promptTemplate = await _promptService.LoadPromptAsync(CodeValidationPrompt);
        var prompt = promptTemplate
            .Replace("{plan_artifacts}", plan.Content)
            .Replace("{code_diff}", diffContent);

        // Call LLM
        var response = await _llmProvider.SendMessageAsync(prompt);

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
```

**Lines of Code:** ~250 lines

---

### 3. Prompt Templates

Create directory: `/prompts/review/anthropic/`

**File 1:** `plan_security_check.txt`

```
You are a security expert reviewing an implementation plan.

Your task is to analyze the provided implementation plan and identify potential security vulnerabilities, risks, and gaps in security controls.

Analyze the plan for:
1. Security vulnerabilities or risks (SQL injection, XSS, authentication bypass, etc.)
2. Missing authentication and authorization checks
3. Data validation gaps (input validation, sanitization)
4. Exposure of sensitive information (credentials, PII, tokens)
5. OWASP Top 10 concerns
6. Missing security logging and monitoring
7. Encryption requirements (data at rest, data in transit)
8. Secure coding practices

Implementation Plan:
{plan_content}

Provide your assessment in the following format:

Risk Level: [Low | Medium | High | Critical]

Findings:
- [List each security issue found]
- [Be specific about the vulnerability and where it occurs]

Recommendations:
- [List specific mitigations for each finding]
- [Include references to security best practices]

Score: [0-100, where 100 = no security issues found]
```

**Lines of Code:** ~30 lines

---

**File 2:** `plan_completeness_check.txt`

```
You are a technical architect reviewing an implementation plan for completeness.

Your task is to analyze the provided implementation plan and identify gaps, missing requirements, and areas that need more detail.

Analyze the plan for:
1. Missing functional requirements or features
2. Gaps in error handling and edge cases
3. Incomplete test coverage (unit, integration, E2E tests)
4. Undefined API contracts or data models
5. Missing database migrations or schema changes
6. Missing deployment steps or infrastructure requirements
7. Incomplete documentation or code comments
8. Missing performance considerations
9. Gaps in monitoring and logging

Implementation Plan:
{plan_content}

Provide your assessment in the following format:

Completeness Score: [0-100, where 100 = fully complete plan]

Findings:
- [List each gap or missing requirement]
- [Be specific about what's missing and why it matters]

Recommendations:
- [List specific additions needed to fill each gap]
- [Prioritize recommendations by importance]

Summary:
[Brief 2-3 sentence summary of overall completeness]
```

**Lines of Code:** ~35 lines

---

**File 3:** `plan_performance_check.txt`

```
You are a performance engineering expert reviewing an implementation plan.

Your task is to analyze the provided implementation plan and identify potential performance bottlenecks, scalability issues, and optimization opportunities.

Analyze the plan for:
1. Database query optimization (N+1 queries, missing indexes, inefficient joins)
2. Caching strategy (missing caching, cache invalidation)
3. API design for performance (pagination, filtering, batch operations)
4. Resource-intensive operations (file processing, large data sets)
5. Scalability concerns (horizontal/vertical scaling, load balancing)
6. Asynchronous processing opportunities (background jobs, queues)
7. Network calls and external API usage
8. Memory management and resource cleanup

Implementation Plan:
{plan_content}

Provide your assessment in the following format:

Performance Risk Level: [Low | Medium | High | Critical]

Findings:
- [List each performance concern or bottleneck]
- [Be specific about the impact and where it occurs]

Recommendations:
- [List specific optimizations for each finding]
- [Include references to performance best practices]

Score: [0-100, where 100 = excellent performance design]
```

**Lines of Code:** ~35 lines

---

**File 4:** `code_plan_validation.txt`

```
You are a meticulous code reviewer validating that code implementation matches an approved implementation plan.

Your task is to compare the code changes (git diff) against the approved plan and verify alignment.

Check for:
1. All planned requirements are implemented
2. No missing functionality from the plan
3. No significant deviations from the planned approach
4. No code written that was not specified in the plan
5. Implementation matches the planned architecture and design patterns

Approved Implementation Plan:
{plan_artifacts}

Code Changes (Git Diff):
{code_diff}

Provide your validation results in the following format:

✅ Requirements Successfully Implemented:
- [List each requirement from plan that is correctly implemented]
- [Reference specific code changes for each]

❌ Requirements Missing or Incomplete:
- [List each requirement from plan that is NOT implemented or incomplete]
- [Explain what's missing]

⚠️ Deviations from Plan:
- [List code that deviates from the planned approach]
- [Explain the deviation and why it might be problematic]

❓ Code Not Specified in Plan:
- [List code written that was NOT in the original plan]
- [This isn't always bad, but should be noted]

Overall Alignment Score: [0-100, where 100 = perfect alignment]

Summary:
[2-3 sentence summary of alignment quality]
```

**Lines of Code:** ~40 lines

---

## Service Registration

**File to Modify:** `/src/PRFactory.Infrastructure/DependencyInjection.cs`

Add to `AddInfrastructure()` method:

```csharp
// Plan Validation Service
services.AddScoped<IPlanValidationService, PlanValidationService>();
```

---

## Web Service Integration

**File to Modify:** `/src/PRFactory.Web/Services/ITicketService.cs`

Add methods:

```csharp
/// <summary>
/// Validate plan using AI analysis
/// </summary>
Task<PlanValidationResult> ValidatePlanAsync(Guid ticketId, string checkType);

/// <summary>
/// Validate plan with custom prompt
/// </summary>
Task<PlanValidationResult> ValidatePlanWithCustomPromptAsync(Guid ticketId, string customPrompt);
```

**File to Modify:** `/src/PRFactory.Web/Services/TicketService.cs`

Add implementation:

```csharp
private readonly IPlanValidationService _planValidationService;

// Update constructor to inject IPlanValidationService

public async Task<PlanValidationResult> ValidatePlanAsync(Guid ticketId, string checkType)
{
    return await _planValidationService.ValidatePlanAsync(ticketId, checkType);
}

public async Task<PlanValidationResult> ValidatePlanWithCustomPromptAsync(
    Guid ticketId,
    string customPrompt)
{
    return await _planValidationService.ValidatePlanWithPromptAsync(ticketId, customPrompt);
}
```

---

## Blazor Component Integration

**File to Create:** `/src/PRFactory.Web/Components/Plans/PlanValidationPanel.razor`

```razor
@namespace PRFactory.Web.Components.Plans
@using PRFactory.Core.Application.Services

<div class="card mb-3">
    <div class="card-header bg-info text-white">
        <h5 class="mb-0">
            <i class="bi bi-shield-check me-2"></i>
            Plan Validation
        </h5>
    </div>
    <div class="card-body">
        @if (!hasValidationRun)
        {
            <div class="mb-3">
                <label class="form-label"><strong>Select Validation Type</strong></label>
                <select class="form-select" @bind="selectedCheckType">
                    <option value="security">Security Review</option>
                    <option value="completeness">Completeness Check</option>
                    <option value="performance">Performance Analysis</option>
                </select>
            </div>

            <button class="btn btn-primary" @onclick="RunValidation" disabled="@isRunning">
                @if (isRunning)
                {
                    <span class="spinner-border spinner-border-sm me-2"></span>
                }
                <i class="bi bi-play-circle me-2"></i>
                Run Validation
            </button>
        }
        else
        {
            <div class="validation-results">
                <div class="alert alert-@GetAlertClass() mb-3">
                    <h6>
                        <i class="bi bi-@GetIcon() me-2"></i>
                        Validation Complete
                    </h6>
                    <strong>Score:</strong> @validationResult!.Score / 100
                    @if (!string.IsNullOrEmpty(validationResult.RiskLevel))
                    {
                        <br />
                        <strong>Risk Level:</strong> @validationResult.RiskLevel
                    }
                </div>

                @if (validationResult.Findings.Any())
                {
                    <h6 class="mt-3">Findings (@validationResult.Findings.Count)</h6>
                    <ul class="list-group mb-3">
                        @foreach (var finding in validationResult.Findings)
                        {
                            <li class="list-group-item">
                                <i class="bi bi-exclamation-circle text-warning me-2"></i>
                                @finding
                            </li>
                        }
                    </ul>
                }

                @if (validationResult.Recommendations.Any())
                {
                    <h6 class="mt-3">Recommendations (@validationResult.Recommendations.Count)</h6>
                    <ul class="list-group mb-3">
                        @foreach (var rec in validationResult.Recommendations)
                        {
                            <li class="list-group-item">
                                <i class="bi bi-lightbulb text-success me-2"></i>
                                @rec
                            </li>
                        }
                    </ul>
                }

                <button class="btn btn-outline-secondary btn-sm" @onclick="ResetValidation">
                    <i class="bi bi-arrow-clockwise me-2"></i>
                    Run Another Validation
                </button>
            </div>
        }

        @if (!string.IsNullOrEmpty(errorMessage))
        {
            <div class="alert alert-danger mt-3">
                <i class="bi bi-exclamation-triangle me-2"></i>
                @errorMessage
            </div>
        }
    </div>
</div>
```

**Code-behind:** `PlanValidationPanel.razor.cs`

```csharp
using Microsoft.AspNetCore.Components;
using PRFactory.Core.Application.Services;
using PRFactory.Web.Services;

namespace PRFactory.Web.Components.Plans;

public partial class PlanValidationPanel
{
    [Parameter, EditorRequired]
    public Guid TicketId { get; set; }

    [Inject]
    private ITicketService TicketService { get; set; } = null!;

    private string selectedCheckType = "security";
    private bool isRunning = false;
    private bool hasValidationRun = false;
    private PlanValidationResult? validationResult;
    private string? errorMessage;

    private async Task RunValidation()
    {
        isRunning = true;
        errorMessage = null;

        try
        {
            validationResult = await TicketService.ValidatePlanAsync(TicketId, selectedCheckType);
            hasValidationRun = true;
        }
        catch (Exception ex)
        {
            errorMessage = $"Validation failed: {ex.Message}";
        }
        finally
        {
            isRunning = false;
        }
    }

    private void ResetValidation()
    {
        hasValidationRun = false;
        validationResult = null;
        errorMessage = null;
    }

    private string GetAlertClass()
    {
        if (validationResult == null) return "info";

        return validationResult.Score switch
        {
            >= 90 => "success",
            >= 70 => "warning",
            _ => "danger"
        };
    }

    private string GetIcon()
    {
        if (validationResult == null) return "info-circle";

        return validationResult.Score switch
        {
            >= 90 => "check-circle",
            >= 70 => "exclamation-triangle",
            _ => "x-circle"
        };
    }
}
```

**Lines of Code:** ~120 lines (razor) + ~70 lines (code-behind) = ~190 lines

---

## Integration with PlanReviewSection

**File to Modify:** `/src/PRFactory.Web/Components/Tickets/PlanReviewSection.razor`

Add after existing plan details:

```razor
<!-- Add Plan Validation Panel -->
<div class="col-lg-12 mb-3">
    <PlanValidationPanel TicketId="@Ticket.Id" />
</div>
```

---

## Unit Tests

**File to Create:** `/tests/PRFactory.Infrastructure.Tests/Application/PlanValidationServiceTests.cs`

Test cases:
1. `ValidatePlan_WithSecurityCheck_ReturnsValidationResult`
2. `ValidatePlan_WithCompletenessCheck_ReturnsValidationResult`
3. `ValidatePlan_WithInvalidCheckType_ThrowsException`
4. `ValidatePlan_WithNonExistentTicket_ThrowsException`
5. `ValidatePlanWithCustomPrompt_ReturnsValidationResult`
6. `ValidateCodeVsPlan_WithApprovedPlan_ReturnsAlignmentResult`
7. `ValidateCodeVsPlan_WithUnapprovedPlan_ThrowsException`
8. `ParseValidationResponse_ExtractsScore_Correctly`
9. `ParseValidationResponse_ExtractsFindings_Correctly`
10. `ParseAlignmentResponse_ExtractsAllSections_Correctly`

**Lines of Code:** ~300 lines

---

## Summary

**Total Estimated Lines of Code:**
- Interface: ~80 lines
- Service Implementation: ~250 lines
- Prompt Templates: ~140 lines (4 files)
- Web Service Integration: ~30 lines
- Blazor Component: ~190 lines
- Service Registration: ~2 lines
- Unit Tests: ~300 lines

**Total: ~992 lines of code**

**Estimated Effort:** 3-5 days
- Day 1: Create interface, implementation, and prompt templates
- Day 2: Integrate with web services and create Blazor component
- Day 3: Write unit tests
- Day 4-5: Integration testing, refinement, and documentation

---

## Dependencies

**Existing Infrastructure:**
- ✅ `IPlanService` - for reading plan content
- ✅ `IAgentPromptService` - for loading prompt templates
- ✅ `ILlmProvider` - for calling Claude API
- ✅ Logging infrastructure

**New Dependencies:**
- None (uses existing infrastructure)

---

## Risks & Mitigations

**Risk:** LLM responses may vary in format
**Mitigation:** Use robust regex parsing with fallbacks; log raw responses for debugging

**Risk:** Prompt templates may need tuning
**Mitigation:** Start with conservative prompts; iterate based on feedback; make prompts configurable

**Risk:** Validation may take time (LLM latency)
**Mitigation:** Show loading spinner; run async; consider caching results

---

## Next Steps

1. Create interface and result classes
2. Implement PlanValidationService with parsing logic
3. Create prompt template files
4. Integrate with web services
5. Build Blazor UI component
6. Write comprehensive unit tests
7. Manual testing with real plans
8. Iterate on prompt templates based on results
