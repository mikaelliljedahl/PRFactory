using FluentAssertions;
using PRFactory.Web.Services;
using Xunit;

namespace PRFactory.Tests.Services;

public class ToastServiceTests
{
    [Fact]
    public void ShowSuccess_ShouldCreateToastWithSuccessType()
    {
        // Arrange
        var service = new ToastService();
        var eventFired = false;
        service.OnToastsChanged += () => eventFired = true;

        // Act
        service.ShowSuccess("Test message", "Test title");

        // Assert
        eventFired.Should().BeTrue();
        var toasts = service.GetToasts();
        toasts.Should().HaveCount(1);
        toasts[0].Type.Should().Be(ToastType.Success);
        toasts[0].Title.Should().Be("Test title");
        toasts[0].Message.Should().Be("Test message");
        toasts[0].Icon.Should().Be("check-circle");
    }

    [Fact]
    public void ShowError_ShouldCreateToastWithErrorType()
    {
        // Arrange
        var service = new ToastService();

        // Act
        service.ShowError("Error occurred", "Error");

        // Assert
        var toasts = service.GetToasts();
        toasts.Should().HaveCount(1);
        toasts[0].Type.Should().Be(ToastType.Error);
        toasts[0].Icon.Should().Be("exclamation-triangle");
    }

    [Fact]
    public void ShowWarning_ShouldCreateToastWithWarningType()
    {
        // Arrange
        var service = new ToastService();

        // Act
        service.ShowWarning("Warning message");

        // Assert
        var toasts = service.GetToasts();
        toasts[0].Type.Should().Be(ToastType.Warning);
        toasts[0].Icon.Should().Be("exclamation-circle");
    }

    [Fact]
    public void ShowInfo_ShouldCreateToastWithInfoType()
    {
        // Arrange
        var service = new ToastService();

        // Act
        service.ShowInfo("Information");

        // Assert
        var toasts = service.GetToasts();
        toasts[0].Type.Should().Be(ToastType.Info);
        toasts[0].Icon.Should().Be("info-circle");
    }

    [Fact]
    public void RemoveToast_ShouldRemoveToastById()
    {
        // Arrange
        var service = new ToastService();
        service.ShowSuccess("Message 1");
        service.ShowError("Message 2");
        var toasts = service.GetToasts();
        var toastToRemove = toasts[0];

        // Act
        service.RemoveToast(toastToRemove.Id);

        // Assert
        var remainingToasts = service.GetToasts();
        remainingToasts.Should().HaveCount(1);
        remainingToasts.Should().NotContain(t => t.Id == toastToRemove.Id);
    }

    [Fact]
    public void ShowToast_WithCustomIcon_ShouldUseCustomIcon()
    {
        // Arrange
        var service = new ToastService();

        // Act
        service.ShowToast("Title", "Message", ToastType.Primary, "custom-icon");

        // Assert
        var toasts = service.GetToasts();
        toasts[0].Icon.Should().Be("custom-icon");
    }

    [Fact]
    public void OnToastsChanged_ShouldFireWhenToastAdded()
    {
        // Arrange
        var service = new ToastService();
        var eventFiredCount = 0;
        service.OnToastsChanged += () => eventFiredCount++;

        // Act
        service.ShowSuccess("Test");

        // Assert
        eventFiredCount.Should().Be(1);
    }

    [Fact]
    public void OnToastsChanged_ShouldFireWhenToastRemoved()
    {
        // Arrange
        var service = new ToastService();
        service.ShowSuccess("Test");
        var toastId = service.GetToasts()[0].Id;
        var eventFiredCount = 0;
        service.OnToastsChanged += () => eventFiredCount++;

        // Act
        service.RemoveToast(toastId);

        // Assert
        eventFiredCount.Should().Be(1);
    }

    [Fact]
    public void GetToasts_ShouldReturnCopyOfList()
    {
        // Arrange
        var service = new ToastService();
        service.ShowSuccess("Test");

        // Act
        var toasts1 = service.GetToasts();
        var toasts2 = service.GetToasts();

        // Assert
        toasts1.Should().NotBeSameAs(toasts2);
        toasts1.Should().BeEquivalentTo(toasts2);
    }
}
