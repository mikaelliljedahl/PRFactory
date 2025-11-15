namespace PRFactory.Domain.Entities;

/// <summary>
/// Agent configuration stored per tenant.
/// Replaces appsettings-based config with database-driven configuration.
/// </summary>
public class AgentConfiguration
{
    public Guid Id { get; set; }

    /// <summary>
    /// Tenant this agent config belongs to (multi-tenant isolation)
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Agent name (e.g., "AnalyzerAgent", "PlannerAgent")
    /// </summary>
    public string AgentName { get; set; } = string.Empty;

    /// <summary>
    /// System prompt / instructions for the agent
    /// </summary>
    public string Instructions { get; set; } = string.Empty;

    /// <summary>
    /// JSON array of enabled tool names (e.g., ["ReadFile", "Grep"])
    /// </summary>
    public string EnabledTools { get; set; } = "[]";

    /// <summary>
    /// Maximum tokens for agent responses
    /// </summary>
    public int MaxTokens { get; set; } = 8000;

    /// <summary>
    /// Temperature (0.0 = deterministic, 1.0 = creative)
    /// </summary>
    public float Temperature { get; set; } = 0.3f;

    /// <summary>
    /// Enable streaming responses
    /// </summary>
    public bool StreamingEnabled { get; set; } = true;

    /// <summary>
    /// Require human approval before execution
    /// </summary>
    public bool RequiresApproval { get; set; } = false;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Tenant? Tenant { get; set; }
}
