# Batch 5: Page Components

## Summary
- **Total Components**: ~36 pages
- **Estimated Complexity**: Complex
- **Estimated Time**: 15-20 hours
- **Priority**: LOWER (optional for 80% coverage)
- **Coverage Contribution**: +29% (36/125 components)

## Overview

This batch covers all page components (routable views with `@page` directive). Pages are the most complex to test because they:
- Have multiple service dependencies
- May use SignalR for real-time updates
- Orchestrate multiple child components
- Handle navigation and routing

**Important**: This batch is **optional** for achieving 80% coverage. After Batches 1-4, we'll have ~78% coverage. Only pursue this batch if you want to exceed 80% or need to test specific high-value pages.

## Recommendation

If you need to test pages to reach 80%, prioritize:
1. **High-value pages**: Tickets/Detail, Tickets/Index (core workflows)
2. **Simple pages**: Auth pages, GettingStarted, Index (easier to test)
3. **Defer complex pages**: Settings pages with heavy service dependencies

---

## Pages to Test

### Core Pages (High Priority)

#### Pages/Index (Dashboard)
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Pages/Index.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Pages/IndexTests.cs`
- **Complexity**: Medium
- **Priority**: HIGH
- **Dependencies**: ITicketService, TicketListItem, LoadingSpinner
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Loads recent tickets on initialization
  2. Renders dashboard cards (stats, recent activity)
  3. Shows loading state while fetching data
  4. Shows empty state when no tickets
  5. Navigates to ticket detail when clicked
- **Mocking Requirements**:
  - `ITicketService.GetRecentTicketsAsync()`

#### Pages/GettingStarted
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Pages/GettingStarted.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Pages/GettingStartedTests.cs`
- **Complexity**: Simple
- **Priority**: MEDIUM
- **Dependencies**: Card, AlertMessage
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Renders getting started content
  2. Shows setup steps
  3. Links to relevant documentation
- **Mocking Requirements**: None

---

### Tickets Pages (High Priority)

#### Pages/Tickets/Index
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Pages/Tickets/Index.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Pages/Tickets/IndexTests.cs`
- **Complexity**: Medium
- **Priority**: HIGH
- **Dependencies**: ITicketService, TicketFilters, TicketListItem, Pagination
- **Code-Behind**: Likely yes
- **Test Scenarios**:
  1. Loads tickets on initialization
  2. Renders ticket list
  3. Shows filters and pagination
  4. Filters tickets when filter changed
  5. Shows empty state when no tickets
  6. Shows loading state
- **Mocking Requirements**:
  - `ITicketService.GetTicketsAsync()`

#### Pages/Tickets/Detail
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Pages/Tickets/Detail.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Pages/Tickets/DetailTests.cs`
- **Complexity**: Complex
- **Priority**: HIGH
- **Dependencies**: ITicketService, TicketHeader, WorkflowTimeline, PlanReviewSection, QuestionAnswerForm, TicketUpdatePreview, SignalR
- **Code-Behind**: Yes (already examined)
- **Test Scenarios**:
  1. Loads ticket on initialization
  2. Renders ticket header and timeline
  3. Shows appropriate component based on workflow state
  4. Shows loading state
  5. Shows error state when ticket not found
  6. Handles different workflow states (Draft, QuestionsPosted, PlanUnderReview, etc.)
  7. **Defer SignalR testing** (too complex for initial coverage)
- **Mocking Requirements**:
  - `ITicketService.GetTicketDtoByIdAsync()`
  - `ITicketService.GetQuestionsAsync()`
  - `ITicketService.GetEventsAsync()`
  - NavigationManager

#### Pages/Tickets/Create
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Pages/Tickets/Create.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Pages/Tickets/CreateTests.cs`
- **Complexity**: Medium
- **Priority**: MEDIUM
- **Dependencies**: ITicketService, FormTextField, FormTextAreaField, SuccessCriteriaEditor
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Renders ticket creation form
  2. Validates required fields
  3. Submits ticket data on form submit
  4. Shows loading state during submission
  5. Navigates to detail on success
  6. Shows error on failure
- **Mocking Requirements**:
  - `ITicketService.CreateTicketAsync()`
  - NavigationManager

---

### Repositories Pages (Medium Priority)

#### Pages/Repositories/Index
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Pages/Repositories/Index.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Pages/Repositories/IndexTests.cs`
- **Complexity**: Medium
- **Priority**: MEDIUM
- **Dependencies**: IRepositoryService, RepositoryListItem
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Loads repositories on initialization
  2. Renders repository list
  3. Shows empty state when no repositories
  4. Navigates to create page
- **Mocking Requirements**:
  - `IRepositoryService.GetRepositoriesAsync()`

