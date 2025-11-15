# Documentation Update Prompts

**Purpose:** Generic, reusable prompts for updating technical documentation across any software project.

---

## Overview

This directory contains prompt templates for conducting comprehensive documentation reviews and updates using AI agents. The prompts guide agents through:

1. **Exploration** - Understanding recent code changes and features
2. **Gap Analysis** - Identifying missing, outdated, or inconsistent documentation
3. **Execution** - Updating all affected documentation files
4. **Verification** - Ensuring accuracy and consistency

---

## Directory Structure

```
/prompts/documentation/
├── anthropic/
│   ├── system.txt             # 63 lines - Detailed documentation specialist persona
│   └── user_template.hbs      # 178 lines - Comprehensive doc update request
├── openai/
│   ├── system.txt             # 28 lines - Concise documentation specialist persona
│   └── user_template.hbs      # 64 lines - Structured doc update request
├── google/
│   ├── system.txt             # 47 lines - Balanced documentation specialist persona
│   └── user_template.hbs      # 127 lines - Detailed doc update request
└── README.md                  # This file (610+ lines)

**Total Files:** 7 (3 providers × 2 files each + 1 README)
**Total Lines:** 507+ lines of prompt templates + 610+ lines documentation
```

---

## When to Use

Use these prompts when:

- **After completing features/epics** - Documentation needs to reflect new implementations
- **Major refactoring** - Architecture or design docs need updating
- **Regular maintenance** - Periodic reviews to catch drift between code and docs
- **Onboarding new projects** - Initial documentation audit and cleanup
- **Before releases** - Ensuring all documentation is current

**Signs documentation needs updating:**
- Team mentions "the docs are wrong"
- New features not documented in user guides
- Architecture docs describe old structure
- Database schema missing new entities
- Naming conventions are inconsistent (UPPERCASE mixed with lowercase)
- Roadmap still shows completed features as "planned"
- Cross-references are broken (files renamed but links not updated)

---

## Template Variables

### Project Context
- `project_name` - Name of the project (e.g., "PRFactory", "MyApp")
- `repository_path` - Absolute path to repository root
- `docs_root_path` - Path to documentation folder (e.g., "/docs", "/documentation")

### Recent Changes
- `recent_epics[]` - Array of recently completed features/epics
  - `name` - Epic/feature name
  - `completion_date` - When it was completed
  - `description` - What was delivered
  - `deliverables` - Key outputs
  - `files_changed_count` - Number of files modified
- `recent_commits[]` - Recent git commits (optional)
  - `hash` - Commit SHA
  - `message` - Commit message
  - `date` - Commit date
- `commit_lookback_days` - How many days of commits to analyze

### Documentation Files
- `core_docs[]` - Core strategic documentation
  - `file_name` - Document name
  - `path` - Full path to file
  - `purpose` - What the document covers
  - `last_updated` - Last update date
  - `known_issues` - Known problems (optional)
- `technical_docs[]` - Technical documentation (schemas, APIs, architecture)
- `user_docs[]` - User-facing documentation (guides, manuals)

### Known Issues
- `known_issues[]` - Pre-identified documentation problems
  - `title` - Issue summary
  - `file_path` - Which file is affected
  - `issue_type` - "outdated", "missing", "incorrect", "inconsistent"
  - `description` - Details of the issue
  - `priority` - "critical", "important", "nice-to-have"
  - `suggested_fix` - Recommended solution (optional)

### Naming Conventions
- `naming_standards[]` - Project-specific naming rules
  - `category` - What this applies to (e.g., "Root docs", "Architecture docs")
  - `convention` - Naming pattern (e.g., "UPPERCASE.md", "kebab-case.md")
  - `example` - Example filename

### Focus Areas
- `focus_areas[]` - Specific areas to prioritize
  - `name` - Area name
  - `description` - What to check

### Additional
- `additional_instructions` - Any special instructions or context

---

## Usage Example

### Basic Usage (Comprehensive Review)

