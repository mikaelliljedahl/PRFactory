using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PRFactory.Tests.Blazor;
using PRFactory.Web.Services;
using PRFactory.Web.UI.Notifications;
using Xunit;

namespace PRFactory.Tests.UI.Notifications;

public class ToastContainerTests : IDisposable
{
    private readonly Bunit.TestContext _testContext;
    private readonly Mock<IToastService> _mockToastService;

    public ToastContainerTests()
    {
        _testContext = new Bunit.TestContext();
        _mockToastService = new Mock<IToastService>();
        _mockToastService.Setup(s => s.GetToasts()).Returns(new List<ToastModel>());
        _testContext.Services.AddSingleton(_mockToastService.Object);
    }

    [Fact]
    public void Render_DisplaysContainer()
    {
        var cut = _testContext.RenderComponent<ToastContainer>();
        Assert.Contains("toast-container", cut.Markup);
    }

    [Fact]
    public void Render_HasCorrectPositioning()
    {
        var cut = _testContext.RenderComponent<ToastContainer>();
        Assert.Contains("position-fixed", cut.Markup);
        Assert.Contains("top-0", cut.Markup);
        Assert.Contains("end-0", cut.Markup);
    }

    public void Dispose()
    {
        _testContext?.Dispose();
    }
}
