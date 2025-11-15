# Batch 4: Business Components - Other

## Summary
- **Total Components**: ~40
- **Estimated Complexity**: Medium-Complex
- **Estimated Time**: 15-20 hours
- **Priority**: MEDIUM
- **Coverage Contribution**: +32% (40/125 components)

## Overview

This batch covers all business components outside of tickets: Settings, Repositories, Tenants, Workflows, Errors, Agent Prompts, Auth, and Layout. These components have service dependencies and implement specific business workflows.

This batch can be parallelized by subdomain:
- **Sub-batch 4a**: Repositories & Tenants (~10 components, 4-5 hours)
- **Sub-batch 4b**: Settings (~13 components, 5-6 hours)
- **Sub-batch 4c**: Workflows, Errors, Auth (~9 components, 3-4 hours)
- **Sub-batch 4d**: Agent Prompts, Misc (~8 components, 3-4 hours)

---

## Sub-batch 4a: Repositories & Tenants (10 components)

### Components/Repositories (5 components)

#### BranchSelector
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/Repositories/BranchSelector.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/Repositories/BranchSelectorTests.cs`
- **Complexity**: Medium
- **Priority**: MEDIUM
- **Dependencies**: IRepositoryService
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Loads branches on initialization
  2. Renders dropdown with available branches
  3. OnBranchSelected callback invoked when branch selected
  4. Shows loading state while fetching branches
  5. Shows error state when load fails
  6. Handles empty branch list
  7. Pre-selects current branch
- **Mocking Requirements**:
  - `IRepositoryService.GetBranchesAsync()`

#### RepositoryConnectionTest
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/Repositories/RepositoryConnectionTest.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/Repositories/RepositoryConnectionTestTests.cs`
- **Complexity**: Medium
- **Priority**: MEDIUM
- **Dependencies**: IRepositoryService, LoadingButton
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Test connection button triggers connection test
  2. Shows success message when connection succeeds
  3. Shows error message when connection fails
  4. Displays connection details (branch, commits)
  5. Loading state during test
  6. Disabled state when no repository configured
- **Mocking Requirements**:
  - `IRepositoryService.TestConnectionAsync()`

#### RepositoryForm
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/Repositories/RepositoryForm.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/Repositories/RepositoryFormTests.cs`
- **Complexity**: Complex
- **Priority**: HIGH
- **Dependencies**: FormTextField, FormSelectField, BranchSelector
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Renders form fields for repository configuration
  2. Validates required fields (name, URL, platform)
  3. OnSubmit callback invoked with repository data
  4. OnCancel callback invoked when cancelled
  5. Pre-fills form when editing existing repository
  6. Shows platform-specific fields (GitHub token, Bitbucket app password, etc.)
  7. Handles validation errors
- **Mocking Requirements**: None (form logic only)

#### RepositoryListItem
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/Repositories/RepositoryListItem.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/Repositories/RepositoryListItemTests.cs`
- **Complexity**: Simple
- **Priority**: MEDIUM
- **Dependencies**: StatusBadge
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Renders repository name and URL
  2. Shows platform icon (GitHub, Bitbucket, Azure DevOps)
  3. Displays connection status badge
  4. Shows last sync timestamp
  5. OnClick navigates to detail view
  6. Handles long URLs gracefully
- **Mocking Requirements**: None (uses repository DTO model)

#### RepositoryStatistics
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/Repositories/RepositoryStatistics.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/Repositories/RepositoryStatisticsTests.cs`
- **Complexity**: Medium
- **Priority**: MEDIUM
- **Dependencies**: IRepositoryService, Card
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Loads statistics on initialization
  2. Displays total commits, branches, PRs
  3. Shows chart/visualization of activity
  4. Shows loading state while fetching
  5. Shows error state when load fails
  6. Handles no data gracefully
- **Mocking Requirements**:
  - `IRepositoryService.GetStatisticsAsync()`

---

### Components/Tenants (3 components)

#### TenantConfigEditor
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/Tenants/TenantConfigEditor.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/Tenants/TenantConfigEditorTests.cs`
- **Complexity**: Complex
- **Priority**: HIGH
- **Dependencies**: FormTextField, FormSelectField, ModelOverridesEditor
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Renders configuration fields
  2. Allows editing tenant settings
  3. Validates configuration values
  4. OnSave callback invoked with config data
  5. Shows validation errors
  6. Handles nested configuration objects
