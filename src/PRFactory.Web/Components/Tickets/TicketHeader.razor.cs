using Microsoft.AspNetCore.Components;
using PRFactory.Web.Models;
using PRFactory.Domain.ValueObjects;

namespace PRFactory.Web.Components.Tickets;

public partial class TicketHeader
{
    [Parameter, EditorRequired]
    public TicketDto Ticket { get; set; } = null!;

    private static string FormatDescription(string description)
    {
        // Simple markdown-like formatting
        // Convert newlines to <br> and preserve paragraphs
        var formatted = System.Net.WebUtility.HtmlEncode(description);
        formatted = formatted.Replace("\n\n", "</p><p>");
        formatted = formatted.Replace("\n", "<br>");
        return $"<p>{formatted}</p>";
    }

    private string GetWorkflowStateHelpText()
    {
        return Ticket.State switch
        {
            WorkflowState.AwaitingAnswers =>
                "The AI has posted clarifying questions to help better understand your requirements. Please review and answer them to continue the workflow.",

            WorkflowState.TicketUpdateUnderReview =>
                "The AI has generated a refined ticket description based on the analysis. Review the changes and approve or reject them.",

            WorkflowState.PlanUnderReview =>
                "The AI has created an implementation plan. Review the plan details and decide whether to approve, refine, or reject it.",

            WorkflowState.PRCreated =>
                "The AI has implemented the code and created a pull request. Review the changes in your repository and merge when ready.",

            WorkflowState.InReview =>
                "The pull request is currently under review. Check your repository for the latest status.",

            WorkflowState.Completed =>
                "The ticket has been completed and the pull request has been merged successfully.",

            WorkflowState.Failed =>
                "The workflow encountered an error and could not complete. Check the error details below.",

            WorkflowState.Cancelled =>
                "This ticket has been cancelled and will not be processed.",

            WorkflowState.Triggered =>
                "The workflow has been triggered and is starting the initial analysis.",

            WorkflowState.Analyzing =>
                "The AI is analyzing your codebase to understand the context and requirements.",

            WorkflowState.Planning =>
                "The AI is creating an implementation plan based on your requirements.",

            WorkflowState.Implementing =>
                "The AI is implementing the code based on the approved plan.",

            _ => "The workflow is processing. Current state: " + Ticket.StateDisplay
        };
    }
}
