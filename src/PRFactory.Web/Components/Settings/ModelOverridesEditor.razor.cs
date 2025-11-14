using System.Text.Json;
using Microsoft.AspNetCore.Components;

namespace PRFactory.Web.Components.Settings;

public partial class ModelOverridesEditor
{
    [Parameter]
    public Dictionary<string, string>? Value { get; set; }

    [Parameter]
    public EventCallback<Dictionary<string, string>?> ValueChanged { get; set; }

    [Parameter]
    public string? HelpText { get; set; }

    private string jsonText = string.Empty;
    private string? validationError = null;

    protected override void OnInitialized()
    {
        if (Value != null && Value.Any())
        {
            jsonText = JsonSerializer.Serialize(Value, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        else
        {
            jsonText = "{\n  \"claude-3-5-sonnet\": \"claude-sonnet-3-5-20250101\"\n}";
        }
    }

    private async Task OnJsonTextChanged(ChangeEventArgs e)
    {
        jsonText = e.Value?.ToString() ?? string.Empty;

        try
        {
            if (string.IsNullOrWhiteSpace(jsonText))
            {
                await ValueChanged.InvokeAsync(null);
                validationError = null;
                return;
            }

            var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonText);

            await ValueChanged.InvokeAsync(parsed);

            validationError = null;
        }
        catch (JsonException ex)
        {
            validationError = $"Invalid JSON: {ex.Message}";
        }
    }
}
