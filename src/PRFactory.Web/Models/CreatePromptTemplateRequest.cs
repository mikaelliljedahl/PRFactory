namespace PRFactory.Web.Models;

/// <summary>
/// Request model for creating a new agent prompt template
/// </summary>
public class CreatePromptTemplateRequest : PromptTemplateFormModel
{
    /// <summary>
    /// The tenant ID (null for system templates)
    /// </summary>
    public Guid? TenantId { get; set; }
}
