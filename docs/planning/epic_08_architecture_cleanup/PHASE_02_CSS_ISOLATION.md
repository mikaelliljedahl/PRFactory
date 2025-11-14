# Phase 2: CSS Isolation

**Duration**: 1 week
**Risk Level**: 🟡 Medium
**Agent Type**: `code-implementation-specialist` OR `simple-code-implementation` (for single files)
**Dependencies**: Phase 1 merged to main

---

## Objective

Migrate inline `<style>` tags to CSS isolation (`.razor.css` files) and establish CSS strategy for maintainability.

**Why this matters**: Inline styles create global CSS pollution and make components harder to maintain. CSS isolation scopes styles to components automatically, preventing conflicts.

---

## Success Criteria

- ✅ Zero `<style>` tags in `.razor` files
- ✅ All complex components have `.razor.css` files (15-20 components)
- ✅ Visual regression tests pass (no styling changes visible to users)
- ✅ CSS strategy documented in `docs/CSS-Strategy.md`
- ✅ All tests pass

---

## Current Issues

### Issue 1: Inline Styles in TicketHeader

**File**: `src/PRFactory.Web/Components/Tickets/TicketHeader.razor` (lines 88-106)

```razor
<style>
    .pr-ticket-header {
        background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        color: white;
        padding: 2rem;
        border-radius: 0.5rem;
        margin-bottom: 1.5rem;
    }
    /* ... more styles */
</style>
```

**Problem**: Styles are global, can conflict with other components.

### Issue 2: Scattered CSS Approach

Mix of:
- Global CSS files (`site.css`, `app.css`)
- Inline `<style>` tags
- No CSS isolation used

---

## Implementation Steps

### Step 1: Audit Components for Inline Styles (30 minutes)

```bash
# Find all components with <style> tags
cd C:/code/github/PRFactory/src/PRFactory.Web
grep -r "<style>" --include="*.razor" Components/ Pages/ UI/

# Save results to file
grep -r "<style>" --include="*.razor" Components/ Pages/ UI/ > inline-styles-audit.txt
```

**Expected files with inline styles**:
- `Components/Tickets/TicketHeader.razor` (confirmed)
- Possibly others in `Components/` or `Pages/`

Create a list of all components that need CSS isolation.

---

### Step 2: Migrate TicketHeader to CSS Isolation (1-2 hours)

#### 2.1 Read TicketHeader.razor

```bash
# Read the component to see inline styles
cat src/PRFactory.Web/Components/Tickets/TicketHeader.razor
```

#### 2.2 Extract Styles to .razor.css

Create `src/PRFactory.Web/Components/Tickets/TicketHeader.razor.css`:

```css
.pr-ticket-header {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
    padding: 2rem;
    border-radius: 0.5rem;
    margin-bottom: 1.5rem;
}

.pr-ticket-title {
    font-size: 1.75rem;
    font-weight: 600;
    margin-bottom: 0.5rem;
}

.pr-ticket-status {
    display: inline-block;
    padding: 0.25rem 0.75rem;
    border-radius: 0.25rem;
    font-size: 0.875rem;
    font-weight: 500;
    text-transform: uppercase;
}

/* Add all other styles from <style> tag */
```

#### 2.3 Remove <style> Tag from .razor File

Edit `TicketHeader.razor` and remove the entire `<style>...</style>` block.

#### 2.4 Verify Styling Still Works

```bash
# Build and run
source /tmp/dotnet-proxy-setup.sh
dotnet build
dotnet run

# Navigate to a ticket page
# Verify TicketHeader styling looks identical
```

**Visual regression test**: Take screenshots before/after to compare.

---

### Step 3: Migrate Other Components (2-3 hours)

For each component found in Step 1:

1. **Create `.razor.css` file** next to `.razor` file
2. **Copy styles** from `<style>` tag to `.razor.css`
3. **Remove `<style>` tag** from `.razor` file
4. **Test visually** - ensure no styling regressions

**Repeat for all components**:
- Use `simple-code-implementation` agent for single-file migrations
- Run in parallel if possible (each component is independent)

---

### Step 4: Add CSS Isolation to Existing UI Components (2-3 hours)

Even components without inline styles benefit from CSS isolation:

#### Components to Add CSS Isolation

**High Priority** (complex styling):
1. `UI/Cards/Card.razor.css`
2. `UI/Dialogs/Modal.razor.css`
3. `UI/Display/EventTimeline.razor.css`
4. `UI/Editors/MarkdownEditor.razor.css`
5. `UI/Alerts/AlertMessage.razor.css`

**Medium Priority**:
6. `UI/Buttons/LoadingButton.razor.css`
7. `UI/Forms/FormTextField.razor.css`
8. `UI/Navigation/Breadcrumbs.razor.css`
9. `UI/Display/LoadingSpinner.razor.css`
10. `UI/Help/ContextualHelp.razor.css`

#### Process for Each Component

1. **Identify component-specific classes**
   ```bash
   # Find classes used in component
   grep -o 'class="[^"]*"' UI/Cards/Card.razor
   ```

2. **Extract component-specific styles from global CSS**
   - Check `wwwroot/css/site.css`
   - Check `wwwroot/css/app.css`
   - Check component-specific CSS files (e.g., `wwwroot/css/markdown-editor.css`)

3. **Create `.razor.css` file**
   - Move component-specific styles to `.razor.css`
   - Keep utility classes (Bootstrap) in markup

4. **Test visually**

---

### Step 5: Document CSS Strategy (1 hour)

Create `C:/code/github/PRFactory/docs/CSS-Strategy.md`:

