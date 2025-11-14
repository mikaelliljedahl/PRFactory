using PRFactory.Domain.Entities;
using PRFactory.Domain.ValueObjects;
using PRFactory.Tests.Builders;

namespace PRFactory.Tests.Fixtures;

/// <summary>
/// Fixture providing pre-configured test data (tenants, repositories, tickets, users)
/// </summary>
public class TestDataFixture
{
    public Tenant DefaultTenant { get; }
    public Repository DefaultRepository { get; }
    public User DefaultUser { get; }
    public Ticket DefaultTicket { get; }
    public TicketUpdate DefaultTicketUpdate { get; }

    public Guid DefaultTenantId => DefaultTenant.Id;
    public Guid DefaultRepositoryId => DefaultRepository.Id;
    public Guid DefaultUserId => DefaultUser.Id;
    public Guid DefaultTicketId => DefaultTicket.Id;

    public TestDataFixture()
    {
        // Create default tenant
        DefaultTenant = new TenantBuilder()
            .WithName("Default Test Tenant")
            .WithTicketPlatform("Jira")
            .WithTicketPlatformUrl("https://test.atlassian.net")
            .WithAutoImplementation(false)
            .Build();

        // Create default repository
        DefaultRepository = new RepositoryBuilder()
            .ForTenant(DefaultTenant.Id)
            .WithName("default-repo")
            .AsGitHub()
            .WithDefaultBranch("main")
            .Build();

        // Create default user
        DefaultUser = new UserBuilder()
            .ForTenant(DefaultTenant.Id)
            .WithEmail("default.user@test.com")
            .WithDisplayName("Default User")
            .Build();

        // Create default ticket
        DefaultTicket = new TicketBuilder()
            .WithTicketKey("TEST-123")
            .WithTenantId(DefaultTenant.Id)
            .WithRepositoryId(DefaultRepository.Id)
            .WithTitle("Default Test Ticket")
            .WithDescription("This is a default test ticket")
            .WithState(WorkflowState.Triggered)
            .Build();

        // Create default ticket update
        DefaultTicketUpdate = new TicketUpdateBuilder()
            .ForTicket(DefaultTicket.Id)
            .WithUpdatedTitle("Updated Test Ticket")
            .WithUpdatedDescription("Updated description with more details")
            .AsDraft()
            .Build();
    }

    /// <summary>
    /// Creates a new tenant with customizable properties
    /// </summary>
    public static Tenant CreateTenant(string name = "Test Tenant", string platform = "Jira")
    {
        return new TenantBuilder()
            .WithName(name)
            .WithTicketPlatform(platform)
            .Build();
    }

    /// <summary>
    /// Creates a new repository for a specific tenant
    /// </summary>
    public static Repository CreateRepository(Guid tenantId, string name = "test-repo", string platform = "GitHub")
    {
        var builder = new RepositoryBuilder()
            .ForTenant(tenantId)
            .WithName(name);

        return platform switch
        {
            "GitHub" => builder.AsGitHub().Build(),
            "Bitbucket" => builder.AsBitbucket().Build(),
            "AzureDevOps" => builder.AsAzureDevOps().Build(),
            _ => builder.AsGitHub().Build()
        };
    }

    /// <summary>
    /// Creates a new user for a specific tenant
    /// </summary>
    public static User CreateUser(Guid tenantId, string email = "test.user@example.com", string displayName = "Test User")
    {
        return new UserBuilder()
            .ForTenant(tenantId)
            .WithEmail(email)
            .WithDisplayName(displayName)
            .Build();
    }

    /// <summary>
    /// Creates a new ticket with default or custom properties
    /// </summary>
    public Ticket CreateTicket(
        string ticketKey = "TEST-456",
        Guid? tenantId = null,
        Guid? repositoryId = null,
        WorkflowState state = WorkflowState.Triggered)
    {
        return new TicketBuilder()
            .WithTicketKey(ticketKey)
            .WithTenantId(tenantId ?? DefaultTenantId)
            .WithRepositoryId(repositoryId ?? DefaultRepositoryId)
            .WithState(state)
            .Build();
    }

    /// <summary>
    /// Creates a ticket with questions
    /// </summary>
    public Ticket CreateTicketWithQuestions(int questionCount = 3)
    {
        return new TicketBuilder()
            .WithTicketKey("TEST-Q123")
            .WithTenantId(DefaultTenantId)
            .WithRepositoryId(DefaultRepositoryId)
            .WithState(WorkflowState.QuestionsPosted)
            .WithQuestions(questionCount)
            .Build();
    }

    /// <summary>
    /// Creates a ticket in planning state
    /// </summary>
    public Ticket CreateTicketInPlanning()
    {
        return new TicketBuilder()
            .WithTicketKey("TEST-PLAN")
            .WithTenantId(DefaultTenantId)
            .WithRepositoryId(DefaultRepositoryId)
            .WithState(WorkflowState.Planning)
            .WithTitle("Ticket in Planning")
            .Build();
    }

    /// <summary>
    /// Creates a ticket with an approved plan
    /// </summary>
    public Ticket CreateTicketWithApprovedPlan()
    {
        return new TicketBuilder()
            .WithTicketKey("TEST-APPROVED")
            .WithTenantId(DefaultTenantId)
            .WithRepositoryId(DefaultRepositoryId)
            .WithState(WorkflowState.PlanApproved)
            .WithPlanBranch("feature/test-approved", "plans/test-approved.md")
            .Build();
    }

    /// <summary>
    /// Creates a ticket update
    /// </summary>
    public TicketUpdate CreateTicketUpdate(Guid? ticketId = null, int version = 1)
    {
        return new TicketUpdateBuilder()
            .ForTicket(ticketId ?? DefaultTicketId)
            .WithVersion(version)
            .Build();
    }

    /// <summary>
    /// Creates an approved ticket update
    /// </summary>
    public TicketUpdate CreateApprovedTicketUpdate(Guid? ticketId = null)
    {
        return new TicketUpdateBuilder()
            .ForTicket(ticketId ?? DefaultTicketId)
            .AsApproved()
            .Build();
    }

    /// <summary>
    /// Creates a rejected ticket update
    /// </summary>
    public TicketUpdate CreateRejectedTicketUpdate(Guid? ticketId = null, string reason = "Needs more details")
    {
        return new TicketUpdateBuilder()
            .ForTicket(ticketId ?? DefaultTicketId)
            .AsRejected(reason)
            .Build();
    }

    /// <summary>
    /// Creates success criteria for testing
    /// </summary>
    public static List<SuccessCriterion> CreateSuccessCriteria()
    {
        return new List<SuccessCriterion>
        {
            SuccessCriterion.CreateMustHave(SuccessCriterionCategory.Functional, "Must implement feature X", true),
            SuccessCriterion.CreateShouldHave(SuccessCriterionCategory.Technical, "Should use pattern Y", true),
            SuccessCriterion.CreateNiceToHave(SuccessCriterionCategory.UX, "Nice to have animation Z", false)
        };
    }
}
