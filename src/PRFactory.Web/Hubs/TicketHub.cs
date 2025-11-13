using Microsoft.AspNetCore.SignalR;

namespace PRFactory.Web.Hubs;

/// <summary>
/// SignalR hub for real-time ticket updates
/// </summary>
public class TicketHub : Hub
{
    private const string ClientLogMessagePrefix = "Client {ConnectionId}";
    private const string SubscribedToTenantMessage = "Client {ConnectionId} subscribed to tenant {TenantId}";
    private const string SubscribedToTicketMessage = "Client {ConnectionId} subscribed to ticket {TicketId}";
    private const string UnsubscribedFromTicketMessage = "Client {ConnectionId} unsubscribed from ticket {TicketId}";
    private const string SubscribedToAllTicketsMessage = "Client {ConnectionId} subscribed to all tickets";
    private const string UnsubscribedFromAllTicketsMessage = "Client {ConnectionId} unsubscribed from all tickets";
    private const string DisconnectedWithErrorMessage = "Client {ConnectionId} disconnected with error";
    private const string DisconnectedMessage = "Client {ConnectionId} disconnected";

    private readonly ILogger<TicketHub> _logger;

    public TicketHub(ILogger<TicketHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Subscribe to updates for a specific tenant
    /// </summary>
    public async Task SubscribeToTenant(Guid tenantId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant-{tenantId}");
        _logger.LogInformation(SubscribedToTenantMessage,
            Context.ConnectionId, tenantId);
    }

    /// <summary>
    /// Subscribe to updates for a specific ticket
    /// Also subscribes to the tenant group for that ticket's tenant
    /// </summary>
    public async Task SubscribeToTicket(Guid ticketId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"ticket-{ticketId}");
        _logger.LogInformation(SubscribedToTicketMessage,
            Context.ConnectionId, ticketId);
    }

    /// <summary>
    /// Unsubscribe from updates for a specific ticket
    /// </summary>
    public async Task UnsubscribeFromTicket(Guid ticketId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ticket-{ticketId}");
        _logger.LogInformation(UnsubscribedFromTicketMessage,
            Context.ConnectionId, ticketId);
    }

    /// <summary>
    /// Subscribe to all ticket updates
    /// </summary>
    public async Task SubscribeToAllTickets()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "all-tickets");
        _logger.LogInformation(SubscribedToAllTicketsMessage,
            Context.ConnectionId);
    }

    /// <summary>
    /// Unsubscribe from all ticket updates
    /// </summary>
    public async Task UnsubscribeFromAllTickets()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "all-tickets");
        _logger.LogInformation(UnsubscribedFromAllTicketsMessage,
            Context.ConnectionId);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation(ClientLogMessagePrefix + " connected", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogError(exception, DisconnectedWithErrorMessage,
                Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation(DisconnectedMessage, Context.ConnectionId);
        }
        await base.OnDisconnectedAsync(exception);
    }
}
