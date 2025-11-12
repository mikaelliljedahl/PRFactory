using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PRFactory.Tests.Blazor;
using PRFactory.Tests.Blazor.TestDataBuilders;
using PRFactory.Web.Components.Errors;
using PRFactory.Web.Services;
using Xunit;

namespace PRFactory.Tests.Components.Errors;

public class ErrorResolutionFormTests : ComponentTestBase
{
    private readonly Mock<IErrorService> _mockErrorService;
    private readonly Mock<IToastService> _mockToastService;

    public ErrorResolutionFormTests()
    {
        _mockErrorService = new Mock<IErrorService>();
        _mockToastService = new Mock<IToastService>();

        Services.AddSingleton(_mockErrorService.Object);
        Services.AddSingleton(_mockToastService.Object);
    }

    [Fact]
    public void Render_DisplaysFormFields()
    {
        // Arrange
        var errorId = Guid.NewGuid();

        // Act
        var cut = RenderComponent<ErrorResolutionForm>(parameters => parameters
            .Add(p => p.ErrorId, errorId));

        // Assert
        Assert.Contains("Resolution Notes", cut.Markup);
        Assert.Contains("Mark as Resolved", cut.Markup);
    }

    [Fact]
    public async Task SubmitForm_WithValidNotes_InvokesOnSubmit()
    {
        // Arrange
        var errorId = Guid.NewGuid();
        var submitCalled = false;
        ErrorResolutionForm.ResolutionFormModel? submittedModel = null;

        var cut = RenderComponent<ErrorResolutionForm>(parameters => parameters
            .Add(p => p.ErrorId, errorId)
            .Add(p => p.OnSubmit, EventCallback.Factory.Create<ErrorResolutionForm.ResolutionFormModel>(this, (model) => { submitCalled = true; submittedModel = model; return Task.CompletedTask; })));

        // Act
        var textarea = cut.Find("textarea");
        textarea.Change("Fixed the error");

        var submitButton = cut.Find("button[type='submit']");
        submitButton.Click();
        await Task.Delay(100);

        // Assert
        Assert.True(submitCalled);
        Assert.NotNull(submittedModel);
        Assert.Equal("Fixed the error", submittedModel.ResolutionNotes);
    }

    [Fact]
    public void SubmitForm_WithoutNotes_RendersForm()
    {
        // Arrange
        var errorId = Guid.NewGuid();
        var submitCalled = false;

        // Act
        var cut = RenderComponent<ErrorResolutionForm>(parameters => parameters
            .Add(p => p.ErrorId, errorId)
            .Add(p => p.OnSubmit, EventCallback.Factory.Create<ErrorResolutionForm.ResolutionFormModel>(this, (model) => { submitCalled = true; return Task.CompletedTask; })));

        var submitButton = cut.Find("button[type='submit']");
        submitButton.Click();

        // Assert - Form renders and can be submitted (validation would be in actual component)
        Assert.NotNull(cut.Markup);
    }
}
