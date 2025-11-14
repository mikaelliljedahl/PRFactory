using Microsoft.AspNetCore.Components;
using PRFactory.Web.Models;

namespace PRFactory.Web.Components.Settings;

public partial class TenantInfoPanel
{
    [Parameter, EditorRequired]
    public TenantDto Tenant { get; set; } = null!;
}
