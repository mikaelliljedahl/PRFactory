namespace PRFactory.Web.Services;

public interface IToastService
{
    event Action? OnToastsChanged;

    void ShowSuccess(string message, string title = "Success");
    void ShowError(string message, string title = "Error");
    void ShowWarning(string message, string title = "Warning");
    void ShowInfo(string message, string title = "Info");
    void ShowToast(string title, string message, ToastType type, string? icon = null, int autoHideDuration = 5000);
    List<ToastModel> GetToasts();
    void RemoveToast(Guid id);
}

public class ToastModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public ToastType Type { get; set; }
    public string Icon { get; set; } = string.Empty;
    public bool IsVisible { get; set; }
}

public enum ToastType
{
    Success,
    Error,
    Warning,
    Info,
    Primary
}
