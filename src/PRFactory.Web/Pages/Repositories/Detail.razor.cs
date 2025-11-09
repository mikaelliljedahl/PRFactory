using Microsoft.AspNetCore.Components;
using PRFactory.Web.Models;
using PRFactory.Web.Services;
using PRFactory.Web.UI.Dialogs;
using PRFactory.Web.UI.Navigation;
using Radzen;

namespace PRFactory.Web.Pages.Repositories;

public partial class Detail
{
    private RepositoryDto? repository;
    private bool isLoading = true;
    private string? errorMessage;
    private List<BreadcrumbItem> breadcrumbItems = new();

    [Parameter]
    public Guid RepositoryId { get; set; }

    [Inject]
    private IRepositoryService RepositoryService { get; set; } = null!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    private ILogger<Detail> Logger { get; set; } = null!;

    [Inject]
    private DialogService DialogService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await LoadRepositoryAsync();

        if (repository != null)
        {
            BuildBreadcrumbs();
        }
    }

    private async Task LoadRepositoryAsync()
    {
        try
        {
            isLoading = true;
            errorMessage = null;
            StateHasChanged();

            repository = await RepositoryService.GetRepositoryByIdAsync(RepositoryId);

            if (repository != null)
            {
                Logger.LogInformation("Loaded repository: {RepositoryId} - {RepositoryName}",
                    RepositoryId, repository.Name);
            }
            else
            {
                Logger.LogWarning("Repository not found: {RepositoryId}", RepositoryId);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading repository {RepositoryId}", RepositoryId);
            errorMessage = "Failed to load repository. Please try again.";
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private async Task HandleDeleteAsync()
    {
        if (repository == null)
        {
            return;
        }

        bool confirmed = await ConfirmDialogHelper.ShowDeleteRepositoryAsync(
            DialogService,
            repository.Name,
            repository.TicketCount);

        if (!confirmed)
        {
            return;
        }

        try
        {
            if (repository.TicketCount > 0)
            {
                errorMessage = $"Cannot delete repository with {repository.TicketCount} associated ticket(s). Please delete or reassign the tickets first.";
                return;
            }

            Logger.LogInformation("Deleting repository: {RepositoryId} - {RepositoryName}",
                RepositoryId, repository.Name);

            await RepositoryService.DeleteRepositoryAsync(RepositoryId);

            Logger.LogInformation("Repository deleted successfully: {RepositoryId}", RepositoryId);

            NavigationManager.NavigateTo("/repositories");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting repository {RepositoryId}", RepositoryId);
            errorMessage = $"Failed to delete repository: {ex.Message}";
            StateHasChanged();
        }
    }

    private string GetPlatformIcon(string platform) => platform switch
    {
        "GitHub" => "github",
        "Bitbucket" => "git",
        "AzureDevOps" => "microsoft",
        "GitLab" => "git",
        _ => "folder2"
    };

    private void BuildBreadcrumbs()
    {
        breadcrumbItems = new List<BreadcrumbItem>
        {
            new BreadcrumbItem { Text = "Dashboard", Href = "/", Icon = "house" },
            new BreadcrumbItem { Text = "Repositories", Href = "/repositories", Icon = "folder" },
            new BreadcrumbItem
            {
                Text = repository?.Name ?? "Detail",
                Icon = "git"
            }
        };
    }
}
