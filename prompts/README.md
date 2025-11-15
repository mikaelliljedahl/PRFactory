# Prompt Templates for Multi-LLM Support

This directory contains prompt templates for all agent types and LLM providers.

## Directory Structure

```
/prompts/
├── plan/                          # Implementation planning agents
│   ├── anthropic/
│   │   ├── system.txt            # 61 lines - Detailed architect persona
│   │   └── user_template.hbs     # 81 lines - Comprehensive planning request
│   ├── openai/
│   │   ├── system.txt            # 15 lines - Concise architect persona
│   │   └── user_template.hbs     # 50 lines - Structured planning request
│   └── google/
│       ├── system.txt            # 19 lines - Balanced architect persona
│       └── user_template.hbs     # 108 lines - Detailed planning request
│
├── code-review/                   # Code review agents
│   ├── anthropic/
│   │   ├── system.txt            # 40 lines - Expert reviewer with examples
│   │   └── user_template.hbs     # 84 lines - Detailed review request
│   ├── openai/
│   │   ├── system.txt            # 16 lines - Concise reviewer
│   │   └── user_template.hbs     # 46 lines - Structured review request
│   └── google/
│       ├── system.txt            # 20 lines - Balanced reviewer
│       └── user_template.hbs     # 81 lines - Comprehensive review request
│
├── implementation/                # Code generation agents
│   ├── anthropic/
│   │   ├── system.txt            # 47 lines - Detailed engineer persona
│   │   └── user_template.hbs     # 99 lines - Complete implementation request
│   ├── openai/
│   │   ├── system.txt            # 23 lines - Concise engineer persona
│   │   └── user_template.hbs     # 51 lines - Focused implementation request
│   └── google/
│       ├── system.txt            # 41 lines - Balanced engineer persona
│       └── user_template.hbs     # 103 lines - Detailed implementation request
│
├── analysis/                      # Codebase analysis agents
│   ├── anthropic/
│   │   ├── system.txt            # 45 lines - Thorough analyst persona
│   │   └── user_template.hbs     # 100 lines - Comprehensive analysis request
│   ├── openai/
│   │   ├── system.txt            # 20 lines - Concise analyst persona
│   │   └── user_template.hbs     # 52 lines - Focused analysis request
│   └── google/
│       ├── system.txt            # 37 lines - Balanced analyst persona
│       └── user_template.hbs     # 106 lines - Detailed analysis request
│
├── documentation/                 # Documentation update agents
│   ├── anthropic/
│   │   ├── system.txt            # 63 lines - Detailed documentation specialist
│   │   └── user_template.hbs     # 178 lines - Comprehensive doc update request
│   ├── openai/
│   │   ├── system.txt            # 28 lines - Concise documentation specialist
│   │   └── user_template.hbs     # 64 lines - Structured doc update request
│   ├── google/
│   │   ├── system.txt            # 47 lines - Balanced documentation specialist
│   │   └── user_template.hbs     # 127 lines - Detailed doc update request
│   └── README.md                  # Documentation update guide
│
├── README.md                      # This file
└── SAMPLE_RENDERED_OUTPUT.md      # Example of rendered template

**Total Files:** 31 (5 agent types × 3 providers × 2 files each, except documentation + docs)
**Total Lines:** 2,093+ lines of carefully crafted prompts
```

## Agent Types

### 1. Planning Agents (`/plan/`)
**Purpose:** Generate comprehensive implementation plans from tickets

**Variables Used:**
- `ticket_number`, `ticket_title`, `ticket_description`, `ticket_url`
- `codebase_structure`, `existing_patterns`, `related_files`
- `technology_stack`, `requirements`, `acceptance_criteria`
- `repository_name`, `current_branch`

**Output:** Detailed implementation plan with:
- Overview and approach
- Step-by-step implementation guide
- Database schema changes
- API specifications
- Testing strategy
- Security considerations
- Dependencies and rollout plan

### 2. Code Review Agents (`/code-review/`)
**Purpose:** Review pull requests for quality, security, and best practices

