using Bunit;
using PRFactory.Domain.ValueObjects;
using PRFactory.Tests.Blazor;
using PRFactory.Tests.Blazor.TestDataBuilders;
using PRFactory.Web.Components.Tickets;
using Xunit;

namespace PRFactory.Tests.Components.Tickets;

public class TicketDiffViewerTests : ComponentTestBase
{
    [Fact]
    public void Renders_TitleChange()
    {
        // Arrange
        var originalTicket = new TicketDtoBuilder()
            .WithTitle("Original Title")
            .WithDescription("Original Description")
            .Build();

        var ticketUpdate = new TicketUpdateDtoBuilder()
            .WithUpdatedTitle("Updated Title")
            .WithUpdatedDescription("Original Description")
            .Build();

        // Act
        var cut = RenderComponent<TicketDiffViewer>(parameters => parameters
            .Add(p => p.OriginalTicket, originalTicket)
            .Add(p => p.TicketUpdate, ticketUpdate));

        // Assert
        Assert.Contains("Original Title", cut.Markup);
        Assert.Contains("Updated Title", cut.Markup);
    }

    [Fact]
    public void Renders_DescriptionChange()
    {
        // Arrange
        var originalTicket = new TicketDtoBuilder()
            .WithTitle("Same Title")
            .WithDescription("Original Description")
            .Build();

        var ticketUpdate = new TicketUpdateDtoBuilder()
            .WithUpdatedTitle("Same Title")
            .WithUpdatedDescription("Updated Description")
            .Build();

        // Act
        var cut = RenderComponent<TicketDiffViewer>(parameters => parameters
            .Add(p => p.OriginalTicket, originalTicket)
            .Add(p => p.TicketUpdate, ticketUpdate));

        // Assert
        Assert.Contains("Original Description", cut.Markup);
        Assert.Contains("Updated Description", cut.Markup);
    }

    [Fact]
    public void Renders_BothTitleAndDescriptionChanges()
    {
        // Arrange
        var originalTicket = new TicketDtoBuilder()
            .WithTitle("Original Title")
            .WithDescription("Original Description")
            .Build();

        var ticketUpdate = new TicketUpdateDtoBuilder()
            .WithUpdatedTitle("Updated Title")
            .WithUpdatedDescription("Updated Description")
            .Build();

        // Act
        var cut = RenderComponent<TicketDiffViewer>(parameters => parameters
            .Add(p => p.OriginalTicket, originalTicket)
            .Add(p => p.TicketUpdate, ticketUpdate));

        // Assert
        Assert.Contains("Original Title", cut.Markup);
        Assert.Contains("Updated Title", cut.Markup);
        Assert.Contains("Original Description", cut.Markup);
        Assert.Contains("Updated Description", cut.Markup);
    }

    [Fact]
    public void Renders_NoChanges_WhenTitleAndDescriptionAreSame()
    {
        // Arrange
        var originalTicket = new TicketDtoBuilder()
            .WithTitle("Same Title")
            .WithDescription("Same Description")
            .Build();

        var ticketUpdate = new TicketUpdateDtoBuilder()
            .WithUpdatedTitle("Same Title")
            .WithUpdatedDescription("Same Description")
            .Build();

        // Act
        var cut = RenderComponent<TicketDiffViewer>(parameters => parameters
            .Add(p => p.OriginalTicket, originalTicket)
            .Add(p => p.TicketUpdate, ticketUpdate));

        // Assert - Should still render, but without change indicators
        Assert.NotNull(cut.Markup);
    }

    [Fact]
    public void Renders_MarkdownContent()
    {
        // Arrange
        var originalTicket = new TicketDtoBuilder()
            .WithDescription("# Original Heading")
            .Build();

        var ticketUpdate = new TicketUpdateDtoBuilder()
            .WithUpdatedDescription("# Updated Heading")
            .Build();

        // Act
        var cut = RenderComponent<TicketDiffViewer>(parameters => parameters
            .Add(p => p.OriginalTicket, originalTicket)
            .Add(p => p.TicketUpdate, ticketUpdate));

        // Assert - Markdown should be rendered
        Assert.NotNull(cut.Markup);
        Assert.Contains("Original Heading", cut.Markup);
        Assert.Contains("Updated Heading", cut.Markup);
    }

    [Fact]
    public void HandlesEmptyDescriptions()
    {
        // Arrange
        var originalTicket = new TicketDtoBuilder()
            .WithDescription("")
            .Build();

        var ticketUpdate = new TicketUpdateDtoBuilder()
            .WithUpdatedDescription("")
            .Build();

        // Act
        var cut = RenderComponent<TicketDiffViewer>(parameters => parameters
            .Add(p => p.OriginalTicket, originalTicket)
            .Add(p => p.TicketUpdate, ticketUpdate));

        // Assert - Should render without error
        Assert.NotNull(cut.Markup);
    }

    [Fact]
    public void HandlesNullDescriptions()
    {
        // Arrange
        var originalTicket = new TicketDtoBuilder()
            .WithDescription(null!)
            .Build();

        var ticketUpdate = new TicketUpdateDtoBuilder()
            .WithUpdatedDescription(null!)
            .Build();

        // Act
        var cut = RenderComponent<TicketDiffViewer>(parameters => parameters
            .Add(p => p.OriginalTicket, originalTicket)
            .Add(p => p.TicketUpdate, ticketUpdate));

        // Assert - Should render without error
        Assert.NotNull(cut.Markup);
    }
}
