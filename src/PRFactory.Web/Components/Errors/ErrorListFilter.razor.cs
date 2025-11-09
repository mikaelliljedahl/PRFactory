using Microsoft.AspNetCore.Components;
using PRFactory.Domain.ValueObjects;

namespace PRFactory.Web.Components.Errors;

public partial class ErrorListFilter
{
    private string _selectedSeverity = string.Empty;
    private string _entityType = string.Empty;
    private string _resolvedStatus = string.Empty;
    private DateTime? _fromDate;
    private DateTime? _toDate;
    private string _searchTerm = string.Empty;

    [Parameter]
    public EventCallback<FilterChangedArgs> OnFiltersChanged { get; set; }

    public string SelectedSeverity
    {
        get => _selectedSeverity;
        set => _selectedSeverity = value;
    }

    public string EntityType
    {
        get => _entityType;
        set => _entityType = value;
    }

    public string ResolvedStatus
    {
        get => _resolvedStatus;
        set => _resolvedStatus = value;
    }

    public DateTime? FromDate
    {
        get => _fromDate;
        set => _fromDate = value;
    }

    public DateTime? ToDate
    {
        get => _toDate;
        set => _toDate = value;
    }

    public string SearchTerm
    {
        get => _searchTerm;
        set => _searchTerm = value;
    }

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
