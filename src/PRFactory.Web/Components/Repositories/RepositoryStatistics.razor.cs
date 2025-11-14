using Microsoft.AspNetCore.Components;
using PRFactory.Core.Application.DTOs;

namespace PRFactory.Web.Components.Repositories;

/// <summary>
/// Component for displaying repository statistics
/// </summary>
public partial class RepositoryStatistics
{
    /// <summary>
    /// Repository statistics data
    /// </summary>
    [Parameter, EditorRequired]
    public RepositoryStatisticsDto Statistics { get; set; } = null!;
}
