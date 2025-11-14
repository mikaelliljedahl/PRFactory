using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace PRFactory.Web.UI.Editors;

public partial class MarkdownEditor : IDisposable
{
    private string currentValue = string.Empty;
    private string previewContent = string.Empty;
    private ViewMode currentViewMode;
    private int lineCount = 1;
    private Timer? debounceTimer;
    private ElementReference textareaRef;
    private ElementReference lineNumbersRef;
    private MarkdownPreview? previewRef;
    private int cursorPosition = 0;

    [Parameter]
    public string? Value { get; set; }

    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    [Parameter]
    public string Height { get; set; } = "500px";

    [Parameter]
    public bool ShowToolbar { get; set; } = true;

    [Parameter]
    public bool ShowPreview { get; set; } = true;

    [Parameter]
    public ViewMode InitialViewMode { get; set; } = ViewMode.Split;

    protected override void OnInitialized()
    {
        currentValue = Value ?? string.Empty;
        currentViewMode = InitialViewMode;
        UpdateLineCount();
        UpdatePreview();
    }

    protected override void OnParametersSet()
    {
        if (Value != currentValue)
        {
            currentValue = Value ?? string.Empty;
            UpdateLineCount();
            UpdatePreview();
        }
    }

    private void OnInput(ChangeEventArgs e)
    {
        currentValue = e.Value?.ToString() ?? string.Empty;
        UpdateLineCount();
        DebouncePreviewUpdate();

        if (ValueChanged.HasDelegate)
        {
            ValueChanged.InvokeAsync(currentValue);
        }
    }

    private void UpdateLineCount()
    {
        lineCount = Math.Max(1, currentValue.Split('\n').Length);
    }

    private void DebouncePreviewUpdate()
    {
        debounceTimer?.Dispose();
        debounceTimer = new Timer(_ =>
        {
            InvokeAsync(() =>
            {
                UpdatePreview();
                StateHasChanged();
            });
        }, null, 300, Timeout.Infinite);
    }

    private void UpdatePreview()
    {
        previewContent = currentValue;
    }

    private void HandleToolbarCommand(ToolbarCommand command)
    {
        var (newValue, newCursorPosition) = ApplyMarkdownCommand(currentValue, cursorPosition, command);
        currentValue = newValue;
        cursorPosition = newCursorPosition;

        UpdateLineCount();
        UpdatePreview();

        if (ValueChanged.HasDelegate)
        {
            ValueChanged.InvokeAsync(currentValue);
        }

        StateHasChanged();
    }

    private (string newValue, int newCursorPosition) ApplyMarkdownCommand(
        string text,
        int cursor,
        ToolbarCommand command)
    {
        return command switch
        {
            ToolbarCommand.Bold => WrapSelection(text, cursor, "**", "**"),
            ToolbarCommand.Italic => WrapSelection(text, cursor, "*", "*"),
            ToolbarCommand.Strikethrough => WrapSelection(text, cursor, "~~", "~~"),
            ToolbarCommand.Heading1 => InsertAtLineStart(text, cursor, "# "),
            ToolbarCommand.Heading2 => InsertAtLineStart(text, cursor, "## "),
            ToolbarCommand.Heading3 => InsertAtLineStart(text, cursor, "### "),
            ToolbarCommand.Heading4 => InsertAtLineStart(text, cursor, "#### "),
            ToolbarCommand.Heading5 => InsertAtLineStart(text, cursor, "##### "),
            ToolbarCommand.Heading6 => InsertAtLineStart(text, cursor, "###### "),
            ToolbarCommand.UnorderedList => InsertAtLineStart(text, cursor, "- "),
            ToolbarCommand.OrderedList => InsertAtLineStart(text, cursor, "1. "),
            ToolbarCommand.Checklist => InsertAtLineStart(text, cursor, "- [ ] "),
            ToolbarCommand.CodeBlock => WrapSelection(text, cursor, "```\n", "\n```"),
            ToolbarCommand.InlineCode => WrapSelection(text, cursor, "`", "`"),
            ToolbarCommand.Blockquote => InsertAtLineStart(text, cursor, "> "),
            ToolbarCommand.HorizontalRule => InsertText(text, cursor, "\n---\n"),
            ToolbarCommand.Link => WrapSelection(text, cursor, "[", "](https://)"),
            ToolbarCommand.Image => WrapSelection(text, cursor, "![", "](https://)"),
            ToolbarCommand.Table => InsertTable(text, cursor),
            _ => (text, cursor)
        };
    }

    private (string, int) WrapSelection(string text, int cursor, string before, string after)
    {
        var newText = text.Insert(cursor, before + after);
        return (newText, cursor + before.Length);
    }

    private (string, int) InsertAtLineStart(string text, int cursor, string prefix)
    {
        var lineStart = text.LastIndexOf('\n', Math.Max(0, cursor - 1)) + 1;
        var newText = text.Insert(lineStart, prefix);
        return (newText, cursor + prefix.Length);
    }

    private (string, int) InsertText(string text, int cursor, string insert)
    {
        var newText = text.Insert(cursor, insert);
        return (newText, cursor + insert.Length);
    }

    private (string, int) InsertTable(string text, int cursor)
    {
        var table = "\n| Header 1 | Header 2 | Header 3 |\n|----------|----------|----------|\n| Cell 1   | Cell 2   | Cell 3   |\n";
        return InsertText(text, cursor, table);
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.CtrlKey || e.MetaKey)
        {
            var command = e.Key.ToLower() switch
            {
                "b" => ToolbarCommand.Bold,
                "i" => ToolbarCommand.Italic,
                "k" => ToolbarCommand.Link,
                _ => (ToolbarCommand?)null
            };

            if (command.HasValue)
            {
                HandleToolbarCommand(command.Value);
                await Task.CompletedTask;
            }
        }
    }

    private void HandleViewModeChange(ViewMode newMode)
    {
        currentViewMode = newMode;
        StateHasChanged();
    }

    private string GetEditorClass()
    {
        return currentViewMode switch
        {
            ViewMode.Split => "view-split",
            ViewMode.EditorOnly => "view-editor-only",
            ViewMode.PreviewOnly => "view-preview-only",
            ViewMode.Fullscreen => "view-fullscreen",
            _ => "view-split"
        };
    }

    public void Dispose()
    {
        debounceTimer?.Dispose();
    }
}

public enum ViewMode
{
    Split,
    EditorOnly,
    PreviewOnly,
    Fullscreen
}

public enum ToolbarCommand
{
    Bold,
    Italic,
    Strikethrough,
    Heading1,
    Heading2,
    Heading3,
    Heading4,
    Heading5,
    Heading6,
    UnorderedList,
    OrderedList,
    Checklist,
    Link,
    Image,
    Table,
    CodeBlock,
    InlineCode,
    Blockquote,
    HorizontalRule,
    Undo,
    Redo,
    Fullscreen
}