- **Mocking Requirements**: None (config editing logic)

#### TenantForm
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/Tenants/TenantForm.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/Tenants/TenantFormTests.cs`
- **Complexity**: Medium
- **Priority**: HIGH
- **Dependencies**: FormTextField, FormSelectField
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Renders form fields for tenant creation
  2. Validates required fields (name, slug)
  3. OnSubmit callback invoked with tenant data
  4. OnCancel callback invoked when cancelled
  5. Pre-fills form when editing existing tenant
  6. Slug auto-generation from name
  7. Handles validation errors
- **Mocking Requirements**: None (form logic only)

#### TenantListItem
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/Tenants/TenantListItem.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/Tenants/TenantListItemTests.cs`
- **Complexity**: Simple
- **Priority**: MEDIUM
- **Dependencies**: StatusBadge
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Renders tenant name and slug
  2. Shows active/inactive status badge
  3. Displays user count
  4. Shows created date
  5. OnClick navigates to detail view
- **Mocking Requirements**: None (uses tenant DTO model)

---

### Components/Misc (2 components)

#### Pagination
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/Pagination.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/PaginationTests.cs`
- **Complexity**: Medium
- **Priority**: MEDIUM
- **Dependencies**: None
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Renders page numbers correctly
  2. Shows previous/next buttons
  3. Disables previous on first page
  4. Disables next on last page
  5. OnPageChanged callback invoked when page clicked
  6. Highlights current page
  7. Shows ellipsis for large page counts
  8. Handles single page gracefully
- **Mocking Requirements**: None

#### TicketFilters
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/TicketFilters.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/TicketFiltersTests.cs`
- **Complexity**: Medium
- **Priority**: MEDIUM
- **Dependencies**: FormSelectField, FormTextField
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Renders filter dropdowns (status, repository, etc.)
  2. OnFilterChanged callback invoked when filters change
  3. Shows clear filters button
  4. Clears all filters when clicked
  5. Pre-selects active filters
- **Mocking Requirements**: None

#### TicketListItem
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/TicketListItem.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/TicketListItemTests.cs`
- **Complexity**: Simple
- **Priority**: HIGH
- **Dependencies**: StatusBadge, RelativeTime
- **Code-Behind**: Likely inline
- **Test Scenarios**:
  1. Renders ticket key and title
  2. Shows workflow state badge
  3. Displays relative time (created, updated)
  4. Shows repository name
  5. OnClick navigates to detail view
- **Mocking Requirements**: None (uses ticket DTO model)

---

## Sub-batch 4b: Settings (13 components)

### Components/Settings

#### AllowedRepositoriesEditor
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/Settings/AllowedRepositoriesEditor.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/Settings/AllowedRepositoriesEditorTests.cs`
- **Complexity**: Medium
- **Priority**: MEDIUM
- **Dependencies**: ISettingsService
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Renders list of allowed repositories
  2. Allows adding repository pattern
  3. Allows removing repository pattern
  4. Validates pattern format
  5. OnChange callback invoked when modified
- **Mocking Requirements**:
  - `ISettingsService` (if needed)

#### ApiKeyProviderForm
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/Settings/ApiKeyProviderForm.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/Settings/ApiKeyProviderFormTests.cs`
- **Complexity**: Medium
- **Priority**: MEDIUM
- **Dependencies**: FormTextField, FormPasswordField
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Renders API key input fields
  2. Validates required fields
  3. OnSubmit callback invoked with API key data
  4. Shows masked API key for existing keys
  5. Allows revealing API key
- **Mocking Requirements**: None

#### CodeReviewSettingsPanel
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/Settings/CodeReviewSettingsPanel.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/Settings/CodeReviewSettingsPanelTests.cs`
- **Complexity**: Medium
- **Priority**: MEDIUM
- **Dependencies**: FormCheckboxField, FormTextField
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Renders code review settings
  2. Allows toggling auto-approve
  3. Allows setting reviewer count
  4. OnSave callback invoked with settings
- **Mocking Requirements**: None

