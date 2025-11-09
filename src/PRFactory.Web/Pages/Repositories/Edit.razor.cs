using Microsoft.AspNetCore.Components;
using PRFactory.Web.Models;

namespace PRFactory.Web.Pages.Repositories;

public partial class Edit
{
    private RepositoryDto? repository;
    private UpdateRepositoryRequest model = new();
    private bool isLoading = true;
    private bool isSubmitting;
    private string? errorMessage;
    private string? successMessage;

    [Parameter]
    public Guid RepositoryId { get; set; }

    [Inject]
    private IRepositoryService RepositoryService { get; set; } = null!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    private ILogger<Edit> Logger { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await LoadRepositoryAsync();
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
                model = new UpdateRepositoryRequest
                {
                    Name = repository.Name,
                    GitPlatform = repository.GitPlatform,
                    CloneUrl = repository.CloneUrl,
                    DefaultBranch = repository.DefaultBranch,
                    IsActive = repository.IsActive,
                    AccessToken = string.Empty
                };
            }

            Logger.LogInformation("Loaded repository: {RepositoryId}", RepositoryId);
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

    private async Task HandleValidSubmit(object formModel)
    {
        try
        {
            isSubmitting = true;
            errorMessage = null;
            successMessage = null;
            StateHasChanged();

            Logger.LogInformation("Updating repository: {RepositoryId}", RepositoryId);

            await RepositoryService.UpdateRepositoryAsync(RepositoryId, model);

            Logger.LogInformation("Repository updated successfully: {RepositoryId}", RepositoryId);

            successMessage = "Repository updated successfully.";

            await LoadRepositoryAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating repository {RepositoryId}", RepositoryId);
            errorMessage = $"Failed to update repository: {ex.Message}";
        }
        finally
        {
            isSubmitting = false;
            StateHasChanged();
        }
    }

    private void HandleCancel()
    {
        NavigationManager.NavigateTo($"/repositories/{RepositoryId}");
    }
}
