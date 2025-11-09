using Microsoft.AspNetCore.Components;
using PRFactory.Web.Models;

namespace PRFactory.Web.Components.Tenants;

public partial class TenantForm
{
    [Parameter, EditorRequired]
    public ITenantRequest Model { get; set; } = null!;

    [Parameter]
    public bool IsEditMode { get; set; }

    [Parameter]
    public bool IsSubmitting { get; set; }

    [Parameter]
    public EventCallback OnValidSubmit { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    // String bindings for numeric fields (workaround for two-way binding with InputText)
    private string maxRetriesString
    {
        get => GetMaxRetries().ToString();
        set
        {
            if (int.TryParse(value, out var result))
            {
                SetMaxRetries(result);
            }
        }
    }

    private string maxTokensString
    {
        get => GetMaxTokens().ToString();
        set
        {
            if (int.TryParse(value, out var result))
            {
                SetMaxTokens(result);
            }
        }
    }

    private string apiTimeoutString
    {
        get => GetApiTimeout().ToString();
        set
        {
            if (int.TryParse(value, out var result))
            {
                SetApiTimeout(result);
            }
        }
    }

    private async Task HandleValidSubmit()
    {
        await OnValidSubmit.InvokeAsync();
    }

    private async Task HandleCancel()
    {
        await OnCancel.InvokeAsync();
    }

    // Helper methods for string-to-int binding (workaround for two-way binding with InputText)
    private int GetMaxRetries() => Model.MaxRetries;
    private void SetMaxRetries(int value) => Model.MaxRetries = value;

    private int GetMaxTokens() => Model.MaxTokensPerRequest;
    private void SetMaxTokens(int value) => Model.MaxTokensPerRequest = value;

    private int GetApiTimeout() => Model.ApiTimeoutSeconds;
    private void SetApiTimeout(int value) => Model.ApiTimeoutSeconds = value;
}
