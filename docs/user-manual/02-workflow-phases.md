# PRFactory User Guide: Understanding the Three Workflow Phases

This guide provides an in-depth look at each phase of the PRFactory workflow.

---

## Overview

PRFactory transforms tickets into pull requests through three distinct phases:

```
┌──────────────────────────────────────────────────────────┐
│                     WORKFLOW PHASES                       │
├──────────────────────────────────────────────────────────┤
│                                                           │
│  Phase 1: REFINEMENT                                     │
│  ├── Clone Repository                                    │
│  ├── Analyze Codebase                                    │
│  ├── Generate Questions                                   │
│  ├── ⏸ Wait for Your Answers                            │
│  ├── Process Answers                                     │
│  ├── Generate Refined Ticket                             │
│  ├── ⏸ Wait for Your Approval                           │
│  └── Post to Ticket System                              │
│                                                           │
│  Phase 2: PLANNING                                       │
│  ├── Generate Implementation Plan                         │
│  ├── Commit Plan to Git Branch                          │
│  ├── Post Plan to Ticket System                         │
│  ├── ⏸ Wait for Your Decision                           │
│  └── (Loop if refined/rejected)                         │
│                                                           │
│  Phase 3: IMPLEMENTATION (Optional)                      │
│  ├── Check Configuration                                 │
│  ├── Implement Code                                      │
│  ├── Commit to Feature Branch                           │
│  ├── Create Pull Request                                 │
│  └── Post PR to Ticket System                           │
│                                                           │
└──────────────────────────────────────────────────────────┘

⏸ = Suspension Point (waiting for you)
```

---

## Phase 1: Refinement

### Purpose
**Clarify requirements** and create a detailed, unambiguous ticket description.

### Why This Matters
- Vague tickets lead to wrong implementations
- AI needs context to generate good code
- Clarifying questions catch edge cases early
- Better requirements = better plans = better code

### What the AI Does

#### 1. Clone Repository
- Downloads your repository to local workspace
- Ensures latest code is available for analysis

#### 2. Analyze Codebase
- Scans project structure, frameworks, patterns
- Identifies related components (controllers, services, models)
- Understands your tech stack and conventions
- **Retry**: Up to 3 attempts if analysis fails

#### 3. Generate Questions
- Creates intelligent questions based on:
  - Ticket description gaps
  - Codebase context
  - Common implementation scenarios
  - Edge cases and error handling

#### 4. Post Questions
- Questions are posted to your ticket system (Jira, Azure DevOps, GitHub Issues)
- You can answer via:
  - **UI**: Question form in ticket detail page
  - **Jira**: `@claude` mention with answers in comment

### Your First Interaction: Answer Questions

**What you'll see**:
```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Clarifying Questions (Step 1 of 2)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

The AI has analyzed your codebase and needs clarification
on the following points before proceeding.

Question 1: Authentication Method
Which authentication approach should we use?

○ JWT tokens (stateless)
○ Session cookies (database-backed)
○ OAuth2 only (delegated)

Question 2: User Roles
Should we implement role-based access control?

[Text area for detailed answer]

Question 3: Password Requirements
What are the password complexity requirements?

☑ Minimum 8 characters
☑ Require uppercase and lowercase
☑ Require numbers
☑ Require special characters
☐ Check against common password lists

[Submit Answers]
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

**Tips for Good Answers**:
- ✅ Be specific: "Use JWT tokens with 1-hour expiration"
- ✅ Mention edge cases: "Handle expired tokens gracefully"
- ✅ Reference existing patterns: "Follow the pattern in OrderController"
- ❌ Don't be vague: "Just make it secure"

### Processing Your Answers
Once you submit, the AI:
- Incorporates your answers into requirements
- Generates a comprehensive ticket description
- Includes all clarified details

### Your Second Interaction: Review Refined Ticket

**What you'll see**:
```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Refined Ticket Description (Step 2 of 2)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📋 Original Ticket:
"Add user authentication"

📝 Refined Description:
Implement JWT-based user authentication system.

Requirements:
• Authentication Method: JWT tokens with 1-hour expiration
• User Storage: PostgreSQL Users table
• Password Hashing: BCrypt with cost factor 12
• Endpoints: /auth/login, /auth/register, /auth/refresh
• Role-Based Access: Admin, User, Guest roles
• Password Requirements:
  - Minimum 8 characters
  - Must include: uppercase, lowercase, number, special char
  - Validation against common password lists

Security Considerations:
• Token refresh mechanism for seamless UX
• Rate limiting on auth endpoints (5 attempts per minute)
• Secure password reset flow with email verification