#### LlmProviderAssignmentPanel
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/Settings/LlmProviderAssignmentPanel.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/Settings/LlmProviderAssignmentPanelTests.cs`
- **Complexity**: Complex
- **Priority**: HIGH
- **Dependencies**: ILlmProviderService, FormSelectField
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Loads available LLM providers
  2. Renders assignment dropdowns for each agent
  3. OnAssignmentChanged callback invoked
  4. Shows provider details (model, token limit)
  5. Handles no providers gracefully
- **Mocking Requirements**:
  - `ILlmProviderService.GetProvidersAsync()`

#### LlmProviderListItem
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/Settings/LlmProviderListItem.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/Settings/LlmProviderListItemTests.cs`
- **Complexity**: Simple
- **Priority**: MEDIUM
- **Dependencies**: StatusBadge
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Renders provider name and type
  2. Shows status badge (active/inactive)
  3. Displays model name
  4. Shows usage statistics
  5. OnClick navigates to detail view
- **Mocking Requirements**: None

#### LlmProviderStatistics
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/Settings/LlmProviderStatistics.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/Settings/LlmProviderStatisticsTests.cs`
- **Complexity**: Medium
- **Priority**: MEDIUM
- **Dependencies**: ILlmProviderService
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Loads statistics on initialization
  2. Displays token usage, request count
  3. Shows cost estimates
  4. Shows loading state
  5. Handles no data
- **Mocking Requirements**:
  - `ILlmProviderService.GetStatisticsAsync()`

#### ModelOverridesEditor
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/Settings/ModelOverridesEditor.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/Settings/ModelOverridesEditorTests.cs`
- **Complexity**: Medium
- **Priority**: MEDIUM
- **Dependencies**: FormSelectField
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Renders model override dropdowns
  2. Allows selecting different models per agent
  3. OnChange callback invoked
  4. Shows default model when no override
- **Mocking Requirements**: None

#### OAuthProviderForm
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/Settings/OAuthProviderForm.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/Settings/OAuthProviderFormTests.cs`
- **Complexity**: Complex
- **Priority**: MEDIUM
- **Dependencies**: FormTextField, FormPasswordField
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Renders OAuth configuration fields
  2. Validates required fields (client ID, secret)
  3. OnSubmit callback invoked
  4. Shows OAuth flow instructions
  5. Handles callback URL generation
- **Mocking Requirements**: None

#### ProviderTypeSelector
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/Settings/ProviderTypeSelector.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/Settings/ProviderTypeSelectorTests.cs`
- **Complexity**: Simple
- **Priority**: MEDIUM
- **Dependencies**: FormSelectField
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Renders provider type dropdown
  2. Shows available types (OpenAI, Anthropic, Azure, etc.)
  3. OnTypeSelected callback invoked
  4. Pre-selects current type
- **Mocking Requirements**: None

#### TenantInfoPanel
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/Settings/TenantInfoPanel.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/Settings/TenantInfoPanelTests.cs`
- **Complexity**: Simple
- **Priority**: MEDIUM
- **Dependencies**: Card
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Renders tenant information
  2. Shows tenant name, slug, created date
  3. Displays subscription tier
  4. Shows usage statistics
- **Mocking Requirements**: None

#### UserListItem
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/Settings/UserListItem.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/Settings/UserListItemTests.cs`
- **Complexity**: Simple
- **Priority**: MEDIUM
- **Dependencies**: StatusBadge, ReviewerAvatar
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Renders user name and email
  2. Shows role badge
  3. Displays avatar
  4. Shows last login timestamp
  5. OnClick navigates to detail view
- **Mocking Requirements**: None

#### UserRoleEditor
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/Settings/UserRoleEditor.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/Settings/UserRoleEditorTests.cs`
- **Complexity**: Medium
- **Priority**: MEDIUM
- **Dependencies**: FormSelectField
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Renders role dropdown
  2. Shows available roles (Admin, User, Viewer)
  3. OnRoleChanged callback invoked
  4. Pre-selects current role
- **Mocking Requirements**: None

#### UserStatistics
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/Settings/UserStatistics.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/Settings/UserStatisticsTests.cs`
- **Complexity**: Medium
- **Priority**: MEDIUM
- **Dependencies**: IUserService
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Loads statistics on initialization
  2. Displays ticket count, PR count
  3. Shows activity chart
  4. Shows loading state
