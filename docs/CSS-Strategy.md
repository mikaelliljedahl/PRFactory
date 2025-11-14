# CSS Strategy

PRFactory uses a **CSS Isolation** pattern to ensure component styles are scoped and maintainable. This document explains when to use CSS isolation, Bootstrap utilities, or global CSS.

---

## CSS Isolation Pattern (Preferred)

Use CSS isolation (`.razor.css` files) for component-specific styles.

### When to use

- Component has 5+ CSS rules
- Component needs custom animations/transitions
- Component has multiple visual variants
- Styles are specific to component (not general utilities)

### How it works

- Create `ComponentName.razor.css` next to `ComponentName.razor`
- Blazor automatically scopes styles with unique attribute (e.g., `b-abc123`)
- No naming conflicts possible
- Styles only apply to that component

### Example Structure

```
Card.razor
Card.razor.cs
Card.razor.css       # Scoped styles
```

### Benefits

- **Scoped**: Styles automatically isolated to component
- **No Conflicts**: Impossible to have naming collisions
- **Maintainable**: Component styles live with component code
- **Type Safe**: Compile-time verification of component structure
- **Automatic**: Blazor handles scoping without manual CSS classes

### Example CSS Isolation File

```css
/* TicketHeader.razor.css */
.description-content {
    white-space: pre-wrap;
    word-wrap: break-word;
}

.ticket-metadata dl {
    font-size: 0.9rem;
}

.ticket-metadata dt {
    font-weight: 600;
    color: #6c757d;
}
```

When Blazor compiles this, it becomes:

```css
.description-content[b-abc123xyz] {
    white-space: pre-wrap;
    word-wrap: break-word;
}

.ticket-metadata[b-abc123xyz] dl {
    font-size: 0.9rem;
}
```

---

## Bootstrap Utilities (For Simple Styling)

Use Bootstrap utility classes for simple layout/spacing.

### When to use

- Simple spacing: `mb-3`, `p-2`, `mt-4`
- Simple layout: `d-flex`, `justify-content-between`
- Simple colors: `text-muted`, `bg-light`
- Simple sizing: `w-100`, `h-auto`

### Example

```razor
<div class="d-flex justify-content-between mb-3">
    <h1>Title</h1>
    <button class="btn btn-primary">Action</button>
</div>
```

### Benefits

- **Fast**: No need to write CSS
- **Consistent**: Uses Bootstrap design system
- **Responsive**: Bootstrap utilities handle breakpoints
- **Standard**: Familiar to all developers

---

## Global CSS (Minimal Use)

Use global CSS **only** for:

- Theme variables (colors, fonts, spacing scales)
- Typography base styles
- Reset/normalize styles
- Third-party library overrides (Radzen)

### Files

- `wwwroot/css/site.css` - Site-wide theme variables
- `wwwroot/css/app.css` - App-level styles

### Example (site.css)

```css
:root {
    --primary-color: #0d6efd;
    --font-family-base: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
}

body {
    font-family: var(--font-family-base);
}
```

---

## Decision Tree

```
Does component need custom styling?
├─ No → Use Bootstrap utilities
└─ Yes → Is it 5+ CSS rules?
    ├─ No → Use Bootstrap utilities or inline style (rare)
    └─ Yes → Use CSS isolation (.razor.css)
```

---

## Migration Checklist

When creating or updating components:

- [ ] Component has no `<style>` tags
- [ ] Complex components have `.razor.css` files
- [ ] Simple utilities use Bootstrap classes
- [ ] Global CSS minimized to theme/reset only
- [ ] Visual regression tests pass

---

## Examples by Component Type

### Pure UI Components (`/UI/*`)

Use CSS isolation for reusable UI components:

- `/UI/Cards/Card.razor.css`
- `/UI/Buttons/LoadingButton.razor.css`
- `/UI/Forms/FormTextField.razor.css`

**Guideline**: If it's in `/UI/*`, it should have CSS isolation if it needs any custom styles.

### Business Components (`/Components/*`)

Use CSS isolation for complex business logic components:

- `/Components/Tickets/TicketHeader.razor.css`
- `/Components/Tickets/TicketDiffViewer.razor.css`
- `/Components/AgentPrompts/PromptVariableReference.razor.css`

