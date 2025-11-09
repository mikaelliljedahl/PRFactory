using Microsoft.AspNetCore.Components;
using PRFactory.Web.Models;
using PRFactory.Web.Services;
using Markdig;

namespace PRFactory.Web.Components.Tickets;

public partial class ReviewCommentThread
{
    [Parameter, EditorRequired]
    public Guid TicketId { get; set; }

    [Parameter]
    public List<ReviewCommentDto> Comments { get; set; } = new();

    [Parameter]
    public EventCallback OnCommentAdded { get; set; }

    [Inject]
    private ITicketService TicketService { get; set; } = null!;

    [Inject]
    private IToastService ToastService { get; set; } = null!;

    [Inject]
    private ILogger<ReviewCommentThread> Logger { get; set; } = null!;

    private string NewCommentContent { get; set; } = string.Empty;
    private bool IsLoading { get; set; } = false;
    private bool IsSubmitting { get; set; } = false;

    private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    private async Task HandlePostComment()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(NewCommentContent))
            {
                ToastService.ShowWarning("Please enter a comment");
                return;
            }

            IsSubmitting = true;

            var comment = await TicketService.AddCommentAsync(TicketId, NewCommentContent);

            // Add to local list for immediate UI update
            Comments.Add(comment);

            // Clear the form
            NewCommentContent = string.Empty;

            ToastService.ShowSuccess("Comment posted successfully");
            Logger.LogInformation("Posted comment to ticket {TicketId}", TicketId);

            // Notify parent component
            await OnCommentAdded.InvokeAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error posting comment to ticket {TicketId}", TicketId);
            ToastService.ShowError($"Error posting comment: {ex.Message}");
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    private void ClearComment()
    {
        NewCommentContent = string.Empty;
    }

    private string FormatMarkdown(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        try
        {
            return Markdown.ToHtml(markdown, MarkdownPipeline);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error formatting markdown");
            // Fallback to plain text with line breaks
            return markdown.Replace("\n", "<br>");
        }
    }
}
