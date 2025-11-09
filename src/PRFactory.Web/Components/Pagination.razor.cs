using Microsoft.AspNetCore.Components;

namespace PRFactory.Web.Components;

public partial class Pagination
{
    [Parameter]
    public int CurrentPage { get; set; } = 1;

    [Parameter]
    public int TotalPages { get; set; } = 1;

    [Parameter]
    public int? TotalItems { get; set; }

    [Parameter]
    public EventCallback<int> OnPageChanged { get; set; }

    private async Task ChangePage(int page)
    {
        if (page < 1 || page > TotalPages || page == CurrentPage)
            return;

        await OnPageChanged.InvokeAsync(page);
    }
}