- **Mocking Requirements**:
  - `IUserService.GetStatisticsAsync()`

#### WorkflowSettingsPanel
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/Settings/WorkflowSettingsPanel.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/Settings/WorkflowSettingsPanelTests.cs`
- **Complexity**: Medium
- **Priority**: MEDIUM
- **Dependencies**: FormCheckboxField, FormTextField
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Renders workflow settings
  2. Allows toggling auto-start
  3. Allows setting timeout values
  4. OnSave callback invoked
- **Mocking Requirements**: None

---

## Sub-batch 4c: Workflows, Errors, Auth (9 components)

### Components/Workflows (3 components)

#### EventDetail
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/Workflows/EventDetail.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/Workflows/EventDetailTests.cs`
- **Complexity**: Medium
- **Priority**: MEDIUM
- **Dependencies**: Card, StackTraceViewer
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Renders event details
  2. Shows event type and timestamp
  3. Displays event data/payload
  4. Shows stack trace for errors
  5. Formats JSON payload
- **Mocking Requirements**: None

#### EventLogFilter
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/Workflows/EventLogFilter.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/Workflows/EventLogFilterTests.cs`
- **Complexity**: Medium
- **Priority**: MEDIUM
- **Dependencies**: FormSelectField, FormTextField
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Renders filter dropdowns
  2. OnFilterChanged callback invoked
  3. Shows clear filters button
  4. Filters by event type, severity, date
- **Mocking Requirements**: None

#### EventStatistics
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/Workflows/EventStatistics.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/Workflows/EventStatisticsTests.cs`
- **Complexity**: Medium
- **Priority**: MEDIUM
- **Dependencies**: IWorkflowService
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Loads statistics on initialization
  2. Displays event counts by type
  3. Shows chart/visualization
  4. Shows loading state
- **Mocking Requirements**:
  - `IWorkflowService.GetEventStatisticsAsync()`

---

### Components/Errors (3 components)

#### ErrorDetail
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/Errors/ErrorDetail.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/Errors/ErrorDetailTests.cs`
- **Complexity**: Medium
- **Priority**: MEDIUM
- **Dependencies**: ErrorCard, StackTraceViewer
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Renders error message
  2. Shows stack trace
  3. Displays error metadata
  4. Shows resolution status
- **Mocking Requirements**: None

#### ErrorListFilter
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/Errors/ErrorListFilter.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/Errors/ErrorListFilterTests.cs`
- **Complexity**: Medium
- **Priority**: MEDIUM
- **Dependencies**: FormSelectField
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Renders filter dropdowns
  2. Filters by severity, status
  3. OnFilterChanged callback invoked
- **Mocking Requirements**: None

#### ErrorResolutionForm
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/Errors/ErrorResolutionForm.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/Errors/ErrorResolutionFormTests.cs`
- **Complexity**: Medium
- **Priority**: MEDIUM
- **Dependencies**: FormTextAreaField, LoadingButton
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Renders resolution form
  2. Validates resolution notes
  3. OnSubmit callback invoked
  4. Shows loading state
- **Mocking Requirements**: None

---

### Components/Auth (2 components)

#### RedirectToLogin
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/Auth/RedirectToLogin.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/Auth/RedirectToLoginTests.cs`
- **Complexity**: Simple
- **Priority**: LOW
- **Dependencies**: NavigationManager
- **Code-Behind**: Likely inline
- **Test Scenarios**:
  1. Redirects to login page on render
  2. Preserves return URL
- **Mocking Requirements**:
  - `NavigationManager`

#### UserProfileDropdown
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/Auth/UserProfileDropdown.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/Auth/UserProfileDropdownTests.cs`
- **Complexity**: Medium
- **Priority**: MEDIUM
- **Dependencies**: IUserService, ReviewerAvatar
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Renders user avatar and name
  2. Shows dropdown menu on click
  3. Displays menu items (Profile, Settings, Logout)
  4. OnLogout callback invoked
- **Mocking Requirements**:
  - `IUserService` (if needed)

---

### Components/Layout (1 component, 2 already exist)

#### NavMenu
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/Layout/NavMenu.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/Layout/NavMenuTests.cs`
- **Complexity**: Medium
- **Priority**: MEDIUM
- **Dependencies**: NavigationManager
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Renders navigation menu items
  2. Highlights active route
  3. Shows menu icons
  4. Collapses on mobile
  5. Shows different items based on user role
