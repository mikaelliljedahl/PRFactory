using Microsoft.AspNetCore.Components;
using PRFactory.Domain.ValueObjects;

namespace PRFactory.Web.Components.Errors;

public partial class ErrorListFilter
{
    [Parameter]
    public EventCallback<FilterChangedArgs> OnFiltersChanged { get; set; }

    public string SelectedSeverity { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public string ResolvedStatus { get; set; } = string.Empty;

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public string SearchTerm { get; set; } = string.Empty;

    private async Task OnFilterChanged()
    {
        if (OnFiltersChanged.HasDelegate)
        {
            var args = new FilterChangedArgs
            {
                Severity = string.IsNullOrWhiteSpace(SelectedSeverity)
                    ? null
                    : Enum.Parse<ErrorSeverity>(SelectedSeverity),
                EntityType = string.IsNullOrWhiteSpace(EntityType) ? null : EntityType,
                IsResolved = ResolvedStatus switch
                {
                    "resolved" => true,
                    "unresolved" => false,
                    _ => null
                },
                FromDate = FromDate,
                ToDate = ToDate,
                SearchTerm = string.IsNullOrWhiteSpace(SearchTerm) ? null : SearchTerm
            };

            await OnFiltersChanged.InvokeAsync(args);
        }
    }

    private async Task ClearFilters()
    {
        SelectedSeverity = string.Empty;
        EntityType = string.Empty;
        ResolvedStatus = string.Empty;
        FromDate = null;
        ToDate = null;
        SearchTerm = string.Empty;

        await OnFilterChanged();
    }

    public class FilterChangedArgs
    {
        public ErrorSeverity? Severity { get; set; }
        public string? EntityType { get; set; }
        public bool? IsResolved { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? SearchTerm { get; set; }
    }
}
