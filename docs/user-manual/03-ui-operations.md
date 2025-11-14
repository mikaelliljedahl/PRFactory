# PRFactory User Guide: UI Operations

This guide explains every operation available in the PRFactory user interface.

---

## Ticket List Page (`/tickets`)

Your central hub for managing all tickets.

### Features

**Filter & Search**:
```
┌────────────────────────────────────────────────────┐
│  Tickets                              [Create New] │
├────────────────────────────────────────────────────┤
│                                                     │
│  Filter:  [All States ▼]  [All Repos ▼]  [🔍]    │
│                                                     │
│  ┌──────────────────────────────────────────────┐ │
│  │ PROJ-123  Add user authentication            │ │
│  │ Status: PlanUnderReview  •  2 hours ago      │ │
│  │ Repository: MainApp                           │ │
│  └──────────────────────────────────────────────┘ │
│                                                     │
│  ┌──────────────────────────────────────────────┐ │
│  │ PROJ-124  Fix payment processing bug         │ │
│  │ Status: AwaitingAnswers  •  1 day ago        │ │
│  │ Repository: PaymentService                    │ │
│  └──────────────────────────────────────────────┘ │
│                                                     │
│  Showing 1-20 of 45    [< Prev] [1] [2] [Next >] │
└────────────────────────────────────────────────────┘
```

**Available Filters**:
- **State**: Pending, AwaitingAnswers, PlanUnderReview, PRCreated, Completed, Failed
- **Repository**: Filter by specific repository
- **Source**: WebUI, Jira, Azure DevOps, GitHub Issues
- **Search**: Search by ticket number, title, description

**Actions**:
- **Click ticket** → Navigate to detail page
- **Create New** → Create new ticket

---

## Ticket Detail Page (`/tickets/{id}`)

The detail page **dynamically changes** based on the ticket's current workflow state.

### State: Pending

**When you see this**: Ticket created but workflow not started

```
┌────────────────────────────────────────────────────┐
│  PROJ-123: Add user authentication                 │
│  Status: Pending                                   │
├────────────────────────────────────────────────────┤
│                                                     │
│  This ticket is ready to start.                   │
│                                                     │
│  [Start Workflow]                                  │
│                                                     │
└────────────────────────────────────────────────────┘
```

**Available Actions**:
- **Start Workflow** → Triggers refinement phase
- **Edit Ticket** → Modify title/description
- **Delete Ticket** → Remove ticket (if not started)

---

### State: AwaitingAnswers

**When you see this**: Questions generated, waiting for your input

```
┌────────────────────────────────────────────────────┐
│  PROJ-123: Add user authentication                 │
│  Status: Awaiting Your Answers                     │
├────────────────────────────────────────────────────┤
│                                                     │
│  Clarifying Questions                             │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│                                                     │
│  1. Which OAuth providers should we support?      │
│     ☐ Google                                      │
│     ☐ GitHub                                      │
│     ☐ Microsoft                                   │
│                                                     │
│  2. Session storage method?                       │
│     ○ PostgreSQL                                  │
│     ○ Redis                                       │
│     ○ Both                                        │
│                                                     │
│  3. Password complexity requirements?             │
│     [Text area...]                                │
│                                                     │
│  [Submit Answers]                                  │
│                                                     │
└────────────────────────────────────────────────────┘
```

**Available Actions**:
- **Submit Answers** → Processes answers, generates refined ticket
- **Save Draft** → Save partial answers for later
- **Skip Question** → Mark question as non-applicable (not recommended)

**Tips**:
- ✅ Answer all questions for best results
- ✅ Be specific and detailed
- ✅ Reference existing code patterns when relevant

---

### State: TicketUpdateGenerated

**When you see this**: Refined ticket description ready for review

