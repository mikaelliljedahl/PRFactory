using Microsoft.AspNetCore.Components;
using PRFactory.Core.Application.Services;
using PRFactory.Web.Models;

namespace PRFactory.Web.UI.Comments;

public partial class InlineCommentPanel
{
    [Parameter, EditorRequired]
    public Guid TicketId { get; set; }

    [Parameter]
    public EventCallback<InlineCommentAnchorDto> OnAnchorSelected { get; set; }

    [Inject]
    private IPlanReviewService PlanReviewService { get; set; } = null!;

    private List<InlineCommentAnchorDto> Anchors { get; set; } = new();
    private bool IsLoading { get; set; } = true;
    private bool showAll = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadAnchors();
    }

    private async Task LoadAnchors()
    {
        IsLoading = true;
        try
        {
            var anchors = await PlanReviewService.GetInlineCommentAnchorsAsync(TicketId);
            Anchors = anchors.Select(a => InlineCommentAnchorDto.FromEntity(a, includeComment: true)).ToList();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private IEnumerable<InlineCommentAnchorDto> GetFilteredAnchors()
    {
        if (showAll)
            return Anchors;

        // For now, show all. In the future, this could filter by resolved status
        return Anchors;
    }

    private void ToggleFilter()
    {
        showAll = !showAll;
    }

    private async Task ScrollToAnchor(InlineCommentAnchorDto anchor)
    {
        if (OnAnchorSelected.HasDelegate)
        {
            await OnAnchorSelected.InvokeAsync(anchor);
        }
    }

    private string FormatRelativeTime(DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;

        if (timeSpan.TotalMinutes < 1)
            return "just now";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes}m ago";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours}h ago";
        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays}d ago";

        return dateTime.ToString("MMM d");
    }

    private string GetCommentPreview(string content)
    {
        const int maxLength = 150;
        if (content.Length <= maxLength)
            return content;

        return content.Substring(0, maxLength) + "...";
    }

    /// <summary>
    /// Refreshes the anchor list from the server
    /// </summary>
    public async Task RefreshAsync()
    {
        await LoadAnchors();
        StateHasChanged();
    }
}
