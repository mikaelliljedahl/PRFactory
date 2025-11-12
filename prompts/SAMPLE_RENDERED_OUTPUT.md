# Sample Rendered Output: Code Review Template

This demonstrates how the code-review template renders with actual variable data.

## Template Used
- **Agent:** code-review
- **Provider:** anthropic
- **File:** `/prompts/code-review/anthropic/user_template.hbs`

## Sample Variables
```json
{
  "ticket_number": "PROJ-456",
  "ticket_title": "Add user authentication with OAuth2",
  "ticket_description": "Implement OAuth2 authentication flow using Google and Microsoft providers. Include token refresh, session management, and user profile synchronization.",
  "ticket_url": "https://jira.example.com/browse/PROJ-456",
  "plan_path": "/plans/PROJ-456-oauth-implementation.md",
  "plan_summary": "1. Add OAuth2 configuration\n2. Implement authentication controllers\n3. Add user session management\n4. Create login/logout UI\n5. Add tests for auth flow",
  "pull_request_url": "https://github.com/example/prfactory/pull/123",
  "branch_name": "feature/PROJ-456-oauth-auth",
  "target_branch": "main",
  "files_changed_count": 8,
  "lines_added": 654,
  "lines_deleted": 42,
  "commits_count": 5,
  "file_changes": [
    {
      "file_path": "src/PRFactory.Web/Controllers/AuthController.cs",
      "change_type": "Added",
      "language": "csharp",
      "lines_added": 145,
      "lines_deleted": 0,
      "is_test_file": false,
      "diff": "+using Microsoft.AspNetCore.Authentication;\n+using Microsoft.AspNetCore.Authentication.Google;\n+\n+namespace PRFactory.Web.Controllers;\n+\n+public class AuthController : Controller\n+{\n+    [HttpGet(\"/auth/login\")]\n+    public IActionResult Login(string provider)\n+    {\n+        return Challenge(new AuthenticationProperties\n+        {\n+            RedirectUri = \"/auth/callback\"\n+        }, provider);\n+    }\n+}"
    },
    {
      "file_path": "src/PRFactory.Infrastructure/Services/UserSessionService.cs",
      "change_type": "Added",
      "language": "csharp",
      "lines_added": 98,
      "lines_deleted": 0,
      "is_test_file": false,
      "diff": "+using PRFactory.Core.Entities;\n+\n+namespace PRFactory.Infrastructure.Services;\n+\n+public class UserSessionService : IUserSessionService\n+{\n+    private readonly IUserRepository _userRepo;\n+    \n+    public async Task<UserSession> CreateSessionAsync(string userId)\n+    {\n+        // Session creation logic\n+    }\n+}"
    }
  ],
  "codebase_structure": "src/\n├── PRFactory.Api/\n├── PRFactory.Web/\n│   ├── Controllers/\n│   ├── Pages/\n│   └── Services/\n├── PRFactory.Infrastructure/\n│   ├── Services/\n│   └── Repositories/\n└── PRFactory.Core/\n    ├── Entities/\n    └── Interfaces/",
  "related_files": [
    {
      "path": "src/PRFactory.Core/Entities/User.cs",
      "description": "User entity with authentication properties"
    },
    {
      "path": "src/PRFactory.Infrastructure/Data/AppDbContext.cs",
      "description": "Database context"
    }
  ],
  "tests_added": 3,
  "test_coverage_percentage": 87.5,
  "test_files": [
    {
      "path": "tests/PRFactory.Web.Tests/Controllers/AuthControllerTests.cs"
    }
  ],
  "repository_name": "PRFactory",
  "author_name": "Claude AI Agent",
  "created_at": "2025-11-11T14:32:00Z"
}
```

## Rendered Output

---

# Code Review Request

## Ticket Information
**Ticket:** PROJ-456
**Title:** Add user authentication with OAuth2
**Description:**
Implement OAuth2 authentication flow using Google and Microsoft providers. Include token refresh, session management, and user profile synchronization.

**Ticket URL:** https://jira.example.com/browse/PROJ-456

