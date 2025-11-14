using Microsoft.AspNetCore.Components;
using PRFactory.Core.Application.DTOs;

namespace PRFactory.Web.Components.Settings;

public partial class LlmProviderAssignmentPanel
{
    [Parameter, EditorRequired]
    public TenantConfigurationDto Configuration { get; set; } = null!;

    [Parameter, EditorRequired]
    public List<TenantLlmProviderDto> Providers { get; set; } = null!;

    [Parameter]
    public bool CanEdit { get; set; }

    private List<TenantLlmProviderDto> ActiveProviders =>
        Providers.Where(p => p.IsActive).ToList();
}