```
┌────────────────────────────────────────────────────┐
│  PROJ-123: Add user authentication                 │
│  Status: Ticket Update Ready for Review            │
├────────────────────────────────────────────────────┤
│                                                     │
│  Refined Ticket Description                       │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│                                                     │
│  📋 Original:                                     │
│  "Add user authentication"                        │
│                                                     │
│  📝 Refined:                                      │
│  "Implement JWT-based user authentication         │
│   system with Google and GitHub OAuth providers.  │
│                                                     │
│   Requirements:                                    │
│   • Authentication Method: JWT tokens             │
│   • Session Storage: PostgreSQL                   │
│   • Password Hashing: BCrypt (cost 12)           │
│   ..."                                            │
│                                                     │
│  [View Full Description]                          │
│                                                     │
│  [Approve]  [Reject & Regenerate]                 │
│                                                     │
└────────────────────────────────────────────────────┘
```

**Available Actions**:

**Approve**:
- Accepts refined description
- Moves to planning phase
- Posts refined description to ticket system

**Reject & Regenerate**:
- Opens rejection reason form
- AI regenerates description with feedback
- Maximum 3 regeneration attempts

---

### State: PlanUnderReview

**When you see this**: Implementation plan ready for review

```
┌────────────────────────────────────────────────────┐
│  PROJ-123: Add user authentication                 │
│  Status: Plan Under Review                         │
├────────────────────────────────────────────────────┤
│                                                     │
│  Implementation Plan                              │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│                                                     │
│  📊 Summary:                                      │
│  • Database Tables: 1 new (Users)                 │
│  • API Endpoints: 4 (/auth/*)                     │
│  • New Files: 6 (550 lines)                       │
│  • Test Cases: 27                                 │
│                                                     │
│  📁 Plan File: plan/PROJ-123.md                   │
│  🔗 Git Branch: plan/PROJ-123                     │
│                                                     │
│  [View Full Plan]  [Download Plan]                │
│                                                     │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│  Team Reviewers                                   │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│                                                     │
│  Required:                                        │
│  • Alice (Tech Lead) ✅ Approved                  │
│  • Bob (Senior Engineer) ⏳ Pending               │
│                                                     │
│  [Assign Reviewers]  [Add Comment]                │
│                                                     │
│  ⚠️ All required reviewers must approve          │
│                                                     │
│  [Approve]  [Refine]  [Reject & Regenerate]       │
│                                                     │
└────────────────────────────────────────────────────┘
```

**Available Actions**:

**1. Approve**
- Accepts plan as-is
- Checks team reviewer approvals (if assigned)
- Proceeds to implementation phase (if enabled)
- Or completes workflow (if auto-implementation disabled)

**2. Refine**
- Opens refinement instructions form
- Keeps overall plan structure
- Incorporates specific improvements
- No retry limit (can refine indefinitely)

**Example Refinement**:
```
┌────────────────────────────────────────────────────┐
│  Refinement Instructions                           │
├────────────────────────────────────────────────────┤
│                                                     │
│  Please update the plan to include:               │
│                                                     │
│  [Text area:                                       │
│   1. Add password reset endpoint                  │
│   2. Include email verification flow              │
│   3. Add rate limiting details                    │
│  ]                                                 │
│                                                     │
│  [Submit Refinement]  [Cancel]                     │
│                                                     │
└────────────────────────────────────────────────────┘
```

**3. Reject & Regenerate**
- Opens rejection reason form
- Discards current plan completely
- Generates new approach from scratch
- Maximum 5 rejection attempts

**Example Rejection**:
```
┌────────────────────────────────────────────────────┐
│  Rejection Reason                                  │
├────────────────────────────────────────────────────┤
│                                                     │
│  Why is this plan unacceptable?                   │
│                                                     │
│  [Text area:                                       │
│   This plan uses MongoDB but our stack is         │
│   PostgreSQL. Please regenerate using Entity      │
│   Framework Core with our existing repository     │
│   pattern.                                        │
│  ]                                                 │
│                                                     │
│  ☑ Regenerate completely from scratch             │
│                                                     │
│  [Submit Rejection]  [Cancel]                      │
│                                                     │
└────────────────────────────────────────────────────┘
```

