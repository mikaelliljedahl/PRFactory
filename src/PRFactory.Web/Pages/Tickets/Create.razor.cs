using Microsoft.AspNetCore.Components;
using PRFactory.Web.Models;
using PRFactory.Web.Services;

namespace PRFactory.Web.Pages.Tickets;

public partial class Create
{
    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    private IRepositoryService RepositoryService { get; set; } = null!;

    [Inject]
    private ITicketService TicketService { get; set; } = null!;

    [Inject]
    private IToastService ToastService { get; set; } = null!;

    [Inject]
    private ILogger<Create> Logger { get; set; } = null!;

    private CreateTicketModel model = new();
    private List<RepositoryDto> repositories = new();
    private bool isSubmitting = false;
    private string? errorMessage;

    protected override async Task OnInitializedAsync()
    {
        await LoadRepositories();
    }

    private async Task LoadRepositories()
    {
        try
        {
            var allRepos = await RepositoryService.GetAllRepositoriesAsync();
            repositories = allRepos.Select(r => new RepositoryDto
            {
                Id = r.Id,
                Name = r.Name
            }).ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load repositories");
            errorMessage = "Failed to load repositories. Please refresh the page.";
            ToastService.ShowError("Failed to load repositories. Please refresh the page.");
        }
    }

    private async Task HandleSubmit()
    {
        isSubmitting = true;
        errorMessage = null;

        try
        {
            // Use TicketService directly (Blazor Server architecture - NO HTTP calls)
            var ticket = await TicketService.CreateTicketAsync(
                ticketKey: model.TicketKey,
                title: model.Title,
                description: model.Description,
                repositoryId: model.RepositoryId);

            // Show success toast
            ToastService.ShowSuccess($"Ticket '{ticket.TicketKey}' created successfully!");

            // Navigate to the ticket detail page
            NavigationManager.NavigateTo($"/tickets/{ticket.Id}");
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to create ticket: {ex.Message}";
            ToastService.ShowError($"Failed to create ticket: {ex.Message}");
        }
        finally
        {
            isSubmitting = false;
        }
    }
}
