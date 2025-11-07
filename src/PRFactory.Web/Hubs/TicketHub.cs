using Microsoft.AspNetCore.SignalR;

namespace PRFactory.Web.Hubs;

/// <summary>
/// SignalR hub for real-time ticket updates
/// </summary>
public class TicketHub : Hub
{
    private readonly ILogger<TicketHub> _logger;

    public TicketHub(ILogger<TicketHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Subscribe to updates for a specific ticket
    /// </summary>
    public async Task SubscribeToTicket(Guid ticketId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"ticket-{ticketId}");
        _logger.LogInformation("Client {ConnectionId} subscribed to ticket {TicketId}",
            Context.ConnectionId, ticketId);
    }

    /// <summary>
    /// Unsubscribe from updates for a specific ticket
    /// </summary>
    public async Task UnsubscribeFromTicket(Guid ticketId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ticket-{ticketId}");
        _logger.LogInformation("Client {ConnectionId} unsubscribed from ticket {TicketId}",
            Context.ConnectionId, ticketId);
    }

    /// <summary>
    /// Subscribe to all ticket updates
    /// </summary>
    public async Task SubscribeToAllTickets()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "all-tickets");
        _logger.LogInformation("Client {ConnectionId} subscribed to all tickets",
            Context.ConnectionId);
    }

    /// <summary>
    /// Unsubscribe from all ticket updates
    /// </summary>
    public async Task UnsubscribeFromAllTickets()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "all-tickets");
        _logger.LogInformation("Client {ConnectionId} unsubscribed from all tickets",
            Context.ConnectionId);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client {ConnectionId} connected", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogError(exception, "Client {ConnectionId} disconnected with error",
                Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation("Client {ConnectionId} disconnected", Context.ConnectionId);
        }
        await base.OnDisconnectedAsync(exception);
    }
}