#### Pages/Repositories/Create
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Pages/Repositories/Create.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Pages/Repositories/CreateTests.cs`
- **Complexity**: Medium
- **Priority**: MEDIUM
- **Dependencies**: IRepositoryService, RepositoryForm
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Renders repository creation form
  2. Validates and submits
  3. Shows loading state
  4. Navigates on success
- **Mocking Requirements**:
  - `IRepositoryService.CreateRepositoryAsync()`

#### Pages/Repositories/Edit
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Pages/Repositories/Edit.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Pages/Repositories/EditTests.cs`
- **Complexity**: Medium
- **Priority**: MEDIUM
- **Dependencies**: IRepositoryService, RepositoryForm
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Loads repository on initialization
  2. Pre-fills form with repository data
  3. Validates and submits
  4. Shows loading state
- **Mocking Requirements**:
  - `IRepositoryService.GetRepositoryByIdAsync()`
  - `IRepositoryService.UpdateRepositoryAsync()`

#### Pages/Repositories/Detail
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Pages/Repositories/Detail.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Pages/Repositories/DetailTests.cs`
- **Complexity**: Medium
- **Priority**: MEDIUM
- **Dependencies**: IRepositoryService, RepositoryStatistics, RepositoryConnectionTest
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Loads repository on initialization
  2. Renders repository details
  3. Shows statistics
  4. Shows connection test
- **Mocking Requirements**:
  - `IRepositoryService.GetRepositoryByIdAsync()`

---

### Tenants Pages (Medium Priority)

#### Pages/Tenants/Index
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Pages/Tenants/Index.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Pages/Tenants/IndexTests.cs`
- **Complexity**: Medium
- **Priority**: MEDIUM
- **Dependencies**: ITenantService, TenantListItem
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Loads tenants
  2. Renders list
  3. Shows empty state
- **Mocking Requirements**:
  - `ITenantService.GetTenantsAsync()`

#### Pages/Tenants/Create
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Pages/Tenants/Create.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Pages/Tenants/CreateTests.cs`
- **Complexity**: Medium
- **Priority**: MEDIUM
- **Dependencies**: ITenantService, TenantForm
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Renders form
  2. Validates and submits
- **Mocking Requirements**:
  - `ITenantService.CreateTenantAsync()`

#### Pages/Tenants/Edit
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Pages/Tenants/Edit.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Pages/Tenants/EditTests.cs`
- **Complexity**: Medium
- **Priority**: MEDIUM
- **Dependencies**: ITenantService, TenantForm
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Loads tenant
  2. Pre-fills form
  3. Validates and submits
- **Mocking Requirements**:
  - `ITenantService.GetTenantByIdAsync()`
  - `ITenantService.UpdateTenantAsync()`

#### Pages/Tenants/Detail
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Pages/Tenants/Detail.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Pages/Tenants/DetailTests.cs`
- **Complexity**: Medium
- **Priority**: MEDIUM
- **Dependencies**: ITenantService, TenantInfoPanel, TenantConfigEditor
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Loads tenant
  2. Renders details
  3. Shows configuration editor
- **Mocking Requirements**:
  - `ITenantService.GetTenantByIdAsync()`

---

### Settings Pages (Lower Priority)

#### Pages/Settings/General
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Pages/Settings/General.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Pages/Settings/GeneralTests.cs`
- **Complexity**: Medium
- **Priority**: LOW
- **Dependencies**: ISettingsService, WorkflowSettingsPanel, CodeReviewSettingsPanel
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Loads settings
  2. Renders panels
  3. Saves changes
- **Mocking Requirements**:
  - `ISettingsService.GetSettingsAsync()`
  - `ISettingsService.SaveSettingsAsync()`

#### Pages/Settings/LlmProviders/Index
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Pages/Settings/LlmProviders/Index.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Pages/Settings/LlmProviders/IndexTests.cs`
- **Complexity**: Medium
- **Priority**: LOW
- **Dependencies**: ILlmProviderService, LlmProviderListItem
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Loads providers
  2. Renders list
- **Mocking Requirements**:
  - `ILlmProviderService.GetProvidersAsync()`

#### Pages/Settings/LlmProviders/Create
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Pages/Settings/LlmProviders/Create.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Pages/Settings/LlmProviders/CreateTests.cs`
- **Complexity**: Complex
- **Priority**: LOW
- **Dependencies**: ILlmProviderService, ApiKeyProviderForm, OAuthProviderForm, ProviderTypeSelector
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Renders form based on provider type
  2. Validates and submits
- **Mocking Requirements**:
  - `ILlmProviderService.CreateProviderAsync()`

