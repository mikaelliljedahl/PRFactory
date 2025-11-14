using LibGit2Sharp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.DTOs;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
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
    private readonly IPlanRevisionRepository _planRevisionRepository;
    private readonly IConfiguration _configuration;
    private readonly string _workspaceBasePath;

    public PlanService(
        ILogger<PlanService> logger,
        ITicketRepository ticketRepository,
        IRepositoryRepository repositoryRepository,
        IPlanRevisionRepository planRevisionRepository,
        IConfiguration configuration)
    {
        _logger = logger;
        _ticketRepository = ticketRepository;
        _repositoryRepository = repositoryRepository;
        _planRevisionRepository = planRevisionRepository;
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
            using var repo = new LibGit2Sharp.Repository(repoPath);

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

    /// <inheritdoc/>
    public async Task<List<PlanRevisionDto>> GetPlanRevisionsAsync(Guid ticketId)
    {
        _logger.LogDebug("Getting plan revisions for ticket {TicketId}", ticketId);

        var revisions = await _planRevisionRepository.GetByTicketIdAsync(ticketId);
        return revisions.Select(PlanRevisionDto.FromEntity).ToList();
    }

    /// <inheritdoc/>
    public async Task<PlanRevisionDto?> GetPlanRevisionAsync(Guid revisionId)
    {
        _logger.LogDebug("Getting plan revision {RevisionId}", revisionId);

        var revision = await _planRevisionRepository.GetByIdAsync(revisionId);
        return revision != null ? PlanRevisionDto.FromEntity(revision) : null;
    }

    /// <inheritdoc/>
    public async Task<PlanRevisionDto> CreateRevisionAsync(
        Guid ticketId,
        PlanRevisionReason reason,
        Guid? createdByUserId = null)
    {
        _logger.LogInformation(
            "Creating plan revision for ticket {TicketId} with reason {Reason}",
            ticketId, reason);

        // Get current plan
        var plan = await GetPlanAsync(ticketId);
        if (plan == null)
        {
            throw new InvalidOperationException($"No plan found for ticket {ticketId}");
        }

        // Get next revision number
        var revisionNumber = await _planRevisionRepository.GetNextRevisionNumberAsync(ticketId);

        // Get commit hash from git
        // TODO: Implement GetLatestCommitHashAsync using LibGit2Sharp
        var commitHash = await GetLatestCommitHashAsync(plan.BranchName);

        // Create revision
        var revision = PlanRevision.Create(
            ticketId: ticketId,
            revisionNumber: revisionNumber,
            branchName: plan.BranchName,
            markdownPath: plan.MarkdownPath ?? "PLAN.md",
            commitHash: commitHash,
            content: plan.Content,
            reason: reason,
            createdByUserId: createdByUserId);

        await _planRevisionRepository.CreateAsync(revision);

        _logger.LogInformation(
            "Created plan revision {RevisionId} (rev #{RevisionNumber}) for ticket {TicketId}",
            revision.Id, revisionNumber, ticketId);

        return PlanRevisionDto.FromEntity(revision);
    }

    /// <inheritdoc/>
    public async Task<PlanRevisionComparisonDto> CompareRevisionsAsync(
        Guid revision1Id,
        Guid revision2Id)
    {
        _logger.LogDebug("Comparing plan revisions {Revision1Id} and {Revision2Id}",
            revision1Id, revision2Id);

        var revision1 = await _planRevisionRepository.GetByIdAsync(revision1Id);
        var revision2 = await _planRevisionRepository.GetByIdAsync(revision2Id);

        if (revision1 == null || revision2 == null)
        {
            throw new ArgumentException("One or both revisions not found");
        }

        if (revision1.TicketId != revision2.TicketId)
        {
            throw new ArgumentException("Revisions must be from the same ticket");
        }

        // Generate diff (simple line-by-line comparison)
        // TODO: Use DiffPlex NuGet package for proper diff algorithm with syntax highlighting
        var diff = GenerateTextDiff(revision1.Content, revision2.Content);

        return new PlanRevisionComparisonDto
        {
            Revision1 = PlanRevisionDto.FromEntity(revision1),
            Revision2 = PlanRevisionDto.FromEntity(revision2),
            DiffLines = diff
        };
    }

    /// <summary>
    /// Gets the latest commit hash from git for a branch
    /// TODO: Implement using LibGit2Sharp to get actual commit hash
    /// </summary>
    private async Task<string> GetLatestCommitHashAsync(string branchName)
    {
        // Placeholder implementation
        // In production, use LibGit2Sharp to get the latest commit hash from the branch
        _logger.LogWarning(
            "GetLatestCommitHashAsync not fully implemented - returning placeholder commit hash for branch {BranchName}",
            branchName);

        return await Task.FromResult($"placeholder-commit-hash-{DateTime.UtcNow:yyyyMMddHHmmss}");
    }

    /// <summary>
    /// Generates a simple line-by-line diff between two text contents
    /// TODO: Replace with DiffPlex NuGet package for proper diff algorithm
    /// </summary>
    private List<DiffLine> GenerateTextDiff(string content1, string content2)
    {
        var lines1 = content1.Split('\n');
        var lines2 = content2.Split('\n');

        var diffLines = new List<DiffLine>();

        // Simple line-by-line comparison
        // For production, use a proper diff algorithm (DiffPlex, diff-match-patch, etc.)
        var maxLines = Math.Max(lines1.Length, lines2.Length);

        for (int i = 0; i < maxLines; i++)
        {
            var line1 = i < lines1.Length ? lines1[i] : null;
            var line2 = i < lines2.Length ? lines2[i] : null;

            if (line1 == line2)
            {
                diffLines.Add(new DiffLine
                {
                    LineNumber = i + 1,
                    Type = DiffLineType.Unchanged,
                    Content = line1 ?? ""
                });
            }
            else if (line1 == null)
            {
                diffLines.Add(new DiffLine
                {
                    LineNumber = i + 1,
                    Type = DiffLineType.Added,
                    Content = line2 ?? ""
                });
            }
            else if (line2 == null)
            {
                diffLines.Add(new DiffLine
                {
                    LineNumber = i + 1,
                    Type = DiffLineType.Removed,
                    Content = line1
                });
            }
            else
            {
                diffLines.Add(new DiffLine
                {
                    LineNumber = i + 1,
                    Type = DiffLineType.Modified,
                    OldContent = line1,
                    Content = line2
                });
            }
        }

        return diffLines;
    }
}
