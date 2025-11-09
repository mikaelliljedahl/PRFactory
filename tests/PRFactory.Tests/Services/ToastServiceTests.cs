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
        Assert.True(eventFired);
        var toasts = service.GetToasts();
        Assert.Equal(1, toasts.Count);
        Assert.Equal(ToastType.Success, toasts[0].Type);
        Assert.Equal("Test title", toasts[0].Title);
        Assert.Equal("Test message", toasts[0].Message);
        Assert.Equal("check-circle", toasts[0].Icon);
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
        Assert.Equal(1, toasts.Count);
        Assert.Equal(ToastType.Error, toasts[0].Type);
        Assert.Equal("exclamation-triangle", toasts[0].Icon);
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
        Assert.Equal(ToastType.Warning, toasts[0].Type);
        Assert.Equal("exclamation-circle", toasts[0].Icon);
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
        Assert.Equal(ToastType.Info, toasts[0].Type);
        Assert.Equal("info-circle", toasts[0].Icon);
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
        Assert.Equal(1, remainingToasts.Count);
        Assert.DoesNotContain(remainingToasts, t => t.Id == toastToRemove.Id);
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
        Assert.Equal("custom-icon", toasts[0].Icon);
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
        Assert.Equal(1, eventFiredCount);
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
        Assert.Equal(1, eventFiredCount);
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
        Assert.NotSame(toasts2, toasts1);
        Assert.Equal(toasts1.Count, toasts2.Count);
        for (int i = 0; i < toasts1.Count; i++)
        {
            Assert.Equal(toasts1[i].Id, toasts2[i].Id);
            Assert.Equal(toasts1[i].Type, toasts2[i].Type);
            Assert.Equal(toasts1[i].Message, toasts2[i].Message);
        }
    }
}
