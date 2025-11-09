using Microsoft.AspNetCore.Components;
using PRFactory.Web.Models;
using PRFactory.Web.Services;

namespace PRFactory.Web.Pages.Tickets;

public partial class Create
{
    [Inject]
    private IHttpClientFactory HttpClientFactory { get; set; } = null!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    private IRepositoryService RepositoryService { get; set; } = null!;

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
        catch (Exception ex)
        {
            // Log error but don't fail page load
        }
    }

    private async Task HandleSubmit()
    {
        isSubmitting = true;
        try
        {
            // TODO: POST to API /api/tickets
            // var httpClient = HttpClientFactory.CreateClient("PRFactoryAPI");
            // var response = await httpClient.PostAsJsonAsync("/api/tickets", model);
            // if (response.IsSuccessStatusCode)
            // {
            //     var ticket = await response.Content.ReadFromJsonAsync<TicketDto>();
            //     NavigationManager.NavigateTo($"/tickets/{ticket.Id}");
            // }

            // Mock navigation for now
            await Task.Delay(1000);
            NavigationManager.NavigateTo("/tickets");
        }
        finally
        {
            isSubmitting = false;
        }
    }
}
