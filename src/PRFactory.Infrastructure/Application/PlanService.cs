using LibGit2Sharp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Interfaces;
using PRFactory.Domain.ValueObjects;

namespace PRFactory.Infrastructure.Application;

/// <summary>
/// Application service for retrieving implementation plans.
/// Plans are stored as markdown files in git branches.
/// </summary>
public class PlanService : IPlanService
{
    private readonly ILogger<PlanService> _logger;
    private readonly ITicketRepository _ticketRepository;
    private readonly IRepositoryRepository _repositoryRepository;
    private readonly IConfiguration _configuration;
    private readonly string _workspaceBasePath;

    public PlanService(
        ILogger<PlanService> logger,
        ITicketRepository ticketRepository,
        IRepositoryRepository repositoryRepository,
        IConfiguration configuration)
    {
        _logger = logger;
        _ticketRepository = ticketRepository;
        _repositoryRepository = repositoryRepository;
        _configuration = configuration;
        _workspaceBasePath = configuration["Workspace:BasePath"] ?? "/var/prfactory/workspace";
    }

    /// <inheritdoc/>
    public async Task<PlanInfo?> GetPlanAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting plan for ticket {TicketId}", ticketId);

        var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken);
        if (ticket == null)
        {
            _logger.LogWarning("Ticket {TicketId} not found", ticketId);
            return null;
        }

        // Check if ticket has a plan branch
        if (string.IsNullOrEmpty(ticket.PlanBranchName))
        {
            _logger.LogDebug("Ticket {TicketId} does not have a plan branch", ticketId);
            return null;
        }

        // Get repository info
        var repository = await _repositoryRepository.GetByIdAsync(ticket.RepositoryId, cancellationToken);
        if (repository == null)
        {
            _logger.LogWarning("Repository {RepositoryId} not found for ticket {TicketId}",
                ticket.RepositoryId, ticketId);
            return null;
        }

        // Find the cloned repository path
        // The workspace structure is: {workspaceBasePath}/{guid}/{repoName}
        // We need to find the directory for this repository
        var repoPath = FindRepositoryPath(repository.Name);
        if (repoPath == null)
        {
            _logger.LogWarning("Repository {RepositoryName} not found in workspace", repository.Name);
            return null;
        }

        try
        {
            // Read plan content from the branch
            var planContent = await ReadPlanFromBranchAsync(
                repoPath,
                ticket.PlanBranchName,
                ticket.PlanMarkdownPath ?? "PLAN.md",
                cancellationToken);

            if (planContent == null)
            {
                _logger.LogWarning("Plan file not found in branch {BranchName} for ticket {TicketId}",
                    ticket.PlanBranchName, ticketId);
                return null;
            }

            return new PlanInfo
            {
                BranchName = ticket.PlanBranchName,
                MarkdownPath = ticket.PlanMarkdownPath,
                Content = planContent,
                CreatedAt = ticket.UpdatedAt,
                IsApproved = ticket.PlanApprovedAt.HasValue,
                ApprovedAt = ticket.PlanApprovedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading plan for ticket {TicketId} from branch {BranchName}",
                ticketId, ticket.PlanBranchName);
            return null;
        }
    }

    /// <summary>
    /// Finds the repository path in the workspace
    /// </summary>
    private string? FindRepositoryPath(string repoName)
    {
        if (!Directory.Exists(_workspaceBasePath))
        {
            return null;
        }

        // Search for the repository in all workspace directories
        var directories = Directory.GetDirectories(_workspaceBasePath, "*", SearchOption.AllDirectories)
            .Where(d => Path.GetFileName(d).Equals(repoName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (directories.Count == 0)
        {
            return null;
        }

        // Return the most recently modified directory
        return directories
            .OrderByDescending(d => Directory.GetLastWriteTime(d))
            .FirstOrDefault();
    }

    /// <summary>
    /// Reads plan content from a specific branch using LibGit2Sharp
    /// </summary>
    private async Task<string?> ReadPlanFromBranchAsync(
        string repoPath,
        string branchName,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            using var repo = new Repository(repoPath);

            // Find the branch
            var branch = repo.Branches[branchName];
            if (branch == null)
            {
                _logger.LogWarning("Branch {BranchName} not found in repository {RepoPath}",
                    branchName, repoPath);
                return null;
            }

            // Get the tree from the branch's tip commit
            var commit = branch.Tip;
            var tree = commit.Tree;

            // Find the file in the tree
            var treeEntry = tree[filePath];
            if (treeEntry == null)
            {
                _logger.LogDebug("File {FilePath} not found in branch {BranchName}",
                    filePath, branchName);
                return null;
            }

            // Read the blob content
            if (treeEntry.TargetType != TreeEntryTargetType.Blob)
            {
                _logger.LogWarning("Tree entry {FilePath} is not a blob", filePath);
                return null;
            }

            var blob = (Blob)treeEntry.Target;
            var contentStream = blob.GetContentStream();
            using var reader = new StreamReader(contentStream);
            return reader.ReadToEnd();

        }, cancellationToken);
    }
}
