using Microsoft.AspNetCore.SignalR;
using PRFactory.Infrastructure.Events;
using PRFactory.Web.Hubs;

namespace PRFactory.Web.Services;

/// <summary>
/// SignalR implementation of IEventBroadcaster.
/// Broadcasts events to real-time clients via TicketHub.
/// </summary>
public class SignalREventBroadcaster : IEventBroadcaster
{
    private readonly IHubContext<TicketHub> _hubContext;
    private readonly ILogger<SignalREventBroadcaster> _logger;

    public SignalREventBroadcaster(
        IHubContext<TicketHub> hubContext,
        ILogger<SignalREventBroadcaster> logger)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Broadcasts an event to all specified SignalR groups
    /// </summary>
    public async Task BroadcastAsync(IEnumerable<string> groups, string eventName, object eventData)
    {
        try
        {
            foreach (var group in groups)
            {
                await _hubContext.Clients.Group(group).SendAsync(eventName, eventData);
                _logger.LogDebug("Broadcasted {EventName} to group {Group}", eventName, group);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast event {EventName} to SignalR groups", eventName);
            throw;
        }
    }
}