**Guideline**: Business components with custom styling need CSS isolation.

### Pages (`/Pages/*`)

Pages typically use composition of components + Bootstrap utilities. Only add CSS isolation if page-specific styles are needed:

- `/Pages/Settings/LlmProviders/Create.razor.css` (wizard steps)

**Guideline**: Most pages shouldn't need CSS isolation - compose from UI components instead.

---

## Anti-Patterns to Avoid

### ❌ DON'T: Inline `<style>` tags

```razor
<!-- BAD -->
<div class="my-component">...</div>

<style>
    .my-component {
        background: red;
    }
</style>
```

**Problem**: Styles are global, not scoped. Creates naming conflicts.

### ❌ DON'T: Duplicate Bootstrap utilities in CSS

```css
/* BAD - Don't recreate Bootstrap utilities */
.my-spacing {
    margin-bottom: 1rem;
}

.my-flex {
    display: flex;
    justify-content: space-between;
}
```

**Solution**: Use Bootstrap classes directly: `mb-3 d-flex justify-content-between`

### ❌ DON'T: Put component-specific styles in global CSS

```css
/* BAD - In site.css */
.ticket-header-title {
    font-size: 1.75rem;
}
```

**Solution**: Move to `TicketHeader.razor.css` with CSS isolation.

---

## Current Status

As of **EPIC 08 Phase 2**:

- ✅ **19 components migrated** to CSS isolation
- ✅ **Zero inline `<style>` tags** in components
- ✅ **CSS isolation files** for all complex components
- ✅ **Bootstrap utilities** used for simple styling
- ✅ **Global CSS** minimized to theme/reset

---

## Migrated Components

### Components/Tickets/ (8 files)

1. `TicketHeader.razor.css`
2. `TicketDiffViewer.razor.css`
3. `WorkflowTimeline.razor.css`
4. `QuestionAnswerForm.razor.css`
5. `TicketUpdatePreview.razor.css`
6. `SuccessCriteriaEditor.razor.css`
7. `PlanReviewSection.razor.css`
8. `TicketUpdateEditor.razor.css`

### Components/ (3 files)

9. `TicketListItem.razor.css`
10. `AgentPrompts/PromptVariableReference.razor.css`
11. `AgentPrompts/PromptPreview.razor.css`

### Components/Settings/ (1 file)

12. `Settings/ProviderTypeSelector.razor.css`

### Pages/Settings/ (1 file)

13. `LlmProviders/Create.razor.css`

### UI/Forms/ (1 file)

14. `Forms/FormCodeEditor.razor.css`

### UI/Display/ (3 files)

15. `Display/StackTraceViewer.razor.css`
16. `Display/EventTimeline.razor.css`
17. `Display/ReviewerAvatar.razor.css`

### UI/Comments/ (2 files)

18. `Comments/CommentAnchorIndicator.razor.css`
19. `Comments/InlineCommentPanel.razor.css`

---

## Future Guidelines

### Adding New Components

When creating new components:

1. **Start with Bootstrap utilities** for layout/spacing
2. **Add CSS isolation** when custom styling is needed (5+ rules)
3. **Never use inline `<style>` tags** in components
4. **Keep global CSS minimal** - only theme variables

### Refactoring Existing Components

When updating old components:

1. **Audit for inline styles** - migrate to CSS isolation
2. **Check for duplicated Bootstrap utilities** - remove and use classes
3. **Move component-specific global CSS** to CSS isolation
4. **Test visual regression** - ensure no styling changes

---

## References

- [Blazor CSS Isolation (Microsoft Docs)](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/css-isolation)
- [Bootstrap 5 Utilities](https://getbootstrap.com/docs/5.3/utilities/api/)
- [PRFactory CLAUDE.md - UI Component Architecture](/home/user/PRFactory/CLAUDE.md#4-blazor-ui-component-architecture)

---

## Questions?

For questions or clarifications, see:

- [CLAUDE.md](/home/user/PRFactory/CLAUDE.md) - Overall architecture vision
- [ARCHITECTURE.md](/home/user/PRFactory/docs/ARCHITECTURE.md) - Technical architecture details
- [IMPLEMENTATION_STATUS.md](/home/user/PRFactory/docs/IMPLEMENTATION_STATUS.md) - Current implementation status