```javascript
const documentationUpdateRequest = {
  project_name: "PRFactory",
  repository_path: "C:\\code\\github\\PRFactory",
  docs_root_path: "C:\\code\\github\\PRFactory\\docs",

  recent_epics: [
    {
      name: "Epic 05: Agent System Foundation",
      completion_date: "2025-11-15",
      description: "Implemented autonomous agent framework with 22 tools",
      deliverables: "22 tools, graph-based orchestration, AG-UI integration",
      files_changed_count: 145
    },
    {
      name: "Epic 08: System Architecture Cleanup",
      completion_date: "2025-11-14",
      description: "Consolidated 3 projects into 1, implemented CSS isolation",
      deliverables: "Single-project architecture, 100% CSS isolation, server-side pagination",
      files_changed_count: 87
    }
  ],

  core_docs: [
    {
      file_name: "ARCHITECTURE.md",
      path: "C:\\code\\github\\PRFactory\\docs\\ARCHITECTURE.md",
      purpose: "System architecture and design patterns",
      last_updated: "2025-11-10"
    },
    {
      file_name: "IMPLEMENTATION_STATUS.md",
      path: "C:\\code\\github\\PRFactory\\docs\\IMPLEMENTATION_STATUS.md",
      purpose: "Feature status tracking and completion",
      last_updated: "2025-11-14"
    },
    {
      file_name: "DATABASE_SCHEMA.md",
      path: "C:\\code\\github\\PRFactory\\docs\\DATABASE_SCHEMA.md",
      purpose: "Entity relationships and database design",
      last_updated: "2025-11-01",
      known_issues: "Missing entities from Epic 05 and Epic 07"
    }
  ]
};

// Render template with data
const template = Handlebars.compile(userTemplateContent);
const userPrompt = template(documentationUpdateRequest);

// Send to AI with system prompt
const response = await sendToAI(systemPrompt, userPrompt);
```

### Targeted Update (Known Issues)

```javascript
const targetedUpdateRequest = {
  project_name: "MyApp",
  repository_path: "/home/user/myapp",
  docs_root_path: "/home/user/myapp/docs",

  known_issues: [
    {
      title: "BLAZOR_TESTING_GUIDE.md has wrong project path",
      file_path: "/home/user/myapp/docs/BLAZOR_TESTING_GUIDE.md",
      issue_type: "incorrect",
      description: "Document references PRFactory.Tests but tests are in PRFactory.Web.Tests",
      priority: "critical",
      suggested_fix: "Replace all instances of PRFactory.Tests with PRFactory.Web.Tests"
    },
    {
      title: "Missing admin routes in UI_NAVIGATION_QUICK_REFERENCE.md",
      file_path: "/home/user/myapp/docs/UI_NAVIGATION_QUICK_REFERENCE.md",
      issue_type: "missing",
      description: "Admin UI routes added in Epic 06 not documented",
      priority: "critical"
    }
  ],

  focus_areas: [
    {
      name: "File path accuracy",
      description: "Ensure all file paths and project references are correct"
    },
    {
      name: "UI navigation completeness",
      description: "Document all admin and settings routes"
    }
  ]
};
```

### Naming Convention Standardization

```javascript
const namingStandardizationRequest = {
  project_name: "MyApp",
  repository_path: "/code/myapp",
  docs_root_path: "/code/myapp/docs",

  naming_standards: [
    {
      category: "Root-level strategic docs",
      convention: "UPPERCASE.md",
      example: "ARCHITECTURE.md, ROADMAP.md, SETUP.md"
    },
    {
      category: "Component/architecture docs",
      convention: "kebab-case.md",
      example: "workflow-execution.md, git-integration.md"
    },
    {
      category: "Epic folders",
      convention: "epic_nn_name",
      example: "epic_01_team_review, epic_02_multi_llm"
    }
  ],

  focus_areas: [
    {
      name: "Naming consistency",
      description: "Standardize all file and folder names to match conventions"
    },
    {
      name: "Cross-reference updates",
      description: "Update all links when files are renamed"
    }
  ]
};
```

---

## Agent Workflow

The documentation update process follows this workflow:

### Phase 1: Exploration (Parallel Subagents)

Launch multiple exploration agents (recommended: haiku for speed) to:
- Explore codebase for recent features and changes
- Review existing documentation structure
- Identify implementation vs documentation gaps

**Example:**
```
Agent 1: "Explore recent features in codebase (Epic 05-08)"
Agent 2: "Review all existing documentation files in /docs"
Agent 3: "Check specific file (BLAZOR_TESTING_GUIDE.md) for accuracy"
```

### Phase 2: Gap Analysis

Synthesize exploration findings into prioritized list:
- **Critical**: Blocks release (wrong paths, missing navigation, broken links)
- **Important**: Needed soon (outdated architecture, missing features)
- **Nice-to-Have**: Future improvements (additional examples, better formatting)

**Output:** Comprehensive gap analysis report with specific file paths and line numbers

### Phase 3: Execution (Parallel Subagents)

Launch multiple update agents (haiku or sonnet depending on complexity) to:
- Fix critical issues first
- Update multiple independent files in parallel
- Standardize naming conventions
- Refresh cross-references

**Example:**
```
Agent 1: "Update UI_NAVIGATION_QUICK_REFERENCE.md with admin routes"
Agent 2: "Update DATABASE_SCHEMA.md with new entities"
Agent 3: "Update ARCHITECTURE.md project structure"
(All running in parallel for maximum efficiency)
```

