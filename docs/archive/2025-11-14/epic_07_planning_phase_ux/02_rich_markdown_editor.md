# Feature 2: Rich Markdown Editor Component

**Goal**: Provide professional split-view markdown editing with live preview, formatting toolbar, and keyboard shortcuts.

**Estimated Effort**: 3 weeks
**Priority**: P1 (High Impact)
**Dependencies**: Existing Markdig library, Radzen components, Bootstrap 5

---

## Executive Summary

Currently, users edit plans in a plain 8-row textarea with no formatting assistance, no live preview, and no shortcuts. This creates friction and limits productivity.

**This feature** delivers a professional markdown editor comparable to Notion/Confluence with:
- Split-view layout (Editor + Live Preview)
- Formatting toolbar (15+ buttons)
- Keyboard shortcuts (Ctrl+B, Ctrl+I, etc.)
- Responsive design (desktop/tablet/mobile)
- Pure Blazor Server (no JavaScript per CLAUDE.md)

**Expected Impact**: User satisfaction score > 4.0/5.0, 50% increase in characters edited per session.

---

## Current State Analysis

### Existing Editor

**File**: `/src/PRFactory.Web/Components/Tickets/TicketUpdateEditor.razor`

```razor
<div class="mb-3">
    <label class="form-label">Description</label>
    <InputTextArea @bind-Value="Model.UpdatedDescription"
                   class="form-control"
                   rows="8"
                   placeholder="Update ticket description..." />
</div>
```

