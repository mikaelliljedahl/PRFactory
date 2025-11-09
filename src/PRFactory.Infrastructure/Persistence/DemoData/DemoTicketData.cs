using PRFactory.Domain.ValueObjects;

namespace PRFactory.Infrastructure.Persistence.DemoData;

/// <summary>
/// Demo ticket scenarios covering all workflow states
/// </summary>
public static class DemoTicketData
{
    public record DemoTicket(
        string TicketKey,
        string Title,
        string Description,
        WorkflowState State,
        int RepositoryIndex,
        TicketSource Source = TicketSource.WebUI);

    public static readonly List<DemoTicket> Tickets = new()
    {
        // 1. Triggered state
        new DemoTicket(
            "DEMO-001",
            "Add user authentication to dashboard",
            "We need to add authentication to the dashboard so that only authorized users can access it. Currently anyone can view the dashboard.",
            WorkflowState.Triggered,
            0
        ),

        // 2. Analyzing state
        new DemoTicket(
            "DEMO-002",
            "Implement rate limiting for API endpoints",
            "Add rate limiting to all API endpoints to prevent abuse. We should use a sliding window algorithm with Redis.",
            WorkflowState.Analyzing,
            1
        ),

        // 3. TicketUpdateGenerated state
        new DemoTicket(
            "DEMO-003",
            "Create export functionality for reports",
            "Users want to export reports to CSV and PDF formats. The export should include all visible columns and respect current filters.",
            WorkflowState.TicketUpdateGenerated,
            0
        ),

        // 4. TicketUpdateUnderReview state
        new DemoTicket(
            "DEMO-004",
            "Add dark mode support",
            "Implement dark mode theme that users can toggle. Should persist user preference and respect system settings.",
            WorkflowState.TicketUpdateUnderReview,
            2
        ),

        // 5. TicketUpdateRejected state
        new DemoTicket(
            "DEMO-005",
            "Optimize database queries",
            "Some queries are slow. We need to optimize them.",
            WorkflowState.TicketUpdateRejected,
            1
        ),

        // 6. TicketUpdateApproved state
        new DemoTicket(
            "DEMO-006",
            "Add email notifications",
            "Send email notifications when important events occur in the system.",
            WorkflowState.TicketUpdateApproved,
            0
        ),

        // 7. TicketUpdatePosted state
        new DemoTicket(
            "DEMO-007",
            "Implement file upload with validation",
            "Add file upload functionality with proper validation for file types and sizes.",
            WorkflowState.TicketUpdatePosted,
            2
        ),

        // 8. QuestionsPosted state
        new DemoTicket(
            "DEMO-008",
            "Create admin dashboard",
            "Build an admin dashboard for system management.",
            WorkflowState.QuestionsPosted,
            1
        ),

        // 9. AwaitingAnswers state
        new DemoTicket(
            "DEMO-009",
            "Add multi-language support",
            "Support multiple languages in the UI.",
            WorkflowState.AwaitingAnswers,
            0
        ),

        // 10. AnswersReceived state
        new DemoTicket(
            "DEMO-010",
            "Implement caching strategy",
            "Add caching to improve performance.",
            WorkflowState.AnswersReceived,
            2
        ),

        // 11. Planning state
        new DemoTicket(
            "DEMO-011",
            "Create REST API documentation",
            "Generate comprehensive API documentation with examples and use cases.",
            WorkflowState.Planning,
            1
        ),

        // 12. PlanPosted state
        new DemoTicket(
            "DEMO-012",
            "Add search functionality",
            "Implement full-text search across all entities with advanced filtering.",
            WorkflowState.PlanPosted,
            0
        ),

        // 13. PlanUnderReview state
        new DemoTicket(
            "DEMO-013",
            "Implement audit logging",
            "Track all user actions for security and compliance purposes.",
            WorkflowState.PlanUnderReview,
            2
        ),

        // 14. PlanApproved state
        new DemoTicket(
            "DEMO-014",
            "Add real-time notifications",
            "Implement WebSocket-based real-time notifications for user actions.",
            WorkflowState.PlanApproved,
            1
        ),

        // 15. PlanRejected state
        new DemoTicket(
            "DEMO-015",
            "Create backup and restore functionality",
            "Implement automated backup and manual restore capabilities.",
            WorkflowState.PlanRejected,
            0
        ),

        // 16. Implementing state
        new DemoTicket(
            "DEMO-016",
            "Add two-factor authentication",
            "Implement 2FA using TOTP authenticator apps for enhanced security.",
            WorkflowState.Implementing,
            2
        ),

        // 17. PRCreated state
        new DemoTicket(
            "DEMO-017",
            "Implement data import wizard",
            "Create a step-by-step wizard for importing data from CSV files.",
            WorkflowState.PRCreated,
            1
        ),

        // 18. InReview state
        new DemoTicket(
            "DEMO-018",
            "Add performance monitoring",
            "Implement application performance monitoring with metrics and dashboards.",
            WorkflowState.InReview,
            0
        ),

        // 19. Completed state
        new DemoTicket(
            "DEMO-019",
            "Fix login redirect issue",
            "Users are not redirected to the original page after login.",
            WorkflowState.Completed,
            2
        ),

        // 20. Failed state
        new DemoTicket(
            "DEMO-020",
            "Migrate to new database",
            "Migrate from SQLite to PostgreSQL.",
            WorkflowState.Failed,
            1
        ),

        // 21. Cancelled state
        new DemoTicket(
            "DEMO-021",
            "Add social media integration",
            "Integrate with Twitter and Facebook APIs.",
            WorkflowState.Cancelled,
            0
        )
    };
}