**Variables Used:**
- `ticket_number`, `ticket_title`, `ticket_description`, `ticket_url`
- `plan_path`, `plan_summary`, `plan_content`
- `pull_request_url`, `branch_name`, `target_branch`
- `files_changed_count`, `lines_added`, `lines_deleted`, `commits_count`
- `file_changes[]` (array with file_path, change_type, language, diff, etc.)
- `codebase_structure`, `related_files[]`
- `tests_added`, `test_coverage_percentage`, `test_files[]`
- `repository_name`, `author_name`, `created_at`

**Output:** Detailed code review with:
- Critical issues (security, bugs)
- Suggested improvements (performance, maintainability)
- Praise for well-written code
- File-specific feedback with line numbers

### 3. Implementation Agents (`/implementation/`)
**Purpose:** Generate production-quality code from implementation plans

**Variables Used:**
- `ticket_number`, `ticket_title`, `ticket_description`, `ticket_url`
- `plan_path`, `plan_content`
- `codebase_structure`, `existing_files[]`
- `technology_stack`, `coding_standards`
- `files_to_create[]`, `files_to_modify[]`
- `repository_name`, `branch_name`, `base_commit`

**Output:** Complete, working code with:
- Full file contents (no TODOs or placeholders)
- Proper error handling and logging
- Database migrations (if needed)
- Comprehensive tests
- XML documentation comments

### 4. Analysis Agents (`/analysis/`)
**Purpose:** Analyze codebase to understand architecture and implementation

**Variables Used:**
- `analysis_objective`, `specific_questions[]`
- `repository_name`, `repository_url`, `branch_name`, `primary_language`
- `codebase_structure`, `technology_stack`
- `files_to_analyze[]` (with path, description, relevance, content)
- `related_files[]`, `architectural_patterns`
- `focus_areas[]`, `constraints[]`

**Output:** Comprehensive analysis with:
- Summary of findings
- Detailed analysis with file references
- Strengths and weaknesses
- Security and performance notes
- Actionable recommendations

### 5. Documentation Agents (`/documentation/`)
**Purpose:** Update and maintain technical documentation across projects

**Variables Used:**
- `project_name`, `repository_path`, `docs_root_path`
- `recent_epics[]`, `recent_commits[]`
- `core_docs[]`, `technical_docs[]`, `user_docs[]`
- `known_issues[]` (with file_path, issue_type, priority)
- `naming_standards[]`, `focus_areas[]`
- `additional_instructions`

**Output:** Comprehensive documentation updates with:
- Gap analysis report (critical/important/nice-to-have)
- Updated documentation files
- Naming convention standardization
- Cross-reference verification
- Summary of changes made

**See:** `/prompts/documentation/README.md` for detailed usage guide

## Provider-Specific Design

### Anthropic (Claude)
- **System Prompts:** Detailed, thorough, with examples (40-61 lines)
- **User Templates:** Comprehensive with extensive context (81-100 lines)
- **Style:** Educational, example-rich, detailed explanations
- **Rationale:** Claude performs well with detailed instructions and examples

### OpenAI (GPT)
- **System Prompts:** Concise, focused (15-23 lines)
- **User Templates:** Structured, to-the-point (46-52 lines)
- **Style:** Bullet points, clear sections, minimal prose
- **Rationale:** GPT prefers shorter, more structured prompts

### Google (Gemini)
- **System Prompts:** Balanced, clear (19-41 lines)
- **User Templates:** Detailed but organized (81-108 lines)
- **Style:** Clear headings, comprehensive but scannable
- **Rationale:** Gemini works well with clear instructions and organized content

## Template Variables Reference

See `/docs/planning/code-review/TEMPLATE_VARIABLES.md` for complete documentation of all available variables.

**Common Variables (All Agent Types):**
- Ticket info: `ticket_number`, `ticket_title`, `ticket_description`, `ticket_url`
- Repository: `repository_name`, `repository_url`, `branch_name`
- Codebase: `codebase_structure`, `related_files[]`
- Tech stack: `technology_stack.framework`, `technology_stack.database`

