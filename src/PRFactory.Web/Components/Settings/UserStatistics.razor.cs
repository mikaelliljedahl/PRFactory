using Microsoft.AspNetCore.Components;
using PRFactory.Core.Application.Services;

namespace PRFactory.Web.Components.Settings;

public partial class UserStatistics
{
    [Parameter, EditorRequired]
    public UserStatisticsDto Statistics { get; set; } = null!;
}
