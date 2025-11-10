using System.Diagnostics.CodeAnalysis;

namespace PRFactory.Infrastructure.Persistence.DemoData;

/// <summary>
/// Demo repository definitions for offline development
/// </summary>
public static class DemoRepositoryData
{
    [SuppressMessage("csharpsquid", "S1075", Justification = "Demo/test data for offline development and testing")]
    public static readonly IReadOnlyList<(string Name, string Platform, string CloneUrl, string Token, string DefaultBranch)> Repositories = new List<(string Name, string Platform, string CloneUrl, string Token, string DefaultBranch)>
    {
        (
            Name: "prfactory-demo",
            Platform: "GitHub",
            CloneUrl: "https://github.com/demo/prfactory-demo.git",
            Token: "github-demo-token-abc123",
            DefaultBranch: "main"
        ),
        (
            Name: "enterprise-app",
            Platform: "Bitbucket",
            CloneUrl: "https://bitbucket.org/demo/enterprise-app.git",
            Token: "bitbucket-demo-token-xyz456",
            DefaultBranch: "master"
        ),
        (
            Name: "azure-project",
            Platform: "AzureDevOps",
            CloneUrl: "https://dev.azure.com/demo/azure-project/_git/main",
            Token: "azure-demo-token-789def",
            DefaultBranch: "main"
        )
    };
}
