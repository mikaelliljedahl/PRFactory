using Radzen;

namespace PRFactory.Web.UI.Dialogs;

/// <summary>
/// Static helper for confirmation dialogs using Radzen DialogService.
/// Usage: await ConfirmDialogHelper.ShowAsync(dialogService, "Delete?", "Are you sure?");
/// </summary>
public static class ConfirmDialogHelper
{
    private const string DefaultCancelText = "Cancel";
    private const string DefaultDangerButtonClass = "btn-danger";
    /// <summary>
    /// Shows a confirmation dialog and returns true if confirmed, false if cancelled
    /// </summary>
    public static async Task<bool> ShowAsync(
        DialogService dialogService,
        string title,
        string message,
        string confirmText = "Confirm",
        string? cancelText = DefaultCancelText,
        string confirmButtonClass = DefaultDangerButtonClass,
        string? icon = null)
    {
        var options = new ConfirmOptions
        {
            OkButtonText = confirmText,
            CancelButtonText = cancelText,
            AutoFocusFirstElement = true
        };

        var result = await dialogService.Confirm(message, title, options);
        return result == true;
    }

    /// <summary>
    /// Shows a delete confirmation dialog
    /// </summary>
    public static async Task<bool> ShowDeleteAsync(
        DialogService dialogService,
        string itemName,
        string? additionalMessage = null)
    {
        var message = $"Are you sure you want to delete '{itemName}'?";
        if (!string.IsNullOrEmpty(additionalMessage))
        {
            message += $"\n\n{additionalMessage}";
        }
        message += "\n\nThis action cannot be undone.";

        return await ShowAsync(
            dialogService,
            "Confirm Delete",
            message,
            "Delete",
            DefaultCancelText,
            DefaultDangerButtonClass,
            "trash");
    }

    /// <summary>
    /// Shows a deactivate confirmation dialog
    /// </summary>
    public static async Task<bool> ShowDeactivateAsync(
        DialogService dialogService,
        string itemName)
    {
        var message = $"Are you sure you want to deactivate '{itemName}'?\n\n" +
                     "Deactivated tenants will not process new tickets.";

        return await ShowAsync(
            dialogService,
            "Confirm Deactivation",
            message,
            "Deactivate",
            DefaultCancelText,
            "btn-warning",
            "pause-circle");
    }

    /// <summary>
    /// Shows an activate confirmation dialog
    /// </summary>
    public static async Task<bool> ShowActivateAsync(
        DialogService dialogService,
        string itemName)
    {
        var message = $"Are you sure you want to activate '{itemName}'?";

        return await ShowAsync(
            dialogService,
            "Confirm Activation",
            message,
            "Activate",
            DefaultCancelText,
            "btn-success",
            "play-circle");
    }

    /// <summary>
    /// Shows a delete repository confirmation dialog with ticket count check
    /// </summary>
    public static async Task<bool> ShowDeleteRepositoryAsync(
        DialogService dialogService,
        string repositoryName,
        int ticketCount = 0)
    {
        var message = $"Are you sure you want to delete the repository '{repositoryName}'?\n\n";

        if (ticketCount > 0)
        {
            message += $"This repository has {ticketCount} associated ticket(s). " +
                      "Deleting this repository may affect ticket processing.\n\n";
        }

        message += "This action cannot be undone.";

        return await ShowAsync(
            dialogService,
            "Delete Repository",
            message,
            "Delete Repository",
            DefaultCancelText,
            DefaultDangerButtonClass,
            "exclamation-triangle");
    }

    /// <summary>
    /// Shows a delete prompt template confirmation dialog with system template check
    /// </summary>
    public static async Task<bool> ShowDeletePromptTemplateAsync(
        DialogService dialogService,
        string templateName,
        bool isSystemTemplate = false)
    {
        if (isSystemTemplate)
        {
            var systemMessage = $"Cannot delete system template '{templateName}'.\n\n" +
                               "System templates are protected and cannot be deleted. " +
                               "Clone it to create a custom version instead.";

            await ShowAsync(
                dialogService,
                "Cannot Delete System Template",
                systemMessage,
                "OK",
                null,
                "btn-info");

            return false;
        }

        var message = $"Are you sure you want to delete the prompt template '{templateName}'?\n\n" +
                     "This action cannot be undone.";

        return await ShowAsync(
            dialogService,
            "Delete Prompt Template",
            message,
            "Delete Template",
            DefaultCancelText,
            DefaultDangerButtonClass,
            "exclamation-triangle");
    }
}
