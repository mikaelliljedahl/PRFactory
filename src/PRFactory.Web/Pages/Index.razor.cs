using Microsoft.AspNetCore.Components;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Domain.ValueObjects;
using PRFactory.Web.Services;

namespace PRFactory.Web.Pages;

public partial class Index
{
    [Inject] private ITicketService TicketService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private List<Ticket> tickets = new();
    private bool isLoading = true;
    private string? errorMessage;

    protected override async Task OnInitializedAsync()
    {
        await LoadTickets();
    }

    private async Task LoadTickets()
    {
        isLoading = true;
        errorMessage = null;
        try
        {
            tickets = await TicketService.GetAllTicketsAsync();
        }
        catch (Exception ex)
        {
            errorMessage = $"Error loading tickets: {ex.Message}";
        }
        finally
        {
            isLoading = false;
        }
    }

    private void ViewTicket(Guid ticketId)
    {
        Navigation.NavigateTo($"/tickets/{ticketId}");
    }
}
