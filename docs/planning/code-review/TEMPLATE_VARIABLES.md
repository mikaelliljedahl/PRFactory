# Code Review Template Variables Reference

This document lists all variables available in Handlebars templates for the code review agent.

---

## Variable Categories

### 1. Ticket Information

Context about the Jira/Azure DevOps/GitHub ticket being implemented.

| Variable | Type | Example | Description |
|----------|------|---------|-------------|
| `{{ticket_number}}` | string | `"PROJ-123"` | Jira/ADO ticket key |
| `{{ticket_title}}` | string | `"Add user authentication"` | Ticket title |
| `{{ticket_description}}` | string | `"Implement OAuth2..."` | Full ticket description |
| `{{ticket_url}}` | string | `"https://jira.../PROJ-123"` | Link to ticket in external system |

**Usage Example**:
```handlebars
## Ticket Information
**Ticket:** {{ticket_number}}
**Title:** {{ticket_title}}
**Description:**
{{ticket_description}}
```

---

### 2. Implementation Plan

Details about the approved implementation plan.

| Variable | Type | Example | Description |
|----------|------|---------|-------------|
| `{{plan_path}}` | string | `"/plans/PROJ-123.md"` | Path to implementation plan file |
| `{{plan_summary}}` | string | `"1. Add auth controller\n2. ..."` | Summary of plan steps |
| `{{plan_content}}` | string | Full plan markdown | Complete plan content |

**Usage Example**:
```handlebars
## Implementation Plan
**Plan Path:** {{plan_path}}
**Plan Summary:**
{{plan_summary}}

Please verify the implementation matches this approved plan.
```

---

### 3. Pull Request Details

Metadata about the pull request being reviewed.

| Variable | Type | Example | Description |
|----------|------|---------|-------------|
| `{{pull_request_url}}` | string | `"https://github.../pull/42"` | PR URL |
| `{{pull_request_number}}` | int | `42` | PR number |
| `{{branch_name}}` | string | `"feature/PROJ-123-auth"` | Source branch name |
| `{{target_branch}}` | string | `"main"` | Target branch (usually main/master) |
| `{{files_changed_count}}` | int | `7` | Number of files changed |
| `{{lines_added}}` | int | `342` | Total lines added across all files |
| `{{lines_deleted}}` | int | `89` | Total lines deleted across all files |
| `{{commits_count}}` | int | `3` | Number of commits in PR |

**Usage Example**:
```handlebars
## Pull Request Details
**PR URL:** {{pull_request_url}}
**Branch:** {{branch_name}} → {{target_branch}}
**Files Changed:** {{files_changed_count}}
**Lines:** +{{lines_added}} -{{lines_deleted}}
**Commits:** {{commits_count}}
```

---

### 4. Code Changes (Array)

Array of file change objects with diffs and metadata.

| Property | Type | Example | Description |
|----------|------|---------|-------------|
| `{{this.file_path}}` | string | `"src/Controllers/AuthController.cs"` | Full file path |
| `{{this.change_type}}` | string | `"Added" \| "Modified" \| "Deleted"` | Type of change |
| `{{this.language}}` | string | `"csharp" \| "typescript" \| "python"` | Programming language |
| `{{this.lines_added}}` | int | `145` | Lines added in this file |
| `{{this.lines_deleted}}` | int | `23` | Lines deleted in this file |
| `{{this.diff}}` | string | Git diff content | Full git diff for file |
| `{{this.is_test_file}}` | bool | `true \| false` | Whether this is a test file |
| `{{this.complexity_score}}` | int | `1-10` | Optional cyclomatic complexity |

**Usage Example**:
```handlebars
## Code Changes

{{#each file_changes}}
### File: {{this.file_path}}
**Change Type:** {{this.change_type}}
**Lines Changed:** +{{this.lines_added}} -{{this.lines_deleted}}
{{#if this.is_test_file}}**This is a test file**{{/if}}

```{{this.language}}
{{this.diff}}
```

{{/each}}
```

**Output Example**:
```markdown
### File: src/Controllers/AuthController.cs
**Change Type:** Added
**Lines Changed:** +145 -0

```csharp
+using Microsoft.AspNetCore.Mvc;
+
+public class AuthController : ControllerBase
+{
+    // ... (diff content)
+}
```
```

---

### 5. Codebase Context

Information about the project structure and related files.

| Variable | Type | Example | Description |
|----------|------|---------|-------------|
| `{{codebase_structure}}` | string | `"src/\n  Controllers/\n  Services/\n..."` | Project directory tree |
| `{{related_files}}` | array | Array of file objects | Files related to changes |

**Related Files Object Structure**:
| Property | Type | Description |
|----------|------|-------------|
| `{{this.path}}` | string | File path (e.g., `"src/Services/UserService.cs"`) |
| `{{this.description}}` | string | Brief description (e.g., `"User authentication service"`) |
| `{{this.content}}` | string | Full file content for context |

**Usage Example**:
```handlebars
## Codebase Context

### Project Structure
{{codebase_structure}}

### Related Files
{{#each related_files}}
- **{{this.path}}** - {{this.description}}
{{/each}}
```

---

### 6. Testing Information

Test coverage and test file details.

