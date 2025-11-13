# Epic 6: Admin UI for Configuration

**Status:** 📋 Not Started (0% complete)
**Priority:** P0 (Critical - Blocks production use)
**Effort:** 2-3 weeks (UI, services, testing)
**Dependencies:** Authentication (PR #52 - Complete)

---

## Strategic Goal

Enable **self-service configuration** for PRFactory tenants by building Admin UI pages for repository management, LLM provider configuration, tenant settings, and user role management.

**Business Impact:**
- ✅ Unblocks customer onboarding without engineering support
- ✅ Enables self-service trials and demos
- ✅ Validates product with non-technical stakeholders
- ✅ Foundation for SaaS business model

**Current State:**
- ❌ No UI for repository management (requires direct DB access)
- ❌ No UI for LLM provider configuration (TenantLlmProvider entity exists but unusable)
- ❌ No UI for tenant settings
- ❌ No UI for user role management
- ✅ All domain entities exist (Repository, TenantLlmProvider, Tenant, User)
- ✅ Authentication and RBAC implemented (PR #52)

**Remaining Work:**
- Build 4 admin UI sections: Repositories, LLM Providers, Tenant Settings, User Management
- Create application services (IRepositoryService, ITenantLlmProviderService, etc.)
- Add API controllers for external integrations (optional)
- Implement connection testing for repositories and LLM providers
- Write comprehensive tests

---

## Success Criteria

✅ **Must Have (This Epic):**
- Tenant admins can add/edit/delete repositories via UI
- Tenant admins can configure LLM providers via UI
- Tenant admins can test repository/LLM provider connections
- Tenant admins can update tenant settings (workflow options, approval thresholds)
- Tenant admins can assign roles to users (Owner, Admin, Member, Viewer)
- All credentials encrypted at rest
- Role-based access control (only Owners/Admins can access settings)
- Comprehensive unit tests (80% coverage minimum)

✅ **Nice to Have (Future):**
- Bulk repository import
- LLM provider health dashboard
- User activity audit logs
- Tenant-level analytics

---

## Current Architecture (What Already Exists)

### Domain Entities

**Repository Entity** (`/src/PRFactory.Domain/Entities/Repository.cs`):
- ✅ Complete domain entity (180 lines)
- Properties: `Id`, `TenantId`, `Name`, `GitPlatform`, `CloneUrl`, `DefaultBranch`, `AccessToken`, `IsActive`
- Methods: `Create()`, `UpdateAccessToken()`, `UpdateDefaultBranch()`, `Activate()`, `Deactivate()`, `RecordAccess()`
- Validation: Git platform must be one of: GitHub, Bitbucket, AzureDevOps
- Encrypted field: `AccessToken`

**TenantLlmProvider Entity** (`/src/PRFactory.Domain/Entities/TenantLlmProvider.cs`):
- ✅ Complete domain entity (320 lines, PR #48)
- Properties: `Id`, `TenantId`, `Name`, `ProviderType`, `UsesOAuth`, `EncryptedApiToken`, `ApiBaseUrl`, `DefaultModel`, `IsDefault`, `IsActive`
- Methods: `CreateApiKeyProvider()`, `CreateOAuthProvider()`, `UpdateApiToken()`, `UpdateConfiguration()`, `SetAsDefault()`, `Activate()`, `Deactivate()`
- Supported providers: AnthropicNative, ZAi, MinimaxM2, OpenRouter, TogetherAI, Custom
- Encrypted field: `EncryptedApiToken`

**Tenant Entity** (`/src/PRFactory.Domain/Entities/Tenant.cs`):
- ✅ Complete domain entity (340 lines)
- Properties: `Id`, `Name`, `IdentityProvider`, `ExternalTenantId`, `TicketPlatform`, `TicketPlatformUrl`, `IsActive`
- Configuration: `TenantConfiguration` (JSON field with 20+ settings)
- Methods: `Create()`, `UpdateCredentials()`, `UpdatePlatformSettings()`, `UpdateConfiguration()`, `Activate()`, `Deactivate()`
- LLM Provider Methods: `AddLlmProvider()`, `RemoveLlmProvider()`, `GetDefaultLlmProvider()`, `SetDefaultLlmProvider()`

**User Entity** (`/src/PRFactory.Domain/Entities/User.cs`):
- ✅ Complete domain entity (PR #52)
- Properties: `Id`, `TenantId`, `Email`, `DisplayName`, `AvatarUrl`, `Role`, `IsActive`, `IdentityProvider`
- Roles: Owner, Admin, Member, Viewer
- Methods: `Create()`, `UpdateProfile()`, `UpdateRole()`, `Activate()`, `Deactivate()`

**TenantConfiguration** (value object in Tenant.cs):
- ✅ Stored as JSON field in Tenants table
- Properties:
  - `AutoImplementAfterPlanApproval` (bool) - Whether to run implementation after plan approval
  - `MaxRetries` (int) - Max retries for failed operations
  - `ClaudeModel` (string) - Model for legacy Claude API key
  - `MaxTokensPerRequest` (int) - Token limit
  - `ApiTimeoutSeconds` (int) - Timeout for API calls
  - `EnableVerboseLogging` (bool) - Logging level
  - `AllowedRepositories` (string[]) - Whitelist (empty = all allowed)
  - `EnableAutoCodeReview` (bool) - Auto trigger code review after PR
  - `CodeReviewLlmProviderId` (Guid?) - Provider for code review
  - `ImplementationLlmProviderId` (Guid?) - Provider for implementation
  - `PlanningLlmProviderId` (Guid?) - Provider for planning
  - `AnalysisLlmProviderId` (Guid?) - Provider for analysis
  - `MaxCodeReviewIterations` (int) - Max code review loops
  - `AutoApproveIfNoIssues` (bool) - Auto-approve PRs if no issues

### Infrastructure Services

**CurrentUserService** (`/src/PRFactory.Infrastructure/Application/CurrentUserService.cs`):
- ✅ Complete (PR #52, 129 lines)
- Methods:
  - `GetCurrentUserIdAsync()` - Gets authenticated user ID from claims
  - `GetCurrentUserAsync()` - Gets full User entity
  - `GetCurrentTenantIdAsync()` - Gets current tenant ID from claims
  - `IsAuthenticatedAsync()` - Checks authentication status

**EncryptionService** (`/src/PRFactory.Infrastructure/Persistence/Encryption/AesEncryptionService.cs`):
- ✅ Complete (149 lines)
- AES-256-GCM authenticated encryption
- Methods: `Encrypt()`, `Decrypt()`
- Used for: AccessToken, EncryptedApiToken, TicketPlatformApiToken

### Repositories

**Existing Repositories:**
- ✅ `IRepositoryRepository` / `RepositoryRepository` (basic CRUD exists)
- ✅ `ITenantRepository` / `TenantRepository` (basic CRUD exists)
- ✅ `IUserRepository` / `UserRepository` (complete, PR #52)
- ❌ `ITenantLlmProviderRepository` / `TenantLlmProviderRepository` (NOT YET IMPLEMENTED)

### Missing Services (Need Implementation)

**IRepositoryService** - NOT IMPLEMENTED
- `GetRepositoriesForTenantAsync(Guid tenantId)` - List all repositories
- `GetRepositoryByIdAsync(Guid id)` - Get single repository
- `CreateRepositoryAsync(CreateRepositoryDto dto)` - Add repository
- `UpdateRepositoryAsync(Guid id, UpdateRepositoryDto dto)` - Update repository
- `DeleteRepositoryAsync(Guid id)` - Soft delete (deactivate)
- `TestRepositoryConnectionAsync(Guid id)` - Test git connection
- `GetRepositoryStatisticsAsync(Guid id)` - Usage stats (# tickets, last access)

**ITenantLlmProviderService** - NOT IMPLEMENTED
- `GetProvidersForTenantAsync(Guid tenantId)` - List all providers
- `GetProviderByIdAsync(Guid id)` - Get single provider
- `CreateApiKeyProviderAsync(CreateApiKeyProviderDto dto)` - Add API key provider
- `CreateOAuthProviderAsync(CreateOAuthProviderDto dto)` - Add OAuth provider
- `UpdateProviderAsync(Guid id, UpdateProviderDto dto)` - Update provider
- `DeleteProviderAsync(Guid id)` - Soft delete (deactivate)
- `SetDefaultProviderAsync(Guid id)` - Set as tenant default
- `TestProviderConnectionAsync(Guid id)` - Test LLM connection
- `RefreshOAuthTokenAsync(Guid id)` - Refresh OAuth token

**ITenantConfigurationService** - PARTIALLY IMPLEMENTED
- `GetConfigurationAsync(Guid tenantId)` - Get tenant config
- `UpdateConfigurationAsync(Guid tenantId, TenantConfigurationDto dto)` - Update config
- Note: Some methods may exist in ITenantService, needs verification

**IUserManagementService** - NOT IMPLEMENTED (IUserService exists but limited)
- `GetUsersForTenantAsync(Guid tenantId)` - List all users
- `GetUserByIdAsync(Guid id)` - Get single user
- `UpdateUserRoleAsync(Guid id, UserRole role)` - Change user role
- `ActivateUserAsync(Guid id)` - Activate user
- `DeactivateUserAsync(Guid id)` - Deactivate user
- `GetUserStatisticsAsync(Guid id)` - User activity stats

---

## Implementation Plan

### Phase 1: Foundation (Week 1)

**1.1 Create Missing Repositories**
- `ITenantLlmProviderRepository` interface
- `TenantLlmProviderRepository` implementation (EF Core)
- Unit tests (CRUD operations, tenant isolation)

**1.2 Create Application Services**
- `IRepositoryService` / `RepositoryService`
- `ITenantLlmProviderService` / `TenantLlmProviderService`
- `ITenantConfigurationService` / `TenantConfigurationService` (or extend existing)
- `IUserManagementService` / `UserManagementService`
- Unit tests for all services (80% coverage minimum)

**1.3 Create DTOs**
- `RepositoryDto`, `CreateRepositoryDto`, `UpdateRepositoryDto`
- `TenantLlmProviderDto`, `CreateApiKeyProviderDto`, `UpdateProviderDto`
- `TenantConfigurationDto`
- `UserManagementDto`

**1.4 Create Web Service Facades** (Blazor Server pattern)
- `RepositoryWebService` - Facade for Blazor components
- `TenantLlmProviderWebService` - Facade for Blazor components
- `TenantSettingsWebService` - Facade for Blazor components
- `UserManagementWebService` - Facade for Blazor components

**Deliverables:**
- ✅ All repositories implemented and tested
- ✅ All application services implemented and tested
- ✅ All DTOs defined
- ✅ Web service facades implemented

---

### Phase 2: Repository Management UI (Week 1-2)

**2.1 Create `/settings/repositories` Pages**

**Page: Repository List** (`/src/PRFactory.Web/Pages/Settings/Repositories/Index.razor`)
- Display table of all repositories for current tenant
- Columns: Name, Platform, Clone URL, Default Branch, Last Accessed, Status
- Actions: Add, Edit, Delete (deactivate), Test Connection
- Filter by platform (GitHub, Bitbucket, Azure DevOps)
- Search by name

**Page: Add Repository** (`/src/PRFactory.Web/Pages/Settings/Repositories/Create.razor`)
- Form fields:
  - Repository Name (text)
  - Git Platform (dropdown: GitHub, Bitbucket, AzureDevOps)
  - Clone URL (text with validation)
  - Access Token (password field)
  - Default Branch (text, default: "main")
- "Test Connection" button (validates credentials before saving)
- "Save" button (encrypts token, saves to DB)
- Error handling and validation messages

**Page: Edit Repository** (`/src/PRFactory.Web/Pages/Settings/Repositories/Edit.razor`)
- Pre-populated form (same fields as Create)
- Can update access token (re-encrypt)
- Can change default branch
- Can activate/deactivate repository
- "Test Connection" button
- "Save" button

**Page: Repository Details** (`/src/PRFactory.Web/Pages/Settings/Repositories/Detail.razor`)
- Read-only view of repository
- Statistics: # tickets, last accessed, created at
- List of tickets using this repository
- "Edit" and "Delete" buttons

**2.2 Create Repository UI Components**

**RepositoryForm.razor** (`/src/PRFactory.Web/Components/Settings/RepositoryForm.razor`)
- Reusable form component (used by Create and Edit pages)
- Props: Repository data, IsEdit mode
- Validation using EditForm + DataAnnotations
- Test Connection button with loading state
- Emits OnSave event

**RepositoryListItem.razor** (`/src/PRFactory.Web/Components/Settings/RepositoryListItem.razor`)
- Single row in repository table
- Display: Name, Platform badge, Clone URL (truncated), Status badge
- Actions: Edit, Delete, Test

**RepositoryConnectionTest.razor** (`/src/PRFactory.Web/Components/Settings/RepositoryConnectionTest.razor`)
- Modal dialog for testing repository connection
- Shows: Git clone test, branch fetch test, connection status
- Success/failure feedback with details

**RepositoryStatistics.razor** (`/src/PRFactory.Web/Components/Settings/RepositoryStatistics.razor`)
- Display card showing:
  - Total tickets using this repository
  - Last accessed timestamp
  - Total commits/PRs created
  - Success/failure rate

**2.3 Backend Implementation**

**RepositoryService.cs** (`/src/PRFactory.Infrastructure/Application/RepositoryService.cs`)
```csharp
public class RepositoryService : IRepositoryService
{
    private readonly IRepositoryRepository _repo;
    private readonly IAesEncryptionService _encryption;
    private readonly ILocalGitService _gitService;
    private readonly ICurrentUserService _currentUser;

    public async Task<RepositoryDto> CreateRepositoryAsync(CreateRepositoryDto dto)
    {
        // 1. Validate tenant access
        var tenantId = await _currentUser.GetCurrentTenantIdAsync();

        // 2. Encrypt access token
        var encryptedToken = _encryption.Encrypt(dto.AccessToken);

        // 3. Create entity
        var repository = Repository.Create(
            tenantId,
            dto.Name,
            dto.GitPlatform,
            dto.CloneUrl,
            encryptedToken,
            dto.DefaultBranch);

        // 4. Save to DB
        await _repo.AddAsync(repository);

        return MapToDto(repository);
    }

    public async Task<ConnectionTestResult> TestRepositoryConnectionAsync(Guid id)
    {
        // 1. Get repository
        var repository = await _repo.GetByIdAsync(id);

        // 2. Decrypt token
        var token = _encryption.Decrypt(repository.AccessToken);

        // 3. Test git clone (shallow)
        var result = await _gitService.TestConnectionAsync(
            repository.CloneUrl,
            token,
            repository.DefaultBranch);

        return result;
    }
}
```

**Deliverables:**
- ✅ Repository management pages (Index, Create, Edit, Detail)
- ✅ Reusable UI components
- ✅ RepositoryService with connection testing
- ✅ Integration tests for repository CRUD

---

### Phase 3: LLM Provider Configuration UI (Week 2)

**3.1 Create `/settings/llm-providers` Pages**

**Page: LLM Provider List** (`/src/PRFactory.Web/Pages/Settings/LlmProviders/Index.razor`)
- Display table of all LLM providers for current tenant
- Columns: Name, Provider Type, Auth Method, Default Model, Is Default, Status
- Actions: Add, Edit, Delete, Set as Default, Test Connection
- Filter by provider type
- Highlight default provider

**Page: Add LLM Provider** (`/src/PRFactory.Web/Pages/Settings/LlmProviders/Create.razor`)
- Step 1: Choose provider type (radio buttons)
  - Anthropic Native (OAuth)
  - Z.ai Unified API
  - Minimax M2
  - OpenRouter
  - Together AI
  - Custom
- Step 2: Configure provider (dynamic form based on type)
  - **OAuth Provider (Anthropic Native):**
    - Provider Name (text)
    - Default Model (dropdown: claude-sonnet-4-5, claude-opus-4-5, etc.)
    - "Authorize with Anthropic" button (OAuth flow)
  - **API Key Providers (Z.ai, Minimax, OpenRouter, Together, Custom):**
    - Provider Name (text)
    - API Key (password field)
    - API Base URL (text, optional for non-custom)
    - Default Model (text)
    - Timeout (ms) (number, default: 300000)
    - Model Overrides (JSON editor, optional)
    - Disable Non-Essential Traffic (checkbox)
- "Test Connection" button
- "Save" button

**Page: Edit LLM Provider** (`/src/PRFactory.Web/Pages/Settings/LlmProviders/Edit.razor`)
- Pre-populated form (same fields as Create)
- Cannot change provider type (immutable)
- Can update API key/token (re-encrypt)
- Can change default model and settings
- "Test Connection" button
- "Save" button

**Page: LLM Provider Details** (`/src/PRFactory.Web/Pages/Settings/LlmProviders/Detail.razor`)
- Read-only view of provider
- Statistics: # tickets using this provider, last used, created at
- Model configuration display
- "Edit" and "Delete" buttons

**3.2 Create LLM Provider UI Components**

**LlmProviderForm.razor** (`/src/PRFactory.Web/Components/Settings/LlmProviderForm.razor`)
- Multi-step wizard component
- Step 1: Provider type selection (radio cards)
- Step 2: Dynamic form based on provider type
- Validation using EditForm + DataAnnotations
- Test Connection button with loading state
- Emits OnSave event

**LlmProviderListItem.razor** (`/src/PRFactory.Web/Components/Settings/LlmProviderListItem.razor`)
- Single row in provider table
- Display: Name, Provider badge, Auth method badge, Default model, Is Default (star icon), Status badge
- Actions: Edit, Delete, Set as Default, Test

**LlmProviderConnectionTest.razor** (`/src/PRFactory.Web/Components/Settings/LlmProviderConnectionTest.razor`)
- Modal dialog for testing LLM connection
- Test prompt: "Hello, respond with 'OK' if you receive this message."
- Shows: Connection status, response time, model used
- Success/failure feedback with details

**ProviderTypeSelector.razor** (`/src/PRFactory.Web/Components/Settings/ProviderTypeSelector.razor`)
- Radio card selector for provider types
- Each card shows: Icon, Name, Description, Auth method
- Highlights selected provider

**3.3 Backend Implementation**

**TenantLlmProviderService.cs** (`/src/PRFactory.Infrastructure/Application/TenantLlmProviderService.cs`)
```csharp
public class TenantLlmProviderService : ITenantLlmProviderService
{
    private readonly ITenantLlmProviderRepository _repo;
    private readonly IAesEncryptionService _encryption;
    private readonly ILlmProviderFactory _llmFactory;
    private readonly ICurrentUserService _currentUser;

    public async Task<TenantLlmProviderDto> CreateApiKeyProviderAsync(CreateApiKeyProviderDto dto)
    {
        // 1. Validate tenant access
        var tenantId = await _currentUser.GetCurrentTenantIdAsync();

        // 2. Encrypt API key
        var encryptedKey = _encryption.Encrypt(dto.ApiKey);

        // 3. Create configuration
        var config = new ApiKeyProviderConfiguration
        {
            TenantId = tenantId,
            Name = dto.Name,
            ProviderType = dto.ProviderType,
            EncryptedApiToken = encryptedKey,
            ApiBaseUrl = dto.ApiBaseUrl,
            DefaultModel = dto.DefaultModel,
            TimeoutMs = dto.TimeoutMs,
            DisableNonEssentialTraffic = dto.DisableNonEssentialTraffic,
            ModelOverrides = dto.ModelOverrides
        };

        // 4. Create entity
        var provider = TenantLlmProvider.CreateApiKeyProvider(config);

        // 5. Save to DB
        await _repo.AddAsync(provider);

        return MapToDto(provider);
    }

    public async Task<ConnectionTestResult> TestProviderConnectionAsync(Guid id)
    {
        // 1. Get provider
        var provider = await _repo.GetByIdAsync(id);

        // 2. Decrypt token
        var token = _encryption.Decrypt(provider.EncryptedApiToken);

        // 3. Create LLM client
        var llmProvider = await _llmFactory.CreateProviderAsync(
            provider.ProviderType,
            token,
            provider.ApiBaseUrl);

        // 4. Send test prompt
        var result = await llmProvider.SendPromptAsync(
            "Hello, respond with 'OK' if you receive this message.");

        return new ConnectionTestResult
        {
            Success = result.Contains("OK"),
            ResponseTime = result.ResponseTimeMs,
            Message = result.Success ? "Connection successful" : result.Error
        };
    }
}
```

**Deliverables:**
- ✅ LLM provider management pages (Index, Create, Edit, Detail)
- ✅ Reusable UI components (wizard, provider selector)
- ✅ TenantLlmProviderService with connection testing
- ✅ Integration tests for LLM provider CRUD

---

### Phase 4: Tenant Settings UI (Week 2)

**4.1 Create `/settings/general` Page**

**Page: Tenant Settings** (`/src/PRFactory.Web/Pages/Settings/General.razor`)
- Single page with tabbed sections:
  - **General Tab:**
    - Tenant Name (text, read-only)
    - Identity Provider (text, read-only)
    - Ticket Platform (text, read-only)
    - Created At (text, read-only)
  - **Workflow Settings Tab:**
    - Auto-Implementation After Plan Approval (checkbox)
    - Max Retries for Failed Operations (number, 1-10)
    - API Timeout Seconds (number, 30-600)
    - Enable Verbose Logging (checkbox)
    - Allowed Repositories (multi-select or tags input)
  - **Code Review Settings Tab:**
    - Enable Auto Code Review (checkbox)
    - Max Code Review Iterations (number, 1-10)
    - Auto-Approve If No Issues (checkbox)
    - Require Human Approval After Review (checkbox, read-only for now)
  - **LLM Provider Assignment Tab:**
    - Analysis LLM Provider (dropdown)
    - Planning LLM Provider (dropdown)
    - Implementation LLM Provider (dropdown)
    - Code Review LLM Provider (dropdown)
    - Note: All dropdowns show tenant's LLM providers + "Use Default" option
- "Save Changes" button (saves all tabs)

**4.2 Create Tenant Settings UI Components**

**TenantConfigurationForm.razor** (`/src/PRFactory.Web/Components/Settings/TenantConfigurationForm.razor`)
- Tabbed form component
- Tabs: General, Workflow, Code Review, LLM Providers
- Validation using EditForm + DataAnnotations
- Emits OnSave event

**WorkflowSettingsPanel.razor** (`/src/PRFactory.Web/Components/Settings/WorkflowSettingsPanel.razor`)
- Panel for workflow-related settings
- Clear descriptions for each setting
- Validation rules displayed

**CodeReviewSettingsPanel.razor** (`/src/PRFactory.Web/Components/Settings/CodeReviewSettingsPanel.razor`)
- Panel for code review settings
- Dependency logic (if EnableAutoCodeReview = false, disable other options)

**LlmProviderAssignmentPanel.razor** (`/src/PRFactory.Web/Components/Settings/LlmProviderAssignmentPanel.razor`)
- Panel for assigning LLM providers to agent roles
- 4 dropdowns (Analysis, Planning, Implementation, Code Review)
- Each dropdown: "Use Default" + list of active providers
- Help text explaining each role

**4.3 Backend Implementation**

**TenantConfigurationService.cs** (`/src/PRFactory.Infrastructure/Application/TenantConfigurationService.cs`)
```csharp
public class TenantConfigurationService : ITenantConfigurationService
{
    private readonly ITenantRepository _tenantRepo;
    private readonly ICurrentUserService _currentUser;

    public async Task<TenantConfigurationDto> GetConfigurationAsync()
    {
        var tenantId = await _currentUser.GetCurrentTenantIdAsync();
        var tenant = await _tenantRepo.GetByIdAsync(tenantId);

        return MapToDto(tenant.Configuration);
    }

    public async Task UpdateConfigurationAsync(TenantConfigurationDto dto)
    {
        var tenantId = await _currentUser.GetCurrentTenantIdAsync();
        var tenant = await _tenantRepo.GetByIdAsync(tenantId);

        var config = new TenantConfiguration
        {
            AutoImplementAfterPlanApproval = dto.AutoImplementAfterPlanApproval,
            MaxRetries = dto.MaxRetries,
            ApiTimeoutSeconds = dto.ApiTimeoutSeconds,
            EnableVerboseLogging = dto.EnableVerboseLogging,
            AllowedRepositories = dto.AllowedRepositories,
            EnableAutoCodeReview = dto.EnableAutoCodeReview,
            MaxCodeReviewIterations = dto.MaxCodeReviewIterations,
            AutoApproveIfNoIssues = dto.AutoApproveIfNoIssues,
            CodeReviewLlmProviderId = dto.CodeReviewLlmProviderId,
            ImplementationLlmProviderId = dto.ImplementationLlmProviderId,
            PlanningLlmProviderId = dto.PlanningLlmProviderId,
            AnalysisLlmProviderId = dto.AnalysisLlmProviderId
        };

        tenant.UpdateConfiguration(config);
        await _tenantRepo.UpdateAsync(tenant);
    }
}
```

**Deliverables:**
- ✅ Tenant settings page with tabbed sections
- ✅ Reusable configuration panels
- ✅ TenantConfigurationService
- ✅ Integration tests for configuration updates

---

### Phase 5: User Management UI (Week 2-3)

**5.1 Create `/settings/users` Pages**

**Page: User List** (`/src/PRFactory.Web/Pages/Settings/Users/Index.razor`)
- Display table of all users for current tenant
- Columns: Display Name, Email, Role, Identity Provider, Last Seen, Status
- Actions: Edit Role, Activate, Deactivate
- Filter by role (Owner, Admin, Member, Viewer)
- Filter by status (Active, Inactive)
- Search by name/email
- Note: No "Add User" button (users are auto-provisioned via OAuth)

**Page: Edit User** (`/src/PRFactory.Web/Pages/Settings/Users/Edit.razor`)
- Form fields:
  - Display Name (text, read-only)
  - Email (text, read-only)
  - Identity Provider (text, read-only)
  - Role (dropdown: Owner, Admin, Member, Viewer)
  - Status (toggle: Active / Inactive)
- "Save" button
- Warning when changing Owner role

**Page: User Details** (`/src/PRFactory.Web/Pages/Settings/Users/Detail.razor`)
- Read-only view of user
- Display: Name, Email, Role, Avatar, Created At, Last Seen
- Statistics: # plan reviews, # comments, # tickets assigned
- Activity timeline (recent plan reviews, comments)
- "Edit" button

**5.2 Create User Management UI Components**

**UserListItem.razor** (`/src/PRFactory.Web/Components/Settings/UserListItem.razor`)
- Single row in user table
- Display: Avatar, Name, Email, Role badge, Last Seen (relative time), Status badge
- Actions: Edit, Deactivate/Activate

**UserRoleEditor.razor** (`/src/PRFactory.Web/Components/Settings/UserRoleEditor.razor`)
- Form for changing user role
- Dropdown: Owner, Admin, Member, Viewer
- Warning message if changing Owner role (only one Owner allowed per tenant)
- Confirmation dialog for role changes

**UserStatistics.razor** (`/src/PRFactory.Web/Components/Settings/UserStatistics.razor`)
- Display card showing:
  - Total plan reviews
  - Total comments
  - Approval rate
  - Recent activity timeline

**5.3 Backend Implementation**

**UserManagementService.cs** (`/src/PRFactory.Infrastructure/Application/UserManagementService.cs`)
```csharp
public class UserManagementService : IUserManagementService
{
    private readonly IUserRepository _userRepo;
    private readonly ICurrentUserService _currentUser;

    public async Task<List<UserManagementDto>> GetUsersForTenantAsync()
    {
        var tenantId = await _currentUser.GetCurrentTenantIdAsync();
        var users = await _userRepo.GetByTenantIdAsync(tenantId);

        return users.Select(MapToDto).ToList();
    }

    public async Task UpdateUserRoleAsync(Guid userId, UserRole newRole)
    {
        // 1. Validate current user has permission (Owner or Admin only)
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser.Role != UserRole.Owner && currentUser.Role != UserRole.Admin)
            throw new UnauthorizedAccessException("Only Owners and Admins can change user roles");

        // 2. Validate not removing last Owner
        if (newRole != UserRole.Owner)
        {
            var tenantId = await _currentUser.GetCurrentTenantIdAsync();
            var owners = await _userRepo.GetUsersByRoleAsync(tenantId, UserRole.Owner);

            if (owners.Count == 1 && owners[0].Id == userId)
                throw new InvalidOperationException("Cannot remove the last Owner role from tenant");
        }

        // 3. Update role
        var user = await _userRepo.GetByIdAsync(userId);
        user.UpdateRole(newRole);
        await _userRepo.UpdateAsync(user);
    }
}
```

**Deliverables:**
- ✅ User management pages (Index, Edit, Detail)
- ✅ Reusable UI components
- ✅ UserManagementService with role management
- ✅ RBAC validation for role changes
- ✅ Integration tests for user management

---

## UI Mockups

### Repository Management

```
┌─────────────────────────────────────────────────────────────────────┐
│ Settings > Repositories                                             │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  [+ Add Repository]                          [Search: _________ 🔍] │
│                                                                      │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │ Name            Platform    Clone URL          Last Access   │  │
│  ├──────────────────────────────────────────────────────────────┤  │
│  │ my-app          [GitHub]    github.com/...     2h ago     ⚙️│  │
│  │ backend-api     [Azure]     dev.azure.com/...  1d ago     ⚙️│  │
│  │ legacy-system   [Bitbucket] bitbucket.org/...  3d ago     ⚙️│  │
│  └──────────────────────────────────────────────────────────────┘  │
│                                                                      │
│  ⚙️ Actions: [Edit] [Test Connection] [Delete]                     │
└─────────────────────────────────────────────────────────────────────┘
```

```
┌─────────────────────────────────────────────────────────────────────┐
│ Add Repository                                         [X Close]     │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  Repository Name *                                                   │
│  [_________________________________]                                 │
│                                                                      │
│  Git Platform *                                                      │
│  ( ) GitHub  ( ) Bitbucket  ( ) Azure DevOps                        │
│                                                                      │
│  Clone URL *                                                         │
│  [_________________________________]                                 │
│  ℹ️ Format: https://github.com/owner/repo.git                       │
│                                                                      │
│  Access Token *                                                      │
│  [_________________________________] 👁️                              │
│  ℹ️ Required permissions: read repo, write branches                 │
│                                                                      │
│  Default Branch                                                      │
│  [main________________________]                                      │
│                                                                      │
│  [Test Connection]                       [Cancel]  [Save Repository]│
└─────────────────────────────────────────────────────────────────────┘
```

### LLM Provider Configuration

```
┌─────────────────────────────────────────────────────────────────────┐
│ Settings > LLM Providers                                            │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  [+ Add LLM Provider]                    [Filter: All ▾]            │
│                                                                      │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │ Name            Type        Auth      Model      Default   │  │
│  ├──────────────────────────────────────────────────────────────┤  │
│  │ Production      [Claude]    OAuth     Sonnet 4.5   ⭐     ⚙️│  │
│  │ Z.ai Unified    [Z.ai]      API Key   GPT-4o              ⚙️│  │
│  │ Dev Minimax     [Minimax]   API Key   MiniMax-M2          ⚙️│  │
│  └──────────────────────────────────────────────────────────────┘  │
│                                                                      │
│  ⚙️ Actions: [Edit] [Set as Default] [Test] [Delete]               │
└─────────────────────────────────────────────────────────────────────┘
```

```
┌─────────────────────────────────────────────────────────────────────┐
│ Add LLM Provider - Step 1/2                            [X Close]     │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  Choose Provider Type:                                               │
│                                                                      │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐              │
│  │ 🤖 Anthropic │  │ 🌐 Z.ai      │  │ 🔧 Minimax   │              │
│  │ Native       │  │ Unified API  │  │ M2           │              │
│  │              │  │              │  │              │              │
│  │ [Select]     │  │ [Select]     │  │ [Select]     │              │
│  └──────────────┘  └──────────────┘  └──────────────┘              │
│                                                                      │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐              │
│  │ 🚀 OpenRouter│  │ ⚡ Together  │  │ ⚙️ Custom    │              │
│  │ 100+ Models  │  │ AI           │  │ Provider     │              │
│  │              │  │              │  │              │              │
│  │ [Select]     │  │ [Select]     │  │ [Select]     │              │
│  └──────────────┘  └──────────────┘  └──────────────┘              │
│                                                                      │
│                                                     [Cancel]  [Next] │
└─────────────────────────────────────────────────────────────────────┘
```

```
┌─────────────────────────────────────────────────────────────────────┐
│ Add LLM Provider - Step 2/2                            [X Close]     │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  Provider: Z.ai Unified API                                          │
│                                                                      │
│  Provider Name *                                                     │
│  [Z.ai Production_________________]                                  │
│                                                                      │
│  API Key *                                                           │
│  [_________________________________] 👁️                              │
│  ℹ️ Get your API key from https://z.ai/dashboard                    │
│                                                                      │
│  Default Model *                                                     │
│  [gpt-4o_______________________▾]                                    │
│  ℹ️ Available: gpt-4o, claude-sonnet-4-5, gemini-2-0-flash          │
│                                                                      │
│  API Timeout (ms)                                                    │
│  [300000_______________________]                                     │
│                                                                      │
│  ☐ Disable Non-Essential Traffic                                    │
│                                                                      │
│  [Test Connection]                       [Back]  [Cancel]  [Save]   │
└─────────────────────────────────────────────────────────────────────┘
```

### Tenant Settings

```
┌─────────────────────────────────────────────────────────────────────┐
│ Settings > General                                                  │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  [General] [Workflow] [Code Review] [LLM Providers]                 │
│  ───────────────────────────────────────────────────────            │
│                                                                      │
│  Tenant Name                                                         │
│  Acme Corporation                                                    │
│                                                                      │
│  Identity Provider                                                   │
│  Azure AD (microsoft.com)                                            │
│                                                                      │
│  Ticket Platform                                                     │
│  Jira Cloud (https://acme.atlassian.net)                            │
│                                                                      │
│  Created                                                             │
│  November 10, 2025                                                   │
│                                                                      │
│  Status                                                              │
│  🟢 Active                                                           │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

```
┌─────────────────────────────────────────────────────────────────────┐
│ Settings > General                                                  │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  [General] [Workflow] [Code Review] [LLM Providers]                 │
│          ───────────────────────────────────────────                │
│                                                                      │
│  ☑️ Auto-Implementation After Plan Approval                         │
│  ℹ️ Automatically start code implementation after plan is approved  │
│                                                                      │
│  Max Retries for Failed Operations                                  │
│  [3_________]  (1-10)                                                │
│                                                                      │
│  API Timeout (seconds)                                               │
│  [300_______]  (30-600)                                              │
│                                                                      │
│  ☐ Enable Verbose Logging                                           │
│  ℹ️ Detailed logging for troubleshooting (may impact performance)   │
│                                                                      │
│  Allowed Repositories (leave empty for all)                          │
│  [my-app] [backend-api]  [+ Add]                                    │
│                                                                      │
│                                                  [Cancel]  [Save]    │
└─────────────────────────────────────────────────────────────────────┘
```

### User Management

```
┌─────────────────────────────────────────────────────────────────────┐
│ Settings > Users                                                    │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ℹ️ Users are auto-provisioned via OAuth. Change roles below.      │
│                                                                      │
│  Filter: [All Roles ▾]  [Active ▾]         [Search: _________ 🔍]  │
│                                                                      │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │ Name           Email            Role      Last Seen   Status │  │
│  ├──────────────────────────────────────────────────────────────┤  │
│  │ 👤 Alice Smith alice@acme.com   [Owner ▾] 5m ago    🟢    ⚙️│  │
│  │ 👤 Bob Jones   bob@acme.com     [Admin ▾] 1h ago    🟢    ⚙️│  │
│  │ 👤 Carol Lee   carol@acme.com   [Member▾] 3d ago    🟢    ⚙️│  │
│  │ 👤 Dave Wilson dave@acme.com    [Viewer▾] 1w ago    🔴    ⚙️│  │
│  └──────────────────────────────────────────────────────────────┘  │
│                                                                      │
│  ⚙️ Actions: [Edit Role] [Deactivate] / [Activate]                 │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Database Schema (Already Complete)

All required database tables already exist:

- ✅ `Repositories` - Repository entity
- ✅ `TenantLlmProviders` - LLM provider configurations (PR #48)
- ✅ `Tenants` - Tenant entity with Configuration JSON field
- ✅ `Users` - User entity with Role field (PR #52)

**No migrations needed for this epic.**

---

## Component Reuse

Leverage existing UI components from `/src/PRFactory.Web/UI/`:

**From `/UI/Alerts/`:**
- `AlertMessage.razor` - Error/success messages
- `DemoModeBanner.razor` - Banner for demo mode

**From `/UI/Buttons/`:**
- `LoadingButton.razor` - Async action buttons (Save, Test Connection)
- `IconButton.razor` - Icon-based actions (Edit, Delete)

**From `/UI/Cards/`:**
- `Card.razor` - Container for forms and lists

**From `/UI/Display/`:**
- `StatusBadge.razor` - Display status (Active, Inactive)
- `LoadingSpinner.razor` - Loading indicators
- `EmptyState.razor` - Empty list states

**From `/UI/Forms/`:**
- `FormTextField.razor` - Text input with validation
- `FormTextAreaField.razor` - Textarea input
- `FormSelectField.razor` - Dropdown select

**From `/UI/Help/`:**
- `ContextualHelp.razor` - Tooltips for form fields

---

## Security & RBAC

**Authorization Requirements:**

1. **Repository Management:**
   - Only Owner and Admin roles can add/edit/delete repositories
   - Members can view repositories (read-only)
   - Viewers cannot access settings

2. **LLM Provider Configuration:**
   - Only Owner and Admin roles can add/edit/delete providers
   - Members can view providers (read-only)
   - Viewers cannot access settings

3. **Tenant Settings:**
   - Only Owner role can update tenant settings
   - Admin/Member/Viewer can view settings (read-only)

4. **User Management:**
   - Only Owner role can change user roles
   - Admin can view users but not change roles
   - Members/Viewers cannot access user management

**Implementation:**

```csharp
// In Blazor pages
@attribute [Authorize(Roles = "Owner,Admin")]

// In application services
public async Task CreateRepositoryAsync(CreateRepositoryDto dto)
{
    var currentUser = await _currentUser.GetCurrentUserAsync();
    if (currentUser.Role != UserRole.Owner && currentUser.Role != UserRole.Admin)
        throw new UnauthorizedAccessException("Insufficient permissions");

    // ... rest of implementation
}
```

**Credential Encryption:**

All sensitive fields are encrypted using AES-256-GCM:
- `Repository.AccessToken`
- `TenantLlmProvider.EncryptedApiToken`
- `Tenant.TicketPlatformApiToken`
- `Tenant.ClaudeApiKey`

---

## Testing Strategy

### Unit Tests (Target: 80% coverage)

**Domain Entity Tests:**
- ✅ Repository.Create() validation
- ✅ TenantLlmProvider.Create() validation
- ✅ User.UpdateRole() business logic

**Service Tests:**
- `RepositoryService` - CRUD operations, encryption, connection testing
- `TenantLlmProviderService` - CRUD operations, encryption, provider testing
- `TenantConfigurationService` - Configuration updates
- `UserManagementService` - Role management, RBAC validation

**Repository Tests:**
- `TenantLlmProviderRepository` - CRUD, tenant isolation, default provider queries

### Integration Tests

**Repository Connection Testing:**
- Test GitHub connection with valid/invalid token
- Test Bitbucket connection
- Test Azure DevOps connection
- Test connection with incorrect clone URL

**LLM Provider Connection Testing:**
- Test Anthropic OAuth provider
- Test Z.ai API key provider
- Test invalid API key error handling
- Test timeout configuration

**Authorization Tests:**
- Verify Owner/Admin can create repositories
- Verify Member cannot create repositories
- Verify Viewer cannot access settings
- Verify role-based access to all settings pages

### Blazor Component Tests (using bUnit)

**Repository Components:**
- `RepositoryForm.razor` - Form rendering, validation, submission
- `RepositoryListItem.razor` - Display rendering, action buttons
- `RepositoryConnectionTest.razor` - Modal rendering, test execution

**LLM Provider Components:**
- `LlmProviderForm.razor` - Wizard steps, dynamic form rendering
- `ProviderTypeSelector.razor` - Provider selection
- `LlmProviderConnectionTest.razor` - Connection testing

**Tenant Settings Components:**
- `TenantConfigurationForm.razor` - Tabs rendering, validation
- `WorkflowSettingsPanel.razor` - Checkbox and input rendering
- `LlmProviderAssignmentPanel.razor` - Dropdown population

**User Management Components:**
- `UserListItem.razor` - User display rendering
- `UserRoleEditor.razor` - Role dropdown, validation, confirmation dialog

### E2E Tests (Optional)

- Full repository add flow: Add → Test → Save → Verify in list
- Full LLM provider add flow: Select type → Configure → Test → Save
- Tenant settings update flow: Change settings → Save → Verify persistence
- User role change flow: Edit role → Confirm → Verify in list

---

## Acceptance Criteria

### Definition of Done

✅ **Repository Management:**
- [ ] Admin can add new repository via UI
- [ ] Admin can test repository connection before saving
- [ ] Access tokens are encrypted at rest
- [ ] Admin can edit repository settings
- [ ] Admin can deactivate (soft delete) repository
- [ ] List shows all repositories for current tenant
- [ ] Unit tests pass (80% coverage)
- [ ] Integration tests pass

✅ **LLM Provider Configuration:**
- [ ] Admin can add API key-based provider (Z.ai, Minimax, OpenRouter, etc.)
- [ ] Admin can add OAuth-based provider (Anthropic Native)
- [ ] Admin can test provider connection before saving
- [ ] API keys are encrypted at rest
- [ ] Admin can set provider as tenant default
- [ ] Admin can edit provider settings
- [ ] Admin can deactivate provider
- [ ] List shows all providers for current tenant
- [ ] Unit tests pass (80% coverage)
- [ ] Integration tests pass

✅ **Tenant Settings:**
- [ ] Owner can view all tenant settings
- [ ] Owner can update workflow settings
- [ ] Owner can update code review settings
- [ ] Owner can assign LLM providers to agent roles
- [ ] Settings are persisted to TenantConfiguration JSON field
- [ ] Validation prevents invalid settings (e.g., max retries > 10)
- [ ] Unit tests pass

✅ **User Management:**
- [ ] Admin can view all users for tenant
- [ ] Owner can change user roles
- [ ] Owner cannot remove last Owner role
- [ ] Admin can activate/deactivate users
- [ ] RBAC enforced (only Owner/Admin can access)
- [ ] Unit tests pass
- [ ] Integration tests pass

✅ **Security:**
- [ ] RBAC enforced on all settings pages
- [ ] Unauthorized users receive 403 Forbidden
- [ ] All credentials encrypted with AES-256-GCM
- [ ] Connection tests don't expose credentials in logs
- [ ] CSRF protection enabled

✅ **Documentation:**
- [ ] README updated with settings navigation
- [ ] User manual includes admin settings section
- [ ] API documentation for services
- [ ] Component documentation for reusable UI components

---

## Risks & Mitigations

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| Connection testing times out | Users cannot verify credentials | Medium | Implement 30s timeout with retry, clear error messages |
| OAuth flow for Anthropic fails | Cannot add OAuth providers | Low | Provide detailed error logging, fallback to API key entry |
| Role management breaks RBAC | Security vulnerability | Low | Comprehensive unit tests, manual security review |
| Encryption key not configured | Cannot save credentials | High | Validation on app startup, clear error message |
| Large tenant with 100+ repositories | UI performance degrades | Low | Implement pagination (20 items per page), search/filter |

---

## Future Enhancements (Post-Epic)

- Bulk repository import (CSV upload)
- LLM provider health monitoring dashboard
- User activity audit logs
- Tenant usage analytics (token consumption, API calls)
- Repository usage statistics (# PRs created, success rate)
- Webhook configuration UI for Jira/Azure DevOps
- Email notifications for configuration changes
- Multi-factor authentication for sensitive actions

---

## References

- [CLAUDE.md](../../CLAUDE.md) - Blazor Server architecture guidelines
- [IMPLEMENTATION_STATUS.md](../IMPLEMENTATION_STATUS.md) - Current implementation status
- [ROADMAP.md](../ROADMAP.md) - Planned enhancements
- [BLAZOR_TESTING_GUIDE.md](../BLAZOR_TESTING_GUIDE.md) - Component testing guide
- [PR #48](https://github.com/PRFactory/PRFactory/pull/48) - Multi-LLM Provider Support (TenantLlmProvider entity)
- [PR #52](https://github.com/PRFactory/PRFactory/pull/52) - Authentication & User Management (User entity, RBAC)

---

**Created:** 2025-11-13
**Author:** Product Team
**Status:** Ready for Implementation
**Next Steps:** Begin Phase 1 (Foundation) - Create missing repositories and application services
