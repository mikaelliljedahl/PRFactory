namespace PRFactory.Infrastructure.Claude.Models;

/// <summary>
/// Represents a detailed implementation plan for a ticket
/// </summary>
/// <param name="MainPlan">The full implementation plan in markdown format</param>
/// <param name="AffectedFiles">List of files that will be modified or created</param>
/// <param name="TestStrategy">Testing strategy and requirements</param>
/// <param name="EstimatedComplexity">Complexity rating from 1-5</param>
public record ImplementationPlan(
    string MainPlan,
    string AffectedFiles,
    string TestStrategy,
    int EstimatedComplexity
);
