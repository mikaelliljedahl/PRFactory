using Microsoft.Extensions.DependencyInjection;

namespace PRFactory.Tests.Blazor;

/// <summary>
/// Base class for testing Blazor pages
/// Includes configuration for real-time features
/// </summary>
public abstract class PageTestBase : TestContextBase
{
    protected PageTestBase()
    {
        // Pages may use SignalR for real-time updates
        // HubConnection is a concrete class and difficult to mock
        // Tests should mock the service layer that interacts with SignalR instead
    }

    /// <summary>
    /// Simulates real-time updates by calling the service layer directly
    /// </summary>
    /// <remarks>
    /// bUnit doesn't directly support SignalR testing.
    /// Tests should mock the service layer that consumes SignalR messages
    /// instead of testing SignalR directly.
    /// </remarks>
    protected void SimulateRealtimeUpdate<TMessage>(TMessage message)
    {
        // Override in specific test classes to simulate real-time updates
        // by calling the mocked service layer directly
    }
}
