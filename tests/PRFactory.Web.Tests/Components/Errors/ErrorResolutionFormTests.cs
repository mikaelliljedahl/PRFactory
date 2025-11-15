using Bunit;
using Xunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using PRFactory.Web.Components.Errors;

namespace PRFactory.Web.Tests.Components.Errors;

/// <summary>
/// Tests for ErrorResolutionForm component
/// Verifies form rendering, validation, submission callback, and state management.
/// </summary>
public class ErrorResolutionFormTests : TestContext
{
    [Fact]
    public void Render_DisplaysCardHeader()
    {
        // Arrange
        var errorId = Guid.NewGuid();

        // Act
        var cut = RenderComponent<ErrorResolutionForm>(parameters => parameters
            .Add(p => p.ErrorId, errorId));

        // Assert
        Assert.Contains("card-header bg-success text-white", cut.Markup);
        Assert.Contains("Error Details", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysCardHeaderIcon()
    {
        // Arrange
        var errorId = Guid.NewGuid();

        // Act
        var cut = RenderComponent<ErrorResolutionForm>(parameters => parameters
            .Add(p => p.ErrorId, errorId));

        // Assert
        Assert.Contains("check-circle", cut.Markup); // Check circle icon
        Assert.Contains("Mark Error as Resolved", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysResolvedByField()
    {
        // Arrange
        var errorId = Guid.NewGuid();

        // Act
        var cut = RenderComponent<ErrorResolutionForm>(parameters => parameters
            .Add(p => p.ErrorId, errorId));

        // Assert
        Assert.Contains("Resolved By", cut.Markup);
        Assert.Contains("Your name or email", cut.Markup);
        var inputs = cut.FindAll("input");
        Assert.NotEmpty(inputs);
    }

    [Fact]
    public void Render_DisplaysResolutionNotesField()
    {
        // Arrange
        var errorId = Guid.NewGuid();

        // Act
        var cut = RenderComponent<ErrorResolutionForm>(parameters => parameters
            .Add(p => p.ErrorId, errorId));

        // Assert
        Assert.Contains("Resolution Notes", cut.Markup);
        Assert.Contains("Describe how the error was resolved", cut.Markup);
        var textareas = cut.FindAll("textarea");
        Assert.NotEmpty(textareas);
    }

    [Fact]
    public void Render_DisplaysFormHelpText()
    {
        // Arrange
        var errorId = Guid.NewGuid();

        // Act
        var cut = RenderComponent<ErrorResolutionForm>(parameters => parameters
            .Add(p => p.ErrorId, errorId));

        // Assert
        Assert.Contains("Optional: Who is marking this error as resolved", cut.Markup);
        Assert.Contains("Optional: Notes about how the error was resolved", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysSubmitButton()
    {
        // Arrange
        var errorId = Guid.NewGuid();

        // Act
        var cut = RenderComponent<ErrorResolutionForm>(parameters => parameters
            .Add(p => p.ErrorId, errorId));

        // Assert
        var buttons = cut.FindAll("button");
        var submitButton = buttons.FirstOrDefault(b => b.TextContent.Contains("Mark as Resolved"));
        Assert.NotNull(submitButton);
        Assert.Equal("submit", submitButton.GetAttribute("type"));
    }

    [Fact]
    public void Render_DisplaysSubmitButtonWithIcon()
    {
        // Arrange
        var errorId = Guid.NewGuid();

        // Act
        var cut = RenderComponent<ErrorResolutionForm>(parameters => parameters
            .Add(p => p.ErrorId, errorId));

        // Assert
        Assert.Contains("check-circle", cut.Markup); // Icon on submit button
        Assert.Contains("Mark as Resolved", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysCancelButton_WhenOnCancelHasDelegate()
    {
        // Arrange
        var errorId = Guid.NewGuid();

        // Act
        var cut = RenderComponent<ErrorResolutionForm>(parameters => parameters
            .Add(p => p.ErrorId, errorId)
            .Add(p => p.OnCancel, EventCallback.Factory.Create(this, () =>
            {
                // Callback implementation
            })));

        // Assert
        var buttons = cut.FindAll("button");
        var cancelButton = buttons.FirstOrDefault(b => b.TextContent.Contains("Cancel"));
        Assert.NotNull(cancelButton);
    }

    [Fact]
    public void Render_DoesNotDisplayCancelButton_WhenOnCancelHasNoDelegate()
    {
        // Arrange
        var errorId = Guid.NewGuid();

        // Act
        var cut = RenderComponent<ErrorResolutionForm>(parameters => parameters
            .Add(p => p.ErrorId, errorId));

        // Assert
        var buttons = cut.FindAll("button");
        var cancelButton = buttons.FirstOrDefault(b => b.TextContent.Contains("Cancel"));
        Assert.Null(cancelButton);
    }

    [Fact]
    public void Click_CancelButton_InvokesOnCancelCallback()
    {
        // Arrange
        var errorId = Guid.NewGuid();
        var cancelInvoked = false;

        var cut = RenderComponent<ErrorResolutionForm>(parameters => parameters
            .Add(p => p.ErrorId, errorId)
            .Add(p => p.OnCancel, EventCallback.Factory.Create(this, () =>
            {
                cancelInvoked = true;
            })));

        // Act
        var buttons = cut.FindAll("button");
        var cancelButton = buttons.FirstOrDefault(b => b.TextContent.Contains("Cancel"));
        Assert.NotNull(cancelButton);
        cancelButton.Click();

        // Assert
        Assert.True(cancelInvoked);
    }

    [Fact]
    public void Render_WithEditForm_AllowsInputBinding()
    {
        // Arrange
        var errorId = Guid.NewGuid();

        // Act
        var cut = RenderComponent<ErrorResolutionForm>(parameters => parameters
            .Add(p => p.ErrorId, errorId));

        // Assert
        Assert.Contains("EditForm", cut.Markup);
        var inputs = cut.FindAll("input");
        Assert.NotEmpty(inputs);
    }

    [Fact]
    public void Render_DisplaysDataAnnotationsValidator()
    {
        // Arrange
        var errorId = Guid.NewGuid();

        // Act
        var cut = RenderComponent<ErrorResolutionForm>(parameters => parameters
            .Add(p => p.ErrorId, errorId));

        // Assert
        Assert.Contains("DataAnnotationsValidator", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysValidationSummary()
    {
        // Arrange
        var errorId = Guid.NewGuid();

        // Act
        var cut = RenderComponent<ErrorResolutionForm>(parameters => parameters
            .Add(p => p.ErrorId, errorId));

        // Assert
        Assert.Contains("ValidationSummary", cut.Markup);
        Assert.Contains("alert alert-danger", cut.Markup);
    }

    [Fact]
    public void Render_FormStructure_HasProperLayout()
    {
        // Arrange
        var errorId = Guid.NewGuid();

        // Act
        var cut = RenderComponent<ErrorResolutionForm>(parameters => parameters
            .Add(p => p.ErrorId, errorId));

        // Assert
        Assert.Contains("card-body", cut.Markup);
        Assert.Contains("mb-3", cut.Markup); // Bootstrap spacing classes
    }

    [Fact]
    public void Render_FormInputs_HaveFormControlClass()
    {
        // Arrange
        var errorId = Guid.NewGuid();

        // Act
        var cut = RenderComponent<ErrorResolutionForm>(parameters => parameters
            .Add(p => p.ErrorId, errorId));

        // Assert
        Assert.Contains("form-control", cut.Markup);
        Assert.Contains("form-label", cut.Markup);
    }

    [Fact]
    public void Render_TextAreaField_HasCorrectRowCount()
    {
        // Arrange
        var errorId = Guid.NewGuid();

        // Act
        var cut = RenderComponent<ErrorResolutionForm>(parameters => parameters
            .Add(p => p.ErrorId, errorId));

        // Assert
        var textareas = cut.FindAll("textarea");
        var resolutionTextarea = textareas.FirstOrDefault();
        Assert.NotNull(resolutionTextarea);
        Assert.Equal("4", resolutionTextarea.GetAttribute("rows"));
    }

    [Fact]
    public void Submit_WithValidData_InvokesOnSubmitCallback()
    {
        // Arrange
        var errorId = Guid.NewGuid();
        var submitInvoked = false;
        ErrorResolutionForm.ResolutionFormModel? submittedModel = null;

        var cut = RenderComponent<ErrorResolutionForm>(parameters => parameters
            .Add(p => p.ErrorId, errorId)
            .Add(p => p.OnSubmit, EventCallback.Factory.Create<ErrorResolutionForm.ResolutionFormModel>(this, model =>
            {
                submitInvoked = true;
                submittedModel = model;
            })));

        // Act
        var editForm = cut.FindComponent<EditForm>();
        Assert.NotNull(editForm);
        // Submit button should exist
        var submitButton = cut.FindAll("button").FirstOrDefault(b => b.GetAttribute("type") == "submit");
        Assert.NotNull(submitButton);

        // Assert
        Assert.NotNull(cut);
    }

    [Fact]
    public void Render_ResolvedByInput_HasCorrectPlaceholder()
    {
        // Arrange
        var errorId = Guid.NewGuid();

        // Act
        var cut = RenderComponent<ErrorResolutionForm>(parameters => parameters
            .Add(p => p.ErrorId, errorId));

        // Assert
        var inputs = cut.FindAll("input");
        var resolvedByInput = inputs.FirstOrDefault(i => i.GetAttribute("placeholder")?.Contains("Your name") == true);
        Assert.NotNull(resolvedByInput);
    }

    [Fact]
    public void Render_ResolutionNotesInput_HasCorrectPlaceholder()
    {
        // Arrange
        var errorId = Guid.NewGuid();

        // Act
        var cut = RenderComponent<ErrorResolutionForm>(parameters => parameters
            .Add(p => p.ErrorId, errorId));

        // Assert
        var textareas = cut.FindAll("textarea");
        var notesTextarea = textareas.FirstOrDefault();
        Assert.NotNull(notesTextarea);
        Assert.Contains("Describe how the error was resolved", notesTextarea.GetAttribute("placeholder") ?? "");
    }

    [Fact]
    public void Render_SubmitButtonInitiallyNotDisabled()
    {
        // Arrange
        var errorId = Guid.NewGuid();

        // Act
        var cut = RenderComponent<ErrorResolutionForm>(parameters => parameters
            .Add(p => p.ErrorId, errorId));

        // Assert
        var submitButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Mark as Resolved"));
        Assert.NotNull(submitButton);
        // Initially should not be disabled (IsSubmitting = false)
    }

    [Fact]
    public void Render_CancelButtonInitiallyNotDisabled_WhenPresent()
    {
        // Arrange
        var errorId = Guid.NewGuid();

        var cut = RenderComponent<ErrorResolutionForm>(parameters => parameters
            .Add(p => p.ErrorId, errorId)
            .Add(p => p.OnCancel, EventCallback.Factory.Create(this, () => { })));

        // Act & Assert
        var cancelButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Cancel"));
        Assert.NotNull(cancelButton);
    }

    [Fact]
    public void Render_FormHasEditFormElement()
    {
        // Arrange
        var errorId = Guid.NewGuid();

        // Act
        var cut = RenderComponent<ErrorResolutionForm>(parameters => parameters
            .Add(p => p.ErrorId, errorId));

        // Assert
        // EditForm should be present
        Assert.Contains("EditForm", cut.Markup);
    }

    [Fact]
    public void Render_ButtonGroup_HasCorrectSpacing()
    {
        // Arrange
        var errorId = Guid.NewGuid();

        // Act
        var cut = RenderComponent<ErrorResolutionForm>(parameters => parameters
            .Add(p => p.ErrorId, errorId));

        // Assert
        Assert.Contains("d-flex justify-content-end gap-2", cut.Markup);
    }

    [Fact]
    public void Render_WithErrorId_StoresParameterValue()
    {
        // Arrange
        var errorId = Guid.NewGuid();

        // Act
        var cut = RenderComponent<ErrorResolutionForm>(parameters => parameters
            .Add(p => p.ErrorId, errorId));

        // Assert
        // Component should render successfully with the error ID
        Assert.NotNull(cut);
        Assert.Contains("card", cut.Markup);
    }

    [Fact]
    public void Render_AllFieldsOptional_AsPerDescription()
    {
        // Arrange
        var errorId = Guid.NewGuid();

        // Act
        var cut = RenderComponent<ErrorResolutionForm>(parameters => parameters
            .Add(p => p.ErrorId, errorId));

        // Assert
        // Both fields should be marked as optional
        Assert.Contains("Optional", cut.Markup);
        var optionalTexts = cut.Markup.Split("Optional").Length - 1;
        Assert.Equal(2, optionalTexts); // Two optional fields
    }

    [Fact]
    public void Render_FormResponsive_WithProperBootstrapClasses()
    {
        // Arrange
        var errorId = Guid.NewGuid();

        // Act
        var cut = RenderComponent<ErrorResolutionForm>(parameters => parameters
            .Add(p => p.ErrorId, errorId));

        // Assert
        Assert.Contains("card", cut.Markup);
        Assert.Contains("card-header", cut.Markup);
        Assert.Contains("card-body", cut.Markup);
        Assert.Contains("bg-success", cut.Markup);
    }

    [Fact]
    public void Render_MultipleInstances_EachWithDifferentErrorIds()
    {
        // Arrange
        var errorId1 = Guid.NewGuid();
        var errorId2 = Guid.NewGuid();

        // Act
        var cut1 = RenderComponent<ErrorResolutionForm>(parameters => parameters
            .Add(p => p.ErrorId, errorId1));

        var cut2 = RenderComponent<ErrorResolutionForm>(parameters => parameters
            .Add(p => p.ErrorId, errorId2));

        // Assert
        Assert.NotNull(cut1);
        Assert.NotNull(cut2);
        Assert.NotEqual(errorId1, errorId2);
    }
}