**4. Assign Reviewers**
```
┌────────────────────────────────────────────────────┐
│  Assign Plan Reviewers                             │
├────────────────────────────────────────────────────┤
│                                                     │
│  Required Reviewers (must approve):               │
│  ☑ Alice (Tech Lead)                              │
│  ☑ Bob (Senior Engineer)                          │
│  ☐ Carol (Architect)                              │
│                                                     │
│  Optional Reviewers (feedback only):              │
│  ☑ Dave (QA Lead)                                 │
│  ☐ Eve (Product Manager)                          │
│                                                     │
│  [Save Reviewers]  [Cancel]                        │
│                                                     │
└────────────────────────────────────────────────────┘
```

**5. Add Comment**
- Add discussion comments to plan
- Mention other reviewers with @username
- Comments visible to all team members

---

### State: PRCreated

**When you see this**: Pull request created, ready for human review

```
┌────────────────────────────────────────────────────┐
│  PROJ-123: Add user authentication                 │
│  Status: Pull Request Created                      │
├────────────────────────────────────────────────────┤
│                                                     │
│  Pull Request #42                                 │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│                                                     │
│  🔗 https://github.com/your-org/repo/pull/42      │
│                                                     │
│  Branch: feature/PROJ-123-auth → main            │
│                                                     │
│  📊 Changes:                                      │
│  • Files: 8 changed (+550, -12)                   │
│  • Commits: 3                                     │
│  • Tests: 27 added                                │
│                                                     │
│  Status Checks:                                   │
│  ✅ Build: Success                                │
│  ✅ Tests: 27/27 passing                          │
│  ✅ Coverage: 87%                                 │
│  ✅ Code Quality: No issues                       │
│                                                     │
│  [View PR on GitHub]                              │
│                                                     │
└────────────────────────────────────────────────────┘
```

**Available Actions**:
- **View PR on GitHub** → Opens PR in external platform
- **View Branch** → Opens git branch in repository
- **View Diff** → Shows file changes inline

**Next Steps**:
1. Review code in GitHub/Bitbucket/Azure DevOps
2. Request reviews from team members
3. Run additional manual tests if needed
4. Merge when ready

---

### State: Completed

**When you see this**: Workflow finished successfully

```
┌────────────────────────────────────────────────────┐
│  PROJ-123: Add user authentication                 │
│  Status: Completed ✅                              │
├────────────────────────────────────────────────────┤
│                                                     │
│  Workflow Summary                                 │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│                                                     │
│  ⏱ Duration: 45 minutes                          │
│  📅 Completed: 2025-01-15 14:30 UTC               │
│                                                     │
│  Phase 1: Refinement ✅                           │
│  • Questions: 3 answered                          │
│  • Ticket refined and approved                    │
│                                                     │
│  Phase 2: Planning ✅                             │
│  • Plan approved (0 refinements)                  │
│  • Team reviewers: 2/2 approved                   │
│                                                     │
│  Phase 3: Implementation ✅                       │
│  • Code implemented                               │
│  • PR #42 created                                 │
│  • Link: https://github.com/.../pull/42          │
│                                                     │
│  [View Timeline]  [View PR]  [Close Ticket]       │
│                                                     │
└────────────────────────────────────────────────────┘
```

**Available Actions**:
- **View Timeline** → See complete workflow history
- **View PR** → Open pull request
- **Close Ticket** → Mark as done in ticket system
- **Archive** → Move to archived tickets

---

### State: Failed

**When you see this**: Something went wrong

```
┌────────────────────────────────────────────────────┐
│  PROJ-123: Add user authentication                 │
│  Status: Failed ❌                                 │
├────────────────────────────────────────────────────┤
│                                                     │
│  Error Details                                    │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│                                                     │
│  ⚠️ Planning phase failed after 5 retry attempts  │
│                                                     │
│  Last Error:                                      │
│  "Plan exceeded maximum rejection limit (5).      │
│   Please create a new ticket or contact support." │
│                                                     │
│  Failed At: Planning Phase (Step 3 of 5)          │
│  Timestamp: 2025-01-15 14:30 UTC                  │
│                                                     │
│  [View Error Log]  [Retry Workflow]  [Contact Support] │
│                                                     │
└────────────────────────────────────────────────────┘
```