**Code Review Specific:**
- PR details: `pull_request_url`, `files_changed_count`, `lines_added`
- File changes: `file_changes[]` with `file_path`, `diff`, `language`
- Testing: `tests_added`, `test_coverage_percentage`

**Planning Specific:**
- Requirements: `requirements.functional[]`, `requirements.non_functional[]`
- Criteria: `acceptance_criteria[]`
- Patterns: `existing_patterns`

**Implementation Specific:**
- Plan: `plan_path`, `plan_content`
- Files: `files_to_create[]`, `files_to_modify[]`
- Standards: `coding_standards`

**Analysis Specific:**
- Objective: `analysis_objective`, `specific_questions[]`
- Files: `files_to_analyze[]` with full content
- Focus: `focus_areas[]`, `constraints[]`

## Handlebars Syntax

All user templates use Handlebars (`.hbs`) templating:

**Variable Substitution:**
```handlebars
{{ticket_number}}
{{ticket_title}}
```

**Array Iteration:**
```handlebars
{{#each file_changes}}
  File: {{this.file_path}}
  Lines: +{{this.lines_added}} -{{this.lines_deleted}}
{{/each}}
```

**Conditionals:**
```handlebars
{{#if test_files}}
  Test files were modified
{{/if}}
```

**Nested Properties:**
```handlebars
{{technology_stack.framework}}
{{technology_stack.database}}
```

## Usage Example

```csharp
// Load prompt template
var systemPrompt = File.ReadAllText("/prompts/code-review/anthropic/system.txt");
var templateContent = File.ReadAllText("/prompts/code-review/anthropic/user_template.hbs");

// Compile Handlebars template
var template = Handlebars.Compile(templateContent);

// Render with variables
var userPrompt = template(new
{
    ticket_number = "PROJ-123",
    ticket_title = "Add feature X",
    pull_request_url = "https://github.com/...",
    file_changes = new[]
    {
        new { file_path = "src/File.cs", diff = "+new code", ... }
    }
});

// Send to LLM
var response = await llmProvider.SendMessageAsync(
    prompt: userPrompt,
    systemPrompt: systemPrompt,
    options: new LlmOptions { Model = "claude-sonnet-4-5" }
);
```

## Configuration

Prompts are configured in `appsettings.json`:

```json
{
  "Prompts": {
    "BasePath": "/prompts"
  }
}
```

## Testing Templates

See `SAMPLE_RENDERED_OUTPUT.md` for an example of how the code-review template renders with actual data.

## Design Principles

1. **Provider Agnostic Output:** All templates for a given agent type produce the same OUTPUT structure, only the INPUT (system prompt) varies
2. **Complete Variable Coverage:** Templates use ALL relevant variables from TEMPLATE_VARIABLES.md
3. **Handlebars Standard:** All user templates use standard Handlebars syntax (no custom extensions)
4. **Provider Strengths:** System prompts leverage each provider's strengths (Claude=detailed, GPT=concise, Gemini=balanced)
5. **Production Quality:** Templates are designed for production use, not prototypes
6. **Extensible:** Easy to add new agent types or providers by following the established patterns

## Adding New Templates

To add a new agent type:

1. Create directory: `/prompts/{agent-name}/`
2. Add provider subdirectories: `anthropic/`, `openai/`, `google/`
3. Create `system.txt` for each provider (role, focus areas, output format)
4. Create `user_template.hbs` for each provider (context, instructions, variables)
5. Document variables in TEMPLATE_VARIABLES.md
6. Add example to SAMPLE_RENDERED_OUTPUT.md

## Maintenance

- **System prompts** can be updated to improve agent behavior
- **User templates** should remain stable (changing variables breaks compatibility)
- **New variables** can be added without breaking existing templates (Handlebars ignores missing variables)
- **Provider-specific tuning** is encouraged (adjust system prompts based on provider performance)

## Related Documentation

- `/docs/planning/EPIC_02_MULTI_LLM.md` - Multi-LLM support epic
- `/docs/planning/code-review/PROMPT_LIBRARY.md` - Database-driven prompt library (future)
- `/docs/planning/code-review/TEMPLATE_VARIABLES.md` - Complete variable reference
