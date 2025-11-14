using Microsoft.AspNetCore.Components;
using PRFactory.Domain.ValueObjects;
using PRFactory.Web.Components.Errors;
using PRFactory.Web.Models;
using PRFactory.Web.Services;
using PRFactory.Web.UI.Cards;
using Radzen;

namespace PRFactory.Web.Pages.Errors;

public partial class Index
{
    [Inject]
    private IErrorService ErrorService { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private ILogger<Index> Logger { get; set; } = null!;

    private List<ErrorDto> Errors { get; set; } = new();
    private ErrorStatisticsDto? Statistics { get; set; }
    private HashSet<Guid> SelectedErrorIds { get; set; } = new();
    private bool IsLoading { get; set; } = true;

    // Pagination
    private int PageSize { get; set; } = 20;
    private int CurrentPage { get; set; } = 1;
    private int TotalCount { get; set; }

    // Filters
    private ErrorSeverity? FilterSeverity { get; set; }
    private string? FilterEntityType { get; set; }
    private bool? FilterIsResolved { get; set; }
    private DateTime? FilterFromDate { get; set; }
    private DateTime? FilterToDate { get; set; }
    private string? FilterSearchTerm { get; set; }

    // For demo purposes - in real app, get from auth/session
    private Guid TenantId { get; set; } = Guid.Parse("00000000-0000-0000-0000-000000000001");

    protected override async Task OnInitializedAsync()
    {
        await LoadStatistics();
        await LoadErrors();
    }

    private async Task LoadStatistics()
    {
        try
        {
            Statistics = await ErrorService.GetStatisticsAsync(TenantId);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to load error statistics");
        }
    }

    private async Task LoadErrors()
    {
        IsLoading = true;
        try
        {
            var (items, totalCount) = await ErrorService.GetErrorsAsync(
                TenantId,
                CurrentPage,
                PageSize,
                FilterSeverity,
                FilterEntityType,
                FilterIsResolved,
                FilterFromDate,
                FilterToDate,
                FilterSearchTerm);

            Errors = items;
            TotalCount = totalCount;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load errors");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadData(LoadDataArgs args)
    {
        CurrentPage = (args.Skip ?? 0) / PageSize + 1;
        await LoadErrors();
    }

    private async Task HandleFiltersChanged(ErrorListFilter.FilterChangedArgs args)
    {
        FilterSeverity = args.Severity;
        FilterEntityType = args.EntityType;
        FilterIsResolved = args.IsResolved;
        FilterFromDate = args.FromDate;
        FilterToDate = args.ToDate;
        FilterSearchTerm = args.SearchTerm;

        CurrentPage = 1;
        await LoadErrors();
    }

    private async Task RefreshErrors()
    {
        await LoadStatistics();
        await LoadErrors();
        StateHasChanged();
    }

    private void ViewErrorDetails(Guid errorId)
    {
        Navigation.NavigateTo($"/errors/{errorId}");
    }

    private async Task ResolveError(Guid errorId)
    {
        try
        {
            await ErrorService.MarkErrorResolvedAsync(errorId);
            await RefreshErrors();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to resolve error {ErrorId}", errorId);
        }
    }

    private async Task BulkResolveErrors()
    {
        try
        {
            await ErrorService.BulkMarkErrorsResolvedAsync(SelectedErrorIds.ToList());
            SelectedErrorIds.Clear();
            await RefreshErrors();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to bulk mark errors as resolved");
        }
    }

    private async Task RetryOperation(Guid errorId)
    {
        try
        {
            var success = await ErrorService.RetryFailedOperationAsync(errorId);
            if (success)
            {
                await RefreshErrors();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to retry operation for error {ErrorId}", errorId);
        }
    }

    private void ToggleErrorSelection(Guid errorId)
    {
        if (SelectedErrorIds.Contains(errorId))
        {
            SelectedErrorIds.Remove(errorId);
        }
        else
        {
            SelectedErrorIds.Add(errorId);
        }
    }

    private void ToggleSelectAll(ChangeEventArgs e)
    {
        if (e.Value is bool isChecked && isChecked)
        {
            SelectedErrorIds = Errors.Select(e => e.Id).ToHashSet();
        }
        else
        {
            SelectedErrorIds.Clear();
        }
    }

    private CardVariant GetHealthCardVariant()
    {
        if (Statistics == null)
            return CardVariant.Primary;

        return Statistics.HealthStatusClass switch
        {
            "success" => CardVariant.Success,
            "warning" => CardVariant.Warning,
            "danger" => CardVariant.Danger,
            _ => CardVariant.Primary
        };
    }
}