**Available Actions**:
- **View Error Log** → See detailed error messages and stack trace
- **Retry Workflow** → Attempt workflow again from beginning
- **Contact Support** → Open support ticket with error details

**Common Failure Reasons**:
- **Max rejections reached** (5 plan rejections)
- **Git operation failed** (permission issues, branch conflicts)
- **AI provider error** (rate limit, timeout, service down)
- **Repository not accessible** (authentication, network issue)

---

## Agent Prompt Management (`/agent-prompts`)

Manage AI prompt templates for different agent types.

### Browse Prompts

```
┌────────────────────────────────────────────────────┐
│  Agent Prompt Templates              [Create New]  │
├────────────────────────────────────────────────────┤
│                                                     │
│  Filter:  [All Categories ▼]  [🔍 Search]         │
│                                                     │
│  System Templates (Read-Only)                     │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│                                                     │
│  ┌──────────────────────────────────────────────┐ │
│  │ 🔵 code-implementation-specialist            │ │
│  │ Category: Implementation                      │ │
│  │ Model: claude-sonnet-4-5-20250929            │ │
│  │ [View] [Copy to My Templates]                │ │
│  └──────────────────────────────────────────────┘ │
│                                                     │
│  My Templates (Tenant-Specific)                   │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│                                                     │
│  ┌──────────────────────────────────────────────┐ │
│  │ 🟢 custom-code-reviewer                      │ │
│  │ Category: Review                              │ │
│  │ Model: gpt-4o                                │ │
│  │ [Edit] [Preview] [Delete]                    │ │
│  └──────────────────────────────────────────────┘ │
│                                                     │
└────────────────────────────────────────────────────┘
```

### Create/Edit Template

```
┌────────────────────────────────────────────────────┐
│  Edit Agent Prompt Template                        │
├────────────────────────────────────────────────────┤
│                                                     │
│  Name: custom-code-reviewer                       │
│  Category: [Review ▼]                             │
│  Recommended Model: [gpt-4o ▼]                    │
│                                                     │
│  System Prompt:                                   │
│  [Text area with prompt content...]              │
│                                                     │
│  User Prompt Template (Handlebars):              │
│  [Text area with template...]                    │
│                                                     │
│  Available Variables:                             │
│  • {{ticket_number}}                              │
│  • {{pull_request_url}}                           │
│  • {{file_changes}}                               │
│  [View all variables]                             │
│                                                     │
│  [Preview with Sample Data]  [Save]  [Cancel]     │
│                                                     │
└────────────────────────────────────────────────────┘
```

---

## Configuration Pages

### Agent Configuration (`/admin/agent-configuration`)

Configure which AI model each agent type uses.

```
┌────────────────────────────────────────────────────┐
│  Agent LLM Provider Configuration                  │
├────────────────────────────────────────────────────┤
│                                                     │
│  Analysis Agents                                  │
│  Provider: [Claude Haiku ▼]  (Fast, cheap)        │
│  Model:    claude-3-5-haiku-20241022              │
│                                                     │
│  Planning Agents                                  │
│  Provider: [Claude Sonnet ▼]  (Balanced)          │
│  Model:    claude-sonnet-4-5-20250929             │
│                                                     │
│  Implementation Agents                            │
│  Provider: [Claude Sonnet ▼]  (Best for coding)   │
│  Model:    claude-sonnet-4-5-20250929             │
│                                                     │
│  Code Review Agents                               │
│  Provider: [GPT-4o ▼]  (Different perspective)    │
│  Model:    gpt-4o                                 │
│                                                     │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│  Code Review Settings                             │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│                                                     │
│  ☑ Enable Auto Code Review                       │
│  Max Review Iterations: [3]                       │
│  ☐ Auto-approve if no issues found                │
│  ☑ Require human approval after review            │
│                                                     │
│  [Save Configuration]                              │
│                                                     │
└────────────────────────────────────────────────────┘
```

---

## Admin UI (`/admin/*`)

