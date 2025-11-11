# PRFactory User Guide: Getting Started

Welcome to PRFactory! This guide will help you understand how to use PRFactory to automate your software development workflow.

---

## What is PRFactory?

PRFactory is an AI-powered development automation tool that helps you:
- **Clarify requirements** by asking intelligent questions about your tickets
- **Generate implementation plans** that your team can review and approve
- **Optionally implement code** based on approved plans
- **Create pull requests** ready for human review and merging

---

## How PRFactory Works

PRFactory uses a **three-phase workflow** to transform a ticket (Jira, Azure DevOps, or GitHub Issues) into a pull request:

```
Phase 1: REFINEMENT       Phase 2: PLANNING        Phase 3: IMPLEMENTATION
(Understand what          (Plan how to build it)   (Build it - optional)
 needs to be built)
        ↓                        ↓                         ↓
Analyze & Clarify  →  Create Plan & Review  →  Implement Code
```

Each phase involves **AI agents** doing the heavy lifting, with **you** providing guidance and approval at key decision points.

---

## Your Role in the Workflow

You interact with PRFactory at specific suspension points:

### ✋ Suspension Point 1: Answer Clarifying Questions
**When**: After the AI analyzes your codebase
**What to do**: Answer questions about requirements, edge cases, and implementation details
**Where**: Ticket detail page or via `@claude` mention in Jira

### ✋ Suspension Point 2: Review Refined Ticket
**When**: After the AI generates an improved ticket description
**What to do**: Approve or reject the refined description
**Where**: Ticket detail page

### ✋ Suspension Point 3: Review Implementation Plan
**When**: After the AI generates a detailed implementation plan
**What to do**:
- **Approve** - Start code implementation (if enabled)
- **Refine** - Provide specific improvements (keeps structure)
- **Reject & Regenerate** - Completely restart planning

**Where**: Ticket detail page

---

## Typical Workflow Example

Let's walk through a typical ticket:

### Step 1: Create a Ticket
1. Navigate to **Tickets → Create New**
2. Fill in:
   - **Title**: "Add user authentication"
   - **Description**: "We need OAuth2 login for users"
   - **Repository**: Select your repository
3. Click **Create Ticket**

> **Note**: Creating a ticket does NOT automatically start the workflow. You must trigger it manually.

---

### Step 2: Trigger the Workflow
1. Open the ticket detail page
2. Click **Start Workflow** button
3. PRFactory begins Phase 1: Refinement

**What happens next**:
- Repository is cloned locally
- Codebase is analyzed
- Clarifying questions are generated
- Questions are posted to your ticket system

---

### Step 3: Answer Questions
**You'll see a question form** like this:

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Clarifying Questions
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

1. Which OAuth providers should we support?
   □ Google
   □ GitHub
   □ Microsoft
   □ Facebook

2. Should we store user sessions in database or Redis?
   ○ Database (PostgreSQL)
   ○ Redis
   ○ Both

3. Do we need "Remember Me" functionality?
   [Text input]

[Submit Answers]
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

**Fill out the form and click Submit**. The AI will process your answers and generate a refined ticket description.

---

### Step 4: Review Refined Ticket
**You'll see a preview** of the improved ticket:

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Refined Ticket Description
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Original:
"We need OAuth2 login for users"

Refined:
"Implement OAuth2 authentication with Google and GitHub
providers. Store user sessions in PostgreSQL with JWT
tokens. Include 'Remember Me' functionality with 30-day
session expiration..."

[Full detailed description shown here]

[Approve]  [Reject & Regenerate]
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

**Click Approve** to move to planning phase.

---

### Step 5: Review Implementation Plan
**You'll see a detailed plan** showing exactly what will be built:

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Implementation Plan
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

1. Database Schema Changes
   - Add Users table (id, email, oauth_provider, ...)
   - Add UserSessions table (id, user_id, token, ...)

2. API Endpoints
   - POST /auth/login
   - POST /auth/callback
   - POST /auth/logout

3. New Files
   - src/Controllers/AuthController.cs (150 lines)
   - src/Services/OAuthService.cs (200 lines)
   - src/Middleware/AuthMiddleware.cs (80 lines)

4. Test Coverage
   - AuthController.Tests.cs (15 test cases)
   - OAuthService.Tests.cs (12 test cases)

[View Full Plan]

[Approve]  [Refine]  [Reject & Regenerate]
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

**You have three options**:

1. **Approve** → PRFactory implements the code (if auto-implementation is enabled)
2. **Refine** → Provide specific improvements while keeping the overall structure
3. **Reject & Regenerate** → Completely restart planning with new approach

---

### Step 6: Code Implementation (Optional)
**If auto-implementation is enabled** for your tenant:

1. Code is generated based on approved plan
2. Changes are committed to a feature branch
3. Pull request is created automatically
4. PR link is posted to your ticket

**You'll see**:
```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Pull Request Created
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

PR #42: Add user authentication (PROJ-123)
Branch: feature/PROJ-123-auth → main

Files Changed: 7
Lines: +450 -12

[View PR on GitHub]

Status: Ready for Review
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

**Next steps**: Review the PR in GitHub/Bitbucket/Azure DevOps and merge when ready.

---

## Understanding Workflow States

As your ticket progresses, you'll see different states:

| State | What It Means | What You Can Do |
|-------|---------------|-----------------|
| **Pending** | Ticket created, workflow not started | Click "Start Workflow" |
| **AwaitingAnswers** | Questions posted, waiting for your input | Submit answers |
| **TicketUpdateGenerated** | Refined ticket ready for review | Approve or reject |
| **PlanUnderReview** | Implementation plan ready | Approve, refine, or reject |
| **PRCreated** | Pull request created | Review and merge PR |
| **Completed** | Workflow finished | View results |
| **Failed** | Something went wrong | View error message |

---

## Next Steps

Now that you understand the basics:
- **[Learn about the Three Workflow Phases](./02-workflow-phases.md)** - Deeper dive into each phase
- **[UI Operations Guide](./03-ui-operations.md)** - All available actions explained
- **[Configuration Guide](./04-configuration.md)** - Customize PRFactory for your needs
- **[FAQ & Troubleshooting](./05-faq.md)** - Common questions answered

---

## Quick Reference Card

**Starting a Workflow**:
1. Create ticket → Open ticket detail → Click "Start Workflow"

**Answering Questions**:
1. Fill out question form → Click "Submit Answers"

**Reviewing Plans**:
1. Read plan → Choose action:
   - **Approve** - Proceed to implementation
   - **Refine** - Provide specific feedback
   - **Reject** - Start over

**Monitoring Progress**:
1. Check ticket detail page for current state
2. Real-time updates appear automatically (via SignalR)

**Getting Help**:
1. Check workflow state message for guidance
2. View error messages if workflow fails
3. Contact your admin for configuration issues
