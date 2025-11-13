using Bunit;
using PRFactory.Domain.ValueObjects;
using PRFactory.Tests.Blazor;
using PRFactory.Tests.Blazor.TestDataBuilders;
using PRFactory.Web.Components;
using Xunit;

namespace PRFactory.Tests.Components;

public class TicketListItemTests : ComponentTestBase
{
    [Fact]
    public void Render_WithTicket_DisplaysTicketInfo()
    {
        // Arrange
        var ticket = new TicketDtoBuilder()
            .WithTicketKey("TEST-123")
            .WithTitle("Test Ticket")
            .WithState(WorkflowState.Planning)
            .Build();

        // Act
        var cut = RenderComponent<TicketListItem>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        Assert.Contains("TEST-123", cut.Markup);
        Assert.Contains("Test Ticket", cut.Markup);
    }

    [Fact]
    public void Render_WithState_DisplaysStatusBadge()
    {
        // Arrange
        var ticket = new TicketDtoBuilder()
            .WithState(WorkflowState.Completed)
            .Build();

        // Act
        var cut = RenderComponent<TicketListItem>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        Assert.Contains("Completed", cut.Markup);
    }

    [Fact]
    public void Render_WithTicket_ContainsViewDetailsLink()
    {
        // Arrange
        var ticket = new TicketDtoBuilder().Build();

        // Act
        var cut = RenderComponent<TicketListItem>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        Assert.Contains("View Details", cut.Markup);
        Assert.Contains($"/tickets/{ticket.Id}", cut.Markup);
    }
}
