using Microsoft.AspNetCore.Components;
using PRFactory.Web.Models;

namespace PRFactory.Web.Components.Repositories;

public partial class RepositoryListItem
{
    [Parameter, EditorRequired]
    public RepositoryDto Repository { get; set; } = null!;

    [Parameter]
    public EventCallback<Guid> OnEdit { get; set; }

    [Parameter]
    public EventCallback<Guid> OnDelete { get; set; }

    [Parameter]
    public EventCallback<Guid> OnTestConnection { get; set; }

    [Parameter]
    public EventCallback<Guid> OnActivate { get; set; }

    [Parameter]
    public EventCallback<Guid> OnDeactivate { get; set; }

    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    private string GetPlatformBadgeClass() => Repository.GitPlatform switch
    {
        "GitHub" => "badge bg-dark",
        "Bitbucket" => "badge bg-primary",
        "AzureDevOps" => "badge bg-info",
        "GitLab" => "badge bg-warning text-dark",
        _ => "badge bg-secondary"
    };

    private string GetPlatformIcon() => Repository.GitPlatform switch
    {
        "GitHub" => "bi-github",
        "Bitbucket" => "bi-bucket",
        "AzureDevOps" => "bi-microsoft",
        "GitLab" => "bi-git",
        _ => "bi-git-branch"
    };

    private string TruncateUrl(string url, int maxLength)
    {
        return url.Length > maxLength ? url[..maxLength] + "..." : url;
    }

    private void HandleViewDetails()
    {
        NavigationManager.NavigateTo($"/repositories/{Repository.Id}");
    }
}
