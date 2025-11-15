using PRFactory.Domain.Entities;

namespace PRFactory.Infrastructure.Persistence.DemoData;

/// <summary>
/// Demo agent configuration data for offline development
/// </summary>
public static class DemoAgentConfigData
{
    /// <summary>
    /// Gets default agent configurations for a tenant
    /// </summary>
    public static List<AgentConfiguration> GetDefaultConfigurations(Guid tenantId)
    {
        var now = DateTime.UtcNow;

        return new List<AgentConfiguration>
        {
            new AgentConfiguration
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                AgentName = "AnalyzerAgent",
                Instructions = "You are a senior software architect analyzing codebases for impact assessment. Be thorough but concise. Always identify risks and dependencies.",
                EnabledTools = "[\"ReadFile\", \"Grep\", \"Glob\", \"CodeSearch\", \"GetJiraTicket\"]",
                MaxTokens = 8000,
                Temperature = 0.3f,
                StreamingEnabled = true,
                RequiresApproval = false,
                CreatedAt = now,
                UpdatedAt = now
            },
            new AgentConfiguration
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                AgentName = "PlannerAgent",
                Instructions = "You are a technical lead creating implementation plans. Break work into small, testable tasks. Identify risks and dependencies. Provide realistic estimates.",
                EnabledTools = "[\"ReadFile\", \"Grep\", \"CodeSearch\", \"GetJiraTicket\"]",
                MaxTokens = 8000,
                Temperature = 0.3f,
                StreamingEnabled = true,
                RequiresApproval = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new AgentConfiguration
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                AgentName = "ImplementerAgent",
                Instructions = "You are a senior developer implementing features according to approved plans. Write clean, tested, well-documented code. Follow the project's coding standards.",
                EnabledTools = "[\"ReadFile\", \"WriteFile\", \"EditFile\", \"Grep\", \"Glob\", \"Bash\"]",
                MaxTokens = 8000,
                Temperature = 0.3f,
                StreamingEnabled = true,
                RequiresApproval = false,
                CreatedAt = now,
                UpdatedAt = now
            },
            new AgentConfiguration
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                AgentName = "ReviewerAgent",
                Instructions = "You are a senior code reviewer performing thorough code reviews. Check for bugs, security issues, performance problems, and adherence to best practices. Provide constructive feedback.",
                EnabledTools = "[\"ReadFile\", \"Grep\", \"Glob\", \"CodeSearch\"]",
                MaxTokens = 8000,
                Temperature = 0.3f,
                StreamingEnabled = true,
                RequiresApproval = false,
                CreatedAt = now,
                UpdatedAt = now
            }
        };
    }
}
