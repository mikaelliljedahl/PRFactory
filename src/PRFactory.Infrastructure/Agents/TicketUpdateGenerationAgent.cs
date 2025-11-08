using Microsoft.Extensions.Logging;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Domain.ValueObjects;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Claude;
using PRFactory.Infrastructure.Claude.Models;
using System.Text.Json;

namespace PRFactory.Infrastructure.Agents;

/// <summary>
/// Generates refined ticket updates based on codebase analysis and Q&A session.
/// Uses Claude AI to create comprehensive ticket descriptions with SMART success criteria.
/// </summary>
public class TicketUpdateGenerationAgent : BaseAgent
{
    private readonly IClaudeClient _claudeClient;
    private readonly ITicketUpdateRepository _ticketUpdateRepository;
    private readonly ITicketRepository _ticketRepository;

    public override string Name => "TicketUpdateGenerationAgent";
    public override string Description => "Generate refined ticket descriptions with success criteria based on analysis and Q&A";

    public TicketUpdateGenerationAgent(
        ILogger<TicketUpdateGenerationAgent> logger,
        IClaudeClient claudeClient,
        ITicketUpdateRepository ticketUpdateRepository,
        ITicketRepository ticketRepository)
        : base(logger)
    {
        _claudeClient = claudeClient ?? throw new ArgumentNullException(nameof(claudeClient));
        _ticketUpdateRepository = ticketUpdateRepository ?? throw new ArgumentNullException(nameof(ticketUpdateRepository));
        _ticketRepository = ticketRepository ?? throw new ArgumentNullException(nameof(ticketRepository));
    }

