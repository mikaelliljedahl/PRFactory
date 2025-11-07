namespace PRFactory.Infrastructure.Events;

/// <summary>
/// Abstraction for broadcasting events to real-time clients.
/// This interface decouples Infrastructure from SignalR/Web layer.
/// </summary>
public interface IEventBroadcaster
{
    /// <summary>
    /// Broadcasts an event to clients in the specified groups
    /// </summary>
    /// <param name="groups">Group names to broadcast to</param>
    /// <param name="eventName">Name of the event method to invoke on clients</param>
    /// <param name="eventData">Event data to send</param>
    Task BroadcastAsync(IEnumerable<string> groups, string eventName, object eventData);
}
