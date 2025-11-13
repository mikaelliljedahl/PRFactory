using Bunit;
using PRFactory.Tests.Blazor;
using PRFactory.Web.UI.Dialogs;
using Xunit;

namespace PRFactory.Tests.UI.Dialogs;

public class ConfirmDialogTests : ComponentTestBase
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
