using Microsoft.AspNetCore.Components;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Web.Services;

namespace PRFactory.Web.Components.Tickets;

public partial class ReviewerAssignment
{
    [Parameter, EditorRequired]
    public Guid TicketId { get; set; }

    [Parameter]
    public EventCallback OnAssigned { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    [Inject]
    private ITicketService TicketService { get; set; } = null!;

    [Inject]
    private IUserService UserService { get; set; } = null!;

    [Inject]
    private ITenantContext TenantContext { get; set; } = null!;

    [Inject]
    private IToastService ToastService { get; set; } = null!;

    [Inject]
    private ILogger<ReviewerAssignment> Logger { get; set; } = null!;

    private List<User> AvailableUsers { get; set; } = new();
    private HashSet<Guid> SelectedRequiredReviewers { get; set; } = new();
    private HashSet<Guid> SelectedOptionalReviewers { get; set; } = new();

    private bool IsLoading { get; set; } = true;
    private bool IsSaving { get; set; } = false;
    private string? ErrorMessage { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await LoadAvailableUsersAsync();
    }

    private async Task LoadAvailableUsersAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var tenantId = await TenantContext.GetTenantIdAsync();
            if (!tenantId.HasValue)
            {
                ErrorMessage = "No tenant context found";
                return;
            }

            AvailableUsers = await UserService.GetByTenantIdAsync(tenantId.Value);
            Logger.LogInformation("Loaded {Count} users for reviewer assignment", AvailableUsers.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading users for reviewer assignment");
            ErrorMessage = $"Error loading users: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ToggleRequiredReviewer(Guid userId, ChangeEventArgs e)
    {
        if (e.Value is bool isChecked)
        {
            if (isChecked)
            {
                SelectedRequiredReviewers.Add(userId);
                // Remove from optional if it was there
                SelectedOptionalReviewers.Remove(userId);
            }
            else
            {
                SelectedRequiredReviewers.Remove(userId);
            }
        }
    }

    private void ToggleOptionalReviewer(Guid userId, ChangeEventArgs e)
    {
        if (e.Value is bool isChecked)
        {
            if (isChecked)
            {
                SelectedOptionalReviewers.Add(userId);
            }
            else
            {
                SelectedOptionalReviewers.Remove(userId);
            }
        }
    }

    private async Task HandleAssignReviewers()
    {
        try
        {
            if (!SelectedRequiredReviewers.Any())
            {
                ToastService.ShowWarning("Please select at least one required reviewer");
                return;
            }

            IsSaving = true;

            var requiredList = SelectedRequiredReviewers.ToList();
            var optionalList = SelectedOptionalReviewers.Any() ? SelectedOptionalReviewers.ToList() : null;

            await TicketService.AssignReviewersAsync(TicketId, requiredList, optionalList);

            ToastService.ShowSuccess("Reviewers assigned successfully");
            Logger.LogInformation("Assigned {RequiredCount} required and {OptionalCount} optional reviewers to ticket {TicketId}",
                requiredList.Count, optionalList?.Count ?? 0, TicketId);

            await OnAssigned.InvokeAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error assigning reviewers to ticket {TicketId}", TicketId);
            ToastService.ShowError($"Error assigning reviewers: {ex.Message}");
        }
        finally
        {
            IsSaving = false;
        }
    }
}
