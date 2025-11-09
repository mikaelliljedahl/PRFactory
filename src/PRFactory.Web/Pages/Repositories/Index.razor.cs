using Microsoft.AspNetCore.Components;
using PRFactory.Web.Models;
using PRFactory.Web.Services;

namespace PRFactory.Web.Pages.Repositories;

public partial class Index
{
    private List<RepositoryDto>? repositories;
    private bool isLoading = true;
    private string? errorMessage;

    [Inject]
    private IRepositoryService RepositoryService { get; set; } = null!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    private ILogger<Index> Logger { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await LoadRepositoriesAsync();
    }

    private async Task LoadRepositoriesAsync()
    {
        try
        {
            isLoading = true;
            errorMessage = null;
            StateHasChanged();

            repositories = await RepositoryService.GetAllRepositoriesAsync();

            Logger.LogInformation("Loaded {Count} repositories", repositories.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading repositories");
            errorMessage = "Failed to load repositories. Please try again.";
            repositories = new List<Repository>();
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private async Task ViewRepositoryAsync(Guid repositoryId)
    {
        Logger.LogInformation("Navigating to repository details: {RepositoryId}", repositoryId);
        NavigationManager.NavigateTo($"/repositories/{repositoryId}");
        await Task.CompletedTask;
    }

    private async Task EditRepositoryAsync(Guid repositoryId)
    {
        Logger.LogInformation("Navigating to edit repository: {RepositoryId}", repositoryId);
        NavigationManager.NavigateTo($"/repositories/{repositoryId}/edit");
        await Task.CompletedTask;
    }

    private async Task DeleteRepositoryAsync(Guid repositoryId)
    {
        try
        {
            var repository = repositories?.FirstOrDefault(r => r.Id == repositoryId);
            if (repository == null)
            {
                Logger.LogWarning("Repository not found: {RepositoryId}", repositoryId);
                return;
            }

            // Simple confirmation - in a real app, you'd want a proper confirmation dialog
            Logger.LogInformation("Deleting repository: {RepositoryId} - {Name}", repositoryId, repository.Name);

            await RepositoryService.DeleteRepositoryAsync(repositoryId);

            // Reload the list
            await LoadRepositoriesAsync();

            Logger.LogInformation("Repository deleted successfully: {RepositoryId}", repositoryId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting repository: {RepositoryId}", repositoryId);
            errorMessage = "Failed to delete repository. Please try again.";
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
}
