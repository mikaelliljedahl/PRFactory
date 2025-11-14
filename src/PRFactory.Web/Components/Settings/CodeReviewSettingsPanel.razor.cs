using Microsoft.AspNetCore.Components;
using PRFactory.Core.Application.DTOs;

namespace PRFactory.Web.Components.Settings;

public partial class CodeReviewSettingsPanel
{
    [Parameter, EditorRequired]
    public TenantConfigurationDto Configuration { get; set; } = null!;

    [Parameter]
    public bool CanEdit { get; set; }
}
