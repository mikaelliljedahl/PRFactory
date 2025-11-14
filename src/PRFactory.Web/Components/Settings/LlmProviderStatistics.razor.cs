using Microsoft.AspNetCore.Components;

namespace PRFactory.Web.Components.Settings;

public partial class LlmProviderStatistics
{
    [Parameter]
    public int TotalRequests { get; set; }

    [Parameter]
    public int SuccessfulRequests { get; set; }

    [Parameter]
    public int FailedRequests { get; set; }

    [Parameter]
    public int AverageResponseTimeMs { get; set; }
}
