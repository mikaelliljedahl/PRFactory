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

    private CreateTicketModel model = new();
    private List<RepositoryDto> repositories = new();
    private bool isSubmitting = false;

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
        catch
        {
            // Log error but don't fail page load
            // Repositories list will remain empty on error
        }
    }

    private async Task HandleSubmit()
    {
        isSubmitting = true;
        try
        {
            // Use TicketService directly (Blazor Server architecture - NO HTTP calls)
            var ticket = await TicketService.CreateTicketAsync(
                ticketKey: model.TicketKey,
                title: model.Title,
                description: model.Description,
                repositoryId: model.RepositoryId);

            // Navigate to the ticket detail page
            NavigationManager.NavigateTo($"/tickets/{ticket.Id}");
        }
        catch (Exception ex)
        {
            // TODO: Show error message to user
            Console.WriteLine($"Error creating ticket: {ex.Message}");
        }
        finally
        {
            isSubmitting = false;
        }
    }
}
