using Microsoft.AspNetCore.Components;
using PRFactory.Core.Application.DTOs;

namespace PRFactory.Web.Components.Settings;

public partial class LlmProviderListItem
{
    [Parameter, EditorRequired]
    public TenantLlmProviderDto Provider { get; set; } = null!;

    [Parameter]
    public bool CanEdit { get; set; }

    [Parameter]
    public EventCallback OnEdit { get; set; }

    [Parameter]
    public EventCallback OnDelete { get; set; }

    [Parameter]
    public EventCallback OnSetDefault { get; set; }

    [Parameter]
    public EventCallback OnTestConnection { get; set; }

    [Parameter]
    public EventCallback OnViewDetails { get; set; }
}
