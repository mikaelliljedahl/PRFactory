using Microsoft.AspNetCore.Components;
using PRFactory.Web.Models;
using PRFactory.Web.Services;
using Radzen;

namespace PRFactory.Web.Pages.Repositories;

public partial class Create
{
    private CreateRepositoryRequest model = new() { DefaultBranch = "main" };
    private List<TenantDto> tenants = new();
    private bool isSubmitting;
    private bool connectionTested;
    private string? errorMessage;

    [Inject]
    private IRepositoryService RepositoryService { get; set; } = null!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    private ILogger<Create> Logger { get; set; } = null!;

    [Inject]
    private DialogService DialogService { get; set; } = null!;

    [Inject]
    private IToastService ToastService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await LoadTenantsAsync();
    }

    private async Task LoadTenantsAsync()
    {
        try
        {
            tenants = await RepositoryService.GetAllTenantsAsync();

            if (tenants.Count == 1)
            {
                model.TenantId = tenants[0].Id;
            }

            Logger.LogInformation("Loaded {Count} tenants", tenants.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading tenants");
            errorMessage = "Failed to load tenants. Please refresh the page.";
            ToastService.ShowError("Failed to load tenants. Please refresh the page.");
        }
    }

    private async Task HandleNextStep(object formModel)
    {
        connectionTested = false;
        await Task.CompletedTask;
    }

    private async Task HandlePreviousStep()
    {
        connectionTested = false;
        await Task.CompletedTask;
    }

    private void HandleTestCompleted(RepositoryConnectionTestResult result)
    {
        connectionTested = result.Success;

        if (result.Success && result.AvailableBranches.Any())
        {
            if (!result.AvailableBranches.Contains(model.DefaultBranch))
            {
                model.DefaultBranch = result.AvailableBranches.First();
            }
        }
    }

    private async Task HandleCreateRepository()
    {
        if (!connectionTested)
        {
            errorMessage = "Please test the connection before creating the repository.";
            ToastService.ShowWarning("Please test the connection before creating the repository.");
            return;
        }

        try
        {
            isSubmitting = true;
            errorMessage = null;
            StateHasChanged();

            Logger.LogInformation("Creating repository: {RepositoryName}", model.Name);

            var created = await RepositoryService.CreateRepositoryAsync(model);

            Logger.LogInformation("Repository created successfully: {RepositoryId}", created.Id);

            // Show success toast
            ToastService.ShowSuccess($"Repository '{created.Name}' created successfully!");

            NavigationManager.NavigateTo($"/repositories/{created.Id}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating repository");
            errorMessage = $"Failed to create repository: {ex.Message}";
            ToastService.ShowError($"Failed to create repository: {ex.Message}");
        }
        finally
        {
            isSubmitting = false;
            StateHasChanged();
        }
    }

    private void HandleCancel()
    {
        NavigationManager.NavigateTo("/repositories");
    }
}