## Implementation Plan
**Plan Path:** /plans/PROJ-456-oauth-implementation.md
**Plan Summary:**
1. Add OAuth2 configuration
2. Implement authentication controllers
3. Add user session management
4. Create login/logout UI
5. Add tests for auth flow

## Pull Request Details
**PR URL:** https://github.com/example/prfactory/pull/123
**Branch:** feature/PROJ-456-oauth-auth → main
**Files Changed:** 8
**Lines Added:** 654
**Lines Deleted:** 42
**Commits:** 5

## Code Changes

### File: src/PRFactory.Web/Controllers/AuthController.cs
**Change Type:** Added
**Language:** csharp
**Lines Changed:** +145 -0

```csharp
+using Microsoft.AspNetCore.Authentication;
+using Microsoft.AspNetCore.Authentication.Google;
+
+namespace PRFactory.Web.Controllers;
+
+public class AuthController : Controller
+{
+    [HttpGet("/auth/login")]
+    public IActionResult Login(string provider)
+    {
+        return Challenge(new AuthenticationProperties
+        {
+            RedirectUri = "/auth/callback"
+        }, provider);
+    }
+}
```

### File: src/PRFactory.Infrastructure/Services/UserSessionService.cs
**Change Type:** Added
**Language:** csharp
**Lines Changed:** +98 -0

```csharp
+using PRFactory.Core.Entities;
+
+namespace PRFactory.Infrastructure.Services;
+
+public class UserSessionService : IUserSessionService
+{
+    private readonly IUserRepository _userRepo;
+
+    public async Task<UserSession> CreateSessionAsync(string userId)
+    {
+        // Session creation logic
+    }
+}
```

## Codebase Context

### Project Structure
src/
├── PRFactory.Api/
├── PRFactory.Web/
│   ├── Controllers/
│   ├── Pages/
│   └── Services/
├── PRFactory.Infrastructure/
│   ├── Services/
│   └── Repositories/
└── PRFactory.Core/
    ├── Entities/
    └── Interfaces/

### Related Files
- **src/PRFactory.Core/Entities/User.cs** - User entity with authentication properties
- **src/PRFactory.Infrastructure/Data/AppDbContext.cs** - Database context

## Testing Coverage
**Tests Added/Modified:** 3
**Test Coverage:** 87.5%

### Test Files
- tests/PRFactory.Web.Tests/Controllers/AuthControllerTests.cs

## Repository Metadata
**Repository:** PRFactory
**Author:** Claude AI Agent
**Created:** 2025-11-11T14:32:00Z

---

**Review Instructions:**
Please review this pull request thoroughly and provide detailed feedback on:

1. **Security Issues:** Check for vulnerabilities, hardcoded secrets, insecure authentication/authorization
2. **Correctness:** Verify logic, edge cases, error handling, null safety
3. **Performance:** Identify inefficient code, database issues, memory problems
4. **Maintainability:** Assess code clarity, documentation, naming, duplication
5. **Testing:** Evaluate test coverage, test quality, missing test cases
6. **Architecture:** Check adherence to SOLID principles and clean architecture patterns
7. **Implementation Plan Compliance:** Verify the code implements what was planned

For each issue found, please:
- Specify file path and line numbers
- Explain WHY it's an issue
- Provide specific HOW to fix it
- Rate severity (Critical/Suggested/Praise)

Also highlight any particularly well-written code or excellent patterns used.

---

## Notes

This rendered output demonstrates:
1. ✅ **Variable substitution**: All `{{variable}}` placeholders replaced with actual values
2. ✅ **Array iteration**: `{{#each file_changes}}` loops through multiple files
3. ✅ **Conditional logic**: `{{#if test_files}}` only shows section if data exists
4. ✅ **Nested properties**: `{{this.file_path}}` accesses object properties
5. ✅ **Formatting preservation**: Code blocks, headings, and structure maintained

This template produces identical output structure across all providers (Anthropic, OpenAI, Google), with only the system prompt varying to match each provider's strengths.