Edge Cases:
• Handle expired tokens with 401 Unauthorized response
• Prevent brute force attacks with account lockout (5 failures)
• Support "Remember Me" with longer-lived refresh tokens

Technical Constraints:
• Follow existing AuthController pattern
• Use Entity Framework Core for user management
• Integrate with existing email service for notifications

[Approve]  [Reject & Regenerate]
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

**Your Decision**:
- **Approve**: Move to planning phase
- **Reject & Regenerate**: AI creates new description (up to 3 retries)

**When to Reject**:
- Requirements are still unclear
- Important details missing
- Incorrect assumptions made

### Completion
Once approved, the refined description is posted to your ticket system and Phase 2 begins.

---

## Phase 2: Planning

### Purpose
**Generate a detailed, reviewable implementation plan** that engineers can follow.

### Why This Matters
- Plans catch architectural issues before code is written
- Team can review and approve approach
- Provides clear roadmap for implementation
- Prevents wasted effort on wrong approach

### What the AI Does

#### 1. Generate Implementation Plan
Using the refined ticket and full codebase context, the AI creates:

- **Database Schema Changes**: SQL DDL statements
- **API Endpoint Specifications**: Routes, methods, request/response formats
- **File Changes**: Which files to create/modify with line count estimates
- **Test Coverage**: Required test cases and assertions
- **Step-by-Step Implementation**: Ordered list of tasks
- **Dependencies**: Required NuGet packages or npm modules

**Example Plan Structure**:
```markdown
# Implementation Plan: Add User Authentication (PROJ-123)

## 1. Database Schema Changes

```sql
CREATE TABLE Users (
    Id UUID PRIMARY KEY,
    Email VARCHAR(255) UNIQUE NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    Role VARCHAR(50) NOT NULL DEFAULT 'User',
    CreatedAt TIMESTAMP NOT NULL,
    LastLoginAt TIMESTAMP
);

CREATE INDEX IX_Users_Email ON Users(Email);
```

## 2. New Files

### AuthController.cs (src/Controllers/AuthController.cs)
- Estimated Lines: 150
- Endpoints: Login, Register, Refresh, Logout
- Dependencies: IUserService, IJwtService

### UserService.cs (src/Services/UserService.cs)
- Estimated Lines: 200
- Methods: CreateUser, ValidateCredentials, UpdateLastLogin
- Dependencies: IUserRepository, IPasswordHasher

### JwtService.cs (src/Services/JwtService.cs)
- Estimated Lines: 100
- Methods: GenerateToken, RefreshToken, ValidateToken
- Dependencies: IConfiguration

## 3. Modified Files

### Startup.cs
- Add JWT authentication middleware
- Configure authentication options
- Register services in DI container

## 4. Testing

### AuthController.Tests.cs
- Test Cases (15):
  1. Login with valid credentials succeeds
  2. Login with invalid credentials fails
  3. Register with valid data creates user
  ... (12 more)

## 5. Implementation Steps

1. Create Users table via EF Core migration
2. Implement UserService with password hashing
3. Implement JwtService with token generation
4. Create AuthController with endpoints
5. Add authentication middleware to pipeline
6. Write unit tests for all components
7. Write integration tests for endpoints
8. Update API documentation
```

#### 2. Parallel Execution: Git + Jira
The AI **simultaneously**:
- **Commits plan** to a git branch (e.g., `plan/PROJ-123`)
- **Posts plan** to your ticket system (Jira comment, Azure DevOps work item)

### Your Third Interaction: Review Plan

**What you'll see**:
```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Implementation Plan Review
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📊 Plan Summary:
• Database Tables: 1 new (Users)
• API Endpoints: 4 (/auth/*)
• New Files: 6 (550 lines total)
• Modified Files: 2
• Test Cases: 27

📁 Plan File: plan/PROJ-123.md
🔗 Git Branch: plan/PROJ-123

[View Full Plan]

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Review Actions:

[Approve]  [Refine]  [Reject & Regenerate]

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

**Your Three Options**:

### Option 1: Approve
**When to use**: Plan looks good, ready to implement
**What happens**:
- If auto-implementation enabled → Phase 3 starts
- If auto-implementation disabled → Workflow completes (you implement manually)

### Option 2: Refine
**When to use**: Plan is mostly good but needs specific improvements
**What happens**:
- You provide refinement instructions (e.g., "Add password reset endpoint")
- AI regenerates plan **keeping overall structure**
- Updated plan posted for re-review

**Example Refinement**:
```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Refinement Instructions

Please update the plan to:
1. Add password reset endpoint (/auth/reset-password)
2. Include email verification for new registrations
3. Add rate limiting configuration details