```markdown
# CSS Strategy

## CSS Isolation Pattern (Preferred)

Use CSS isolation (`.razor.css` files) for component-specific styles.

**When to use**:
- Component has 5+ CSS rules
- Component needs custom animations/transitions
- Component has multiple visual variants
- Styles are specific to component (not general utilities)

**How it works**:
- Create `ComponentName.razor.css` next to `ComponentName.razor`
- Blazor automatically scopes styles with unique attribute (e.g., `b-abc123`)
- No naming conflicts possible

**Example**:
\`\`\`
Card.razor
Card.razor.cs
Card.razor.css       # Scoped styles
\`\`\`

## Bootstrap Utilities (For Simple Styling)

Use Bootstrap utility classes for simple layout/spacing.

**When to use**:
- Simple spacing: `mb-3`, `p-2`, `mt-4`
- Simple layout: `d-flex`, `justify-content-between`
- Simple colors: `text-muted`, `bg-light`
- Simple sizing: `w-100`, `h-auto`

**Example**:
\`\`\`razor
<div class="d-flex justify-content-between mb-3">
    <h1>Title</h1>
    <button class="btn btn-primary">Action</button>
</div>
\`\`\`

## Global CSS (Minimal Use)

Use global CSS only for:
- Theme variables (colors, fonts, spacing scales)
- Typography base styles
- Reset/normalize styles
- Third-party library overrides (Radzen)

**Files**:
- `wwwroot/css/site.css` - Site-wide theme variables
- `wwwroot/css/app.css` - App-level styles

## Decision Tree

\`\`\`
Does component need custom styling?
├─ No → Use Bootstrap utilities
└─ Yes → Is it 5+ CSS rules?
    ├─ No → Use Bootstrap utilities or inline style (rare)
    └─ Yes → Use CSS isolation (.razor.css)
\`\`\`

## Migration Checklist

- [ ] Component has no `<style>` tags
- [ ] Complex components have `.razor.css` files
- [ ] Simple utilities use Bootstrap classes
- [ ] Global CSS minimized
- [ ] Visual regression tests pass
\`\`\`

---

### Step 6: Testing (1-2 hours)

#### 6.1 Build & Unit Tests

```bash
source /tmp/dotnet-proxy-setup.sh
dotnet build
dotnet test
dotnet format --verify-no-changes
```

#### 6.2 Visual Regression Testing

**Before migration**:
1. Take screenshots of all key pages:
   - Home dashboard
   - Ticket list
   - Ticket detail
   - Repository list
   - Settings pages

**After migration**:
1. Take screenshots of same pages
2. Compare screenshots (should be pixel-perfect identical)
3. Use browser dev tools to verify CSS isolation attributes applied

**Check**:
- Elements have unique attribute (e.g., `b-abc123xyz`)
- Styles scoped to component only
- No visual differences

#### 6.3 Browser Compatibility

Test in multiple browsers:
- [ ] Chrome/Edge (Chromium)
- [ ] Firefox
- [ ] Safari (if available)

---

### Step 7: Cleanup (30 minutes)

#### 7.1 Remove Unused Global CSS

If any global CSS was moved to component isolation:
- Remove from `wwwroot/css/site.css`
- Remove from `wwwroot/css/app.css`
- Remove component-specific CSS files (e.g., `contextual-help.css` if styles moved to `ContextualHelp.razor.css`)

#### 7.2 Verify Audit

```bash
# Should return zero results
grep -r "<style>" --include="*.razor" src/PRFactory.Web/Components/ src/PRFactory.Web/Pages/ src/PRFactory.Web/UI/
```

---

## Validation Checklist

- [ ] Zero `<style>` tags in `.razor` files
- [ ] 15-20 components have `.razor.css` files
- [ ] CSS-Strategy.md document created
- [ ] All tests pass (build, unit, integration)
- [ ] Visual regression tests pass (no styling changes)
- [ ] Browser compatibility verified
- [ ] Global CSS cleaned up (unused styles removed)
- [ ] Documentation updated

---

## Deliverables

1. **CSS Isolation Files** (15-20 files)
   - `Components/Tickets/TicketHeader.razor.css`
   - `UI/Cards/Card.razor.css`
   - `UI/Dialogs/Modal.razor.css`
   - `UI/Display/EventTimeline.razor.css`
   - `UI/Editors/MarkdownEditor.razor.css`
   - ... (and others identified in audit)

2. **Updated Component Files** (inline styles removed)
   - `Components/Tickets/TicketHeader.razor`
   - ... (all components with inline styles)

3. **Documentation**
   - `docs/CSS-Strategy.md`

4. **Commit**
   ```bash
   git add .
   git commit -m "feat(epic-08): migrate to CSS isolation

   - Remove all inline <style> tags from components
   - Create CSS isolation files for 15-20 components
   - Document CSS strategy in docs/CSS-Strategy.md
   - Clean up unused global CSS
   - Verify no visual regressions

   Closes Phase 2 of EPIC 08"
   ```

---

## Common Issues & Solutions

### Issue: CSS not scoped correctly

**Symptom**: Styles leak to other components

**Solution**: Verify file naming matches exactly:
- `Card.razor` → `Card.razor.css` (exact match)
- Not `card.razor.css` (case mismatch)

### Issue: Blazor not finding .razor.css file

**Solution**: Clean and rebuild
```bash
dotnet clean
dotnet build
```

### Issue: Styles working locally but not in Docker

**Solution**: Verify `.razor.css` files included in Docker build (check Dockerfile COPY command)

---

## Phase 2 Complete!

Once all validation checks pass, Phase 2 is complete. Merge to main before proceeding to Phase 3.

**Next**: `PHASE_03_PAGINATION.md`
