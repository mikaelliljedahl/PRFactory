using Microsoft.AspNetCore.Components;
using PRFactory.Web.Models;

namespace PRFactory.Web.Components.Errors;

public partial class ErrorDetail
{
    [Parameter, EditorRequired]
    public ErrorDto? Error { get; set; }

    [Parameter]
    public List<ErrorDto>? RelatedErrors { get; set; }
}
