using Bunit;
using Moq;
using Xunit;
using PRFactory.Web.Components.Tickets;
using PRFactory.Web.Models;
using PRFactory.Web.Services;
using PRFactory.Domain.ValueObjects;

namespace PRFactory.Web.Tests.Components.Tickets;

/// <summary>
/// Tests for TicketUpdateEditor component
/// </summary>
public class TicketUpdateEditorTests : TestContext
{
    private readonly Mock<ITicketService> _mockTicketService;

    public TicketUpdateEditorTests()
    {
        _mockTicketService = new Mock<ITicketService>();
        Services.AddSingleton(_mockTicketService.Object);
    }

    [Fact]
    public void TicketUpdateEditor_WithTicketUpdate_DisplaysTitleField()
    {
        // Arrange
        var ticketUpdate = new TicketUpdateDto
        {
            Id = Guid.NewGuid(),
            TicketId = Guid.NewGuid(),
            UpdatedTitle = "Test Title",
            UpdatedDescription = "Test Description",
            AcceptanceCriteria = "Test Criteria",
            SuccessCriteria = new List<SuccessCriterionDto>
            {
                new SuccessCriterionDto
                {
                    Category = SuccessCriterionCategory.Functional,
                    Description = "Criterion",
                    Priority = 0,
                    IsTestable = true
                }
            },
            GeneratedAt = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<TicketUpdateEditor>(parameters => parameters
            .Add(p => p.TicketUpdate, ticketUpdate));

        // Assert
        Assert.Contains("Updated Title", cut.Markup);
        var titleInput = cut.Find("input#updatedTitle");
        Assert.NotNull(titleInput);
    }

    [Fact]
    public void TicketUpdateEditor_WithTicketUpdate_DisplaysDescriptionField()
    {
        // Arrange
        var ticketUpdate = new TicketUpdateDto
        {
            Id = Guid.NewGuid(),
            TicketId = Guid.NewGuid(),
            UpdatedTitle = "Test Title",
            UpdatedDescription = "Test Description",
            AcceptanceCriteria = "Test Criteria",
            SuccessCriteria = new List<SuccessCriterionDto>
            {
                new SuccessCriterionDto
                {
                    Category = SuccessCriterionCategory.Functional,
                    Description = "Criterion",
                    Priority = 0,
                    IsTestable = true
                }
            },
            GeneratedAt = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<TicketUpdateEditor>(parameters => parameters
            .Add(p => p.TicketUpdate, ticketUpdate));

        // Assert
        Assert.Contains("Updated Description", cut.Markup);
        Assert.Contains("Markdown supported", cut.Markup);
    }

    [Fact]
    public void TicketUpdateEditor_WithTicketUpdate_DisplaysAcceptanceCriteriaField()
    {
        // Arrange
        var ticketUpdate = new TicketUpdateDto
        {
            Id = Guid.NewGuid(),
            TicketId = Guid.NewGuid(),
            UpdatedTitle = "Test Title",
            UpdatedDescription = "Test Description",
            AcceptanceCriteria = "Test Criteria",
            SuccessCriteria = new List<SuccessCriterionDto>
            {
                new SuccessCriterionDto
                {
                    Category = SuccessCriterionCategory.Functional,
                    Description = "Criterion",
                    Priority = 0,
                    IsTestable = true
                }
            },
            GeneratedAt = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<TicketUpdateEditor>(parameters => parameters
            .Add(p => p.TicketUpdate, ticketUpdate));

        // Assert
        Assert.Contains("Acceptance Criteria", cut.Markup);
    }

    [Fact]
    public void TicketUpdateEditor_ShowsSaveDraftButton()
    {
        // Arrange
        var ticketUpdate = new TicketUpdateDto
        {
            Id = Guid.NewGuid(),
            TicketId = Guid.NewGuid(),
            UpdatedTitle = "Test Title",
            UpdatedDescription = "Test Description",
            AcceptanceCriteria = "Test Criteria",
            SuccessCriteria = new List<SuccessCriterionDto>
            {
                new SuccessCriterionDto
                {
                    Category = SuccessCriterionCategory.Functional,
                    Description = "Criterion",
                    Priority = 0,
                    IsTestable = true
                }
            },
            GeneratedAt = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<TicketUpdateEditor>(parameters => parameters
            .Add(p => p.TicketUpdate, ticketUpdate));

        // Assert
        Assert.Contains("Save Draft", cut.Markup);
    }

    [Fact]
    public async Task TicketUpdateEditor_SaveWithValidData_CallsService()
    {
        // Arrange
        var ticketUpdate = new TicketUpdateDto
        {
            Id = Guid.NewGuid(),
            TicketId = Guid.NewGuid(),
            UpdatedTitle = "Test Title",
            UpdatedDescription = "Test Description",
            AcceptanceCriteria = "Test Criteria",
            SuccessCriteria = new List<SuccessCriterionDto>
            {
                new SuccessCriterionDto
                {
                    Category = SuccessCriterionCategory.Functional,
                    Description = "Criterion",
                    Priority = 0,
                    IsTestable = true
                }
            },
            GeneratedAt = DateTime.UtcNow
        };

        _mockTicketService
            .Setup(x => x.UpdateTicketUpdateAsync(ticketUpdate.Id, It.IsAny<TicketUpdateDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var callbackInvoked = false;

        // Act
        var cut = RenderComponent<TicketUpdateEditor>(parameters => parameters
            .Add(p => p.TicketUpdate, ticketUpdate)
            .Add(p => p.OnSaved, () =>
            {
                callbackInvoked = true;
                return Task.CompletedTask;
            }));

        var form = cut.Find("form");
        await form.SubmitAsync();

        // Assert
        _mockTicketService.Verify(
            x => x.UpdateTicketUpdateAsync(ticketUpdate.Id, It.IsAny<TicketUpdateDto>(), It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.True(callbackInvoked);
    }

    [Fact]
    public async Task TicketUpdateEditor_SaveWithoutSuccessCriteria_ShowsValidationError()
    {
        // Arrange
        var ticketUpdate = new TicketUpdateDto
        {
            Id = Guid.NewGuid(),
            TicketId = Guid.NewGuid(),
            UpdatedTitle = "Test Title",
            UpdatedDescription = "Test Description",
            AcceptanceCriteria = "Test Criteria",
            SuccessCriteria = new List<SuccessCriterionDto>(),
            GeneratedAt = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<TicketUpdateEditor>(parameters => parameters
            .Add(p => p.TicketUpdate, ticketUpdate));

        var form = cut.Find("form");
        await form.SubmitAsync();

        // Assert
        Assert.Contains("At least one success criterion is required", cut.Markup);
        _mockTicketService.Verify(
            x => x.UpdateTicketUpdateAsync(It.IsAny<Guid>(), It.IsAny<TicketUpdateDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TicketUpdateEditor_SaveWithEmptySuccessCriteriaDescription_ShowsValidationError()
    {
        // Arrange
        var ticketUpdate = new TicketUpdateDto
        {
            Id = Guid.NewGuid(),
            TicketId = Guid.NewGuid(),
            UpdatedTitle = "Test Title",
            UpdatedDescription = "Test Description",
            AcceptanceCriteria = "Test Criteria",
            SuccessCriteria = new List<SuccessCriterionDto>
            {
                new SuccessCriterionDto
                {
                    Category = SuccessCriterionCategory.Functional,
                    Description = "",
                    Priority = 0,
                    IsTestable = true
                }
            },
            GeneratedAt = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<TicketUpdateEditor>(parameters => parameters
            .Add(p => p.TicketUpdate, ticketUpdate));

        var form = cut.Find("form");
        await form.SubmitAsync();

        // Assert
        Assert.Contains("All success criteria must have a description", cut.Markup);
        _mockTicketService.Verify(
            x => x.UpdateTicketUpdateAsync(It.IsAny<Guid>(), It.IsAny<TicketUpdateDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TicketUpdateEditor_Saving_ShowsLoadingState()
    {
        // Arrange
        var ticketUpdate = new TicketUpdateDto
        {
            Id = Guid.NewGuid(),
            TicketId = Guid.NewGuid(),
            UpdatedTitle = "Test Title",
            UpdatedDescription = "Test Description",
            AcceptanceCriteria = "Test Criteria",
            SuccessCriteria = new List<SuccessCriterionDto>
            {
                new SuccessCriterionDto
                {
                    Category = SuccessCriterionCategory.Functional,
                    Description = "Criterion",
                    Priority = 0,
                    IsTestable = true
                }
            },
            GeneratedAt = DateTime.UtcNow
        };

        var tcs = new TaskCompletionSource();
        _mockTicketService
            .Setup(x => x.UpdateTicketUpdateAsync(ticketUpdate.Id, It.IsAny<TicketUpdateDto>(), It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        // Act
        var cut = RenderComponent<TicketUpdateEditor>(parameters => parameters
            .Add(p => p.TicketUpdate, ticketUpdate));

        var form = cut.Find("form");
        var submitTask = form.SubmitAsync();

        // Assert - Check loading state
        Assert.Contains("Saving...", cut.Markup);

        // Clean up
        tcs.SetResult();
        await submitTask;
    }

    [Fact]
    public async Task TicketUpdateEditor_SaveSuccess_ShowsSuccessMessage()
    {
        // Arrange
        var ticketUpdate = new TicketUpdateDto
        {
            Id = Guid.NewGuid(),
            TicketId = Guid.NewGuid(),
            UpdatedTitle = "Test Title",
            UpdatedDescription = "Test Description",
            AcceptanceCriteria = "Test Criteria",
            SuccessCriteria = new List<SuccessCriterionDto>
            {
                new SuccessCriterionDto
                {
                    Category = SuccessCriterionCategory.Functional,
                    Description = "Criterion",
                    Priority = 0,
                    IsTestable = true
                }
            },
            GeneratedAt = DateTime.UtcNow
        };

        _mockTicketService
            .Setup(x => x.UpdateTicketUpdateAsync(ticketUpdate.Id, It.IsAny<TicketUpdateDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var cut = RenderComponent<TicketUpdateEditor>(parameters => parameters
            .Add(p => p.TicketUpdate, ticketUpdate));

        var form = cut.Find("form");
        await form.SubmitAsync();

        // Assert
        Assert.Contains("Ticket update saved successfully", cut.Markup);
    }

    [Fact]
    public async Task TicketUpdateEditor_ServiceError_ShowsErrorMessage()
    {
        // Arrange
        var ticketUpdate = new TicketUpdateDto
        {
            Id = Guid.NewGuid(),
            TicketId = Guid.NewGuid(),
            UpdatedTitle = "Test Title",
            UpdatedDescription = "Test Description",
            AcceptanceCriteria = "Test Criteria",
            SuccessCriteria = new List<SuccessCriterionDto>
            {
                new SuccessCriterionDto
                {
                    Category = SuccessCriterionCategory.Functional,
                    Description = "Criterion",
                    Priority = 0,
                    IsTestable = true
                }
            },
            GeneratedAt = DateTime.UtcNow
        };

        _mockTicketService
            .Setup(x => x.UpdateTicketUpdateAsync(ticketUpdate.Id, It.IsAny<TicketUpdateDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var cut = RenderComponent<TicketUpdateEditor>(parameters => parameters
            .Add(p => p.TicketUpdate, ticketUpdate));

        var form = cut.Find("form");
        await form.SubmitAsync();

        // Assert
        Assert.Contains("Error saving ticket update: Service error", cut.Markup);
    }
}
