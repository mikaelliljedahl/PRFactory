using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;
using Microsoft.AspNetCore.Mvc;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Infrastructure.Agents.Services;

namespace PRFactory.Web.Controllers;

/// <summary>
/// Manages AI agent prompt templates for customizable workflow stages
/// </summary>
[ApiController]
[Route("api/agent-prompt-templates")]
[Produces("application/json")]
public class AgentPromptTemplatesController : ControllerBase
{
    private const string TemplateNotFoundErrorMessage = "Template with ID {id} not found";

    private readonly IAgentPromptTemplateRepository _repository;
    private readonly IAgentPromptService _promptService;
    private readonly ILogger<AgentPromptTemplatesController> _logger;

    public AgentPromptTemplatesController(
        IAgentPromptTemplateRepository repository,
        IAgentPromptService promptService,
        ILogger<AgentPromptTemplatesController> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _promptService = promptService ?? throw new ArgumentNullException(nameof(promptService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all system prompt templates
    /// </summary>
    /// <returns>List of system templates</returns>
    [HttpGet("system")]
    [ProducesResponseType(typeof(List<AgentPromptTemplate>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSystemTemplates(CancellationToken ct)
    {
        var templates = await _repository.GetSystemTemplatesAsync(ct);
        return Ok(templates);
    }

    /// <summary>
    /// Gets all templates available for a tenant (system + tenant-specific)
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of available templates</returns>
    [HttpGet("tenant/{tenantId:guid}")]
    [ProducesResponseType(typeof(List<AgentPromptTemplate>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTenantTemplates(Guid tenantId, CancellationToken ct)
    {
        var templates = await _promptService.GetAvailableTemplatesAsync(tenantId, ct);
        return Ok(templates);
    }

    /// <summary>
    /// Gets all templates in a specific category for a tenant
    /// </summary>
    /// <param name="category">Category name (e.g., "Implementation", "Testing", "Planning")</param>
    /// <param name="tenantId">Optional tenant ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of templates in the category</returns>
    [HttpGet("category/{category}")]
    [ProducesResponseType(typeof(List<AgentPromptTemplate>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTemplatesByCategory(
        string category,
        [FromQuery] Guid? tenantId,
        CancellationToken ct)
    {
        var templates = await _promptService.GetPromptTemplatesByCategoryAsync(category, tenantId, ct);
        return Ok(templates);
    }

    /// <summary>
    /// Gets a specific template by ID
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The template</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AgentPromptTemplate), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTemplate(Guid id, CancellationToken ct)
    {
        var template = await _repository.GetByIdAsync(id, ct);
        if (template == null)
        {
            return NotFound(new { error = TemplateNotFoundErrorMessage });
        }

        return Ok(template);
    }

    /// <summary>
    /// Gets a template by name
    /// </summary>
    /// <param name="name">Template name</param>
    /// <param name="tenantId">Optional tenant ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The template</returns>
    [HttpGet("by-name/{name}")]
    [ProducesResponseType(typeof(AgentPromptTemplate), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTemplateByName(
        string name,
        [FromQuery] Guid? tenantId,
        CancellationToken ct)
    {
        var template = await _promptService.GetPromptTemplateAsync(name, tenantId, ct);
        if (template == null)
        {
            return NotFound(new { error = $"Template '{name}' not found" });
        }

        return Ok(template);
    }

    /// <summary>
    /// Creates a new tenant-specific template
    /// </summary>
    /// <param name="request">Template creation request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The created template</returns>
    [HttpPost]
    [ProducesResponseType(typeof(AgentPromptTemplate), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTemplate(
        [FromBody] CreateTemplateRequest request,
        CancellationToken ct)
    {
        if (request.TenantId == null)
        {
            return BadRequest(new { error = "TenantId is required for creating tenant templates" });
        }

        // Check if template with same name already exists for this tenant
        var existing = await _repository.ExistsAsync(request.Name, request.TenantId, ct);
        if (existing)
        {
            return BadRequest(new { error = $"Template with name '{request.Name}' already exists for this tenant" });
        }

        var template = AgentPromptTemplate.CreateTenantTemplate(
            tenantId: request.TenantId.Value,
            name: request.Name,
            description: request.Description,
            promptContent: request.PromptContent,
            category: request.Category,
            recommendedModel: request.RecommendedModel,
            color: request.Color
        );

        await _repository.AddAsync(template, ct);

        _logger.LogInformation(
            "Created tenant template {TemplateName} for tenant {TenantId}",
            template.Name,
            template.TenantId);

        return CreatedAtAction(
            nameof(GetTemplate),
            new { id = template.Id },
            template);
    }

    /// <summary>
    /// Clones a system template for tenant-specific customization
    /// </summary>
    /// <param name="id">System template ID to clone</param>
    /// <param name="tenantId">Tenant ID to clone for</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The cloned template</returns>
    [HttpPost("{id:guid}/clone")]
    [ProducesResponseType(typeof(AgentPromptTemplate), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CloneTemplate(
        Guid id,
        [FromQuery] Guid tenantId,
        CancellationToken ct)
    {
        var sourceTemplate = await _repository.GetByIdAsync(id, ct);
        if (sourceTemplate == null)
        {
            return NotFound(new { error = TemplateNotFoundErrorMessage });
        }

        // Check if clone already exists
        var existing = await _repository.ExistsAsync(sourceTemplate.Name, tenantId, ct);
        if (existing)
        {
            return BadRequest(new { error = $"Template with name '{sourceTemplate.Name}' already exists for this tenant" });
        }

        var clonedTemplate = sourceTemplate.CloneForTenant(tenantId);
        await _repository.AddAsync(clonedTemplate, ct);

        _logger.LogInformation(
            "Cloned template {TemplateName} ({SourceId}) for tenant {TenantId} (new ID: {NewId})",
            sourceTemplate.Name,
            sourceTemplate.Id,
            tenantId,
            clonedTemplate.Id);

        return CreatedAtAction(
            nameof(GetTemplate),
            new { id = clonedTemplate.Id },
            clonedTemplate);
    }

    /// <summary>
    /// Updates a tenant-specific template
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <param name="request">Update request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The updated template</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(AgentPromptTemplate), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTemplate(
        Guid id,
        [FromBody] UpdateTemplateRequest request,
        CancellationToken ct)
    {
        var template = await _repository.GetByIdAsync(id, ct);
        if (template == null)
        {
            return NotFound(new { error = TemplateNotFoundErrorMessage });
        }

        if (template.IsSystemTemplate)
        {
            return BadRequest(new { error = "System templates cannot be modified" });
        }

        template.Update(
            description: request.Description,
            promptContent: request.PromptContent,
            category: request.Category,
            recommendedModel: request.RecommendedModel,
            color: request.Color
        );

        await _repository.UpdateAsync(template, ct);

        _logger.LogInformation("Updated template {TemplateName} ({TemplateId})", template.Name, template.Id);

        return Ok(template);
    }

    /// <summary>
    /// Deletes a tenant-specific template
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTemplate(Guid id, CancellationToken ct)
    {
        var template = await _repository.GetByIdAsync(id, ct);
        if (template == null)
        {
            return NotFound(new { error = TemplateNotFoundErrorMessage });
        }

        if (template.IsSystemTemplate)
        {
            return BadRequest(new { error = "System templates cannot be deleted" });
        }

        await _repository.DeleteAsync(id, ct);

        _logger.LogInformation("Deleted template {TemplateName} ({TemplateId})", template.Name, template.Id);

        return NoContent();
    }
}

/// <summary>
/// Request model for creating a new template
/// </summary>
public record CreateTemplateRequest(
    Guid? TenantId,
    string Name,
    string Description,
    string PromptContent,
    string Category,
    string? RecommendedModel = null,
    string? Color = null
);

/// <summary>
/// Request model for updating a template
/// </summary>
public record UpdateTemplateRequest(
    string? Description = null,
    string? PromptContent = null,
    string? Category = null,
    string? RecommendedModel = null,
    string? Color = null
);
