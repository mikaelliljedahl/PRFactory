using Mapster;
using PRFactory.Domain.Entities;
using PRFactory.Domain.ValueObjects;
using PRFactory.Web.Models;

namespace PRFactory.Web.Mapping;

/// <summary>
/// Configures Mapster mappings for entity-to-DTO conversions.
/// This centralizes all mapping logic to eliminate manual mapping code scattered across services.
/// </summary>
public static class MappingConfiguration
{
    /// <summary>
    /// Configures all Mapster mappings for the application
    /// </summary>
    public static void Configure()
    {
        // Configure Ticket → TicketDto mapping
        TypeAdapterConfig<Ticket, TicketDto>
            .NewConfig()
            .Map(dest => dest.RepositoryName, src => src.Repository != null ? src.Repository.Name : null);

        // Configure Repository → RepositoryDto mapping (Web.Models)
        // Note: This requires additional data (tenant name, ticket count) not available in the entity
        // Services will need to manually set these properties after mapping
        TypeAdapterConfig<Repository, Web.Models.RepositoryDto>
            .NewConfig()
            .Map(dest => dest.TenantName, src => string.Empty) // Will be set manually in service
            .Map(dest => dest.TicketCount, src => 0); // Will be set manually in service

        // Configure ErrorLog → ErrorDto mapping
        TypeAdapterConfig<ErrorLog, ErrorDto>
            .NewConfig();

        // Configure TicketUpdate → TicketUpdateDto mapping
        TypeAdapterConfig<TicketUpdate, TicketUpdateDto>
            .NewConfig()
            .Map(dest => dest.SuccessCriteria, src => src.SuccessCriteria);

        // Configure SuccessCriterion → SuccessCriterionDto mapping
        TypeAdapterConfig<SuccessCriterion, SuccessCriterionDto>
            .NewConfig();

        // Configure Tenant → TenantDto mapping
        TypeAdapterConfig<Tenant, TenantDto>
            .NewConfig();

        // Configure WorkflowEvent base mapping (common properties)
        TypeAdapterConfig<WorkflowEvent, WorkflowEventDto>
            .NewConfig()
            .Map(dest => dest.EventType, src => src.EventType)
            .Map(dest => dest.Description, src => string.Empty) // Will be set by service based on event type
            .Map(dest => dest.Icon, src => "circle") // Default icon, will be set by service
            .Map(dest => dest.Severity, src => EventSeverity.Info) // Default severity, will be set by service
            .Map(dest => dest.FromState, src => default(WorkflowState?))
            .Map(dest => dest.ToState, src => default(WorkflowState?))
            .Map(dest => dest.TicketKey, src => (string?)null)
            .Map(dest => dest.MetadataJson, src => (string?)null);

        // Note: WorkflowEvent is a base class with multiple derived types (WorkflowStateChanged, QuestionAdded, etc.)
        // The service layer will handle the specific event type mapping logic

        // Configure ReviewComment → ReviewCommentDto mapping
        TypeAdapterConfig<ReviewComment, ReviewCommentDto>
            .NewConfig()
            .Map(dest => dest.AuthorName, src => src.Author.DisplayName)
            .Map(dest => dest.AuthorEmail, src => src.Author.Email)
            .Map(dest => dest.AuthorAvatarUrl, src => src.Author.AvatarUrl);

        // Configure PlanReview → ReviewerDto mapping
        TypeAdapterConfig<PlanReview, ReviewerDto>
            .NewConfig()
            .Map(dest => dest.Id, src => src.Reviewer.Id)
            .Map(dest => dest.DisplayName, src => src.Reviewer.DisplayName)
            .Map(dest => dest.Email, src => src.Reviewer.Email)
            .Map(dest => dest.AvatarUrl, src => src.Reviewer.AvatarUrl)
            .Map(dest => dest.Status, src => src.Status)
            .Map(dest => dest.IsRequired, src => src.IsRequired)
            .Map(dest => dest.AssignedAt, src => src.AssignedAt)
            .Map(dest => dest.ReviewedAt, src => src.ReviewedAt)
            .Map(dest => dest.Decision, src => src.Decision);

        // Configure Tenant → TenantDto mapping
        // Note: Related counts (RepositoryCount, TicketCount) and credential flags
        // will need to be set manually by the service after mapping
        TypeAdapterConfig<Tenant, TenantDto>
            .NewConfig()
            .Map(dest => dest.AutoImplementAfterPlanApproval, src => src.Configuration.AutoImplementAfterPlanApproval)
            .Map(dest => dest.MaxRetries, src => src.Configuration.MaxRetries)
            .Map(dest => dest.ClaudeModel, src => src.Configuration.ClaudeModel)
            .Map(dest => dest.MaxTokensPerRequest, src => src.Configuration.MaxTokensPerRequest)
            .Map(dest => dest.EnableCodeReview, src => src.Configuration.EnableCodeReview)
            .Map(dest => dest.RepositoryCount, src => 0) // Will be set manually
            .Map(dest => dest.TicketCount, src => 0) // Will be set manually
            .Map(dest => dest.HasTicketPlatformApiToken, src => !string.IsNullOrEmpty(src.TicketPlatformApiToken))
            .Map(dest => dest.HasClaudeApiKey, src => !string.IsNullOrEmpty(src.ClaudeApiKey));
    }
}
