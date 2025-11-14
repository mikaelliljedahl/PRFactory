# Epic 06 - Phase 5: User Management UI

**Status:** 📋 Ready for Implementation
**Estimated Effort:** 3-5 days (24-40 hours)
**Priority:** P0 - Critical (Blocks user administration)
**Dependencies:** Phase 1 Complete ✅, Authentication (PR #52) Complete ✅

---

## Table of Contents

- [Overview](#overview)
- [Goals & Success Criteria](#goals--success-criteria)
- [Architecture Overview](#architecture-overview)
- [User Roles](#user-roles)
- [Page Specifications](#page-specifications)
- [Component Specifications](#component-specifications)
- [Service Layer Integration](#service-layer-integration)
- [RBAC Business Rules](#rbac-business-rules)
- [Security & Authorization](#security--authorization)
- [Testing Strategy](#testing-strategy)
- [Implementation Checklist](#implementation-checklist)
- [UI Mockups](#ui-mockups)

---

## Overview

### What We're Building

User management pages that allow tenant Owners to:
- View all users in their tenant
- Search and filter users by role and status
- Change user roles (Owner, Admin, Member, Viewer)
- Activate/deactivate users
- View user statistics and activity
- Enforce "cannot remove last Owner" business rule

**Note**: Users are auto-provisioned via OAuth (PR #52). No manual user creation - users are added when they first sign in via Microsoft/Google OAuth.

### Why This Matters

**Business Impact:**
- Enables tenant self-service user administration
- Allows proper role-based access control management
- Supports team growth and user lifecycle management
- Validates enterprise-grade user management

**Technical Foundation:**
- Demonstrates OAuth user provisioning integration
- Shows RBAC enforcement patterns
- Establishes user management best practices

---

## Goals & Success Criteria

### Must Have (Phase 5)

- [x] **Service Layer Complete** - `UserManagementService` with role management ✅
- [ ] **User List Page** - Display all users for current tenant
- [ ] **Edit User Page** - Change user role with validation
- [ ] **User Detail Page** - View user statistics and activity
- [ ] **RBAC Enforcement** - Only Owner can change roles
- [ ] **Business Rule Enforcement** - Cannot remove last Owner
- [ ] **Search & Filter** - Search by name/email, filter by role/status
- [ ] **Responsive UI** - Works on desktop and tablet
- [ ] **80% Test Coverage** - Unit tests for all components and services

### Nice to Have (Future)

- [ ] User invitation system (send invite before they sign in)
- [ ] User activity timeline (detailed audit log)
- [ ] User session management (force logout)
- [ ] Bulk role changes
- [ ] User export (CSV/Excel)

---

## Architecture Overview

### Component Hierarchy

```
/settings/users (Route Group)
│
├── Index.razor (Page)
│   ├── UserListItem.razor (Component) ×N
│   ├── EmptyState.razor (UI Component)
│   ├── LoadingSpinner.razor (UI Component)
│   └── Pagination.razor (UI Component)
│
├── Edit.razor (Page)
│   └── UserRoleEditor.razor (Component)
│
└── Detail.razor (Page)
    ├── Card.razor (UI Component)
    ├── UserStatistics.razor (Component)
    ├── StatusBadge.razor (UI Component)
    └── IconButton.razor (UI Component)
```

### Data Flow

```
User signs in via OAuth (first time)
  ↓
ProvisioningService.ProvisionUserAsync()
  ↓
User entity created (first user = Owner, others = Member)
  ↓
User appears in User List
  ↓
Owner navigates to Edit User page
  ↓
Owner changes role to Admin
  ↓
UserManagementService.UpdateUserRoleAsync()
  ↓
Validation: Cannot remove last Owner
  ↓
User.UpdateRole() domain method
  ↓
Database persisted
  ↓
Success toast shown
```

### File Structure

```
PRFactory.Web/
├── Pages/
│   └── Settings/
│       └── Users/
│           ├── Index.razor
│           ├── Index.razor.cs
│           ├── Edit.razor
│           ├── Edit.razor.cs
│           ├── Detail.razor
│           └── Detail.razor.cs
│
└── Components/
    └── Settings/
        ├── UserListItem.razor
        ├── UserListItem.razor.cs
        ├── UserRoleEditor.razor
        ├── UserRoleEditor.razor.cs
        ├── UserStatistics.razor
        └── UserStatistics.razor.cs
```

---

## User Roles

### Role Hierarchy

| Role | Permissions | Description |
|------|-------------|-------------|
| **Owner** | Full access | Can manage all settings, users, and roles. At least one Owner required. |
| **Admin** | Most access | Can manage repositories, LLM providers, and tickets. Cannot manage users or tenant settings. |
| **Member** | Standard access | Can create and manage tickets. Can view settings. Cannot edit settings. |
| **Viewer** | Read-only | Can view tickets. Cannot view settings. |

### Role Assignment Rules

1. **First user = Owner**: When a tenant is auto-provisioned, the first user becomes Owner
2. **Subsequent users = Member**: All users after the first become Members by default
3. **Only Owner can change roles**: Admins cannot promote/demote users
4. **Cannot remove last Owner**: At least one Owner must exist at all times
5. **Cannot self-demote last Owner**: The last Owner cannot demote themselves

---

## Page Specifications

### 1. User List Page (`/settings/users`)

**File:** `/src/PRFactory.Web/Pages/Settings/Users/Index.razor`

#### Purpose
Display all users for the current tenant with search, filter, and action capabilities.

#### Route
```csharp
@page "/settings/users"
@attribute [Authorize(Roles = "Owner,Admin")]
```

**Note**: Only Owner and Admin can view user list.

#### Page State (Index.razor.cs)

```csharp
public partial class Index
{
    [Inject] private IUserManagementService UserService { get; set; } = null!;
    [Inject] private IToastService ToastService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private ICurrentUserService CurrentUserService { get; set; } = null!;

    private List<UserManagementDto> users = new();
    private List<UserManagementDto> filteredUsers = new();
    private bool isLoading = true;
    private string searchTerm = string.Empty;
    private UserRole? selectedRole = null;
    private bool showActiveOnly = true;

    private bool canEditUsers;

    protected override async Task OnInitializedAsync()
    {
        await LoadUsersAsync();
        await CheckPermissionsAsync();
    }

    private async Task LoadUsersAsync()
    {
        try
        {
            isLoading = true;
            users = await UserService.GetUsersForTenantAsync();
            ApplyFilters();
        }
        catch (Exception ex)
        {
            await ToastService.ShowErrorAsync($"Failed to load users: {ex.Message}");
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task CheckPermissionsAsync()
    {
        var currentUser = await CurrentUserService.GetCurrentUserAsync();
        canEditUsers = currentUser.Role == UserRole.Owner;
    }

    private void ApplyFilters()
    {
        filteredUsers = users
            .Where(u => string.IsNullOrEmpty(searchTerm) ||
                       u.DisplayName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                       u.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .Where(u => selectedRole == null || u.Role == selectedRole)
            .Where(u => !showActiveOnly || u.IsActive)
            .OrderByDescending(u => u.Role == UserRole.Owner)
            .ThenByDescending(u => u.Role == UserRole.Admin)
            .ThenBy(u => u.DisplayName)
            .ToList();
    }

    private void OnSearchChanged(string value)
    {
        searchTerm = value;
        ApplyFilters();
    }

    private void OnRoleFilterChanged(UserRole? role)
    {
        selectedRole = role;
        ApplyFilters();
    }

    private void OnShowActiveOnlyChanged(bool value)
    {
        showActiveOnly = value;
        ApplyFilters();
    }

    private void NavigateToEdit(Guid id)
    {
        if (canEditUsers)
            Navigation.NavigateTo($"/settings/users/edit/{id}");
    }

    private void NavigateToDetail(Guid id)
    {
        Navigation.NavigateTo($"/settings/users/{id}");
    }

    private async Task HandleActivateAsync(Guid id)
    {
        if (!canEditUsers)
        {
            await ToastService.ShowErrorAsync("You don't have permission to activate users");
            return;
        }

        try
        {
            await UserService.ActivateUserAsync(id);
            await ToastService.ShowSuccessAsync("User activated successfully");
            await LoadUsersAsync();
        }
        catch (Exception ex)
        {
            await ToastService.ShowErrorAsync($"Failed to activate user: {ex.Message}");
        }
    }

    private async Task HandleDeactivateAsync(Guid id)
    {
        if (!canEditUsers)
        {
            await ToastService.ShowErrorAsync("You don't have permission to deactivate users");
            return;
        }

        var confirmed = await JSRuntime.InvokeAsync<bool>(
            "confirm",
            "Are you sure you want to deactivate this user? They will no longer be able to access the system.");

        if (!confirmed)
            return;

        try
        {
            await UserService.DeactivateUserAsync(id);
            await ToastService.ShowSuccessAsync("User deactivated successfully");
            await LoadUsersAsync();
        }
        catch (Exception ex)
        {
            await ToastService.ShowErrorAsync($"Failed to deactivate user: {ex.Message}");
        }
    }
}
```

#### Markup (Index.razor)

```razor
@page "/settings/users"
@attribute [Authorize(Roles = "Owner,Admin")]

<PageContainer>
    <PageHeader Icon="bi-people" Title="Users">
        <div class="text-muted">
            <i class="bi bi-info-circle me-1"></i>
            Users are auto-provisioned via OAuth. Change roles below.
        </div>
    </PageHeader>

    @if (isLoading)
    {
        <LoadingSpinner Message="Loading users..." />
    }
    else if (!filteredUsers.Any())
    {
        @if (users.Any())
        {
            <AlertMessage Type="AlertType.Info"
                         Message="No users match your filters." />
        }
        else
        {
            <EmptyState Icon="bi-people"
                       Title="No users found"
                       Message="Users will appear here after they sign in via OAuth." />
        }
    }
    else
    {
        <Card>
            <div class="row mb-3">
                <div class="col-md-4">
                    <FormTextField Label="Search"
                                  @bind-Value="searchTerm"
                                  OnChange="@((e) => OnSearchChanged(e.Value.ToString()))"
                                  Placeholder="Search by name or email..." />
                </div>
                <div class="col-md-3">
                    <FormSelectField Label="Role"
                                    Value="selectedRole"
                                    OnChange="@((e) => OnRoleFilterChanged(e.Value))">
                        <option value="">All Roles</option>
                        <option value="@UserRole.Owner">Owner</option>
                        <option value="@UserRole.Admin">Admin</option>
                        <option value="@UserRole.Member">Member</option>
                        <option value="@UserRole.Viewer">Viewer</option>
                    </FormSelectField>
                </div>
                <div class="col-md-3">
                    <div class="form-check mt-4">
                        <input class="form-check-input" type="checkbox"
                               id="showActiveOnly"
                               @bind="showActiveOnly"
                               @onchange="@((e) => OnShowActiveOnlyChanged((bool)e.Value))" />
                        <label class="form-check-label" for="showActiveOnly">
                            Active only
                        </label>
                    </div>
                </div>
            </div>

            <div class="table-responsive">
                <table class="table table-hover">
                    <thead>
                        <tr>
                            <th>User</th>
                            <th>Email</th>
                            <th>Role</th>
                            <th>Identity Provider</th>
                            <th>Last Seen</th>
                            <th>Status</th>
                            <th>Actions</th>
                        </tr>
                    </thead>
                    <tbody>
                        @foreach (var user in filteredUsers)
                        {
                            <UserListItem User="user"
                                        CanEdit="canEditUsers"
                                        OnEdit="@(() => NavigateToEdit(user.Id))"
                                        OnActivate="@(() => HandleActivateAsync(user.Id))"
                                        OnDeactivate="@(() => HandleDeactivateAsync(user.Id))"
                                        OnViewDetails="@(() => NavigateToDetail(user.Id))" />
                        }
                    </tbody>
                </table>
            </div>

            <div class="mt-3 text-muted">
                Showing @filteredUsers.Count of @users.Count users
            </div>
        </Card>
    }
</PageContainer>
```

---

### 2. Edit User Page (`/settings/users/edit/{id}`)

**File:** `/src/PRFactory.Web/Pages/Settings/Users/Edit.razor`

#### Purpose
Allow Owners to change user roles with validation.

#### Route
```csharp
@page "/settings/users/edit/{id:guid}"
@attribute [Authorize(Roles = "Owner")]
```

#### Page State (Edit.razor.cs)

```csharp
public partial class Edit
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private IUserManagementService UserService { get; set; } = null!;
    [Inject] private IToastService ToastService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private UserManagementDto? user;
    private UserRole selectedRole;
    private bool isActive;
    private bool isLoading = true;
    private bool isSaving = false;

    protected override async Task OnInitializedAsync()
    {
        await LoadUserAsync();
    }

    private async Task LoadUserAsync()
    {
        try
        {
            isLoading = true;
            user = await UserService.GetUserByIdAsync(Id);

            if (user == null)
            {
                await ToastService.ShowErrorAsync("User not found");
                Navigation.NavigateTo("/settings/users");
                return;
            }

            // Initialize form state
            selectedRole = user.Role;
            isActive = user.IsActive;
        }
        catch (Exception ex)
        {
            await ToastService.ShowErrorAsync($"Failed to load user: {ex.Message}");
            Navigation.NavigateTo("/settings/users");
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task HandleSaveAsync()
    {
        if (user == null)
            return;

        try
        {
            isSaving = true;

            // Update role if changed
            if (selectedRole != user.Role)
            {
                await UserService.UpdateUserRoleAsync(Id, selectedRole);
            }

            // Update active status if changed
            if (isActive != user.IsActive)
            {
                if (isActive)
                {
                    await UserService.ActivateUserAsync(Id);
                }
                else
                {
                    await UserService.DeactivateUserAsync(Id);
                }
            }

            await ToastService.ShowSuccessAsync("User updated successfully");

            Navigation.NavigateTo("/settings/users");
        }
        catch (InvalidOperationException ex)
        {
            // Business rule violation (e.g., cannot remove last Owner)
            await ToastService.ShowErrorAsync(ex.Message);
        }
        catch (Exception ex)
        {
            await ToastService.ShowErrorAsync($"Failed to update user: {ex.Message}");
        }
        finally
        {
            isSaving = false;
        }
    }

    private void HandleCancel()
    {
        Navigation.NavigateTo("/settings/users");
    }
}
```

#### Markup (Edit.razor)

```razor
@page "/settings/users/edit/{id:guid}"
@attribute [Authorize(Roles = "Owner")]

<PageContainer>
    <PageHeader Icon="bi-pencil" Title="Edit User">
        <Breadcrumbs>
            <li class="breadcrumb-item"><a href="/settings/users">Users</a></li>
            <li class="breadcrumb-item active">Edit</li>
        </Breadcrumbs>
    </PageHeader>

    @if (isLoading)
    {
        <LoadingSpinner Message="Loading user..." />
    }
    else if (user != null)
    {
        <Card Title="@user.DisplayName" Icon="bi-person">
            <div class="row mb-3">
                <div class="col-md-6">
                    <dl>
                        <dt>Email</dt>
                        <dd>@user.Email</dd>

                        <dt>Identity Provider</dt>
                        <dd>@user.IdentityProvider</dd>

                        <dt>Created</dt>
                        <dd><RelativeTime Timestamp="user.CreatedAt" /></dd>
                    </dl>
                </div>
                <div class="col-md-6">
                    @if (!string.IsNullOrEmpty(user.AvatarUrl))
                    {
                        <img src="@user.AvatarUrl" alt="@user.DisplayName"
                             class="rounded-circle" width="100" height="100" />
                    }
                </div>
            </div>

            <hr />

            <UserRoleEditor SelectedRole="@selectedRole"
                           IsActive="@isActive"
                           CurrentUser="user"
                           OnRoleChanged="@((role) => selectedRole = role)"
                           OnActiveChanged="@((active) => isActive = active)" />

            <hr />

            <div class="d-flex gap-2">
                <LoadingButton Text="Save Changes"
                              Icon="bi-save"
                              OnClick="HandleSaveAsync"
                              IsLoading="isSaving"
                              LoadingText="Saving..."
                              Class="btn-primary" />

                <button type="button" class="btn btn-secondary" @onclick="HandleCancel">
                    Cancel
                </button>
            </div>
        </Card>
    }
</PageContainer>
```

---

### 3. User Detail Page (`/settings/users/{id}`)

**File:** `/src/PRFactory.Web/Pages/Settings/Users/Detail.razor`

#### Purpose
Display user details and statistics in a read-only view.

#### Route
```csharp
@page "/settings/users/{id:guid}"
@attribute [Authorize(Roles = "Owner,Admin")]
```

#### Page State (Detail.razor.cs)

```csharp
public partial class Detail
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private IUserManagementService UserService { get; set; } = null!;
    [Inject] private IToastService ToastService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private ICurrentUserService CurrentUserService { get; set; } = null!;

    private UserManagementDto? user;
    private UserStatisticsDto? statistics;
    private bool isLoading = true;
    private bool canEdit;

    protected override async Task OnInitializedAsync()
    {
        await LoadUserAsync();
        await CheckPermissionsAsync();
    }

    private async Task LoadUserAsync()
    {
        try
        {
            isLoading = true;

            user = await UserService.GetUserByIdAsync(Id);

            if (user == null)
            {
                await ToastService.ShowErrorAsync("User not found");
                Navigation.NavigateTo("/settings/users");
                return;
            }

            statistics = await UserService.GetUserStatisticsAsync(Id);
        }
        catch (Exception ex)
        {
            await ToastService.ShowErrorAsync($"Failed to load user: {ex.Message}");
            Navigation.NavigateTo("/settings/users");
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task CheckPermissionsAsync()
    {
        var currentUser = await CurrentUserService.GetCurrentUserAsync();
        canEdit = currentUser.Role == UserRole.Owner;
    }

    private void HandleEdit()
    {
        if (canEdit)
            Navigation.NavigateTo($"/settings/users/edit/{Id}");
    }

    private void HandleBack()
    {
        Navigation.NavigateTo("/settings/users");
    }
}
```

#### Markup (Detail.razor)

```razor
@page "/settings/users/{id:guid}"
@attribute [Authorize(Roles = "Owner,Admin")]

<PageContainer>
    @if (isLoading)
    {
        <LoadingSpinner Message="Loading user details..." />
    }
    else if (user != null)
    {
        <PageHeader Icon="bi-person" Title="@user.DisplayName">
            <div class="btn-group">
                @if (canEdit)
                {
                    <IconButton Icon="bi-pencil" Text="Edit"
                               OnClick="HandleEdit" Class="btn-primary" />
                }
                <IconButton Icon="bi-arrow-left" Text="Back"
                           OnClick="HandleBack" Class="btn-secondary" />
            </div>
        </PageHeader>

        <div class="row">
            <div class="col-md-8">
                <Card Title="User Information" Icon="bi-info-circle">
                    <div class="row">
                        <div class="col-md-8">
                            <dl class="row">
                                <dt class="col-sm-4">Email</dt>
                                <dd class="col-sm-8">@user.Email</dd>

                                <dt class="col-sm-4">Role</dt>
                                <dd class="col-sm-8">
                                    <span class="badge bg-@GetRoleBadgeColor(user.Role)">
                                        @user.Role
                                    </span>
                                </dd>

                                <dt class="col-sm-4">Identity Provider</dt>
                                <dd class="col-sm-8">@user.IdentityProvider</dd>

                                <dt class="col-sm-4">Status</dt>
                                <dd class="col-sm-8">
                                    <StatusBadge Status="@(user.IsActive ? "Active" : "Inactive")" />
                                </dd>

                                <dt class="col-sm-4">Created</dt>
                                <dd class="col-sm-8">
                                    <RelativeTime Timestamp="user.CreatedAt" />
                                </dd>

                                @if (user.LastSeenAt.HasValue)
                                {
                                    <dt class="col-sm-4">Last Seen</dt>
                                    <dd class="col-sm-8">
                                        <RelativeTime Timestamp="user.LastSeenAt.Value" />
                                    </dd>
                                }
                            </dl>
                        </div>
                        <div class="col-md-4 text-center">
                            @if (!string.IsNullOrEmpty(user.AvatarUrl))
                            {
                                <img src="@user.AvatarUrl" alt="@user.DisplayName"
                                     class="rounded-circle mb-2" width="120" height="120" />
                            }
                            else
                            {
                                <div class="avatar-placeholder rounded-circle mb-2"
                                     style="width: 120px; height: 120px; background: #e0e0e0; display: flex; align-items: center; justify-content: center;">
                                    <i class="bi bi-person" style="font-size: 60px; color: #666;"></i>
                                </div>
                            }
                            <div class="text-muted small">@user.Email</div>
                        </div>
                    </div>
                </Card>
            </div>

            <div class="col-md-4">
                @if (statistics != null)
                {
                    <UserStatistics Statistics="statistics" />
                }
            </div>
        </div>
    }
</PageContainer>

@code {
    private string GetRoleBadgeColor(UserRole role) => role switch
    {
        UserRole.Owner => "danger",
        UserRole.Admin => "primary",
        UserRole.Member => "success",
        UserRole.Viewer => "secondary",
        _ => "secondary"
    };
}
```

---

## Component Specifications

### 1. UserListItem Component

**File:** `/src/PRFactory.Web/Components/Settings/UserListItem.razor`

#### Purpose
Single row in the user table with action buttons.

#### Props (Parameters)

```csharp
public partial class UserListItem
{
    [Parameter, EditorRequired]
    public UserManagementDto User { get; set; } = null!;

    [Parameter]
    public bool CanEdit { get; set; }

    [Parameter]
    public EventCallback OnEdit { get; set; }

    [Parameter]
    public EventCallback OnActivate { get; set; }

    [Parameter]
    public EventCallback OnDeactivate { get; set; }

    [Parameter]
    public EventCallback OnViewDetails { get; set; }

    private string GetRoleBadgeColor() => User.Role switch
    {
        UserRole.Owner => "danger",
        UserRole.Admin => "primary",
        UserRole.Member => "success",
        UserRole.Viewer => "secondary",
        _ => "secondary"
    };
}
```

#### Markup (UserListItem.razor)

```razor
<tr>
    <td>
        <div class="d-flex align-items-center">
            @if (!string.IsNullOrEmpty(User.AvatarUrl))
            {
                <img src="@User.AvatarUrl" alt="@User.DisplayName"
                     class="rounded-circle me-2" width="32" height="32" />
            }
            else
            {
                <div class="avatar-placeholder rounded-circle me-2"
                     style="width: 32px; height: 32px; background: #e0e0e0; display: flex; align-items: center; justify-content: center;">
                    <i class="bi bi-person" style="font-size: 16px; color: #666;"></i>
                </div>
            }
            <a href="#" @onclick="OnViewDetails" @onclick:preventDefault>
                @User.DisplayName
            </a>
        </div>
    </td>
    <td>
        <code class="text-muted">@User.Email</code>
    </td>
    <td>
        <span class="badge bg-@GetRoleBadgeColor()">
            @User.Role
        </span>
    </td>
    <td>
        @User.IdentityProvider
    </td>
    <td>
        @if (User.LastSeenAt.HasValue)
        {
            <RelativeTime Timestamp="User.LastSeenAt.Value" />
        }
        else
        {
            <span class="text-muted">Never</span>
        }
    </td>
    <td>
        <StatusBadge Status="@(User.IsActive ? "Active" : "Inactive")" />
    </td>
    <td>
        <div class="btn-group btn-group-sm">
            <button type="button" class="btn btn-outline-primary"
                    title="View Details"
                    @onclick="OnViewDetails">
                <i class="bi bi-eye"></i>
            </button>

            @if (CanEdit)
            {
                <button type="button" class="btn btn-outline-secondary"
                        title="Edit"
                        @onclick="OnEdit">
                    <i class="bi bi-pencil"></i>
                </button>

                @if (User.IsActive)
                {
                    <button type="button" class="btn btn-outline-warning"
                            title="Deactivate"
                            @onclick="OnDeactivate">
                        <i class="bi bi-x-circle"></i>
                    </button>
                }
                else
                {
                    <button type="button" class="btn btn-outline-success"
                            title="Activate"
                            @onclick="OnActivate">
                        <i class="bi bi-check-circle"></i>
                    </button>
                }
            }
        </div>
    </td>
</tr>
```

---

### 2. UserRoleEditor Component

**File:** `/src/PRFactory.Web/Components/Settings/UserRoleEditor.razor`

#### Purpose
Form for changing user role with validation and warnings.

#### Props (Parameters)

```csharp
public partial class UserRoleEditor
{
    [Parameter, EditorRequired]
    public UserRole SelectedRole { get; set; }

    [Parameter]
    public EventCallback<UserRole> OnRoleChanged { get; set; }

    [Parameter]
    public bool IsActive { get; set; }

    [Parameter]
    public EventCallback<bool> OnActiveChanged { get; set; }

    [Parameter, EditorRequired]
    public UserManagementDto CurrentUser { get; set; } = null!;

    [Inject] private IUserManagementService UserService { get; set; } = null!;
    [Inject] private ICurrentUserService CurrentUserService { get; set; } = null!;

    private bool showOwnerWarning = false;
    private int ownerCount = 0;

    protected override async Task OnInitializedAsync()
    {
        await CheckOwnerCountAsync();
    }

    private async Task CheckOwnerCountAsync()
    {
        try
        {
            var tenantId = await CurrentUserService.GetCurrentTenantIdAsync();
            var users = await UserService.GetUsersForTenantAsync();
            ownerCount = users.Count(u => u.Role == UserRole.Owner);

            // Show warning if trying to demote the last Owner
            showOwnerWarning = CurrentUser.Role == UserRole.Owner &&
                              SelectedRole != UserRole.Owner &&
                              ownerCount <= 1;
        }
        catch
        {
            // Ignore errors in warning check
        }
    }

    private async Task HandleRoleChanged(ChangeEventArgs e)
    {
        if (Enum.TryParse<UserRole>(e.Value?.ToString(), out var newRole))
        {
            SelectedRole = newRole;
            await OnRoleChanged.InvokeAsync(newRole);
            await CheckOwnerCountAsync();
        }
    }

    private async Task HandleActiveChanged(ChangeEventArgs e)
    {
        if (bool.TryParse(e.Value?.ToString(), out var newActive))
        {
            IsActive = newActive;
            await OnActiveChanged.InvokeAsync(newActive);
        }
    }
}
```

#### Markup (UserRoleEditor.razor)

```razor
<div class="row">
    <div class="col-md-6">
        <div class="mb-3">
            <label class="form-label">
                Role
                <ContextualHelp Content="Owner: Full access. Admin: Manage repositories/LLM providers. Member: Create tickets. Viewer: Read-only." />
            </label>
            <select class="form-select" @onchange="HandleRoleChanged">
                <option value="@UserRole.Owner" selected="@(SelectedRole == UserRole.Owner)">
                    Owner
                </option>
                <option value="@UserRole.Admin" selected="@(SelectedRole == UserRole.Admin)">
                    Admin
                </option>
                <option value="@UserRole.Member" selected="@(SelectedRole == UserRole.Member)">
                    Member
                </option>
                <option value="@UserRole.Viewer" selected="@(SelectedRole == UserRole.Viewer)">
                    Viewer
                </option>
            </select>
            <small class="form-text text-muted">
                @GetRoleDescription(SelectedRole)
            </small>
        </div>

        @if (showOwnerWarning)
        {
            <div class="alert alert-danger">
                <i class="bi bi-exclamation-triangle me-2"></i>
                <strong>Warning:</strong> This is the last Owner. At least one Owner must exist.
                This change will be blocked.
            </div>
        }
    </div>

    <div class="col-md-6">
        <div class="form-check">
            <input class="form-check-input" type="checkbox" id="isActive"
                   checked="@IsActive"
                   @onchange="HandleActiveChanged" />
            <label class="form-check-label" for="isActive">
                Active
            </label>
            <small class="form-text text-muted d-block">
                Inactive users cannot access the system
            </small>
        </div>
    </div>
</div>

@code {
    private string GetRoleDescription(UserRole role) => role switch
    {
        UserRole.Owner => "Full access to all settings and users",
        UserRole.Admin => "Can manage repositories, LLM providers, and tickets",
        UserRole.Member => "Can create and manage tickets",
        UserRole.Viewer => "Read-only access to tickets",
        _ => ""
    };
}
```

---

### 3. UserStatistics Component

**File:** `/src/PRFactory.Web/Components/Settings/UserStatistics.razor`

#### Purpose
Display user activity statistics in a card format.

#### Props (Parameters)

```csharp
public partial class UserStatistics
{
    [Parameter, EditorRequired]
    public UserStatisticsDto Statistics { get; set; } = null!;
}
```

#### Markup (UserStatistics.razor)

```razor
<Card Title="Activity Statistics" Icon="bi-bar-chart">
    <div class="row text-center">
        <div class="col-6 mb-3">
            <div class="display-6">@Statistics.TotalPlanReviews</div>
            <small class="text-muted">Plan Reviews</small>
        </div>
        <div class="col-6 mb-3">
            <div class="display-6">@Statistics.TotalComments</div>
            <small class="text-muted">Comments</small>
        </div>
        <div class="col-6 mb-3">
            <div class="display-6">@Statistics.TicketsAssigned</div>
            <small class="text-muted">Tickets Assigned</small>
        </div>
        <div class="col-6 mb-3">
            <div class="display-6">@Statistics.ApprovalRate%</div>
            <small class="text-muted">Approval Rate</small>
        </div>
    </div>

    <hr />

    <dl class="mb-0">
        <dt>Member Since</dt>
        <dd><RelativeTime Timestamp="Statistics.MemberSince" /></dd>

        @if (Statistics.LastActivity.HasValue)
        {
            <dt>Last Activity</dt>
            <dd><RelativeTime Timestamp="Statistics.LastActivity.Value" /></dd>
        }
    </dl>
</Card>
```

---

## Service Layer Integration

### IUserManagementService Interface

**File:** `/src/PRFactory.Core/Application/Services/IUserManagementService.cs`

```csharp
public interface IUserManagementService
{
    // List & Retrieve
    Task<List<UserManagementDto>> GetUsersForTenantAsync(CancellationToken cancellationToken = default);
    Task<UserManagementDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Role Management
    Task UpdateUserRoleAsync(Guid id, UserRole role, CancellationToken cancellationToken = default);

    // Activation
    Task ActivateUserAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeactivateUserAsync(Guid id, CancellationToken cancellationToken = default);

    // Statistics
    Task<UserStatisticsDto> GetUserStatisticsAsync(Guid id, CancellationToken cancellationToken = default);
}
```

### DTOs

**File:** `/src/PRFactory.Core/Application/DTOs/UserManagementDto.cs`

```csharp
public class UserManagementDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public UserRole Role { get; set; }
    public string IdentityProvider { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastSeenAt { get; set; }
}

public class UserStatisticsDto
{
    public int TotalPlanReviews { get; set; }
    public int TotalComments { get; set; }
    public int TicketsAssigned { get; set; }
    public int ApprovalRate { get; set; }
    public DateTime MemberSince { get; set; }
    public DateTime? LastActivity { get; set; }
}
```

---

## RBAC Business Rules

### Critical Rules (Must Enforce)

1. **Only Owner can change roles**
   - Admins cannot promote/demote users
   - Service layer validates current user is Owner

2. **Cannot remove last Owner**
   - At least one Owner must exist
   - Validation check before role change

3. **Cannot self-demote last Owner**
   - The last Owner cannot demote themselves
   - Prevents accidental lockout

4. **First user becomes Owner**
   - Auto-provisioning sets first user as Owner
   - Subsequent users become Members

---

## Security & Authorization

### RBAC Rules

| Role | View Users | Edit Roles | Activate/Deactivate |
|------|------------|------------|---------------------|
| **Owner** | ✅ | ✅ | ✅ |
| **Admin** | ✅ | ❌ | ❌ |
| **Member** | ❌ | ❌ | ❌ |
| **Viewer** | ❌ | ❌ | ❌ |

---

## Testing Strategy

### Unit Tests

```csharp
[Fact]
public async Task UpdateUserRole_LastOwner_ThrowsException()
{
    // Arrange
    var ownerId = Guid.NewGuid();
    var users = new List<User>
    {
        new User { Id = ownerId, Role = UserRole.Owner }
    };

    _mockUserRepo.Setup(r => r.GetUsersByRoleAsync(It.IsAny<Guid>(), UserRole.Owner, default))
        .ReturnsAsync(users);

    // Act & Assert
    await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        await _service.UpdateUserRoleAsync(ownerId, UserRole.Admin));
}
```

---

## Implementation Checklist

### Day 1-2: User List Page
- [ ] Create `Index.razor` and `Index.razor.cs`
- [ ] Create `UserListItem` component
- [ ] Add search and filter functionality
- [ ] Write unit tests

### Day 3: Edit User Page
- [ ] Create `Edit.razor` and `Edit.razor.cs`
- [ ] Create `UserRoleEditor` component
- [ ] Implement "cannot remove last Owner" validation
- [ ] Write unit tests

### Day 4: User Detail Page
- [ ] Create `Detail.razor` and `Detail.razor.cs`
- [ ] Create `UserStatistics` component
- [ ] Write unit tests

### Day 5: Testing & Polish
- [ ] Integration tests
- [ ] E2E tests
- [ ] Bug fixes and polish

---

## UI Mockups

### User List Page

```
┌────────────────────────────────────────────────────────────────────────┐
│ [🏠] > [⚙️ Settings] > Users                                           │
├────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  👥 Users                                                              │
│  ℹ️ Users are auto-provisioned via OAuth. Change roles below.         │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐ │
│  │ Search: [_____________] 🔍   Role: [All ▾]  ☑ Active only       │ │
│  │                                                                   │ │
│  │ ┌─────────────────────────────────────────────────────────────┐ │ │
│  │ │ User        Email            Role    Provider  Last Seen   │ │ │
│  │ ├─────────────────────────────────────────────────────────────┤ │ │
│  │ │ 👤 Alice    alice@acme.com   Owner   Azure AD  5m ago   [⚙️]│ │ │
│  │ │ 👤 Bob      bob@acme.com     Admin   Azure AD  1h ago   [⚙️]│ │ │
│  │ │ 👤 Carol    carol@acme.com   Member  Google   3d ago   [⚙️]│ │ │
│  │ └─────────────────────────────────────────────────────────────┘ │ │
│  │                                                                   │ │
│  │ Showing 3 of 3 users                                              │ │
│  └──────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  Actions: [👁 View] [✏️ Edit] [✓ Activate] / [✗ Deactivate]          │
└────────────────────────────────────────────────────────────────────────┘
```

---

**Document Version:** 1.0
**Last Updated:** 2025-11-13
**Author:** AI Planning Assistant
**Status:** Ready for Implementation
