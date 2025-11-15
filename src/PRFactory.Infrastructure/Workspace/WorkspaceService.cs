using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;

namespace PRFactory.Infrastructure.Workspace;

/// <summary>
/// Implementation of workspace service for managing ticket workspaces.
/// </summary>
public class WorkspaceService : IWorkspaceService
{
    private readonly ILogger<WorkspaceService> _logger;
    private readonly string _workspaceBasePath;

    public WorkspaceService(
        IConfiguration configuration,
        ILogger<WorkspaceService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Read from configuration, default to /var/prfactory/workspace
        _workspaceBasePath = configuration["Workspace:BasePath"] ?? "/var/prfactory/workspace";

        _logger.LogInformation("WorkspaceService initialized with base path: {BasePath}", _workspaceBasePath);
    }

    public string GetWorkspaceDirectory(Guid ticketId)
    {
        if (ticketId == Guid.Empty)
            throw new ArgumentException("Ticket ID cannot be empty", nameof(ticketId));

        // Use ticket ID as subdirectory (consistent, predictable)
        return Path.Combine(_workspaceBasePath, ticketId.ToString());
    }

    public string GetRepositoryPath(Guid ticketId)
    {
        var workspaceDir = GetWorkspaceDirectory(ticketId);
        return Path.Combine(workspaceDir, "repo");
    }

    public string GetDiffPath(Guid ticketId)
    {
        var workspaceDir = GetWorkspaceDirectory(ticketId);
        return Path.Combine(workspaceDir, "diff.patch");
    }

    public async Task<string?> ReadDiffAsync(Guid ticketId)
    {
        var diffPath = GetDiffPath(ticketId);

        if (!File.Exists(diffPath))
        {
            _logger.LogDebug("Diff file not found for ticket {TicketId} at {DiffPath}", ticketId, diffPath);
            return null;
        }

        _logger.LogDebug("Reading diff file for ticket {TicketId} from {DiffPath}", ticketId, diffPath);

        try
        {
            return await File.ReadAllTextAsync(diffPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading diff file for ticket {TicketId} from {DiffPath}", ticketId, diffPath);
            throw new InvalidOperationException($"Failed to read diff file for ticket {ticketId}", ex);
        }
    }

    public async Task WriteDiffAsync(Guid ticketId, string diffContent)
    {
        if (string.IsNullOrEmpty(diffContent))
        {
            _logger.LogWarning("Attempted to write empty diff for ticket {TicketId}", ticketId);
            throw new ArgumentException("Diff content cannot be empty", nameof(diffContent));
        }

        var diffPath = GetDiffPath(ticketId);
        var workspaceDir = Path.GetDirectoryName(diffPath)!;

        // Ensure workspace directory exists
        if (!Directory.Exists(workspaceDir))
        {
            _logger.LogInformation("Creating workspace directory: {WorkspaceDir}", workspaceDir);
            Directory.CreateDirectory(workspaceDir);
        }

        _logger.LogInformation("Writing diff file for ticket {TicketId} to {DiffPath} ({Size} bytes)",
            ticketId, diffPath, diffContent.Length);

        try
        {
            await File.WriteAllTextAsync(diffPath, diffContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing diff file for ticket {TicketId} to {DiffPath}", ticketId, diffPath);
            throw new InvalidOperationException($"Failed to write diff file for ticket {ticketId}", ex);
        }
    }

    public Task<bool> DiffExistsAsync(Guid ticketId)
    {
        var diffPath = GetDiffPath(ticketId);
        return Task.FromResult(File.Exists(diffPath));
    }

    public Task DeleteDiffAsync(Guid ticketId)
    {
        var diffPath = GetDiffPath(ticketId);

        if (File.Exists(diffPath))
        {
            _logger.LogInformation("Deleting diff file for ticket {TicketId} at {DiffPath}", ticketId, diffPath);
            File.Delete(diffPath);
        }
        else
        {
            _logger.LogDebug("Diff file not found for deletion: {DiffPath}", diffPath);
        }

        return Task.CompletedTask;
    }

    public Task<string> EnsureWorkspaceExistsAsync(Guid ticketId)
    {
        var workspaceDir = GetWorkspaceDirectory(ticketId);

        if (!Directory.Exists(workspaceDir))
        {
            _logger.LogInformation("Creating workspace directory: {WorkspaceDir}", workspaceDir);
            Directory.CreateDirectory(workspaceDir);
        }

        return Task.FromResult(workspaceDir);
    }
}