**Limitations**:
- Plain textarea (no toolbar, no preview)
- No markdown syntax assistance
- No live preview (user must save and reload to see rendered output)
- No keyboard shortcuts
- No fullscreen mode
- Fixed 8 rows (doesn't expand for large plans)

---

## Component Architecture

### Component Hierarchy

```
MarkdownEditor.razor (root component)
├── MarkdownToolbar.razor (formatting buttons)
│   ├── TextFormattingGroup (Bold, Italic, Strike)
│   ├── StructureGroup (H1-H6, Blockquote, HR)
│   ├── ListGroup (UL, OL, Checklist)
│   ├── InsertGroup (Link, Image, Table, Code)
│   └── ActionGroup (Undo, Redo, Fullscreen)
├── EditorPane (textarea with line numbers)
└── MarkdownPreview.razor (Markdig rendering)
```

### Component Interfaces

```csharp
// MarkdownEditor.razor.cs
public partial class MarkdownEditor
{
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
}

public enum ViewMode
{
    Split,        // Editor left, preview right
    EditorOnly,   // Editor full width
    PreviewOnly,  // Preview full width (read-only)
    Fullscreen    // Fullscreen split view
}
```

---

## Implementation Plan

### Week 1: Core Editor & Toolbar

#### Day 1-2: MarkdownEditor Component

**File**: `/src/PRFactory.Web/UI/Editors/MarkdownEditor.razor`

```razor
@namespace PRFactory.Web.UI.Editors
@using Markdig

<div class="markdown-editor @GetEditorClass()">
    @if (ShowToolbar)
    {
        <MarkdownToolbar OnCommand="HandleToolbarCommand"
                         OnViewModeChanged="HandleViewModeChange" />
    }

    <div class="editor-container">
        @if (currentViewMode != ViewMode.PreviewOnly)
        {
            <div class="editor-pane">
                <div class="line-numbers" @ref="lineNumbersRef">
                    @for (int i = 1; i <= lineCount; i++)
                    {
                        <div>@i</div>
                    }
                </div>
                <textarea @ref="textareaRef"
                          @bind="currentValue"
                          @bind:event="oninput"
                          @bind:after="OnValueChanged"
                          @onkeydown="HandleKeyDown"
                          @onscroll="OnEditorScroll"
                          class="editor-textarea"
                          placeholder="Write markdown here..."
                          spellcheck="false"></textarea>
            </div>
        }

        @if (currentViewMode != ViewMode.EditorOnly)
        {
            <MarkdownPreview Content="@previewContent"
                             @ref="previewRef"
                             OnScroll="OnPreviewScroll" />
        }
    </div>
</div>
```

**Code-behind**: `MarkdownEditor.razor.cs`

```csharp
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

    private void OnValueChanged()
    {
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
        var (newValue, selectionStart) = ApplyMarkdownCommand(
            currentValue,
            command);

        currentValue = newValue;
        OnValueChanged();

        // Focus textarea and set selection (would need JSInterop in real implementation)
        StateHasChanged();
    }

    private (string newValue, int selectionStart) ApplyMarkdownCommand(
        string text,
        ToolbarCommand command)
    {
        // Implementation for each command
        return command switch
        {
            ToolbarCommand.Bold => WrapSelection(text, "**", "**"),
            ToolbarCommand.Italic => WrapSelection(text, "*", "*"),
            ToolbarCommand.Strikethrough => WrapSelection(text, "~~", "~~"),
            ToolbarCommand.Heading1 => InsertAtLineStart(text, "# "),
            ToolbarCommand.Heading2 => InsertAtLineStart(text, "## "),
            ToolbarCommand.Heading3 => InsertAtLineStart(text, "### "),
            ToolbarCommand.UnorderedList => InsertAtLineStart(text, "- "),
            ToolbarCommand.OrderedList => InsertAtLineStart(text, "1. "),
            ToolbarCommand.CodeBlock => WrapSelection(text, "```\n", "\n```"),
            ToolbarCommand.Blockquote => InsertAtLineStart(text, "> "),
            _ => (text, 0)
        };
    }

    private (string, int) WrapSelection(string text, string before, string after)
    {
        // Simplified - in real implementation would respect textarea selection
        var newText = $"{before}{text}{after}";
        return (newText, before.Length);
    }

    private (string, int) InsertAtLineStart(string text, string prefix)
    {
        var lines = text.Split('\n');
        if (lines.Length == 0) return (prefix, prefix.Length);

        lines[0] = prefix + lines[0];
        return (string.Join('\n', lines), prefix.Length);
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        // Handle keyboard shortcuts
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

    private void OnEditorScroll()
    {
        // Sync preview scroll (would need JSInterop for perfect sync)
    }

    private void OnPreviewScroll()
    {
        // Sync editor scroll (would need JSInterop for perfect sync)
    }

    public void Dispose()
    {
        debounceTimer?.Dispose();
    }
}

public enum ToolbarCommand
{
    Bold, Italic, Strikethrough,
    Heading1, Heading2, Heading3, Heading4, Heading5, Heading6,
    UnorderedList, OrderedList, Checklist,
    Link, Image, Table, CodeBlock, InlineCode,
    Blockquote, HorizontalRule,
    Undo, Redo, Fullscreen
}
```

---

#### Day 3-4: MarkdownToolbar Component

**File**: `/src/PRFactory.Web/UI/Editors/MarkdownToolbar.razor`

```razor
@namespace PRFactory.Web.UI.Editors

<div class="markdown-toolbar">
    <div class="toolbar-group">
        <button type="button" class="btn btn-sm btn-outline-secondary"
                @onclick="() => OnCommand.InvokeAsync(ToolbarCommand.Bold)"
                title="Bold (Ctrl+B)">
            <i class="bi bi-type-bold"></i>
        </button>
        <button type="button" class="btn btn-sm btn-outline-secondary"
                @onclick="() => OnCommand.InvokeAsync(ToolbarCommand.Italic)"
                title="Italic (Ctrl+I)">
            <i class="bi bi-type-italic"></i>
        </button>
        <button type="button" class="btn btn-sm btn-outline-secondary"
                @onclick="() => OnCommand.InvokeAsync(ToolbarCommand.Strikethrough)"
                title="Strikethrough">
            <i class="bi bi-type-strikethrough"></i>
        </button>
    </div>

    <div class="toolbar-separator"></div>

    <div class="toolbar-group">
        <RadzenDropDown Data="@headingOptions"
                        TValue="ToolbarCommand"
                        ValueProperty="Value"
                        TextProperty="Label"
                        @bind-Value="selectedHeading"
                        Change="@((ToolbarCommand value) => OnCommand.InvokeAsync(value))"
                        Style="width: 100px"
                        Placeholder="Heading" />
    </div>

    <div class="toolbar-separator"></div>

    <div class="toolbar-group">
        <button type="button" class="btn btn-sm btn-outline-secondary"
                @onclick="() => OnCommand.InvokeAsync(ToolbarCommand.UnorderedList)"
                title="Bullet List">
            <i class="bi bi-list-ul"></i>
        </button>
        <button type="button" class="btn btn-sm btn-outline-secondary"
                @onclick="() => OnCommand.InvokeAsync(ToolbarCommand.OrderedList)"
                title="Numbered List">
            <i class="bi bi-list-ol"></i>
        </button>
        <button type="button" class="btn btn-sm btn-outline-secondary"
                @onclick="() => OnCommand.InvokeAsync(ToolbarCommand.Checklist)"
                title="Checklist">
            <i class="bi bi-check2-square"></i>
        </button>
    </div>

    <div class="toolbar-separator"></div>

    <div class="toolbar-group">
        <button type="button" class="btn btn-sm btn-outline-secondary"
                @onclick="() => OnCommand.InvokeAsync(ToolbarCommand.Link)"
                title="Insert Link (Ctrl+K)">
            <i class="bi bi-link-45deg"></i>
        </button>
        <button type="button" class="btn btn-sm btn-outline-secondary"
                @onclick="() => OnCommand.InvokeAsync(ToolbarCommand.Image)"
                title="Insert Image">
            <i class="bi bi-image"></i>
        </button>
        <button type="button" class="btn btn-sm btn-outline-secondary"
                @onclick="() => OnCommand.InvokeAsync(ToolbarCommand.CodeBlock)"
                title="Code Block">
            <i class="bi bi-code-square"></i>
        </button>
    </div>

    <div class="toolbar-separator"></div>

    <div class="toolbar-group ms-auto">
        <RadzenDropDown Data="@viewModeOptions"
                        TValue="ViewMode"
                        ValueProperty="Value"
                        TextProperty="Label"
                        @bind-Value="currentViewMode"
                        Change="@((ViewMode value) => OnViewModeChanged.InvokeAsync(value))"
                        Style="width: 120px" />
    </div>
</div>

@code {
    [Parameter]
    public EventCallback<ToolbarCommand> OnCommand { get; set; }

    [Parameter]
    public EventCallback<ViewMode> OnViewModeChanged { get; set; }

    private ToolbarCommand selectedHeading = ToolbarCommand.Heading2;
    private ViewMode currentViewMode = ViewMode.Split;

    private List<HeadingOption> headingOptions = new()
    {
        new() { Label = "H1", Value = ToolbarCommand.Heading1 },
        new() { Label = "H2", Value = ToolbarCommand.Heading2 },
        new() { Label = "H3", Value = ToolbarCommand.Heading3 },
        new() { Label = "H4", Value = ToolbarCommand.Heading4 },
        new() { Label = "H5", Value = ToolbarCommand.Heading5 },
        new() { Label = "H6", Value = ToolbarCommand.Heading6 }
    };

    private List<ViewModeOption> viewModeOptions = new()
    {
        new() { Label = "Split", Value = ViewMode.Split },
        new() { Label = "Editor Only", Value = ViewMode.EditorOnly },
        new() { Label = "Preview Only", Value = ViewMode.PreviewOnly },
        new() { Label = "Fullscreen", Value = ViewMode.Fullscreen }
    };

    private class HeadingOption
    {
        public string Label { get; set; } = string.Empty;
        public ToolbarCommand Value { get; set; }
    }

    private class ViewModeOption
    {
        public string Label { get; set; } = string.Empty;
        public ViewMode Value { get; set; }
    }
}
```

---

### Week 2: Preview & Styling

#### Day 5-6: MarkdownPreview Component

**File**: `/src/PRFactory.Web/UI/Editors/MarkdownPreview.razor`

```razor
@namespace PRFactory.Web.UI.Editors
@using Markdig

<div class="markdown-preview" @onscroll="HandleScroll">
    @if (!string.IsNullOrWhiteSpace(Content))
    {
        @((MarkupString)RenderMarkdown(Content))
    }
    else
    {
        <div class="empty-state text-muted text-center py-5">
            <i class="bi bi-eye fs-1"></i>
            <p class="mt-2">Preview will appear here</p>
        </div>
    }
</div>

@code {
    [Parameter]
    public string Content { get; set; } = string.Empty;

    [Parameter]
    public EventCallback OnScroll { get; set; }

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    private string RenderMarkdown(string markdown)
    {
        try
        {
            return Markdown.ToHtml(markdown, Pipeline);
        }
        catch
        {
            return "<p class='text-danger'>Error rendering markdown</p>";
        }
    }

    private async Task HandleScroll()
    {
        if (OnScroll.HasDelegate)
        {
            await OnScroll.InvokeAsync();
        }
    }
}
```

---

#### Day 7-8: CSS Styling

**File**: `/src/PRFactory.Web/wwwroot/css/markdown-editor.css`

```css
/* Markdown Editor Container */
.markdown-editor {
    border: 1px solid #dee2e6;
    border-radius: 0.375rem;
    background: white;
}

.markdown-editor.view-fullscreen {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    z-index: 9999;
    border-radius: 0;
}

/* Toolbar */
.markdown-toolbar {
    display: flex;
    align-items: center;
    padding: 0.5rem;
    border-bottom: 1px solid #dee2e6;
    background: #f8f9fa;
    gap: 0.5rem;
}

.toolbar-group {
    display: flex;
    gap: 0.25rem;
}

.toolbar-separator {
    width: 1px;
    height: 24px;
    background: #dee2e6;
    margin: 0 0.5rem;
}

/* Editor Container */
.editor-container {
    display: grid;
    height: 500px;
}

.view-split .editor-container {
    grid-template-columns: 1fr 1fr;
}

.view-editor-only .editor-container {
    grid-template-columns: 1fr;
}

.view-preview-only .editor-container {
    grid-template-columns: 1fr;
}

/* Editor Pane */
.editor-pane {
    display: flex;
    border-right: 1px solid #dee2e6;
    overflow: hidden;
}

.line-numbers {
    padding: 1rem 0.5rem;
    background: #f8f9fa;
    color: #6c757d;
    text-align: right;
    font-family: 'Courier New', monospace;
    font-size: 0.875rem;
    line-height: 1.5;
    user-select: none;
    min-width: 3rem;
    border-right: 1px solid #dee2e6;
}

.editor-textarea {
    flex: 1;
    padding: 1rem;
    border: none;
    outline: none;
    resize: none;
    font-family: 'Courier New', monospace;
    font-size: 0.875rem;
    line-height: 1.5;
    overflow-y: auto;
}

/* Preview Pane */
.markdown-preview {
    padding: 1rem;
    overflow-y: auto;
}

.markdown-preview h1,
.markdown-preview h2,
.markdown-preview h3,
.markdown-preview h4,
.markdown-preview h5,
.markdown-preview h6 {
    margin-top: 1.5rem;
    margin-bottom: 0.75rem;
}

.markdown-preview pre {
    background: #f8f9fa;
    padding: 1rem;
    border-radius: 0.375rem;
    overflow-x: auto;
}

.markdown-preview code {
    background: #f8f9fa;
    padding: 0.2rem 0.4rem;
    border-radius: 0.25rem;
    font-family: 'Courier New', monospace;
    font-size: 0.875rem;
}

.markdown-preview table {
    width: 100%;
    border-collapse: collapse;
    margin: 1rem 0;
}

.markdown-preview table th,
.markdown-preview table td {
    border: 1px solid #dee2e6;
    padding: 0.5rem;
}

.markdown-preview table th {
    background: #f8f9fa;
    font-weight: 600;
}

/* Responsive Design */
@media (max-width: 768px) {
    .view-split .editor-container {
        grid-template-columns: 1fr;
        grid-template-rows: 1fr 1fr;
    }

    .editor-pane {
        border-right: none;
        border-bottom: 1px solid #dee2e6;
    }
}

@media (max-width: 576px) {
    .markdown-toolbar {
        flex-wrap: wrap;
    }

    .toolbar-group {
        flex-wrap: wrap;
    }

    .line-numbers {
        min-width: 2rem;
    }
}
```

---

### Week 3: Integration & Testing

#### Day 9-10: Component Integration

**Update TicketUpdateEditor**:

```razor
@namespace PRFactory.Web.Components.Tickets

<EditForm Model="@Model" OnValidSubmit="@HandleSubmit">
    <DataAnnotationsValidator />

    <div class="mb-3">
        <label class="form-label">Description</label>
        <MarkdownEditor @bind-Value="@Model.UpdatedDescription"
                        Height="600px"
                        ShowToolbar="true"
                        ShowPreview="true" />
        <ValidationMessage For="@(() => Model.UpdatedDescription)" />
    </div>

    <div class="mb-3">
        <label class="form-label">Acceptance Criteria</label>
        <MarkdownEditor @bind-Value="@Model.AcceptanceCriteria"
                        Height="400px"
                        ShowToolbar="true"
                        ShowPreview="true" />
        <ValidationMessage For="@(() => Model.AcceptanceCriteria)" />
    </div>

    <LoadingButton IsLoading="@isSubmitting"
                   Type="submit"
                   Icon="save">
        Save Changes
    </LoadingButton>
</EditForm>
```

---

#### Day 11-13: Unit Tests

**File**: `/tests/PRFactory.Web.Tests/UI/Editors/MarkdownEditorTests.cs`

```csharp
public class MarkdownEditorTests : TestContext
{
    [Fact]
    public void Render_WithInitialValue_DisplaysValue()
    {
        // Arrange
        var initialValue = "# Hello World";

        // Act
        var cut = RenderComponent<MarkdownEditor>(parameters => parameters
            .Add(p => p.Value, initialValue));

        // Assert
        var textarea = cut.Find("textarea");
        Assert.Equal(initialValue, textarea.GetAttribute("value"));
    }

    [Fact]
    public void ToolbarCommand_Bold_WrapsTextWithAsterisks()
    {
        // Arrange
        var cut = RenderComponent<MarkdownEditor>(parameters => parameters
            .Add(p => p.Value, "text"));

        // Act
        var boldButton = cut.Find("button[title='Bold (Ctrl+B)']");
        boldButton.Click();

        // Assert
        var textarea = cut.Find("textarea");
        Assert.Contains("**", textarea.GetAttribute("value"));
    }

    [Theory]
    [InlineData(ViewMode.Split, "view-split")]
    [InlineData(ViewMode.EditorOnly, "view-editor-only")]
    [InlineData(ViewMode.PreviewOnly, "view-preview-only")]
    [InlineData(ViewMode.Fullscreen, "view-fullscreen")]
    public void ViewMode_Change_AppliesCorrectCssClass(
        ViewMode mode,
        string expectedClass)
    {
        // Arrange
        var cut = RenderComponent<MarkdownEditor>(parameters => parameters
            .Add(p => p.InitialViewMode, mode));

        // Act & Assert
        var editor = cut.Find(".markdown-editor");
        Assert.Contains(expectedClass, editor.ClassName);
    }

    [Fact]
    public async Task ValueChanged_TriggersCallback()
    {
        // Arrange
        var callbackInvoked = false;
        var newValue = string.Empty;

        var cut = RenderComponent<MarkdownEditor>(parameters => parameters
            .Add(p => p.Value, "initial")
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<string>(
                this,
                value =>
                {
                    callbackInvoked = true;
                    newValue = value;
                })));

        // Act
        var textarea = cut.Find("textarea");
        await textarea.InputAsync("updated");

        // Assert
        Assert.True(callbackInvoked);
        Assert.Equal("updated", newValue);
    }

    [Fact]
    public void Preview_RendersMarkdownCorrectly()
    {
        // Arrange
        var markdown = "# Heading\n\n**Bold text**";

        // Act
        var cut = RenderComponent<MarkdownPreview>(parameters => parameters
            .Add(p => p.Content, markdown));

        // Assert
        cut.MarkupMatches("<h1>Heading</h1><p><strong>Bold text</strong></p>");
    }
}
```

---

## Acceptance Criteria

- [ ] MarkdownEditor component created in `/UI/Editors/`
- [ ] Formatting toolbar with 15+ buttons (Bold, Italic, H1-H6, Lists, Links, etc.)
- [ ] Split-view with live preview (300ms debounce)
- [ ] View mode toggle (Split, Editor Only, Preview Only, Fullscreen)
- [ ] Line numbers in editor pane
- [ ] Keyboard shortcuts (Ctrl+B, Ctrl+I, Ctrl+K)
- [ ] Responsive design (desktop/tablet/mobile)
- [ ] CSS styling in `/wwwroot/css/markdown-editor.css`
- [ ] Integration with TicketUpdateEditor
- [ ] Integration with PlanReviewSection
- [ ] Unit tests (80%+ coverage)
- [ ] Manual test: Edit large plan (5000+ chars) → Performance acceptable
- [ ] Manual test: Toolbar buttons work → Markdown inserted correctly
- [ ] Manual test: Live preview updates → Debounced 300ms

---

## Files Created/Modified

### New Files (6 files)

- `/src/PRFactory.Web/UI/Editors/MarkdownEditor.razor`
- `/src/PRFactory.Web/UI/Editors/MarkdownEditor.razor.cs`
- `/src/PRFactory.Web/UI/Editors/MarkdownToolbar.razor`
- `/src/PRFactory.Web/UI/Editors/MarkdownPreview.razor`
- `/src/PRFactory.Web/wwwroot/css/markdown-editor.css`
- `/tests/PRFactory.Web.Tests/UI/Editors/MarkdownEditorTests.cs`

### Modified Files (3 files)

- `/src/PRFactory.Web/Components/Tickets/TicketUpdateEditor.razor`
- `/src/PRFactory.Web/Components/Tickets/PlanReviewSection.razor`
- `/src/PRFactory.Web/Components/Tickets/ReviewCommentThread.razor`

---

## Success Metrics

- User satisfaction score > 4.0/5.0 for editing experience
- Average edit session length increases by 50%
- Support tickets related to "how to format markdown" decrease by 80%

---

**End of Feature 2: Rich Markdown Editor**
