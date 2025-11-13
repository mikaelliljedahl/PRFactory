# C# 12 Collection Expressions - Quick Reference

A quick guide to modernizing collection initializations in PRFactory.

---

## Pattern Quick Reference

### List Literals
```csharp
// Before
new List<string> { "a", "b", "c" }

// After
["a", "b", "c"]
```

### Empty Lists (Null Coalescing)
```csharp
// Before
x ?? new List<T>()

// After
x ?? []
```

### Array Literals
```csharp
// Before
new object[] { a, b, c }

// After
[a, b, c]
```

### Spread Operator (Concat Alternative)
```csharp
// Before
collection1.Concat(collection2).ToList()

// After
[..collection1, ..collection2]
```

### Field Initialization
```csharp
// Before
public ICollection<T> Items { get; } = new List<T>();

// After
public ICollection<T> Items { get; } = [];
```

### Combining with Null Coalesce
```csharp
// Before
required.Concat(optional ?? new List<T>()).ToList()

// After
[..required, ..(optional ?? [])]
```

---

## Found in PRFactory

### Phase 1: Easy Wins (Start Here)

#### ReviewComment.cs
```csharp
// Line 63, 76
MentionedUserIds = mentionedUserIds ?? new List<Guid>();
// Change to:
MentionedUserIds = mentionedUserIds ?? [];
```

#### CodeReviewResult.cs
```csharp
// Lines 126-128
CriticalIssues = criticalIssues ?? new List<string>();
Suggestions = suggestions ?? new List<string>();
Praise = praise ?? new List<string>();
// Change to:
CriticalIssues = criticalIssues ?? [];
Suggestions = suggestions ?? [];
Praise = praise ?? [];
```

#### CliAgentCapabilities.cs
```csharp
// Line 68
SupportedFormats = supportedFormats ?? new List<string>(),
// Change to:
SupportedFormats = supportedFormats ?? [],

// Lines 85, 102
SupportedFormats = new List<string> { "json", "markdown", "text" },
SupportedFormats = new List<string> { "json", "text" },
// Change to:
SupportedFormats = ["json", "markdown", "text"],
SupportedFormats = ["json", "text"],
```

#### LlmProviderFactory.cs
```csharp
// Line 103
return new List<string> { AnthropicProviderName, "google", "openai" };
// Change to:
return [AnthropicProviderName, "google", "openai"];
```

#### ClaudeCodeCliLlmProvider.cs
```csharp
// Lines 72, 114
var args = new List<string> { "--headless", "--prompt", prompt };
// Change to:
var args = ["--headless", "--prompt", prompt];
```

---

### Phase 2: Medium Complexity

#### User.cs
```csharp
// Lines 98-99
public ICollection<PlanReview> PlanReviews { get; private set; } = new List<PlanReview>();
public ICollection<ReviewComment> Comments { get; private set; } = new List<ReviewComment>();
// Change to:
public ICollection<PlanReview> PlanReviews { get; private set; } = [];
public ICollection<ReviewComment> Comments { get; private set; } = [];
```

#### PlanReviewService.cs
```csharp
// Line 46
var allReviewerIds = requiredReviewerIds.Concat(optionalReviewerIds ?? new List<Guid>()).ToList();
// Change to:
var allReviewerIds = [..requiredReviewerIds, ..(optionalReviewerIds ?? [])];
```

#### WorkflowOrchestrator.cs
```csharp
// Line 491
string.Join("\n", reviewComplete.CriticalIssues.Concat(reviewComplete.Suggestions))
// Change to:
string.Join("\n", [..reviewComplete.CriticalIssues, ..reviewComplete.Suggestions])
```

#### CodeReviewGraph.cs
```csharp
// Line 191
ReviewFeedback: string.Join("\n", criticalIssues.Concat(suggestions))
// Change to:
ReviewFeedback: string.Join("\n", [..criticalIssues, ..suggestions])
```

#### GitServiceCollectionExtensions.cs & GitPlatformService.cs
```csharp
// Lines 148, 274
new object[] { id, ct }
// Change to:
[id, ct]
```

---

### Phase 3: Manual Review Required

#### JiraContent.cs (Line 144) - SKIP
```csharp
// This one is complex - ternary with Dictionary
Attrs = language != null ? new Dictionary<string, object> { ["language"] = language } : null

// NOT worth modernizing - keep as-is
```

#### AgentRegistry.cs (Line 120) - VERIFY TYPES
```csharp
// Before
return _agents.Values.ToList().AsReadOnly();

// After - VERIFY this change doesn't break callers expecting IList<T>
return [.._agents.Values];
```

#### WorkflowStateTransitions.cs (Line 137) - VERIFY TYPES
```csharp
// Before
new List<WorkflowState>().AsReadOnly()

// After - VERIFY semantics
[].AsReadOnly()
```

---

## Implementation Checklist

- [ ] Phase 1: Easy wins (ReviewComment, CodeReviewResult, CliAgentCapabilities, etc.)
- [ ] Run tests: `dotnet test`
- [ ] Run formatter: `dotnet format --verify-no-changes`
- [ ] Phase 2: Medium complexity (User, PlanReviewService, etc.)
- [ ] Run tests again
- [ ] Phase 3: Manual review (check AsReadOnly patterns, skip Dictionary ternary)
- [ ] Final test run
- [ ] Commit: "refactor: Modernize collection initializations to C# 12 expressions"

---

## Testing

After each phase, run:

```bash
# Build
dotnet build

# Test
dotnet test

# Format check
dotnet format PRFactory.sln --verify-no-changes
```

Ensure all tests pass before proceeding to next phase.

---

## Common Mistakes to Avoid

### Don't Mix Styles
```csharp
// Bad - inconsistent
var a = ["x", "y"];
var b = new List<string> { "x", "y" };

// Good - consistent
var a = ["x", "y"];
var b = ["x", "y"];
```

### Watch Type Inference with var
```csharp
// Careful with var - may infer different type
var items = [];  // This is array, not List!

// Better - explicit type
List<string> items = [];  // Clear intent
```

### Null Safety in Spreads
```csharp
// Before (safe - null coalesced)
list1.Concat(list2 ?? new List<T>()).ToList()

// After (must preserve null coalesce)
[..list1, ..(list2 ?? [])]

// Not this (list2 could be null!)
[..list1, ..list2]  // WRONG if list2 can be null
```

### Return Type Changes
```csharp
// Before returns IList<T>
return new List<T>().AsReadOnly();

// After returns ICollection<T> - may break callers!
return [].AsReadOnly();  // Type mismatch!

// Better - maintain same return type
return (IList<T>)[];  // Explicit cast
```

---

## Impact Summary

- **Lines Saved:** ~28 lines of code
- **Tokens Saved:** ~642 tokens
- **Risk Level:** Low-Medium (if phased)
- **Testing Required:** Yes
- **Breaking Changes:** Minimal (check AsReadOnly patterns)

---

For full details, see:
- `/home/user/PRFactory/COLLECTION_EXPRESSIONS_MODERNIZATION.md`
- `/home/user/PRFactory/collection_expressions_analysis.json`
