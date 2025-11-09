# Strategic Implementation Plan: PRFactory (v2)

**Date:** 2025-11-09
**From:** Product Owner
**Subject:** Detailed plan for remaining strategic points, based on our CLI agent architecture and focus on a standalone web-based system.

---

## 1. Core Principle: From Strategy to CLI

Our architecture, built on CLI agents (e.g., `cli plan`, `cli code`, `cli test`, `cli review`), is the foundation for implementing our strategy. The PRFactory Web UI acts as the central orchestration hub, and the CLI agents are the specialized workers.

**Every strategic goal must be mapped to a function in these CLI tools or the Web UI that orchestrates them.** This plan details how to implement our high-level epics using this architecture.

---

## 2. Remaining Epics & Implementation Steps

The following **Prio 1** and **Prio 2** epics are the core focus for the next iteration.

---

### Epic 1 (Prio 1): "Team Review" (From Single-Player to Multi-Player)

**Strategic Goal:** Address our primary weakness ("Single-Player") by making the Phase 2 (Planning) review a collaborative, team-based function.

#### Implementation via CLI & UI:

#### 1. Web UI - Commenting System

**What:** Implement a threaded commenting and discussion field in the Web UI, allowing the team to discuss the AI-generated plan artifacts from Phase 2.

**Actionable Tasks:**

- **Database:** Design and implement a new table (e.g., `PlanComments`) with fields for:
  - `CommentID`
  - `PlanID` (foreign key)
  - `UserID`
  - `ParentCommentID` (for threading)
  - `Content`
  - `Timestamp`
  - `Status` (e.g., Active, Resolved)

- **Backend API:** Create endpoints for CRUD operations:
  - `POST /api/plans/{id}/comments`
  - `GET /api/plans/{id}/comments`
  - `PUT /api/comments/{id}`
  - `DELETE /api/comments/{id}`

- **Frontend UI:** Build the comment thread component (e.g., in React, Vue, or Blazor) that:
  - Fetches and displays comments
  - Allows replying to specific threads
  - Permits editing/deleting

- **Feature:** Implement @mentions to notify other team members (future iteration, but plan schema for it)

#### 2. Web UI - Approval Workflow

**What:** A plan (Phase 2) must have a formal state machine, not just a binary "approved" status. The statuses should be: **Draft**, **PendingReview**, **ChangesRequested**, **Approved**.

**Actionable Tasks:**

- **Database:** Add a `Status` column (string or enum) to the `Plans` table

- **Backend API:** Create endpoints to trigger status transitions:
  - `POST /api/plans/{id}/request-review`
  - `POST /api/plans/{id}/request-changes`
  - `POST /api/plans/{id}/approve`

- **Business Logic:** Implement logic to enforce workflow rules:
  - Only a team lead can set status to `Approved`
  - `ChangesRequested` requires a comment

- **Frontend UI:**
  - Clearly display the plan's current status
  - Provide buttons (e.g., "Request Review", "Approve Plan") that are visible based on user role and current status
  - The "Implement" (Phase 3) action must be locked until the plan status is `Approved`

#### 3. Agent: `cli review` (Strategic Expansion - CodeRabbit Inspiration)

**What:** The `cli review` agent is currently conceptualized for code. We must expand its capability to review plans and to validate code against plans.

**Actionable Tasks:**

**Function 1 (Plan Review):**

- **Command:** `cli review --plan <plan_file.md> --prompt "Is this plan secure and scalable?"`

- **Logic:** The agent must be capable of ingesting a plan artifact and a natural language prompt

- **Prompts:** Develop a new set of system prompts for this agent, e.g.:
  - `review_plan_security_prompt.txt`
  - `review_plan_completeness_prompt.txt`

- **UI Integration:** The Web UI should allow a user to trigger this agent against the current plan with pre-defined or custom prompts (e.g., a "Run Security Analysis" button)

**Function 2 (Code-vs-Plan Validation):**

- **Command:** `cli review --validate --plan-dir <plan/> --diff <git_diff.patch>`

