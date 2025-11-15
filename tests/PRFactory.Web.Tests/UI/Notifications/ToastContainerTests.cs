using Bunit;
using Xunit;
using Moq;
using PRFactory.Web.UI.Notifications;
using PRFactory.Web.Services;

namespace PRFactory.Web.Tests.UI.Notifications;

/// <summary>
/// Tests for ToastContainer component
/// </summary>
public class ToastContainerTests : TestContext
{
    private readonly Mock<IToastService> _mockToastService;

    public ToastContainerTests()
    {
        _mockToastService = new Mock<IToastService>();
        Services.AddSingleton(_mockToastService.Object);
    }

    [Fact]
    public void ToastContainer_RendersContainerInCorrectPosition()
    {
        // Arrange
        _mockToastService
            .Setup(s => s.GetToasts())
            .Returns(new List<ToastModel>());

        // Act
        var cut = RenderComponent<ToastContainer>();

        // Assert
        Assert.Contains("toast-container", cut.Markup);
        Assert.Contains("position-fixed", cut.Markup);
        Assert.Contains("top-0", cut.Markup);
        Assert.Contains("end-0", cut.Markup);
    }

    [Fact]
    public void ToastContainer_RendersMultipleToasts()
    {
        // Arrange
        var toasts = new List<ToastModel>
        {
            new ToastModel
            {
                Id = Guid.NewGuid(),
                Title = "Success",
                Message = "First toast",
                Type = ToastType.Success,
                Icon = "check-circle",
                IsVisible = true
            },
            new ToastModel
            {
                Id = Guid.NewGuid(),
                Title = "Error",
                Message = "Second toast",
                Type = ToastType.Error,
                Icon = "exclamation-triangle",
                IsVisible = true
            }
        };

        _mockToastService
            .Setup(s => s.GetToasts())
            .Returns(toasts);

        // Act
        var cut = RenderComponent<ToastContainer>();

        // Assert
        Assert.Contains("First toast", cut.Markup);
        Assert.Contains("Second toast", cut.Markup);
    }

    [Fact]
    public void ToastContainer_StacksToastsVertically()
    {
        // Arrange
        var toasts = new List<ToastModel>
        {
            new ToastModel
            {
                Id = Guid.NewGuid(),
                Title = "Toast 1",
                Message = "Message 1",
                Type = ToastType.Info,
                IsVisible = true
            },
            new ToastModel
            {
                Id = Guid.NewGuid(),
                Title = "Toast 2",
                Message = "Message 2",
                Type = ToastType.Warning,
                IsVisible = true
            }
        };

        _mockToastService
            .Setup(s => s.GetToasts())
            .Returns(toasts);

        // Act
        var cut = RenderComponent<ToastContainer>();

        // Assert - Both toasts should be present in the container
        var toastElements = cut.FindAll(".toast");
        Assert.Equal(2, toastElements.Count);
    }

    [Fact]
    public void ToastContainer_RemovesToastWhenDismissed()
    {
        // Arrange
        var toastId = Guid.NewGuid();
        var toasts = new List<ToastModel>
        {
            new ToastModel
            {
                Id = toastId,
                Title = "Test",
                Message = "Test message",
                Type = ToastType.Info,
                IsVisible = true
            }
        };

        _mockToastService
            .Setup(s => s.GetToasts())
            .Returns(toasts);

        // Act
        var cut = RenderComponent<ToastContainer>();

        // Simulate dismissing the toast
        var closeButton = cut.Find(".btn-close");
        closeButton.Click();

        // Assert
        _mockToastService.Verify(s => s.RemoveToast(toastId), Times.Once);
    }

    [Fact]
    public void ToastContainer_HandlesEmptyToastList()
    {
        // Arrange
        _mockToastService
            .Setup(s => s.GetToasts())
            .Returns(new List<ToastModel>());

        // Act
        var cut = RenderComponent<ToastContainer>();

        // Assert - Container should still render but with no toasts
        Assert.Contains("toast-container", cut.Markup);
        var toastElements = cut.FindAll(".toast");
        Assert.Empty(toastElements);
    }

    [Fact]
    public void ToastContainer_SubscribesToToastServiceEvents()
    {
        // Arrange
        _mockToastService
            .Setup(s => s.GetToasts())
            .Returns(new List<ToastModel>());

        // Act
        var cut = RenderComponent<ToastContainer>();

        // Assert - Verify the event handler was attached
        _mockToastService.VerifyAdd(s => s.OnToastsChanged += It.IsAny<Action>(), Times.Once);
    }

    [Fact]
    public void ToastContainer_UnsubscribesFromToastServiceOnDispose()
    {
        // Arrange
        _mockToastService
            .Setup(s => s.GetToasts())
            .Returns(new List<ToastModel>());

        // Act
        using (var ctx = new Bunit.TestContext())
        {
            ctx.Services.AddSingleton(_mockToastService.Object);
            var cut = ctx.RenderComponent<ToastContainer>();
            // Component disposal happens automatically when context is disposed
        }

        // Assert - Verify the event handler was removed
        _mockToastService.VerifyRemove(s => s.OnToastsChanged -= It.IsAny<Action>(), Times.Once);
    }

    [Fact]
    public void ToastContainer_UpdatesWhenToastServiceChanges()
    {
        // Arrange
        var initialToasts = new List<ToastModel>();
        _mockToastService
            .Setup(s => s.GetToasts())
            .Returns(initialToasts);

        var cut = RenderComponent<ToastContainer>();

        // Assert initial state
        var initialToastElements = cut.FindAll(".toast");
        Assert.Empty(initialToastElements);

        // Act - Add a toast
        var newToast = new ToastModel
        {
            Id = Guid.NewGuid(),
            Title = "New Toast",
            Message = "New message",
            Type = ToastType.Success,
            IsVisible = true
        };
        initialToasts.Add(newToast);

        // Simulate the OnToastsChanged event
        _mockToastService.Raise(s => s.OnToastsChanged += null);

        // Assert - Toast should now be visible
        cut.WaitForAssertion(() => Assert.Contains("New Toast", cut.Markup));
    }
}
