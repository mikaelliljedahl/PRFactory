using Microsoft.AspNetCore.Components;
using PRFactory.Domain.ValueObjects;
using PRFactory.Web.Models;

namespace PRFactory.Web.Components.Tickets;

public partial class SuccessCriteriaEditor
{
    [Parameter, EditorRequired]
    public List<SuccessCriterionDto> SuccessCriteria { get; set; } = new();

    [Parameter]
    public EventCallback<List<SuccessCriterionDto>> SuccessCriteriaChanged { get; set; }

    private void AddCriterion()
    {
        SuccessCriteria.Add(new SuccessCriterionDto
        {
            Category = SuccessCriterionCategory.Functional,
            Priority = 0,
            IsTestable = true,
            Description = string.Empty
        });
        NotifyChanged();
    }

    private void RemoveCriterion(int index)
    {
        if (index >= 0 && index < SuccessCriteria.Count)
        {
            SuccessCriteria.RemoveAt(index);
            NotifyChanged();
        }
    }

    private async Task NotifyChanged()
    {
        await SuccessCriteriaChanged.InvokeAsync(SuccessCriteria);
        StateHasChanged();
    }
}