### Phase 4: Verification

Final checks:
- No broken links
- Naming conventions 100% compliant
- Cross-document consistency
- Accuracy matches implementation

---

## Best Practices

### DO

✅ **Use parallel subagents** for efficiency (explore + analyze + execute simultaneously)
✅ **Fix critical issues first** (wrong paths, missing key docs)
✅ **Standardize naming early** to prevent future confusion
✅ **Update cross-references** when renaming files
✅ **Document with examples** (code snippets, file paths, diagrams)
✅ **Verify against implementation** to ensure accuracy
✅ **Keep documentation DRY** (link to details instead of duplicating)

### DON'T

❌ **Create documentation as session logs** (write for future developers, not chronicling work)
❌ **Mix naming conventions** in the same category (UPPERCASE vs lowercase)
❌ **Leave broken links** after renaming files
❌ **Skip verification** of updates against actual code
❌ **Update only some docs** and forget related ones (update all or none)
❌ **Document features that don't exist** or are only partially implemented

---

## Documentation Categories

### Strategic Documentation
**Purpose:** High-level vision, status, planning

**Files:** IMPLEMENTATION_STATUS.md, ROADMAP.md, ARCHITECTURE.md, SETUP.md

**Update when:**
- Features are completed
- Architecture changes
- Roadmap milestones achieved
- Deployment process changes

### Technical Documentation
**Purpose:** Deep technical details for developers

**Files:** DATABASE_SCHEMA.md, API.md, WORKFLOW.md, architecture/\*.md

**Update when:**
- Database schema changes
- APIs are added/modified
- Workflows change
- Architecture evolves

### User Documentation
**Purpose:** End-user guides and references

**Files:** UI_NAVIGATION_QUICK_REFERENCE.md, user-manual/\*.md, GETTING_STARTED.md

**Update when:**
- UI changes
- New features released
- Workflows change
- Screenshots become outdated

### Development Documentation
**Purpose:** Help contributors get started

**Files:** CONTRIBUTING.md, TESTING_GUIDE.md, DEVELOPMENT.md, CLAUDE.md

**Update when:**
- Development setup changes
- Testing patterns evolve
- Contribution guidelines change
- Architecture guidance needed

---

## Success Criteria

Documentation updates are successful when:

1. ✅ **Accuracy**: All docs match current implementation
2. ✅ **Completeness**: All major features are documented
3. ✅ **Consistency**: Naming and formatting are standardized
4. ✅ **Currency**: Recently completed work is marked as done
5. ✅ **Integrity**: No broken links or references
6. ✅ **Clarity**: New developers can understand the system from docs

---

## Customization

### For Different Project Types

**Web Applications:**
- Focus on UI navigation, API endpoints, database schemas
- Emphasize user workflows and feature documentation

**Libraries/Frameworks:**
- Focus on API documentation, usage examples, integration guides
- Emphasize public interfaces and extension points

**CLI Tools:**
- Focus on command documentation, configuration guides, examples
- Emphasize usage patterns and common workflows

**Internal Tools:**
- Focus on setup guides, operational documentation, troubleshooting
- Emphasize maintenance and deployment procedures

### Provider-Specific Adaptations

All three major LLM providers are supported with optimized prompts:

#### Anthropic (Claude)
- **System Prompt**: 63 lines - Detailed, thorough, example-rich
- **User Template**: 178 lines - Comprehensive with extensive context
- **Style**: Educational, example-rich, detailed explanations
- **Rationale**: Claude performs well with detailed instructions and examples

#### OpenAI (GPT)
- **System Prompt**: 28 lines - Concise, focused bullet points
- **User Template**: 64 lines - Structured, to-the-point
- **Style**: Bullet points, clear sections, minimal prose
- **Rationale**: GPT prefers shorter, more structured prompts

#### Google (Gemini)
- **System Prompt**: 47 lines - Balanced, clear guidelines
- **User Template**: 127 lines - Detailed but organized
- **Style**: Clear headings, comprehensive but scannable
- **Rationale**: Gemini works well with clear instructions and organized content

---

## Related Documentation

- `/prompts/README.md` - Overview of all prompt templates
- `/prompts/plan/` - Planning agent prompts
- `/prompts/code-review/` - Code review prompts
- `/prompts/analysis/` - Codebase analysis prompts
- `/prompts/implementation/` - Implementation prompts

---

## Maintenance

**Review prompts when:**
- Documentation update workflow changes
- New documentation categories are added
- Agent execution patterns evolve
- Feedback indicates improvements needed

**Keep prompts updated with:**
- New documentation best practices
- Evolving project needs
- Lessons learned from past updates
- Team feedback on documentation quality

