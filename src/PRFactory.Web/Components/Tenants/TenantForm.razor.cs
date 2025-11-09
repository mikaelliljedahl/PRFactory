using Microsoft.AspNetCore.Components;
using PRFactory.Web.Models;

namespace PRFactory.Web.Components.Tenants;

public partial class TenantForm
{
    [Parameter, EditorRequired]
    public object Model { get; set; } = null!;

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

    // Helper methods to handle both CreateTenantRequest and UpdateTenantRequest
    private int GetMaxRetries()
    {
        return Model switch
        {
            CreateTenantRequest create => create.MaxRetries,
            UpdateTenantRequest update => update.MaxRetries,
            _ => 3
        };
    }

    private void SetMaxRetries(int value)
    {
        switch (Model)
        {
            case CreateTenantRequest create:
                create.MaxRetries = value;
                break;
            case UpdateTenantRequest update:
                update.MaxRetries = value;
                break;
        }
    }

    private int GetMaxTokens()
    {
        return Model switch
        {
            CreateTenantRequest create => create.MaxTokensPerRequest,
            UpdateTenantRequest update => update.MaxTokensPerRequest,
            _ => 8000
        };
    }

    private void SetMaxTokens(int value)
    {
        switch (Model)
        {
            case CreateTenantRequest create:
                create.MaxTokensPerRequest = value;
                break;
            case UpdateTenantRequest update:
                update.MaxTokensPerRequest = value;
                break;
        }
    }

    private int GetApiTimeout()
    {
        return Model switch
        {
            CreateTenantRequest create => create.ApiTimeoutSeconds,
            UpdateTenantRequest update => update.ApiTimeoutSeconds,
            _ => 300
        };
    }

    private void SetApiTimeout(int value)
    {
        switch (Model)
        {
            case CreateTenantRequest create:
                create.ApiTimeoutSeconds = value;
                break;
            case UpdateTenantRequest update:
                update.ApiTimeoutSeconds = value;
                break;
        }
    }
}