- **Logic:** This is a critical and complex task. The agent must be architected to:
  1. Ingest all artifacts from the approved plan directory (e.g., `plan/05-implementation_steps.md`)
  2. Ingest the git diff of the newly implemented code (see Epic 4)
  3. Use a sophisticated "diff analysis" prompt to compare them

- **Prompt (Example):** "You are a meticulous reviewer. Your task is to validate if the provided code diff successfully implements all functional requirements from the approved plan. List all deviations, missed requirements, and any code that was written but not specified in the plan."

- **UI Integration:** The output of this validation must be displayed in the Web UI as a final quality gate before the PR is created

---

### Epic 2 (Prio 1): "AI Agnosticism"

**Strategic Goal:** Remove our technical debt and vendor lock-in to Claude AI. This is a critical, non-negotiable refactor.

#### Implementation via CLI:

#### 1. Global CLI Parameter Factory

**What:** All agents (`plan`, `code`, `test`, `review`) must accept global parameters to define the AI provider and model. E.g., `--provider openai` and `--model gpt-4o`.

**Actionable Tasks:**

- **CLI Framework:** Implement a base options class (e.g., `BaseAgentOptions`) in the CLI framework (System.CommandLine or similar) that includes:
  - `--provider` (enum: anthropic, openai, google, etc.)
  - `--model` (string)

- **Dependency Injection:** Create an `ILlmProvider` interface and a concrete `LlmProviderFactory`. This factory will:
  - Receive the CLI options
  - Be responsible for instantiating the correct, configured client (e.g., `AnthropicApiClient` or `OpenApiClient`)

- **Refactor:** All agents must be refactored to consume the `ILlmProvider` interface, not a specific client

#### 2. Externalized Prompt Templates

**What:** Agent logic (prompts) must not be hard-coded in C# strings. They must be loaded from external files.

**Why:** Different models (e.g., Claude vs. GPT) have different prompting requirements (e.g., system prompts, user/assistant roles).

**Actionable Tasks:**

- **Directory Structure:** Create a standard directory structure for prompts:
  ```
  /prompts/{agent_name}/{provider_name}/system.txt
  /prompts/{agent_name}/{provider_name}/user_template.md
  ```

- **Prompt Loader Service:** Create a `PromptLoaderService` that, given the agent name and provider, reads the correct text files from disk

- **Templating:** Use a simple templating engine (e.g., Scriban or even `string.Replace`) to inject variables (like user instructions or file content) into the loaded prompt templates

---

### Epic 3 (Prio 2): "Deeper Planning Phase" (Inspiration: MetaGPT)

**Strategic Goal:** Make our Phase 2 (Planning) our core value proposition. The generated plan must be significantly more valuable than a simple implementation outline.

#### Implementation via CLI:

#### 1. Enhance `cli plan` - Multi-Artifact Generation

**What:** The `cli plan` agent must evolve from a single-prompt agent to a multi-step orchestrator that simulates a full development team, generating a directory of artifacts.

**Example Output Directory `plan/`:**
```
01-user_stories.md
02-api_design.yml (OpenAPI spec snippet)
03-database_schema.sql (DDL for new tables/changes)
04-test_cases.md (A list of human-readable test cases that must pass)
05-implementation_steps.md (The detailed, technical coding plan)
```

**Actionable Tasks:**

- **Orchestrator Logic:** Refactor `cli plan` to be an orchestrator. It will execute a "Chain of Thought" or "multi-agent" sequence:

  - **Step 1 (PM Persona):** Call LLM with "Product Manager" persona to analyze the issue and generate `01-user_stories.md`

  - **Step 2 (Architect Persona):** Call LLM with "Software Architect" persona (providing user stories as context) to generate `02-api_design.yml` and `03-database_schema.sql`

  - **Step 3 (QA Persona):** Call LLM with "QA Engineer" persona (providing stories/API as context) to generate `04-test_cases.md`

  - **Step 4 (Tech Lead Persona):** Call LLM with "Tech Lead" persona (providing all previous artifacts as context) to generate the final `05-implementation_steps.md`

