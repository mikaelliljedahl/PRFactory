using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.ValueObjects;
using PRFactory.Web.Models;
using System.Net.Http.Json;

namespace PRFactory.Web.Services;

/// <summary>
/// Implementation of ticket service.
/// Uses direct application service injection for ticket update operations (Blazor Server architecture).
/// Uses HttpClient for other operations (legacy - should be refactored to use application services).
/// </summary>
public class TicketService : ITicketService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TicketService> _logger;
    private readonly ITicketUpdateService _ticketUpdateService;

    public TicketService(
        IHttpClientFactory httpClientFactory,
        ILogger<TicketService> logger,
        ITicketUpdateService ticketUpdateService)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _ticketUpdateService = ticketUpdateService;
    }

    private HttpClient CreateClient()
    {
        return _httpClientFactory.CreateClient("PRFactoryApi");
    }

    public async Task<List<Ticket>> GetAllTicketsAsync(CancellationToken ct = default)
    {
        try
        {
            var client = CreateClient();
            var tickets = await client.GetFromJsonAsync<List<Ticket>>("/api/tickets", ct);
            return tickets ?? new List<Ticket>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all tickets");
            throw;
        }
    }

    public async Task<Ticket?> GetTicketByIdAsync(Guid ticketId, CancellationToken ct = default)
    {
        try
        {
            var client = CreateClient();
            return await client.GetFromJsonAsync<Ticket>($"/api/tickets/{ticketId}", ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Ticket {TicketId} not found", ticketId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching ticket {TicketId}", ticketId);
            throw;
        }
    }

    public async Task<List<Ticket>> GetTicketsByRepositoryAsync(Guid repositoryId, CancellationToken ct = default)
    {
        try
        {
            var client = CreateClient();
            var tickets = await client.GetFromJsonAsync<List<Ticket>>($"/api/repositories/{repositoryId}/tickets", ct);
            return tickets ?? new List<Ticket>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching tickets for repository {RepositoryId}", repositoryId);
            throw;
        }
    }

    public async Task TriggerWorkflowAsync(Guid ticketId, CancellationToken ct = default)
    {
        try
        {
            var client = CreateClient();
            var response = await client.PostAsync($"/api/tickets/{ticketId}/trigger", null, ct);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Triggered workflow for ticket {TicketId}", ticketId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering workflow for ticket {TicketId}", ticketId);
            throw;
        }
    }

    public async Task ApprovePlanAsync(Guid ticketId, string? comments = null, CancellationToken ct = default)
    {
        try
        {
            var client = CreateClient();
            var request = new { Comments = comments };
            var response = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/approve", request, ct);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Approved plan for ticket {TicketId}", ticketId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving plan for ticket {TicketId}", ticketId);
            throw;
        }
    }

    public async Task RejectPlanAsync(Guid ticketId, string rejectionReason, CancellationToken ct = default)
    {
        try
        {
            var client = CreateClient();
            var request = new { RejectionReason = rejectionReason };
            var response = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/reject", request, ct);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Rejected plan for ticket {TicketId}", ticketId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting plan for ticket {TicketId}", ticketId);
            throw;
        }
    }

    public async Task SubmitAnswersAsync(Guid ticketId, Dictionary<string, string> answers, CancellationToken ct = default)
    {
        try
        {
            var client = CreateClient();
            var response = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/answers", answers, ct);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Submitted answers for ticket {TicketId}", ticketId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting answers for ticket {TicketId}", ticketId);
            throw;
        }
    }

    public async Task<TicketUpdateDto?> GetLatestTicketUpdateAsync(Guid ticketId, CancellationToken ct = default)
    {
        try
        {
            // Use application service directly (Blazor Server architecture)
            var ticketUpdate = await _ticketUpdateService.GetLatestTicketUpdateAsync(ticketId, ct);
            if (ticketUpdate == null)
            {
                _logger.LogWarning("No ticket update found for ticket {TicketId}", ticketId);
                return null;
            }

            // Map entity to DTO
            return MapToDto(ticketUpdate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching ticket update for ticket {TicketId}", ticketId);
            throw;
        }
    }

    public async Task UpdateTicketUpdateAsync(Guid ticketUpdateId, TicketUpdateDto ticketUpdate, CancellationToken ct = default)
    {
        try
        {
            // Use application service directly (Blazor Server architecture)
            await _ticketUpdateService.UpdateTicketUpdateAsync(
                ticketUpdateId,
                ticketUpdate.UpdatedTitle,
                ticketUpdate.UpdatedDescription,
                ticketUpdate.AcceptanceCriteria,
                ct);

            _logger.LogInformation("Updated ticket update {TicketUpdateId}", ticketUpdateId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating ticket update {TicketUpdateId}", ticketUpdateId);
            throw;
        }
    }

    public async Task ApproveTicketUpdateAsync(Guid ticketUpdateId, CancellationToken ct = default)
    {
        try
        {
            // Use application service directly (Blazor Server architecture)
            await _ticketUpdateService.ApproveTicketUpdateAsync(ticketUpdateId, approvedBy: null, ct);
            _logger.LogInformation("Approved ticket update {TicketUpdateId}", ticketUpdateId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving ticket update {TicketUpdateId}", ticketUpdateId);
            throw;
        }
    }

    public async Task RejectTicketUpdateAsync(Guid ticketUpdateId, string rejectionReason, CancellationToken ct = default)
    {
        try
        {
            // Use application service directly (Blazor Server architecture)
            await _ticketUpdateService.RejectTicketUpdateAsync(
                ticketUpdateId,
                rejectionReason,
                rejectedBy: null,
                regenerate: true,
                ct);

            _logger.LogInformation("Rejected ticket update {TicketUpdateId}", ticketUpdateId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting ticket update {TicketUpdateId}", ticketUpdateId);
            throw;
        }
    }

    /// <summary>
    /// Maps a TicketUpdate entity to a TicketUpdateDto
    /// </summary>
    private TicketUpdateDto MapToDto(TicketUpdate ticketUpdate)
    {
        return new TicketUpdateDto
        {
            Id = ticketUpdate.Id,
            TicketId = ticketUpdate.TicketId,
            UpdatedTitle = ticketUpdate.UpdatedTitle,
            UpdatedDescription = ticketUpdate.UpdatedDescription,
            AcceptanceCriteria = ticketUpdate.AcceptanceCriteria,
            Version = ticketUpdate.Version,
            IsDraft = ticketUpdate.IsDraft,
            IsApproved = ticketUpdate.IsApproved,
            RejectionReason = ticketUpdate.RejectionReason,
            GeneratedAt = ticketUpdate.GeneratedAt,
            ApprovedAt = ticketUpdate.ApprovedAt,
            PostedAt = ticketUpdate.PostedAt,
            SuccessCriteria = ticketUpdate.SuccessCriteria.Select(sc => new SuccessCriterionDto
            {
                Category = sc.Category,
                Description = sc.Description,
                Priority = sc.Priority,
                IsTestable = sc.IsTestable
            }).ToList()
        };
    }
}