#### Pages/Settings/LlmProviders/Edit
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Pages/Settings/LlmProviders/Edit.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Pages/Settings/LlmProviders/EditTests.cs`
- **Complexity**: Complex
- **Priority**: LOW
- **Dependencies**: ILlmProviderService, ApiKeyProviderForm, OAuthProviderForm
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Loads provider
  2. Pre-fills form
  3. Validates and submits
- **Mocking Requirements**:
  - `ILlmProviderService.GetProviderByIdAsync()`
  - `ILlmProviderService.UpdateProviderAsync()`

#### Pages/Settings/LlmProviders/Detail
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Pages/Settings/LlmProviders/Detail.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Pages/Settings/LlmProviders/DetailTests.cs`
- **Complexity**: Medium
- **Priority**: LOW
- **Dependencies**: ILlmProviderService, LlmProviderStatistics
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Loads provider
  2. Renders details
  3. Shows statistics
- **Mocking Requirements**:
  - `ILlmProviderService.GetProviderByIdAsync()`

#### Pages/Settings/Users/Index
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Pages/Settings/Users/Index.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Pages/Settings/Users/IndexTests.cs`
- **Complexity**: Medium
- **Priority**: LOW
- **Dependencies**: IUserService, UserListItem
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Loads users
  2. Renders list
- **Mocking Requirements**:
  - `IUserService.GetUsersAsync()`

#### Pages/Settings/Users/Edit
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Pages/Settings/Users/Edit.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Pages/Settings/Users/EditTests.cs`
- **Complexity**: Medium
- **Priority**: LOW
- **Dependencies**: IUserService, UserRoleEditor
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Loads user
  2. Renders role editor
  3. Saves changes
- **Mocking Requirements**:
  - `IUserService.GetUserByIdAsync()`
  - `IUserService.UpdateUserAsync()`

#### Pages/Settings/Users/Detail
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Pages/Settings/Users/Detail.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Pages/Settings/Users/DetailTests.cs`
- **Complexity**: Medium
- **Priority**: LOW
- **Dependencies**: IUserService, UserStatistics
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Loads user
  2. Renders details
  3. Shows statistics
- **Mocking Requirements**:
  - `IUserService.GetUserByIdAsync()`

---

### Agent Prompts Pages (Lower Priority)

#### Pages/AgentPrompts/Index
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Pages/AgentPrompts/Index.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Pages/AgentPrompts/IndexTests.cs`
- **Complexity**: Medium
- **Priority**: LOW
- **Dependencies**: IPromptService, PromptTemplateListItem
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Loads templates
  2. Renders list
- **Mocking Requirements**:
  - `IPromptService.GetTemplatesAsync()`

#### Pages/AgentPrompts/Create
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Pages/AgentPrompts/Create.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Pages/AgentPrompts/CreateTests.cs`
- **Complexity**: Complex
- **Priority**: LOW
- **Dependencies**: IPromptService, PromptTemplateForm
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Renders form
  2. Validates and submits
- **Mocking Requirements**:
  - `IPromptService.CreateTemplateAsync()`

#### Pages/AgentPrompts/Edit
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Pages/AgentPrompts/Edit.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Pages/AgentPrompts/EditTests.cs`
- **Complexity**: Complex
- **Priority**: LOW
- **Dependencies**: IPromptService, PromptTemplateForm
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Loads template
  2. Pre-fills form
  3. Validates and submits
- **Mocking Requirements**:
  - `IPromptService.GetTemplateByIdAsync()`
  - `IPromptService.UpdateTemplateAsync()`

#### Pages/AgentPrompts/Preview
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Pages/AgentPrompts/Preview.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Pages/AgentPrompts/PreviewTests.cs`
- **Complexity**: Medium
- **Priority**: LOW
- **Dependencies**: IPromptService, PromptPreview
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Loads template
  2. Renders preview
- **Mocking Requirements**:
  - `IPromptService.GetTemplateByIdAsync()`

---

### Workflows Pages (Lower Priority)

#### Pages/Workflows/Index
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Pages/Workflows/Index.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Pages/Workflows/IndexTests.cs`
- **Complexity**: Medium
- **Priority**: LOW
- **Dependencies**: IWorkflowService
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Loads workflows
  2. Renders list
- **Mocking Requirements**:
  - `IWorkflowService.GetWorkflowsAsync()`

#### Pages/Workflows/Events
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Pages/Workflows/Events.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Pages/Workflows/EventsTests.cs`
- **Complexity**: Medium
- **Priority**: LOW
- **Dependencies**: IWorkflowService, EventLogFilter, EventDetail
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Loads events
  2. Renders list with filters
- **Mocking Requirements**:
  - `IWorkflowService.GetEventsAsync()`

---

### Errors Pages (Lower Priority)

#### Pages/Errors/Index
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Pages/Errors/Index.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Pages/Errors/IndexTests.cs`
- **Complexity**: Medium
- **Priority**: LOW
- **Dependencies**: IErrorService, ErrorListFilter
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Loads errors
  2. Renders list with filters
