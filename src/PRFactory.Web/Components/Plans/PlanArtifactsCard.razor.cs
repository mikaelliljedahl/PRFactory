using Microsoft.AspNetCore.Components;
using PRFactory.Core.Application.DTOs;

namespace PRFactory.Web.Components.Plans;

/// <summary>
/// Displays plan artifacts in a tabbed interface
/// </summary>
public partial class PlanArtifactsCard
{
    [Parameter, EditorRequired]
    public PlanDto? Plan { get; set; }

    [Parameter]
    public string AdditionalClass { get; set; } = "mb-3";

    [Parameter]
    public EventCallback<PlanVersionDto> OnVersionSelected { get; set; }

    private Task HandleVersionSelected(PlanVersionDto version)
    {
        return OnVersionSelected.InvokeAsync(version);
    }
}
