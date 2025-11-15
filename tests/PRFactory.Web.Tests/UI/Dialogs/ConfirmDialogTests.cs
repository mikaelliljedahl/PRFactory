using Bunit;
using PRFactory.Web.UI.Dialogs;
using Xunit;

namespace PRFactory.Web.Tests.UI.Dialogs;

public class ConfirmDialogTests : TestContext
{
    [Fact]
    public void Render_EmptyComponent_RendersNothing()
    {
        // Arrange & Act
        // ConfirmDialog is an empty component designed to be used via RadzenDialog service
        var cut = RenderComponent<ConfirmDialog>();

        // Assert
        Assert.Empty(cut.Markup);
    }
}