---

## Example Session

### User Request
```
User: "We just completed Epic 05 and Epic 08. Can you update all the documentation?"
```

### AI Response
```
I'll update the documentation to reflect Epic 05 (Agent System Foundation) and Epic 08 (System Architecture Cleanup).

Let me start by exploring the codebase and documentation in parallel...

[Launches 3 exploration agents using haiku for speed]:
1. Agent 1: Explore Epic 05 and Epic 08 implementations
2. Agent 2: Review all existing documentation files
3. Agent 3: Check specific known issues (BLAZOR_TESTING_GUIDE.md)

[After exploration, provides gap analysis]:

Critical Issues Found:
- ARCHITECTURE.md still shows 3-project structure (Epic 08 consolidated to 1)
- DATABASE_SCHEMA.md missing 5 new entities from Epic 05 & 07
- UI_NAVIGATION_QUICK_REFERENCE.md missing admin routes from Epic 06
- BLAZOR_TESTING_GUIDE.md has wrong project path

Important Updates Needed:
- IMPLEMENTATION_STATUS.md UI components section outdated
- WORKFLOW.md missing agent chat workflows (Epic 05)
- ROADMAP.md completed epics still in "planned" section

[Executes updates in parallel]:

Launching 7 update agents to fix all issues simultaneously...
Agent 1: Updating BLAZOR_TESTING_GUIDE.md paths
Agent 2: Updating UI_NAVIGATION_QUICK_REFERENCE.md
Agent 3: Updating DATABASE_SCHEMA.md entities
Agent 4: Updating ARCHITECTURE.md structure
Agent 5: Updating WORKFLOW.md workflows
Agent 6: Updating IMPLEMENTATION_STATUS.md
Agent 7: Updating ROADMAP.md

[All agents complete]

Summary of Changes:
✅ Updated 8 documentation files
✅ Fixed all critical issues
✅ Standardized naming conventions (100% compliance)
✅ Updated cross-references
✅ Verified accuracy against implementation

All documentation now accurately reflects Epic 05 and Epic 08 completion.
```

---

## Tips for Best Results

### Provide Context
The more context you provide, the better the updates:
- Recent epic/feature names and dates
- Known documentation issues
- Specific areas of concern
- Naming conventions used in your project

### Use Parallel Execution
Documentation updates are embarrassingly parallel:
- Multiple files can be updated simultaneously
- Exploration can happen while analysis is ongoing
- Use haiku agents for speed, sonnet for complex updates

### Verify After Updates
Always verify:
```bash
# Check for broken links (example for markdown)
find docs/ -name "*.md" -exec grep -H "\[.*\](.*)" {} \; | grep "404\|broken"

# Verify naming consistency
ls docs/ | grep -v "^[A-Z_]*\.md$\|^[a-z-]*\.md$"

# Check for outdated dates
grep -r "Last Updated:" docs/
```

### Maintain Regular Updates
Don't let documentation drift:
- Update docs in the same PR as code changes
- Run documentation audits after each epic/sprint
- Schedule quarterly comprehensive reviews
- Assign documentation ownership

---

## Troubleshooting

### "Agent says documentation is current but I know it's outdated"

**Solution:** Provide specific known issues:
```javascript
known_issues: [
  {
    title: "Missing feature X in docs",
    file_path: "/docs/ARCHITECTURE.md",
    description: "Feature X was added in Epic 05 but not documented",
    priority: "critical"
  }
]
```

### "Updates are taking too long"

**Solution:**
- Use haiku model for simple updates
- Launch agents in parallel (not sequentially)
- Break large updates into smaller batches
- Focus on critical issues first

### "Naming standardization broke links"

**Solution:** Always update cross-references:
- Agent should check for references before renaming
- Update all links in the same operation
- Verify no broken links after rename

### "Documentation still doesn't match implementation"

**Solution:**
- Be more specific in focus_areas
- Provide file paths to check
- Include recent commit information
- Ask for verification step

---

## Contributing

To improve these documentation prompts:

1. **Test with different project types** and provide feedback
2. **Suggest additional template variables** that would be useful
3. **Add provider-specific prompts** (OpenAI, Google) if you optimize for those models
4. **Share success stories** or lessons learned
5. **Report issues** or gaps in the current prompts

---

## License

These prompts follow the same license as the PRFactory project.

---

## Summary

Documentation update prompts enable:
- ✅ Automated documentation maintenance
- ✅ Comprehensive gap analysis
- ✅ Parallel execution for efficiency
- ✅ Consistent naming and formatting
- ✅ Verification of accuracy
- ✅ Cross-project reusability

**Use these prompts to keep your documentation accurate, complete, and helpful for all team members.**