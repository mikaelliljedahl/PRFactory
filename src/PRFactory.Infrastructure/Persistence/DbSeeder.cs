using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PRFactory.Domain.Entities;
using PRFactory.Domain.ValueObjects;
using PRFactory.Infrastructure.Persistence.DemoData;
using PRFactory.Infrastructure.Persistence.Encryption;

namespace PRFactory.Infrastructure.Persistence;

/// <summary>
/// Seeds the database with demo data for offline/single-user development mode
/// </summary>
public class DbSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<DbSeeder> _logger;

    public DbSeeder(
        ApplicationDbContext context,
        IEncryptionService encryptionService,
        ILogger<DbSeeder> logger)
    {
        _context = context;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    /// <summary>
    /// Seeds demo data if not already present (idempotent)
    /// </summary>
    public async Task SeedAsync()
    {
        _logger.LogInformation("Checking if demo data needs to be seeded...");

        // Check if demo tenant already exists
        var existingTenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == DemoTenantData.DemoTenantId);

        if (existingTenant != null)
        {
            _logger.LogInformation("Demo data already exists. Skipping seeding.");
            return;
        }

        _logger.LogInformation("Seeding demo data for offline development...");

        // Seed in order
        await SeedTenantAsync();
        await SeedRepositoriesAsync();
        await SeedTicketsAsync();
        await SeedAgentPromptTemplatesAsync();

        await _context.SaveChangesAsync();

        _logger.LogInformation("Demo data seeding completed successfully");
    }

    private async Task SeedTenantAsync()
    {
        _logger.LogInformation("Seeding demo tenant...");

        // Encrypt credentials
        var encryptedJiraToken = _encryptionService.Encrypt(DemoTenantData.JiraApiToken);
        var encryptedClaudeKey = _encryptionService.Encrypt(DemoTenantData.ClaudeApiKey);

        // Create tenant using factory method with platform parameter
        var tenant = Tenant.Create(
            DemoTenantData.TenantName,
            DemoTenantData.JiraUrl,
            encryptedJiraToken,
            encryptedClaudeKey,
            "Jira"
        );

        // Override the auto-generated ID with our hardcoded demo ID
        var idProperty = typeof(Tenant).GetProperty("Id");
        idProperty?.SetValue(tenant, DemoTenantData.DemoTenantId);

        // Configure tenant settings
        tenant.UpdateConfiguration(new TenantConfiguration
        {
            AutoImplementAfterPlanApproval = false,
            MaxRetries = 3,
            ClaudeModel = "claude-sonnet-4-5-20250929",
            MaxTokensPerRequest = 8000,
            ApiTimeoutSeconds = 300,
            EnableVerboseLogging = true,
            EnableCodeReview = false
        });

        await _context.Tenants.AddAsync(tenant);
        _logger.LogInformation("Demo tenant seeded: {TenantName} (ID: {TenantId})",
            DemoTenantData.TenantName, DemoTenantData.DemoTenantId);
    }

    private async Task SeedRepositoriesAsync()
    {
        _logger.LogInformation("Seeding demo repositories...");

        foreach (var repo in DemoRepositoryData.Repositories)
        {
            var encryptedToken = _encryptionService.Encrypt(repo.Token);

            var repository = Repository.Create(
                DemoTenantData.DemoTenantId,
                repo.Name,
                repo.Platform,
                repo.CloneUrl,
                encryptedToken,
                repo.DefaultBranch
            );

            await _context.Repositories.AddAsync(repository);
            _logger.LogInformation("Seeded repository: {RepoName} ({Platform})", repo.Name, repo.Platform);
        }
    }

    private async Task SeedTicketsAsync()
    {
        _logger.LogInformation("Seeding demo tickets...");

        // Get repositories to associate tickets
        var repositories = await _context.Repositories
            .Where(r => r.TenantId == DemoTenantData.DemoTenantId)
            .ToListAsync();

        foreach (var demoTicket in DemoTicketData.Tickets)
        {
            var repository = repositories[demoTicket.RepositoryIndex];

            var ticket = Ticket.Create(
                demoTicket.TicketKey,
                DemoTenantData.DemoTenantId,
                repository.Id,
                "Jira",
                demoTicket.Source
            );

            ticket.UpdateTicketInfo(demoTicket.Title, demoTicket.Description);

            // Transition ticket to target state
            await TransitionTicketToState(ticket, demoTicket.State);

            // Add questions and answers for appropriate states
            if (ShouldHaveQuestions(demoTicket.State))
            {
                AddQuestionsToTicket(ticket);
            }

            if (ShouldHaveAnswers(demoTicket.State))
            {
                AddAnswersToTicket(ticket);
            }

            // Add ticket update for refinement workflow states
            if (ShouldHaveTicketUpdate(demoTicket.State))
            {
                await AddTicketUpdateAsync(ticket, demoTicket.State);
            }

            // Add workflow events for timeline
            AddWorkflowEventsToTicket(ticket, demoTicket.State);

            await _context.Tickets.AddAsync(ticket);
            _logger.LogInformation("Seeded ticket: {TicketKey} - {Title} (State: {State})",
                demoTicket.TicketKey, demoTicket.Title, demoTicket.State);
        }
    }

    private async Task TransitionTicketToState(Ticket ticket, WorkflowState targetState)
    {
        var currentState = WorkflowState.Triggered;

        // Define the path to reach target state
        var statePath = GetStateTransitionPath(targetState);

        foreach (var state in statePath)
        {
            if (state != currentState)
            {
                ticket.TransitionTo(state);
                currentState = state;
            }
        }
    }

    private List<WorkflowState> GetStateTransitionPath(WorkflowState targetState)
    {
        // Map each state to its transition path from Triggered
        return targetState switch
        {
            WorkflowState.Triggered => new() { WorkflowState.Triggered },
            WorkflowState.Analyzing => new() { WorkflowState.Triggered, WorkflowState.Analyzing },
            WorkflowState.TicketUpdateGenerated => new() { WorkflowState.Triggered, WorkflowState.Analyzing, WorkflowState.TicketUpdateGenerated },
            WorkflowState.TicketUpdateUnderReview => new() { WorkflowState.Triggered, WorkflowState.Analyzing, WorkflowState.TicketUpdateGenerated, WorkflowState.TicketUpdateUnderReview },
            WorkflowState.TicketUpdateRejected => new() { WorkflowState.Triggered, WorkflowState.Analyzing, WorkflowState.TicketUpdateGenerated, WorkflowState.TicketUpdateUnderReview, WorkflowState.TicketUpdateRejected },
            WorkflowState.TicketUpdateApproved => new() { WorkflowState.Triggered, WorkflowState.Analyzing, WorkflowState.TicketUpdateGenerated, WorkflowState.TicketUpdateUnderReview, WorkflowState.TicketUpdateApproved },
            WorkflowState.TicketUpdatePosted => new() { WorkflowState.Triggered, WorkflowState.Analyzing, WorkflowState.TicketUpdateGenerated, WorkflowState.TicketUpdateUnderReview, WorkflowState.TicketUpdateApproved, WorkflowState.TicketUpdatePosted },
            WorkflowState.QuestionsPosted => new() { WorkflowState.Triggered, WorkflowState.Analyzing, WorkflowState.TicketUpdateGenerated, WorkflowState.TicketUpdateUnderReview, WorkflowState.TicketUpdateApproved, WorkflowState.TicketUpdatePosted, WorkflowState.QuestionsPosted },
            WorkflowState.AwaitingAnswers => new() { WorkflowState.Triggered, WorkflowState.Analyzing, WorkflowState.TicketUpdateGenerated, WorkflowState.TicketUpdateUnderReview, WorkflowState.TicketUpdateApproved, WorkflowState.TicketUpdatePosted, WorkflowState.QuestionsPosted, WorkflowState.AwaitingAnswers },
            WorkflowState.AnswersReceived => new() { WorkflowState.Triggered, WorkflowState.Analyzing, WorkflowState.TicketUpdateGenerated, WorkflowState.TicketUpdateUnderReview, WorkflowState.TicketUpdateApproved, WorkflowState.TicketUpdatePosted, WorkflowState.QuestionsPosted, WorkflowState.AwaitingAnswers, WorkflowState.AnswersReceived },
            WorkflowState.Planning => new() { WorkflowState.Triggered, WorkflowState.Analyzing, WorkflowState.TicketUpdateGenerated, WorkflowState.TicketUpdateUnderReview, WorkflowState.TicketUpdateApproved, WorkflowState.TicketUpdatePosted, WorkflowState.Planning },
            WorkflowState.PlanPosted => new() { WorkflowState.Triggered, WorkflowState.Analyzing, WorkflowState.TicketUpdateGenerated, WorkflowState.TicketUpdateUnderReview, WorkflowState.TicketUpdateApproved, WorkflowState.TicketUpdatePosted, WorkflowState.Planning, WorkflowState.PlanPosted },
            WorkflowState.PlanUnderReview => new() { WorkflowState.Triggered, WorkflowState.Analyzing, WorkflowState.TicketUpdateGenerated, WorkflowState.TicketUpdateUnderReview, WorkflowState.TicketUpdateApproved, WorkflowState.TicketUpdatePosted, WorkflowState.Planning, WorkflowState.PlanPosted, WorkflowState.PlanUnderReview },
            WorkflowState.PlanApproved => new() { WorkflowState.Triggered, WorkflowState.Analyzing, WorkflowState.TicketUpdateGenerated, WorkflowState.TicketUpdateUnderReview, WorkflowState.TicketUpdateApproved, WorkflowState.TicketUpdatePosted, WorkflowState.Planning, WorkflowState.PlanPosted, WorkflowState.PlanUnderReview, WorkflowState.PlanApproved },
            WorkflowState.PlanRejected => new() { WorkflowState.Triggered, WorkflowState.Analyzing, WorkflowState.TicketUpdateGenerated, WorkflowState.TicketUpdateUnderReview, WorkflowState.TicketUpdateApproved, WorkflowState.TicketUpdatePosted, WorkflowState.Planning, WorkflowState.PlanPosted, WorkflowState.PlanUnderReview, WorkflowState.PlanRejected },
            WorkflowState.Implementing => new() { WorkflowState.Triggered, WorkflowState.Analyzing, WorkflowState.TicketUpdateGenerated, WorkflowState.TicketUpdateUnderReview, WorkflowState.TicketUpdateApproved, WorkflowState.TicketUpdatePosted, WorkflowState.Planning, WorkflowState.PlanPosted, WorkflowState.PlanUnderReview, WorkflowState.PlanApproved, WorkflowState.Implementing },
            WorkflowState.ImplementationFailed => new() { WorkflowState.Triggered, WorkflowState.Analyzing, WorkflowState.TicketUpdateGenerated, WorkflowState.TicketUpdateUnderReview, WorkflowState.TicketUpdateApproved, WorkflowState.TicketUpdatePosted, WorkflowState.Planning, WorkflowState.PlanPosted, WorkflowState.PlanUnderReview, WorkflowState.PlanApproved, WorkflowState.Implementing, WorkflowState.ImplementationFailed },
            WorkflowState.PRCreated => new() { WorkflowState.Triggered, WorkflowState.Analyzing, WorkflowState.TicketUpdateGenerated, WorkflowState.TicketUpdateUnderReview, WorkflowState.TicketUpdateApproved, WorkflowState.TicketUpdatePosted, WorkflowState.Planning, WorkflowState.PlanPosted, WorkflowState.PlanUnderReview, WorkflowState.PlanApproved, WorkflowState.Implementing, WorkflowState.PRCreated },
            WorkflowState.InReview => new() { WorkflowState.Triggered, WorkflowState.Analyzing, WorkflowState.TicketUpdateGenerated, WorkflowState.TicketUpdateUnderReview, WorkflowState.TicketUpdateApproved, WorkflowState.TicketUpdatePosted, WorkflowState.Planning, WorkflowState.PlanPosted, WorkflowState.PlanUnderReview, WorkflowState.PlanApproved, WorkflowState.Implementing, WorkflowState.PRCreated, WorkflowState.InReview },
            WorkflowState.Completed => new() { WorkflowState.Triggered, WorkflowState.Analyzing, WorkflowState.TicketUpdateGenerated, WorkflowState.TicketUpdateUnderReview, WorkflowState.TicketUpdateApproved, WorkflowState.TicketUpdatePosted, WorkflowState.Planning, WorkflowState.PlanPosted, WorkflowState.PlanUnderReview, WorkflowState.PlanApproved, WorkflowState.Implementing, WorkflowState.PRCreated, WorkflowState.InReview, WorkflowState.Completed },
            WorkflowState.Failed => new() { WorkflowState.Triggered, WorkflowState.Analyzing, WorkflowState.Failed },
            WorkflowState.Cancelled => new() { WorkflowState.Triggered, WorkflowState.Cancelled },
            _ => new() { WorkflowState.Triggered }
        };
    }

    private bool ShouldHaveQuestions(WorkflowState state)
    {
        return state >= WorkflowState.QuestionsPosted && state <= WorkflowState.AnswersReceived;
    }

    private bool ShouldHaveAnswers(WorkflowState state)
    {
        return state >= WorkflowState.AnswersReceived;
    }

    private bool ShouldHaveTicketUpdate(WorkflowState state)
    {
        return state >= WorkflowState.TicketUpdateGenerated &&
               state <= WorkflowState.Planning &&
               state != WorkflowState.AnswersReceived; // Skip this intermediate state
    }

    private void AddQuestionsToTicket(Ticket ticket)
    {
        var questions = new[]
        {
            Question.Create("What specific user roles need access to this feature?", "requirements"),
            Question.Create("Are there any performance requirements or constraints?", "technical"),
            Question.Create("What should happen when errors occur?", "edge-cases"),
            Question.Create("Do we need to support mobile devices?", "ux")
        };

        foreach (var question in questions)
        {
            ticket.AddQuestion(question);
        }
    }

    private void AddAnswersToTicket(Ticket ticket)
    {
        var questions = ticket.Questions.ToList();
        if (questions.Count >= 4)
        {
            ticket.AddAnswer(questions[0].Id, "Admin and Manager roles should have full access. Regular users should have read-only access.");
            ticket.AddAnswer(questions[1].Id, "The operation should complete within 2 seconds for up to 10,000 records.");
            ticket.AddAnswer(questions[2].Id, "Show user-friendly error messages and log technical details for debugging.");
            ticket.AddAnswer(questions[3].Id, "Yes, the interface should be responsive and work on tablets and phones.");
        }
    }

    private async Task AddTicketUpdateAsync(Ticket ticket, WorkflowState state)
    {
        var successCriteria = new List<SuccessCriterion>
        {
            SuccessCriterion.CreateMustHave(
                SuccessCriterionCategory.Functional,
                "Feature implements all core functionality described in requirements"
            ),
            SuccessCriterion.CreateMustHave(
                SuccessCriterionCategory.Technical,
                "Code follows established patterns and conventions in the codebase"
            ),
            SuccessCriterion.CreateMustHave(
                SuccessCriterionCategory.Testing,
                "All critical paths are covered by automated tests"
            ),
            SuccessCriterion.CreateShouldHave(
                SuccessCriterionCategory.UX,
                "UI is intuitive and follows design system guidelines"
            ),
            SuccessCriterion.CreateShouldHave(
                SuccessCriterionCategory.Performance,
                "Feature meets performance requirements under normal load"
            )
        };

        var ticketUpdate = TicketUpdate.Create(
            ticket.Id,
            $"[Refined] {ticket.Title}",
            $"{ticket.Description}\n\n## Technical Approach\n\nThis feature will be implemented using the existing patterns in the codebase. We'll create new components as needed and ensure proper error handling throughout.\n\n## Implementation Notes\n\n- Follow clean architecture principles\n- Use dependency injection for services\n- Add comprehensive logging\n- Include proper validation",
            successCriteria,
            @"## Acceptance Criteria

- [ ] User can access the feature from the main navigation
- [ ] All inputs are validated before processing
- [ ] Error messages are clear and actionable
- [ ] Feature works correctly on desktop and mobile
- [ ] Performance meets stated requirements
- [ ] Code passes all automated tests
- [ ] Documentation is updated"
        );

        // Set appropriate state for ticket update based on workflow state
        if (state >= WorkflowState.TicketUpdateApproved)
        {
            ticketUpdate.Approve();
        }
        else if (state == WorkflowState.TicketUpdateRejected)
        {
            ticketUpdate.Reject("The technical approach needs more detail. Please specify which specific components will be created and modified.");
        }

        if (state >= WorkflowState.TicketUpdatePosted && ticketUpdate.IsApproved)
        {
            ticketUpdate.MarkAsPosted();
        }

        await _context.TicketUpdates.AddAsync(ticketUpdate);
    }

    private void AddWorkflowEventsToTicket(Ticket ticket, WorkflowState targetState)
    {
        // Events are already added by the TransitionTo method in the Ticket entity
        // Additional custom events can be added here if needed

        if (targetState >= WorkflowState.PlanPosted)
        {
            var planEvent = new PlanCreated(ticket.Id, $"plan/{ticket.TicketKey.ToLower()}");
            ticket.Events.Add(planEvent);
            ticket.SetPlanBranch($"plan/{ticket.TicketKey.ToLower()}", $"docs/plan-{ticket.TicketKey.ToLower()}.md");
        }

        if (targetState >= WorkflowState.PRCreated)
        {
            var prNumber = Random.Shared.Next(100, 999);
            var prUrl = $"https://github.com/demo/repo/pull/{prNumber}";
            ticket.SetPullRequest(prUrl, prNumber);
        }

        if (targetState == WorkflowState.Failed)
        {
            ticket.RecordError("Unexpected error during implementation: Database connection timeout");
        }
    }

    private async Task SeedAgentPromptTemplatesAsync()
    {
        _logger.LogInformation("Seeding agent prompt templates...");

        foreach (var template in DemoPromptData.Templates)
        {
            var promptTemplate = AgentPromptTemplate.CreateSystemTemplate(
                template.Name,
                template.Description,
                template.Content,
                template.Category,
                template.RecommendedModel,
                template.Color
            );

            await _context.AgentPromptTemplates.AddAsync(promptTemplate);
            _logger.LogInformation("Seeded prompt template: {TemplateName} ({Category})",
                template.Name, template.Category);
        }
    }
}