- **Mocking Requirements**:
  - `IErrorService.GetErrorsAsync()`

#### Pages/Errors/Detail
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Pages/Errors/Detail.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Pages/Errors/DetailTests.cs`
- **Complexity**: Medium
- **Priority**: LOW
- **Dependencies**: IErrorService, ErrorDetail, ErrorResolutionForm
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Loads error
  2. Renders details
  3. Shows resolution form
- **Mocking Requirements**:
  - `IErrorService.GetErrorByIdAsync()`

---

### Auth Pages (Lower Priority)

#### Pages/Auth/Login
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Pages/Auth/Login.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Pages/Auth/LoginTests.cs`
- **Complexity**: Medium
- **Priority**: LOW
- **Dependencies**: IAuthService, FormTextField, FormPasswordField
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Renders login form
  2. Validates credentials
  3. Redirects on success
- **Mocking Requirements**:
  - `IAuthService.LoginAsync()`

#### Pages/Auth/Welcome
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Pages/Auth/Welcome.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Pages/Auth/WelcomeTests.cs`
- **Complexity**: Simple
- **Priority**: LOW
- **Dependencies**: None
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Renders welcome message
  2. Shows getting started links
- **Mocking Requirements**: None

#### Pages/Auth/PersonalAccountNotSupported
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Pages/Auth/PersonalAccountNotSupported.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Pages/Auth/PersonalAccountNotSupportedTests.cs`
- **Complexity**: Simple
- **Priority**: LOW
- **Dependencies**: None
- **Code-Behind**: Likely inline
- **Test Scenarios**:
  1. Renders error message
  2. Shows instructions
- **Mocking Requirements**: None

---

### Admin Pages (Lower Priority)

#### Pages/Admin/AgentConfiguration
- **Path**: `/home/user/PRFactory/src/PRFactory.Web/Pages/Admin/AgentConfiguration.razor`
- **Test Path**: `/home/user/PRFactory/tests/PRFactory.Web.Tests/Pages/Admin/AgentConfigurationTests.cs`
- **Complexity**: Complex
- **Priority**: LOW
- **Dependencies**: IAgentService, LlmProviderAssignmentPanel, ModelOverridesEditor
- **Code-Behind**: Yes
- **Test Scenarios**:
  1. Loads configuration
  2. Renders panels
  3. Saves changes
- **Mocking Requirements**:
  - `IAgentService.GetConfigurationAsync()`
  - `IAgentService.SaveConfigurationAsync()`

---

## Testing Priority Order (If Pursuing This Batch)

### Phase 1 (High-Value Core Pages, ~4 hours)
1. Tickets/Index
2. Tickets/Detail (without SignalR)
3. Tickets/Create
4. Index (Dashboard)

### Phase 2 (Medium-Value Pages, ~4 hours)
5. Repositories/Index, Create, Edit, Detail
6. Tenants/Index, Create, Edit, Detail

### Phase 3 (Lower-Value Pages, ~4 hours)
7. Settings/General
8. Settings/LlmProviders/Index
9. Workflows/Index, Events

### Phase 4 (Auth and Simple Pages, ~2 hours)
10. Auth/Welcome, PersonalAccountNotSupported
11. GettingStarted

### Phase 5 (Admin and Remaining, ~2-4 hours)
12. Settings/Users pages
13. AgentPrompts pages
14. Errors pages
15. Admin/AgentConfiguration

---

## Success Criteria

- ✅ High-value pages tested (Tickets, Dashboard)
- ✅ Each page has 3-6 core test scenarios covered
- ✅ Services properly mocked with Moq
- ✅ NavigationManager mocked where needed
- ✅ Tests use xUnit Assert (not FluentAssertions)
- ✅ Tests use bUnit RenderComponent pattern
- ✅ All tests pass: `dotnet test`
- ✅ Code compiles: `dotnet build`
- ✅ Format checks pass: `dotnet format --verify-no-changes`

---

## Notes

- **This batch is OPTIONAL** for 80% coverage target
- Pages are the most complex to test (multiple dependencies, navigation, state)
- **Defer SignalR testing** - too complex for initial coverage goal
- Focus on high-value pages first (Tickets, Dashboard)
- Many pages are CRUD operations (similar patterns)
- Consider testing page templates rather than every CRUD page
- If time-constrained, test representative pages from each category
