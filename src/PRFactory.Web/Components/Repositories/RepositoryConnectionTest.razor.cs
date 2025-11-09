using Microsoft.AspNetCore.Components;
using PRFactory.Web.Models;
using PRFactory.Web.Services;

namespace PRFactory.Web.Components.Repositories;

public partial class RepositoryConnectionTest
{
    private bool showErrorDetails;

    [Parameter, EditorRequired]
    public string CloneUrl { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public string AccessToken { get; set; } = string.Empty;

    [Parameter]
    public bool ShowErrorDetails { get; set; } = true;

    [Parameter]
    public EventCallback<RepositoryConnectionTestResult> OnTestCompleted { get; set; }

    [Inject]
    private IRepositoryService RepositoryService { get; set; } = null!;

    [Inject]
    private ILogger<RepositoryConnectionTest> Logger { get; set; } = null!;

    private bool IsTesting { get; set; }
    private RepositoryConnectionTestResult? TestResult { get; set; }

    private bool CanTest => !string.IsNullOrWhiteSpace(CloneUrl) && !string.IsNullOrWhiteSpace(AccessToken);

    private async Task HandleTestConnection()
    {
        if (!CanTest)
        {
            return;
        }

        try
        {
            IsTesting = true;
            TestResult = null;
            StateHasChanged();

            Logger.LogInformation("Testing connection to repository: {CloneUrl}", CloneUrl);

            TestResult = await RepositoryService.TestConnectionAsync(CloneUrl, AccessToken);

            Logger.LogInformation("Connection test completed. Success: {Success}", TestResult.Success);

            if (OnTestCompleted.HasDelegate)
            {
                await OnTestCompleted.InvokeAsync(TestResult);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error testing repository connection");
            TestResult = new RepositoryConnectionTestResult
            {
                Success = false,
                Message = "An unexpected error occurred while testing the connection.",
                ErrorDetails = ex.ToString(),
                TestedAt = DateTime.UtcNow
            };
        }
        finally
        {
            IsTesting = false;
            StateHasChanged();
        }
    }

    private void ToggleErrorDetails()
    {
        showErrorDetails = !showErrorDetails;
    }
}