- **UI Integration:** The Web UI must be updated to display all these artifacts, not just a single file

#### 2. Activate `cli revise` - Iterative Planning

**What:** The AI-generated plan is a draft. A human must be able to revise it using natural language. This is the core of "AI assists, humans decide".

**Flow:**
1. `cli plan --issue "..."` → generates `plan/`
2. Human reviews in Web UI. Finds a flaw.
3. User enters instruction: "You forgot to add rate limiting to the new API endpoint."
4. Web UI calls `cli revise --instruction "..."`
5. `cli revise` agent updates the relevant files in the `plan/` directory

**Actionable Tasks:**

- **Build `cli revise`:** Create the new agent command

- **Context Loading:** The agent must load the full context of the `plan/` directory

- **Intelligent Update Logic:** The agent's prompt must instruct it to only update the relevant files. E.g., the instruction above should modify `02-api_design.yml` and `05-implementation_steps.md`, but leave the other files untouched

- **UI Integration:** Build the UI input field for sending the revision instruction. The UI must then refresh to show the updated plan artifacts

---

### Epic 4 (Prio 2): "Core: Web-based Git Visualization" (Inspiration: Warp)

**Strategic Goal:** Fulfill the "standalone web-based system" vision. The user must not be forced to leave our UI to see the results of the implementation (Phase 3). We will build a rich diff viewer inside our web app.

#### Implementation via CLI & UI:

#### 1. Agent: `cli code` - Structured Output Artifact

**What:** The `cli code` agent must not just (or even) push to remote. Its primary output for the web UI should be a structured artifact representing its work, specifically a `diff.patch` file.

**Actionable Tasks:**

- **CLI Logic:** After `cli code` runs and before it commits (or as part of its local commit), it must run `git diff` against HEAD and save the output to a predictable location, e.g., `workspace/task-123/diff.patch`

#### 2. Web UI - Diff Viewer Component

**What:** The Web UI (in the Phase 3 view) must fetch and display this `diff.patch` file in a clean, professional, side-by-side or unified diff format.

**Actionable Tasks:**

- **Backend API:** Create an endpoint `GET /api/tasks/{id}/diff` that retrieves the patch file from the workspace

- **Frontend Library:** Integrate a JavaScript-based diff viewing library (e.g., **diff2html** is excellent for this)

- **Frontend UI:** Build a component that:
  - Fetches the diff content
  - Passes it to the diff2html library for rendering
  - This provides the "Warp-like" clean presentation of changes

#### 3. Web UI - Final "Create Pull Request" Action

**What:** After the human user has reviewed the AI-generated plan (Phase 2) AND the AI-generated diff (Phase 3) inside our UI, they give the final approval.

**Actionable Tasks:**

- **Frontend UI:** Add a final "Approve and Create Pull Request" button

- **Backend API:** This button calls a new endpoint, e.g., `POST /api/tasks/{id}/create-pr`

- **Agent/Logic:** This backend endpoint executes the final CLI commands:
  1. `git push` (pushing the agent's work to the remote branch)
  2. Call the GitHub/GitLab API to create the Pull Request, automatically populating it with the plan artifacts

- **Result:** This keeps the user inside the PRFactory ecosystem from start to finish

---

## 3. Summary: Next Steps

Our CLI architecture is the correct path. The focus for the next iteration is:

1. **Refactor:** Make all agents AI-agnostic (Prio 1)
2. **Build Out:** Implement the collaborative Team Review features (Web UI + `cli review`) (Prio 1) and the `cli revise` agent
3. **Deepen:** Make the `cli plan` agent a multi-artifact orchestrator (Prio 2)
4. **Enclose:** Build the Web UI Diff Viewer to create a complete, standalone workflow (Prio 2)

---

**Related Documents:**
- [CLI LLM Providers Plan](./CLI_LLM_PROVIDERS.md)
- [Microsoft Agent Framework Integration](./MICROSOFT_AGENT_FRAMEWORK_INTEGRATION.md)
- [Implementation Status](../IMPLEMENTATION_STATUS.md)
- [Roadmap](../ROADMAP.md)