| Variable | Type | Example | Description |
|----------|------|---------|-------------|
| `{{tests_added}}` | int | `5` | Number of test files added/modified |
| `{{test_coverage_percentage}}` | float | `87.5` | Code coverage percentage |
| `{{test_files}}` | array | `[{path: "..."}]` | Array of test file paths |

**Usage Example**:
```handlebars
## Testing Coverage
**Tests Added:** {{tests_added}}
**Coverage:** {{test_coverage_percentage}}%

{{#if test_files}}
### Test Files Modified
{{#each test_files}}
- {{this.path}}
{{/each}}
{{/if}}
```

---

### 7. Metadata

General repository and PR metadata.

| Variable | Type | Example | Description |
|----------|------|---------|-------------|
| `{{repository_name}}` | string | `"PRFactory"` | Repository name |
| `{{repository_url}}` | string | `"https://github.com/..."` | Repository URL |
| `{{author_name}}` | string | `"Claude Agent"` | Commit author name |
| `{{created_at}}` | datetime | `"2025-01-15T10:30:00Z"` | PR creation timestamp |

**Usage Example**:
```handlebars
**Repository:** {{repository_name}}
**Author:** {{author_name}}
**Created:** {{created_at}}
```

---

## Handlebars Helpers

Custom helpers available in templates.

### 1. `{{code language content}}`
Formats code with syntax highlighting markers.

**Syntax**:
```handlebars
{{code "csharp" someCodeVariable}}
```

**Output**:
````
```csharp
public class Example { }
```
````

---

### 2. `{{truncate text maxLength}}`
Truncates long text with ellipsis.

**Syntax**:
```handlebars
{{truncate ticket_description 500}}
```

**Output**:
```
This is a very long description that will be truncated after 500 characters...
```

---

### 3. `{{filesize bytes}}`
Formats file size in human-readable format.

**Syntax**:
```handlebars
{{filesize 1048576}}
```

**Output**:
```
1.00 MB
```

---

## Complete Template Example

**File**: `/prompts/code-review/openai/user_template.hbs`

```handlebars
# Code Review Request

## Ticket Information
**Ticket:** {{ticket_number}}
**Title:** {{ticket_title}}
**Description:**
{{truncate ticket_description 500}}

## Implementation Plan
**Plan Path:** {{plan_path}}
**Plan Summary:**
{{plan_summary}}

## Pull Request Details
**PR URL:** {{pull_request_url}}
**Branch:** {{branch_name}} → {{target_branch}}
**Files Changed:** {{files_changed_count}}
**Lines:** +{{lines_added}} -{{lines_deleted}}

## Code Changes

{{#each file_changes}}
### File: {{this.file_path}}
**Change Type:** {{this.change_type}}
**Lines:** +{{this.lines_added}} -{{this.lines_deleted}}

```{{this.language}}
{{this.diff}}
```

{{/each}}

## Testing Coverage
**Tests Added:** {{tests_added}}
**Coverage:** {{test_coverage_percentage}}%

---

**Review Instructions:**
Please review this pull request against the implementation plan and provide detailed feedback on:
1. Security vulnerabilities
2. Logic errors or edge cases
3. Code maintainability
4. Test coverage adequacy
5. Compliance with implementation plan
```

---

## Variable Population

Variables are populated by `CodeReviewAgent.ExecuteAsync()`:

```csharp
var templateVariables = new
{
    // Ticket info from context.Ticket
    ticket_number = context.Ticket.TicketKey,
    ticket_title = context.Ticket.Title,
    ticket_description = context.Ticket.Description,

    // Plan info from database
    plan_path = plan.FilePath,
    plan_summary = plan.Summary,

    // PR info from git platform API
    pull_request_url = prDetails.Url,
    branch_name = prDetails.SourceBranch,
    files_changed_count = prDetails.FilesChanged.Count,

    // Code changes from git diff
    file_changes = prDetails.FilesChanged.Select(f => new {
        file_path = f.Path,
        change_type = f.ChangeType,
        diff = f.Diff,
        // ...
    }),

    // Testing from coverage analysis
    test_coverage_percentage = prDetails.TestCoverage?.Percentage ?? 0,

    // ... etc
};

var userPrompt = _promptService.RenderTemplate(
    "code-review",
    provider.ProviderName,
    "user_template",
    templateVariables);
```

---

## Best Practices

### 1. Use Truncation for Long Content
```handlebars
{{truncate ticket_description 500}}
{{truncate plan_content 2000}}
```

### 2. Conditional Blocks
```handlebars
{{#if test_files}}
### Test Files Modified
{{#each test_files}}
- {{this.path}}
{{/each}}
{{/if}}
```

### 3. Safe Defaults
```handlebars
**Coverage:** {{test_coverage_percentage}}%
<!-- Will show "0%" if test_coverage_percentage is null -->
```

### 4. Language Detection
```handlebars
```{{this.language}}
{{this.diff}}
```
<!-- Automatically uses detected language for syntax highlighting -->
```

---

## Adding New Variables

To add new template variables:

1. **Update CodeReviewAgent.cs**:
   ```csharp
   var templateVariables = new {
       // ... existing variables
       new_variable = someValue,
   };
   ```

2. **Document in this file**:
   Add to appropriate category with type, example, description.

3. **Update example templates**:
   Show usage in `/prompts/code-review/*/user_template.hbs`.

4. **Test rendering**:
   Verify variable appears correctly in rendered output.
