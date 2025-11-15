using Microsoft.Extensions.DependencyInjection;

namespace PRFactory.Web.Configuration;

/// <summary>
/// Configuration for Microsoft AG-UI integration.
///
/// NOTE: PRFactory uses a CUSTOM SSE implementation that is AG-UI protocol compatible.
/// This file documents why we don't use the official MapAGUI extension method.
/// </summary>
public static class AGUIConfiguration
{
    /// <summary>
    /// Configures AG-UI services for the application.
    ///
    /// IMPORTANT: We DO NOT call AddAGUI() or use MapAGUI() because:
    ///
    /// 1. **Multi-Agent Routing**: MapAGUI expects a single agent instance. PRFactory
    ///    dynamically selects agents (AnalyzerAgent, PlannerAgent, etc.) based on
    ///    tenant configuration and workflow state.
    ///
    /// 2. **Custom Chat History**: Our IAgentChatService persists conversation history
    ///    to the database and supports follow-up questions with entity tracking.
    ///
    /// 3. **Tenant Isolation**: Each request needs tenant context resolution before
    ///    creating the appropriate agent with tenant-specific configuration.
    ///
    /// 4. **Service Integration**: We inject ITicketService, IAgentFactory,
    ///    ITenantContext, and other services that MapAGUI doesn't support.
    ///
    /// 5. **Advanced Features**: We support question answering, agent status updates,
    ///    and real-time workflow state synchronization via SignalR.
    ///
    /// PROTOCOL COMPLIANCE: Our custom SSE implementation at /api/agent/chat/stream
    /// follows the AG-UI protocol specification:
    /// - Accepts user messages via query parameters
    /// - Returns Server-Sent Events (SSE) with "data: {json}\n\n" format
    /// - Streams chunks with types: Reasoning, ToolUse, Response, Complete, Error
    /// - Supports conversation history and multi-turn interactions
    ///
    /// MIGRATION PATH: When the official AG-UI package reaches GA (General Availability)
    /// and supports multi-agent scenarios, we can migrate to MapAGUI. Until then, our
    /// custom implementation provides the flexibility PRFactory requires.
    ///
    /// PACKAGE REFERENCE: Microsoft.Agents.AI.Hosting.AGUI.AspNetCore v1.0.0-preview
    /// is included in project dependencies for future use but not actively consumed.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection ConfigureAGUIProtocol(this IServiceCollection services)
    {
        // NOTE: We don't call AddAGUI() here because we use custom SSE implementation.
        // This method exists to document our AG-UI protocol compliance and design decisions.

        // Our AG-UI-compatible implementation is registered in Program.cs:
        // - IAgentChatService: Handles streaming responses with AG-UI protocol
        // - AgentChatController: Provides SSE endpoint at /api/agentchat/stream
        // - IAgentFactory: Creates agents dynamically based on tenant config

        return services;
    }
}

/// <summary>
/// Documentation of AG-UI protocol compliance in PRFactory.
/// </summary>
public static class AGUIProtocolCompliance
{
    /// <summary>
    /// AG-UI chunk types supported by PRFactory's SSE implementation.
    /// </summary>
    public static class ChunkTypes
    {
        /// <summary>Reasoning: Agent's thinking process (e.g., "Analyzing ticket requirements...")</summary>
        public const string Reasoning = "Reasoning";

        /// <summary>ToolUse: Agent invoking a tool (e.g., "Searching codebase...")</summary>
        public const string ToolUse = "ToolUse";

        /// <summary>Response: Agent's text response to user</summary>
        public const string Response = "Response";

        /// <summary>Complete: Final chunk indicating stream completion</summary>
        public const string Complete = "Complete";

        /// <summary>Error: Error occurred during execution</summary>
        public const string Error = "Error";
    }

    /// <summary>
    /// SSE format specification:
    /// data: {"type":"Reasoning","content":"Analyzing...","chunkId":"1","isFinal":false}
    /// data: {"type":"Response","content":"Here is...","chunkId":"2","isFinal":false}
    /// data: {"type":"Complete","content":"","chunkId":"3","isFinal":true}
    /// </summary>
    public const string SSEFormat = "data: {json}\n\n";

    /// <summary>
    /// Official AG-UI endpoint pattern (for reference).
    /// PRFactory uses /api/agent/chat/stream instead of MapAGUI.
    /// </summary>
    public const string OfficialEndpointPattern = "/";

    /// <summary>
    /// PRFactory's AG-UI-compatible endpoint.
    /// </summary>
    public const string PRFactoryEndpoint = "/api/agent/chat/stream";
}
