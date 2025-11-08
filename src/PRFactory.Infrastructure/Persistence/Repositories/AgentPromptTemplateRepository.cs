using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;

namespace PRFactory.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for AgentPromptTemplate entity operations.
/// </summary>
public class AgentPromptTemplateRepository : IAgentPromptTemplateRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AgentPromptTemplateRepository> _logger;

    public AgentPromptTemplateRepository(
        ApplicationDbContext context,
        ILogger<AgentPromptTemplateRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AgentPromptTemplate?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.AgentPromptTemplates
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<AgentPromptTemplate?> GetByNameAsync(string name, Guid? tenantId = null, CancellationToken ct = default)
    {
        // First try to find tenant-specific template, then fallback to system template
        if (tenantId.HasValue)
        {
            var tenantTemplate = await _context.AgentPromptTemplates
                .FirstOrDefaultAsync(t => t.Name == name && t.TenantId == tenantId.Value, ct);

            if (tenantTemplate != null)
                return tenantTemplate;
        }

        // Fallback to system template
        return await _context.AgentPromptTemplates
            .FirstOrDefaultAsync(t => t.Name == name && t.IsSystemTemplate, ct);
    }

    public async Task<List<AgentPromptTemplate>> GetAvailableForTenantAsync(Guid tenantId, CancellationToken ct = default)
    {
        // Get both system templates and tenant-specific templates
        return await _context.AgentPromptTemplates
            .Where(t => t.IsSystemTemplate || t.TenantId == tenantId)
            .OrderBy(t => t.Category)
            .ThenBy(t => t.Name)
            .ToListAsync(ct);
    }

    public async Task<List<AgentPromptTemplate>> GetByCategoryAsync(string category, Guid? tenantId = null, CancellationToken ct = default)
    {
        var query = _context.AgentPromptTemplates
            .Where(t => t.Category == category);

        if (tenantId.HasValue)
        {
            query = query.Where(t => t.IsSystemTemplate || t.TenantId == tenantId.Value);
        }
        else
        {
            query = query.Where(t => t.IsSystemTemplate);
        }

        return await query
            .OrderBy(t => t.Name)
            .ToListAsync(ct);
    }

    public async Task<List<AgentPromptTemplate>> GetSystemTemplatesAsync(CancellationToken ct = default)
    {
        return await _context.AgentPromptTemplates
            .Where(t => t.IsSystemTemplate)
            .OrderBy(t => t.Category)
            .ThenBy(t => t.Name)
            .ToListAsync(ct);
    }

    public async Task<List<AgentPromptTemplate>> GetTenantTemplatesAsync(Guid tenantId, CancellationToken ct = default)
    {
        return await _context.AgentPromptTemplates
            .Where(t => t.TenantId == tenantId)
            .OrderBy(t => t.Category)
            .ThenBy(t => t.Name)
            .ToListAsync(ct);
    }

    public async Task AddAsync(AgentPromptTemplate template, CancellationToken ct = default)
    {
        _context.AgentPromptTemplates.Add(template);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Created {TemplateType} template {TemplateName} ({TemplateId})",
            template.IsSystemTemplate ? "system" : "tenant",
            template.Name,
            template.Id);
    }

    public async Task AddRangeAsync(IEnumerable<AgentPromptTemplate> templates, CancellationToken ct = default)
    {
        var templateList = templates.ToList();
        _context.AgentPromptTemplates.AddRange(templateList);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Created {Count} agent prompt templates in bulk", templateList.Count);
    }

    public async Task UpdateAsync(AgentPromptTemplate template, CancellationToken ct = default)
    {
        _context.AgentPromptTemplates.Update(template);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Updated template {TemplateName} ({TemplateId})",
            template.Name,
            template.Id);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var template = await GetByIdAsync(id, ct);
        if (template == null)
        {
            _logger.LogWarning("Attempted to delete non-existent template {TemplateId}", id);
            return;
        }

        if (template.IsSystemTemplate)
        {
            throw new InvalidOperationException("System templates cannot be deleted");
        }

        _context.AgentPromptTemplates.Remove(template);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Deleted template {TemplateName} ({TemplateId})", template.Name, template.Id);
    }

    public async Task<bool> ExistsAsync(string name, Guid? tenantId = null, CancellationToken ct = default)
    {
        if (tenantId.HasValue)
        {
            return await _context.AgentPromptTemplates
                .AnyAsync(t => t.Name == name && (t.IsSystemTemplate || t.TenantId == tenantId.Value), ct);
        }

        return await _context.AgentPromptTemplates
            .AnyAsync(t => t.Name == name && t.IsSystemTemplate, ct);
    }
}
