namespace PRFactory.Web.Models;

/// <summary>
/// Request model for updating an existing agent prompt template.
/// Inherits from PromptTemplateFormModel to enable form binding.
/// Note: Name property is inherited but not updatable (used for display only in edit form).
/// </summary>
public class UpdatePromptTemplateRequest : PromptTemplateFormModel
{
}
