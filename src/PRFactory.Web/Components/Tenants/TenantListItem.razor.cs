using Microsoft.AspNetCore.Components;
using PRFactory.Web.Models;

namespace PRFactory.Web.Components.Tenants;

public partial class TenantListItem
{
    [Parameter, EditorRequired]
    public TenantDto Tenant { get; set; } = null!;

    [Parameter]
    public EventCallback<Guid> OnView { get; set; }

    [Parameter]
    public EventCallback<Guid> OnEdit { get; set; }

    [Parameter]
    public EventCallback<Guid> OnActivate { get; set; }

    [Parameter]
    public EventCallback<Guid> OnDeactivate { get; set; }

    [Parameter]
    public EventCallback<Guid> OnDelete { get; set; }
}
