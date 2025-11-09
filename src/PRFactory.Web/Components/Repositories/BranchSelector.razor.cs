using Microsoft.AspNetCore.Components;

namespace PRFactory.Web.Components.Repositories;

public partial class BranchSelector
{
    [Parameter]
    public Guid RepositoryId { get; set; }

    [Parameter]
    public string? SelectedBranch { get; set; }

    [Parameter]
    public EventCallback<string?> SelectedBranchChanged { get; set; }

    [Parameter]
    public bool Required { get; set; }

    [Parameter]
    public string? HelpText { get; set; }

    [Inject]
    private IRepositoryService RepositoryService { get; set; } = null!;

    [Inject]
    private ILogger<BranchSelector> Logger { get; set; } = null!;

    private List<string> AvailableBranches { get; set; } = new();
    private bool IsLoading { get; set; }
    private string? ErrorMessage { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        if (RepositoryId != Guid.Empty && !AvailableBranches.Any())
        {
            await LoadBranchesAsync();
        }
    }

    private async Task HandleRefreshBranches()
    {
        await LoadBranchesAsync();
    }

    private async Task LoadBranchesAsync()
    {
        if (RepositoryId == Guid.Empty)
        {
            ErrorMessage = "Repository ID is required to load branches";
            return;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = null;
            StateHasChanged();

            Logger.LogInformation("Loading branches for repository {RepositoryId}", RepositoryId);

            AvailableBranches = await RepositoryService.GetBranchesAsync(RepositoryId);

            Logger.LogInformation("Loaded {Count} branches", AvailableBranches.Count);

            if (!AvailableBranches.Any())
            {
                ErrorMessage = "No branches found in repository";
            }
            else if (string.IsNullOrEmpty(SelectedBranch) || !AvailableBranches.Contains(SelectedBranch))
            {
                var defaultBranch = AvailableBranches.FirstOrDefault(b => b == "main")
                                 ?? AvailableBranches.FirstOrDefault(b => b == "master")
                                 ?? AvailableBranches.First();

                SelectedBranch = defaultBranch;
                await SelectedBranchChanged.InvokeAsync(SelectedBranch);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading branches for repository {RepositoryId}", RepositoryId);
            ErrorMessage = $"Failed to load branches: {ex.Message}";
            AvailableBranches = new List<string>();
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private async Task OnBranchChanged()
    {
        await SelectedBranchChanged.InvokeAsync(SelectedBranch);
    }
}
