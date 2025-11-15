using Bunit;
using Xunit;

namespace PRFactory.Web.Tests.Blazor;

/// <summary>
/// Base class for testing business/domain components
/// Inherits from TestContextBase and provides additional helpers for component testing
/// </summary>
public abstract class ComponentTestBase : TestContextBase
{
    /// <summary>
    /// Renders a component with the specified parameters
    /// </summary>
    protected new IRenderedComponent<TComponent> RenderComponent<TComponent>(
        Action<ComponentParameterCollectionBuilder<TComponent>>? parameterBuilder = null)
        where TComponent : Microsoft.AspNetCore.Components.IComponent
    {
        return base.RenderComponent<TComponent>(parameterBuilder);
    }

    /// <summary>
    /// Finds an element by its ID
    /// </summary>
    protected static AngleSharp.Dom.IElement FindElementById(IRenderedFragment fragment, string id)
    {
        return fragment.Find($"#{id}");
    }

    /// <summary>
    /// Finds all elements matching the CSS selector
    /// </summary>
    protected static System.Collections.Generic.IEnumerable<AngleSharp.Dom.IElement> FindElements(
        IRenderedFragment fragment,
        string cssSelector)
    {
        return fragment.FindAll(cssSelector);
    }

    /// <summary>
    /// Verifies that an element exists with the given CSS selector
    /// </summary>
    protected static void AssertElementExists(IRenderedFragment fragment, string cssSelector)
    {
        var element = fragment.Find(cssSelector);
        Assert.NotNull(element);
    }

    /// <summary>
    /// Verifies that an element does not exist with the given CSS selector
    /// </summary>
    protected static void AssertElementDoesNotExist(IRenderedFragment fragment, string cssSelector)
    {
        var elements = fragment.FindAll(cssSelector);
        Assert.Empty(elements);
    }

    /// <summary>
    /// Verifies that an element contains the expected text
    /// </summary>
    protected static void AssertElementContainsText(IRenderedFragment fragment, string cssSelector, string expectedText)
    {
        var element = fragment.Find(cssSelector);
        Assert.NotNull(element);
        Assert.Contains(expectedText, element.TextContent);
    }
}