- **Mocking Requirements**:
  - `NavigationManager`

#### MainLayout
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/Layout/MainLayout.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/Layout/MainLayoutTests.cs`
- **Complexity**: Medium
- **Priority**: LOW (layout testing can be deferred)
- **Dependencies**: NavMenu, UserProfileDropdown, ToastContainer
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Renders header, sidebar, content area
  2. Includes NavMenu
  3. Includes UserProfileDropdown
  4. Includes ToastContainer
  5. Renders page content in main area
- **Mocking Requirements**: None

#### EmptyLayout
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/Layout/EmptyLayout.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/Layout/EmptyLayoutTests.cs`
- **Complexity**: Simple
- **Priority**: LOW
- **Dependencies**: None
- **Code-Behind**: Likely inline
- **Test Scenarios**:
  1. Renders body content without navigation
  2. No header/sidebar
- **Mocking Requirements**: None

---

## Sub-batch 4d: Agent Prompts, Misc (4 components)

### Components/AgentPrompts (4 components)

#### PromptPreview
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/AgentPrompts/PromptPreview.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/AgentPrompts/PromptPreviewTests.cs`
- **Complexity**: Medium
- **Priority**: MEDIUM
- **Dependencies**: MarkdownPreview
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Renders prompt template with variables replaced
  2. Shows variable values
  3. Displays formatted markdown
  4. Handles missing variables
- **Mocking Requirements**: None

#### PromptTemplateForm
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/AgentPrompts/PromptTemplateForm.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/AgentPrompts/PromptTemplateFormTests.cs`
- **Complexity**: Complex
- **Priority**: MEDIUM
- **Dependencies**: FormTextField, FormTextAreaField, MarkdownEditor
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Renders form fields for prompt template
  2. Validates required fields
  3. OnSubmit callback invoked
  4. Shows variable reference helper
  5. Handles template variables
- **Mocking Requirements**: None

#### PromptTemplateListItem
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/AgentPrompts/PromptTemplateListItem.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/AgentPrompts/PromptTemplateListItemTests.cs`
- **Complexity**: Simple
- **Priority**: MEDIUM
- **Dependencies**: StatusBadge
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Renders template name
  2. Shows agent type
  3. Displays status badge
  4. Shows last updated timestamp
  5. OnClick navigates to detail
- **Mocking Requirements**: None

#### PromptVariableReference
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Components/AgentPrompts/PromptVariableReference.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Components/AgentPrompts/PromptVariableReferenceTests.cs`
- **Complexity**: Simple
- **Priority**: LOW
- **Dependencies**: None
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Renders list of available variables
  2. Shows variable descriptions
  3. Allows copying variable syntax
  4. OnVariableSelected callback invoked
- **Mocking Requirements**: None

---

## Testing Priority Order

### Phase 1 (Critical, ~6 hours)
- Sub-batch 4a: Repositories & Tenants (10 components)

### Phase 2 (Important, ~6 hours)
- Sub-batch 4b: Settings (first 7 components - forms and panels)

### Phase 3 (Medium, ~4 hours)
- Sub-batch 4b: Settings (remaining 6 components - list items and statistics)

### Phase 4 (Lower, ~4 hours)
- Sub-batch 4c: Workflows, Errors, Auth (9 components)

### Phase 5 (Optional, ~2 hours)
- Sub-batch 4d: Agent Prompts (4 components)

---

## Success Criteria

- ✅ All ~40 components have test files
- ✅ Each component has 3-8 test scenarios covered
- ✅ Services properly mocked with Moq
- ✅ Tests use xUnit Assert (not FluentAssertions)
- ✅ Tests use bUnit RenderComponent pattern
- ✅ All tests pass: `dotnet test`
- ✅ Code compiles: `dotnet build`
- ✅ Format checks pass: `dotnet format --verify-no-changes`

---

## Notes

- This batch can be highly parallelized by subdomain
- Focus on core workflows first (Repositories, Tenants, Settings)
- Defer layout and auth components if time-constrained
- Some components may require complex service mocking
- Prioritize components with business logic over simple list items
