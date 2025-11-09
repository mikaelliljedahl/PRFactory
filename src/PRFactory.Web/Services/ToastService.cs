namespace PRFactory.Web.Services;

public class ToastService : IToastService
{
    private readonly List<ToastModel> _toasts = new();

    public event Action? OnToastsChanged;

    public void ShowToast(string title, string message, ToastType type, string? icon = null, int autoHideDuration = 5000)
    {
        var toast = new ToastModel
        {
            Id = Guid.NewGuid(),
            Title = title,
            Message = message,
            Type = type,
            Icon = icon ?? GetDefaultIcon(type),
            IsVisible = true
        };

        _toasts.Add(toast);
        OnToastsChanged?.Invoke();

        if (autoHideDuration > 0)
        {
            _ = Task.Delay(autoHideDuration).ContinueWith(_ =>
            {
                RemoveToast(toast.Id);
            });
        }
    }

    public void ShowSuccess(string message, string title = "Success")
    {
        ShowToast(title, message, ToastType.Success, "check-circle");
    }

    public void ShowError(string message, string title = "Error")
    {
        ShowToast(title, message, ToastType.Error, "exclamation-triangle");
    }

    public void ShowWarning(string message, string title = "Warning")
    {
        ShowToast(title, message, ToastType.Warning, "exclamation-circle");
    }

    public void ShowInfo(string message, string title = "Info")
    {
        ShowToast(title, message, ToastType.Info, "info-circle");
    }

    public List<ToastModel> GetToasts()
    {
        return _toasts.ToList();
    }

    public void RemoveToast(Guid id)
    {
        var toast = _toasts.FirstOrDefault(t => t.Id == id);
        if (toast != null)
        {
            _toasts.Remove(toast);
            OnToastsChanged?.Invoke();
        }
    }

    private string GetDefaultIcon(ToastType type)
    {
        return type switch
        {
            ToastType.Success => "check-circle",
            ToastType.Error => "exclamation-triangle",
            ToastType.Warning => "exclamation-circle",
            ToastType.Info => "info-circle",
            _ => "bell"
        };
    }
}