**Access**: Owner and Admin roles only

The Admin UI provides self-service configuration for repositories, LLM providers, tenant settings, and user management.

### Repository Management (`/admin/repositories`)

Manage Git repositories for PRFactory to use.

**Features**:
- Add repositories (GitHub, Bitbucket, Azure DevOps, GitLab)
- Test repository connections before saving
- Edit repository settings
- View repository statistics
- Encrypted credential storage (AES-256-GCM)

**Actions**:
- **Create Repository**: Add new Git repository with connection testing
- **Edit Repository**: Update access tokens, default branch
- **Test Connection**: Verify credentials work
- **Deactivate**: Soft delete repository

### LLM Provider Configuration (`/admin/settings/llm-providers`)

Configure AI providers for different agent types.

**Supported Provider Types**:
1. **Anthropic Native** - OAuth 2.0 authentication
2. **Z.ai Unified API** - API key with multi-model support
3. **Minimax M2** - API key authentication
4. **OpenRouter** - API key with 100+ models
5. **Together AI** - API key authentication
6. **Custom** - Fully configurable endpoint

**Features**:
- Multi-step wizard for adding providers
- Connection testing before save
- Default provider management
- Model override configuration (JSON)
- Encrypted API keys/tokens

**Actions**:
- **Create Provider**: Multi-step wizard with type selection
- **Edit Provider**: Update configuration, model overrides
- **Set as Default**: Make provider the tenant default
- **Test Connection**: Verify provider credentials

### Tenant Settings (`/admin/settings/general`)

Configure tenant-wide workflow behavior.

**Settings Tabs**:

**1. General** (Read-only)
- Tenant information
- User statistics
- Repository and provider counts

**2. Workflow Settings**
- Auto-implementation after plan approval
- Max retries for failed operations
- API timeout settings
- Verbose logging toggle
- Allowed repositories whitelist

**3. Code Review Settings**
- Enable/disable automated code review
- Max code review iterations
- Auto-approve if no issues
- Security scan requirements

**4. LLM Provider Assignment**
- Assign providers to agent roles:
  - Analysis agents
  - Planning agents
  - Implementation agents
  - Code Review agents
- Per-workflow provider overrides

**Access**: Only Owner role can edit settings (Admin/Member can view read-only)

### User Management (`/admin/settings/users`)

Manage user roles and permissions.

**User Roles**:
- **Owner**: Full admin access (can manage everything)
- **Admin**: Repository & provider management (cannot change settings/roles)
- **Member**: Read-only access to admin UI
- **Viewer**: Read-only access (no admin UI)

**Features**:
- Auto-provisioning from OAuth (first user becomes Owner)
- Search and filter users by role/status
- Change user roles (Owner only)
- Activate/deactivate users
- User statistics and activity

**Business Rules**:
- ❌ Cannot remove the last Owner from a tenant
- ❌ Cannot demote yourself if you are the last Owner
- ✅ Owner can assign multiple Owners for redundancy

**Actions**:
- **Edit User Role**: Change role with validation
- **Activate/Deactivate**: Toggle user active status
- **View Statistics**: See user activity metrics

---

## Real-Time Updates

All pages use **SignalR** for real-time updates. You'll see changes automatically without refreshing:

**Indicators**:
- 🔵 **Blue dot** = Update in progress
- ✅ **Green checkmark** = Update complete
- ❌ **Red X** = Update failed

**Events that trigger updates**:
- Workflow state changes
- Question generation complete
- Plan generation complete
- PR creation complete
- Team reviewer approval/rejection
- Error occurrences

---

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl + N` | Create new ticket |
| `Ctrl + /` | Focus search box |
| `Esc` | Close modal/dialog |
| `Enter` | Submit form (when in text input) |
| `Ctrl + S` | Save draft (when editing) |

---

## Next Steps

- **[Configuration Guide](./04-configuration.md)** - Customize PRFactory settings
- **[FAQ & Troubleshooting](./05-faq.md)** - Common questions and solutions
- **[Getting Started](./01-getting-started.md)** - Return to basics