    protected override async Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken)
    {
        if (context.Ticket == null)
        {
            Logger.LogError("Ticket entity is missing from context");
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = "Ticket entity is required"
            };
        }

        if (context.Analysis == null)
        {
            Logger.LogError("Codebase analysis is missing from context");
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = "Codebase analysis must be completed before generating ticket update"
            };
        }

        if (context.Ticket.Questions.Count == 0 || context.Ticket.Answers.Count == 0)
        {
            Logger.LogError("Questions and answers are required for ticket update generation");
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = "Questions and answers must be available before generating ticket update"
            };
        }

        Logger.LogInformation("Generating ticket update for ticket {TicketKey}", context.Ticket.TicketKey);

        try
        {
            // Check if we're regenerating after rejection
            var latestVersion = await _ticketUpdateRepository.GetLatestVersionNumberAsync(
                context.Ticket.Id,
                cancellationToken);
            var nextVersion = latestVersion + 1;

            // Get rejection reason if this is a retry
            var rejectionContext = string.Empty;
            if (nextVersion > 1)
            {
                var previousUpdate = await _ticketUpdateRepository.GetLatestDraftByTicketIdAsync(
                    context.Ticket.Id,
                    cancellationToken);

                if (previousUpdate?.RejectionReason != null)
                {
                    rejectionContext = $@"

PREVIOUS ATTEMPT WAS REJECTED:
Rejection Reason: {previousUpdate.RejectionReason}

Please address the rejection feedback in this updated version.";
                }
            }

            // Build comprehensive prompt
            var systemPrompt = BuildSystemPrompt();
            var userPrompt = BuildUserPrompt(context, rejectionContext);

            var messages = new List<Message>
            {
                new Message("user", userPrompt)
            };

            // Call Claude for ticket update generation
            Logger.LogDebug("Calling Claude to generate ticket update (version {Version})", nextVersion);
            var response = await _claudeClient.SendMessageAsync(
                systemPrompt,
                messages,
                maxTokens: 4000,
                ct: cancellationToken
            );

            // Parse JSON response
            var jsonResponse = ExtractJsonFromResponse(response);
            var ticketUpdateDto = JsonSerializer.Deserialize<TicketUpdateDto>(
                jsonResponse,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (ticketUpdateDto == null)
            {
                Logger.LogError("Failed to parse ticket update response from Claude");
                return new AgentResult
                {
                    Status = AgentStatus.Failed,
                    Error = "Failed to parse ticket update response"
                };
            }

            // Validate the response
            if (string.IsNullOrWhiteSpace(ticketUpdateDto.UpdatedTitle))
            {
                Logger.LogError("Generated ticket update has empty title");
                return new AgentResult
                {
                    Status = AgentStatus.Failed,
                    Error = "Generated ticket update must have a title"
                };
            }

            if (ticketUpdateDto.SuccessCriteria == null || ticketUpdateDto.SuccessCriteria.Count == 0)
            {
                Logger.LogError("Generated ticket update has no success criteria");
                return new AgentResult
                {
                    Status = AgentStatus.Failed,
                    Error = "Generated ticket update must have success criteria"
                };
            }

            // Convert DTOs to domain objects
            var successCriteria = ticketUpdateDto.SuccessCriteria
                .Select(sc => new SuccessCriterion(
                    ParseCategory(sc.Category),
                    sc.Description,
                    sc.Priority,
                    sc.IsTestable))
                .ToList();

            // Create TicketUpdate entity
            var ticketUpdate = TicketUpdate.Create(
                ticketId: context.Ticket.Id,
                updatedTitle: ticketUpdateDto.UpdatedTitle,
                updatedDescription: ticketUpdateDto.UpdatedDescription,
                successCriteria: successCriteria,
                acceptanceCriteria: ticketUpdateDto.AcceptanceCriteria,
                version: nextVersion
            );

            // Save to database
            await _ticketUpdateRepository.CreateAsync(ticketUpdate, cancellationToken);

            // Update ticket workflow state
            context.Ticket.UpdateWorkflowState(WorkflowState.TicketUpdateGenerated);
            await _ticketRepository.UpdateAsync(context.Ticket, cancellationToken);

            // Store in context
            context.State["TicketUpdateId"] = ticketUpdate.Id;
            context.State["TicketUpdateVersion"] = ticketUpdate.Version;

            Logger.LogInformation(
                "Generated ticket update {TicketUpdateId} (version {Version}) for ticket {TicketKey} with {CriteriaCount} success criteria",
                ticketUpdate.Id, ticketUpdate.Version, context.Ticket.TicketKey, successCriteria.Count);

            return new AgentResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object>
                {
                    ["TicketUpdateId"] = ticketUpdate.Id,
                    ["Version"] = ticketUpdate.Version,
                    ["SuccessCriteriaCount"] = successCriteria.Count,
                    ["UpdatedTitle"] = ticketUpdate.UpdatedTitle
                }
            };
        }
        catch (JsonException ex)
        {
            Logger.LogError(ex, "Failed to parse JSON response from Claude for ticket {TicketKey}", context.Ticket.TicketKey);
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = $"Failed to parse ticket update response: {ex.Message}",
                ErrorDetails = ex.ToString()
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to generate ticket update for ticket {TicketKey}", context.Ticket.TicketKey);
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = $"Failed to generate ticket update: {ex.Message}",
                ErrorDetails = ex.ToString()
            };
        }
    }

    private string BuildSystemPrompt()
    {
        return @"You are an expert software architect refining vague software tickets into comprehensive, actionable requirements.

Your task is to synthesize information from:
1. The original ticket (often vague or incomplete)
2. Codebase analysis (architecture, patterns, affected files)
3. Q&A session with the developer (clarifying questions and answers)

Generate a refined ticket that includes:
1. **Clear, Specific Title**: Actionable and concise (5-10 words)
2. **Comprehensive Description**: Detailed context including:
   - Background and motivation
   - Current state analysis
   - What needs to be changed/added
   - Technical considerations from codebase analysis
   - Key insights from Q&A session
3. **SMART Success Criteria**: Categorized and prioritized
   - Specific, Measurable, Achievable, Relevant, Time-bound
   - Categories: Functional, Technical, Testing, UX, Security, Performance
   - Priority: 0=must-have, 1=should-have, 2=nice-to-have
   - Mark as testable when possible
4. **Structured Acceptance Criteria**: Markdown checklist format

Respond with JSON in this exact format:
{
  ""updatedTitle"": ""Clear, actionable title"",
  ""updatedDescription"": ""Comprehensive description with context, background, and technical details"",
  ""successCriteria"": [
    {
      ""category"": ""Functional|Technical|Testing|UX|Security|Performance"",
      ""description"": ""Specific, measurable criterion"",
      ""priority"": 0,
      ""isTestable"": true
    }
  ],
  ""acceptanceCriteria"": ""- [ ] Criterion 1\n- [ ] Criterion 2\n- [ ] Criterion 3""
}

Guidelines:
- Title should be action-oriented (e.g., ""Add user authentication to API endpoints"")
- Description should be 3-5 paragraphs with full context
- Include at least 5-10 success criteria across different categories
- Ensure at least 3 must-have (priority 0) criteria
- Acceptance criteria should be a markdown checklist (- [ ] format)
- Make criteria testable whenever possible
- Incorporate technical insights from codebase analysis
- Address all concerns raised in Q&A session";
    }

    private string BuildUserPrompt(AgentContext context, string rejectionContext)
    {
        var ticket = context.Ticket!;
        var analysis = context.Analysis!;

        // Build Q&A pairs
        var qaPairs = new System.Text.StringBuilder();
        foreach (var question in ticket.Questions)
        {
            var answer = ticket.Answers.FirstOrDefault(a => a.QuestionId == question.Id);
            if (answer != null)
            {
                qaPairs.AppendLine($"Q: {question.Text}");
                qaPairs.AppendLine($"A: {answer.Text}");
                qaPairs.AppendLine();
            }
        }

        return $@"Original Ticket:
Title: {ticket.Title}
Description: {ticket.Description}
Ticket Key: {ticket.TicketKey}

Codebase Analysis:
Architecture: {analysis.Architecture}
Summary: {analysis.Summary}

Affected Files:
{string.Join("\n", analysis.AffectedFiles.Select(f => $"- {f}"))}

Technical Considerations:
{string.Join("\n", analysis.TechnicalConsiderations.Select(tc => $"- {tc}"))}

Q&A Session:
{qaPairs}{rejectionContext}

Task: Generate a refined, comprehensive ticket update with clear title, detailed description, categorized success criteria, and structured acceptance criteria.

Return the response in the exact JSON format specified.";
    }

    private string ExtractJsonFromResponse(string response)
    {
        // Try to find JSON block in markdown code fence
        var jsonStart = response.IndexOf("```json", StringComparison.OrdinalIgnoreCase);
        if (jsonStart >= 0)
        {
            jsonStart = response.IndexOf('{', jsonStart);
            var jsonEnd = response.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                return response.Substring(jsonStart, jsonEnd - jsonStart + 1);
            }
        }

        // Try to find raw JSON
        jsonStart = response.IndexOf('{');
        var jsonEnd2 = response.LastIndexOf('}');
        if (jsonStart >= 0 && jsonEnd2 > jsonStart)
        {
            return response.Substring(jsonStart, jsonEnd2 - jsonStart + 1);
        }

        return response;
    }

    private SuccessCriterionCategory ParseCategory(string category)
    {
        return category?.ToLower() switch
        {
            "functional" => SuccessCriterionCategory.Functional,
            "technical" => SuccessCriterionCategory.Technical,
            "testing" => SuccessCriterionCategory.Testing,
            "ux" => SuccessCriterionCategory.UX,
            "security" => SuccessCriterionCategory.Security,
            "performance" => SuccessCriterionCategory.Performance,
            _ => SuccessCriterionCategory.Functional // Default
        };
    }

    /// <summary>
    /// DTO for deserializing ticket update from Claude's JSON response
    /// </summary>
    private class TicketUpdateDto
    {
        public string UpdatedTitle { get; set; } = string.Empty;
        public string UpdatedDescription { get; set; } = string.Empty;
        public List<SuccessCriterionDto> SuccessCriteria { get; set; } = new();
        public string AcceptanceCriteria { get; set; } = string.Empty;
    }

    private class SuccessCriterionDto
    {
        public string Category { get; set; } = "Functional";
        public string Description { get; set; } = string.Empty;
        public int Priority { get; set; } = 0;
        public bool IsTestable { get; set; } = true;
    }
}
