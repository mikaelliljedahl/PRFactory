using PRFactory.Domain.Entities;
using System.Net.Http.Json;

namespace PRFactory.Web.Services;

/// <summary>
/// Implementation of ticket service using HttpClient to call PRFactory.Api
/// </summary>
public class TicketService : ITicketService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TicketService> _logger;

    public TicketService(IHttpClientFactory httpClientFactory, ILogger<TicketService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
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
}