[Submit Refinement]
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

### Option 3: Reject & Regenerate
**When to use**: Plan approach is fundamentally wrong
**What happens**:
- You provide rejection reason
- AI **discards plan** and starts fresh
- Completely new approach generated

**Example Rejection**:
```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Rejection Reason

This plan uses MongoDB but our stack is PostgreSQL.
Please regenerate using:
• Entity Framework Core with PostgreSQL
• Our existing repository pattern
• The AuthenticationService pattern from OrderModule

[Submit Rejection]
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

### Retry Behavior
- **Refine**: No retry limit (keeps iterating)
- **Reject & Regenerate**: Maximum 5 attempts
- After 5 rejections, workflow fails (prevents infinite loops)

### Team Review (Optional)
**If team reviewers are assigned**:
1. Multiple team members can be assigned as reviewers
2. **Required reviewers** must approve before you can approve
3. **Optional reviewers** provide feedback but don't block approval
4. Reviewers can add comments and approve/reject independently

**Team Review UI**:
```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Team Reviewers

Required:
• Alice (Tech Lead) ✅ Approved
• Bob (Senior Engineer) ⏳ Pending

Optional:
• Charlie (QA) ⏳ Pending

⚠️ Cannot approve until all required reviewers approve

[Assign Additional Reviewers]  [Add Comment]
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

---

## Phase 3: Implementation (Optional)

### Purpose
**Optionally implement code** based on approved plan (tenant-configurable).

### Configuration Check
**Before executing**, system checks:
```
Tenant Configuration:
  AutoImplementAfterPlanApproval = true/false
```

- **If `true`**: Phase 3 executes automatically
- **If `false`**: Workflow completes, you implement manually

### What the AI Does

#### 1. Implement Code
- Generates all code following approved plan
- Creates new files (controllers, services, tests)
- Modifies existing files (Startup, configuration)
- Follows project conventions and patterns

#### 2. Commit Changes
- All changes committed to feature branch (e.g., `feature/PROJ-123-auth`)
- Commit message references ticket number
- Includes plan reference in commit description

#### 3. Parallel Execution: PR + Jira
The AI **simultaneously**:
- **Creates pull request** on GitHub/Bitbucket/Azure DevOps
- **Posts PR link** to your ticket system

### Your Final Step: Review PR

**What you'll see**:
```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Pull Request Created ✅
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

PR #42: Add user authentication (PROJ-123)
🔗 https://github.com/your-org/repo/pull/42

Branch: feature/PROJ-123-auth → main

📊 Changes:
• Files: 8 changed (+550, -12)
• Commits: 3
• Tests: 27 added

Status: ✅ All checks passed
• Build: ✅ Success
• Tests: ✅ 27/27 passing
• Code Coverage: ✅ 87%

[View PR on GitHub]
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

**Next Steps**:
1. Open PR in GitHub/Bitbucket/Azure DevOps
2. Review code changes
3. Run additional manual tests if needed
4. Request human reviews
5. Merge when ready

---

## Workflow Completion

Once the PR is created (or plan is approved if auto-implementation disabled), the workflow is marked as **Completed**.

**Completion Summary**:
```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Workflow Complete 🎉
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Ticket: PROJ-123 - Add user authentication
Status: Completed
Duration: 45 minutes

Phase 1: Refinement ✅
  • Questions answered
  • Ticket refined and approved

Phase 2: Planning ✅
  • Plan generated and approved
  • 0 refinement iterations

Phase 3: Implementation ✅
  • Code implemented
  • PR #42 created

Next Steps:
1. Review and merge PR #42
2. Deploy to staging environment
3. Mark ticket as complete in Jira

[View Ticket]  [View PR]  [View Timeline]
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

---

## Key Takeaways

### Suspension Points (You Control the Flow)
- **After Questions**: Submit answers when ready
- **After Refined Ticket**: Approve or reject
- **After Plan**: Approve, refine, or reject

### No Suspension Points (AI Runs Automatically)
- Code implementation
- PR creation
- Jira/Azure DevOps posting

### Workflow Flexibility
- **Refine plans** iteratively without limit
- **Reject plans** up to 5 times
- **Skip implementation** with configuration
- **Assign team reviewers** for collaborative approval

### Real-Time Updates
All changes appear automatically in the UI (via SignalR WebSocket connection).

---

## Next Steps

- **[UI Operations Guide](./03-ui-operations.md)** - Detailed explanation of all UI actions
- **[Configuration Guide](./04-configuration.md)** - Customize workflow behavior
- **[FAQ & Troubleshooting](./05-faq.md)** - Common questions and issues
