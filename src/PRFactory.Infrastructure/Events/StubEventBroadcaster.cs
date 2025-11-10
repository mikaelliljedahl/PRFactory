namespace PRFactory.Infrastructure.Events;

/// <summary>
/// Stub implementation of IEventBroadcaster for testing and when the Web layer is not available.
/// This allows Infrastructure layer tests to run without requiring SignalR/Web dependencies.
/// </summary>
public class StubEventBroadcaster : IEventBroadcaster
{
    /// <summary>
    /// No-op implementation that does not broadcast events
    /// </summary>
    public Task BroadcastAsync(IEnumerable<string> groups, string eventName, object eventData)
    {
        // No-op: Do nothing for stub implementation
        return Task.CompletedTask;
    }
}
