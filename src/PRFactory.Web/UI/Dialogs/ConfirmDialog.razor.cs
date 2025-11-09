using Radzen;

namespace PRFactory.Web.UI.Dialogs;

/// <summary>
/// Static helper for confirmation dialogs using Radzen DialogService.
/// Usage: await ConfirmDialogHelper.ShowAsync(dialogService, "Delete?", "Are you sure?");
/// </summary>
public static class ConfirmDialogHelper
{
    /// <summary>
    /// Shows a confirmation dialog and returns true if confirmed, false if cancelled
    /// </summary>
    public static async Task<bool> ShowAsync(
        DialogService dialogService,
        string title,
        string message,
        string confirmText = "Confirm",
        string cancelText = "Cancel",
        string confirmButtonClass = "btn-danger",
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
            "Cancel",
            "btn-danger",
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
            "Cancel",
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
            "Cancel",
            "btn-success",
            "play-circle");
    }
}
