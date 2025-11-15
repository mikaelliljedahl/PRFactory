using Microsoft.AspNetCore.Components;
using PRFactory.Core.Application.DTOs;

namespace PRFactory.Web.Components.Plans;

/// <summary>
/// Displays plan version history
/// </summary>
public partial class PlanVersionHistory
{
    [Parameter, EditorRequired]
    public List<PlanVersionDto>? Versions { get; set; }

    [Parameter]
    public EventCallback<PlanVersionDto> OnVersionSelected { get; set; }

    private Task SelectVersion(PlanVersionDto version)
    {
        return OnVersionSelected.InvokeAsync(version);
    }
}
