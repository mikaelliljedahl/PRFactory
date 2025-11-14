# Phase 1: Project Consolidation

**Duration**: 4-5 days
**Risk Level**: 🔴 High (structural changes)
**Agent Type**: `code-implementation-specialist`
**Dependencies**: None (foundation phase)

---

## Objective

Consolidate `PRFactory.Api`, `PRFactory.Worker`, and `PRFactory.Web` into a single `PRFactory.Web` project to simplify development, deployment, and maintenance.

**Why this matters**: Currently, developers must run 3 separate projects (3 terminals or docker-compose), deploy 3 containers, and maintain 3 sets of configuration files. This consolidation reduces complexity by 66% while preserving all functionality.

---

## Success Criteria

Before marking this phase complete, verify:

- ✅ Single `dotnet run` command starts everything (UI + API + Background Worker)
- ✅ All existing tests pass (100%)
- ✅ Docker build succeeds with new unified Dockerfile
- ✅ Jira webhook receiver works (POST to `/api/webhooks/jira`)
- ✅ OAuth authentication works (Google/Microsoft login)
- ✅ Background agent execution works (workflows process tickets)
- ✅ Blazor UI loads and SignalR connections work
- ✅ No regressions in existing functionality
- ✅ CI/CD pipeline updated and working

---

## Current Architecture (Before)

```
PRFactory Solution
├── PRFactory.Domain          # Shared entities, interfaces
├── PRFactory.Infrastructure  # Shared services, repositories, agents
├── PRFactory.Api             # ⚠️ REST API (Jira webhooks, OAuth)
├── PRFactory.Worker          # ⚠️ Background agent executor
└── PRFactory.Web             # ⚠️ Blazor Server UI
```

**Deployment**: 3 separate processes/containers:
- `PRFactory.Api` - Port 5000 (HTTP API)
- `PRFactory.Worker` - No exposed ports (background service)
- `PRFactory.Web` - Port 5003 (Blazor UI)

**Communication**: All 3 projects share SQLite database, no HTTP calls between them.

---

## Target Architecture (After)

```
PRFactory Solution
├── PRFactory.Domain          # Shared entities, interfaces
├── PRFactory.Infrastructure  # Shared services, repositories, agents
└── PRFactory.Web             # ✅ ALL-IN-ONE
    ├── Pages/               # Blazor UI (existing)
    ├── Components/          # Blazor components (existing)
    ├── UI/                  # Pure UI library (existing)
    ├── Controllers/         # ⬅️ MOVED from Api
    ├── BackgroundServices/  # ⬅️ MOVED from Worker
    ├── Middleware/          # ⬅️ MOVED from Api
    ├── Hubs/                # SignalR (existing)
    └── Services/            # Web facades (existing)
```

**Deployment**: 1 unified process/container:
- `PRFactory.Web` - Ports 5000 (API) and 5003 (Blazor UI)

---

## Implementation Steps

### Step 1: Preparation (30 minutes)

#### 1.1 Create Backup

```bash
# Create git tag for rollback point
git tag -a pre-consolidation-backup -m "Backup before Phase 1 consolidation"
git push origin --tags

# Document current test baseline
source /tmp/dotnet-proxy-setup.sh
dotnet test > phase1-test-baseline.txt
```

#### 1.2 Verify Prerequisites

- [ ] All tests passing on current branch
- [ ] No uncommitted changes (`git status` clean)
- [ ] .NET 10 SDK installed
- [ ] Docker running
- [ ] NuGet proxy configured (source `/tmp/dotnet-proxy-setup.sh`)

---

### Step 2: Move Controllers from Api to Web (1-2 hours)

#### 2.1 Copy Controller Files

```bash
# Create Controllers directory in Web project
mkdir -p C:/code/github/PRFactory/src/PRFactory.Web/Controllers

# Copy all controllers
cp C:/code/github/PRFactory/src/PRFactory.Api/Controllers/*.cs \
   C:/code/github/PRFactory/src/PRFactory.Web/Controllers/
```

**Files to copy** (verify these exist):
- `WebhookController.cs` - Jira webhook receiver
- `AuthController.cs` - OAuth authentication
- `TicketUpdatesController.cs` - Ticket update API (future mobile apps)
- `AgentPromptTemplatesController.cs` - Prompt template CRUD API (future admin)

#### 2.2 Update Namespaces

For each copied controller file, update the namespace:

```csharp
// BEFORE
namespace PRFactory.Api.Controllers;

// AFTER
namespace PRFactory.Web.Controllers;
```

**Files to update**:
- `Controllers/WebhookController.cs`
- `Controllers/AuthController.cs`
- `Controllers/TicketUpdatesController.cs`
- `Controllers/AgentPromptTemplatesController.cs`

#### 2.3 Verify Controller Dependencies

Ensure all `using` statements resolve correctly. Controllers should depend on:
- `PRFactory.Domain` entities
- `PRFactory.Infrastructure` services
- ASP.NET Core (`Microsoft.AspNetCore.Mvc`)

If any dependencies are missing, add them to `PRFactory.Web.csproj` (Step 6).

---

### Step 3: Move Background Services from Worker to Web (1 hour)

#### 3.1 Copy Background Service Files

```bash
# Create BackgroundServices directory
mkdir -p C:/code/github/PRFactory/src/PRFactory.Web/BackgroundServices

# Copy background service files
cp C:/code/github/PRFactory/src/PRFactory.Worker/AgentHostService.cs \
   C:/code/github/PRFactory/src/PRFactory.Web/BackgroundServices/

# Copy workflow resume handler if separate file
cp C:/code/github/PRFactory/src/PRFactory.Worker/WorkflowResumeHandler.cs \
   C:/code/github/PRFactory/src/PRFactory.Web/BackgroundServices/
```

**Files to copy**:
- `AgentHostService.cs` - Main background service (polling loop)
- `WorkflowResumeHandler.cs` (if exists as separate file)
- Any other background task files in Worker project

#### 3.2 Update Namespaces

```csharp
// BEFORE
namespace PRFactory.Worker;

// AFTER
namespace PRFactory.Web.BackgroundServices;
```

**Files to update**:
- `BackgroundServices/AgentHostService.cs`
- `BackgroundServices/WorkflowResumeHandler.cs`

#### 3.3 Verify Background Service Dependencies

Ensure all `using` statements resolve. Background services depend on:
- `PRFactory.Infrastructure` services (WorkflowOrchestrator, etc.)
- `Microsoft.Extensions.Hosting` (IHostedService)

---

### Step 4: Move Middleware from Api to Web (30 minutes)

#### 4.1 Copy Middleware Files

```bash
# Create Middleware directory
mkdir -p C:/code/github/PRFactory/src/PRFactory.Web/Middleware

# Copy Jira webhook authentication middleware
cp C:/code/github/PRFactory/src/PRFactory.Api/Middleware/JiraWebhookAuthenticationMiddleware.cs \
   C:/code/github/PRFactory/src/PRFactory.Web/Middleware/
```

**Files to copy**:
- `JiraWebhookAuthenticationMiddleware.cs`
- Any other middleware files in Api/Middleware/

#### 4.2 Update Namespaces

```csharp
// BEFORE
namespace PRFactory.Api.Middleware;

// AFTER
namespace PRFactory.Web.Middleware;
```

#### 4.3 Copy Middleware Extensions

If there's a middleware extensions file (e.g., `MiddlewareExtensions.cs`), copy and update it too.

---

### Step 5: Merge Configuration Files (1-2 hours)

#### 5.1 Backup Current Web Configuration

```bash
cp C:/code/github/PRFactory/src/PRFactory.Web/appsettings.json \
   C:/code/github/PRFactory/src/PRFactory.Web/appsettings.json.backup
```

#### 5.2 Merge appsettings.json

**Source files to merge**:
- `PRFactory.Api/appsettings.json`
- `PRFactory.Worker/appsettings.json`
- `PRFactory.Web/appsettings.json` (existing)

**Target**: `PRFactory.Web/appsettings.json` (merged)

**Configuration sections to include** (merge from all 3 sources):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=/data/prfactory.db"
  },

  "Authentication": {
    "Google": {
      "ClientId": "",
      "ClientSecret": ""
    },
    "Microsoft": {
      "ClientId": "",
      "ClientSecret": ""
    }
  },

  "Jira": {
    "BaseUrl": "",
    "ApiToken": "",
    "WebhookSecret": "",
    "UserEmail": ""
  },

  "Claude": {
    "ApiKey": "",
    "Model": "claude-sonnet-4-5-20250929",
    "MaxTokens": 8000,
    "Temperature": 0.7
  },

  "Git": {
    "GitHub": {
      "Token": "",
      "BaseUrl": "https://api.github.com"
    },
    "Bitbucket": {
      "Token": "",
      "BaseUrl": "https://api.bitbucket.org"
    },
    "AzureDevOps": {
      "Token": "",
      "Organization": "",
      "BaseUrl": "https://dev.azure.com"
    }
  },

  "AgentHost": {
    "MaxConcurrentExecutions": 10,
    "PollIntervalSeconds": 5,
    "BatchSize": 20,
    "MaxRetries": 3,
    "GracefulShutdownTimeoutSeconds": 300
  },

  "AgentFramework": {
    "MaxConcurrentWorkflows": 10,
    "WorkflowTimeoutMinutes": 120,
    "CheckpointIntervalSeconds": 30
  },

  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000"
    ]
  },

  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.AspNetCore.SignalR": "Information"
    }
  }
}
```

**Important**:
- Remove any `ApiSettings.BaseUrl` if present (unused in Blazor Server)
- Remove unused `BackgroundTasks` configuration from Worker (not implemented)
- Keep all sections even if empty (can be configured later)

#### 5.3 Merge appsettings.Development.json

Repeat the same merge process for development configuration.

---

### Step 6: Update PRFactory.Web.csproj (30 minutes)

#### 6.1 Add Missing Package References

Open `C:/code/github/PRFactory/src/PRFactory.Web/PRFactory.Web.csproj` and add:

**From Api project** (if not already present):
```xml
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.8.1" />
<PackageReference Include="FluentValidation.AspNetCore" Version="11.3.0" />
<PackageReference Include="Refit" Version="7.2.22" />
<PackageReference Include="Refit.HttpClientFactory" Version="7.2.22" />
```

**From Worker project** (if not already present):
```xml
<PackageReference Include="Polly" Version="8.2.0" />
<PackageReference Include="Microsoft.Extensions.Hosting.WindowsServices" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Hosting.Systemd" Version="9.0.0" />
```

**Verify no duplicates**: Check that packages aren't already referenced before adding.

#### 6.2 Verify Existing References

Ensure these are already present (should be):
```xml
<PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.Server" Version="..." />
<PackageReference Include="Radzen.Blazor" Version="5.9.0" />
<PackageReference Include="Serilog.AspNetCore" Version="..." />
<!-- ... other existing packages -->
```

---

### Step 7: Update Program.cs (2-3 hours) 🔴 **CRITICAL**

This is the most important step. The consolidated `Program.cs` must:
1. Register all services (Blazor + API + Background Services)
2. Configure middleware pipeline correctly
3. Map all endpoints (Blazor, API, SignalR)

#### 7.1 Read Current Web Program.cs

First, understand the existing Web `Program.cs` structure:

```bash
cat C:/code/github/PRFactory/src/PRFactory.Web/Program.cs
```

#### 7.2 Update Program.cs

Replace/merge the `Program.cs` content with the consolidated version:

```csharp
using PRFactory.Infrastructure;
using PRFactory.Infrastructure.Data;
using PRFactory.Web.BackgroundServices;
using PRFactory.Web.Hubs;
using PRFactory.Web.Middleware;
using PRFactory.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// LOGGING (existing)
// ============================================================
builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));

// ============================================================
// DATABASE (existing)
// ============================================================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// ============================================================
// INFRASTRUCTURE (existing)
// ============================================================
builder.Services.AddInfrastructure(builder.Configuration);

// ============================================================
// IDENTITY & AUTHENTICATION (existing)
// ============================================================
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
    })
    .AddMicrosoftAccount(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"] ?? "";
        options.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"] ?? "";
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireOwner", policy => policy.RequireRole("Owner"));
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Owner", "Admin"));
});

// ============================================================
// BLAZOR SERVER (existing)
// ============================================================
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSignalR();

// ============================================================
// API CONTROLLERS (NEW - from Api project)
// ============================================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "PRFactory API", Version = "v1" });
});

// ============================================================
// CORS (NEW - for API endpoints)
// ============================================================
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:3000" };
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// ============================================================
// WEB SERVICES (existing - facades for Blazor components)
// ============================================================
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IRepositoryService, RepositoryService>();
builder.Services.AddScoped<IWorkflowEventService, WorkflowEventService>();
builder.Services.AddScoped<IAgentPromptService, AgentPromptService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IErrorService, ErrorService>();
builder.Services.AddScoped<IToastService, ToastService>();
// Add other web services as needed

// ============================================================
// BACKGROUND SERVICES (NEW - from Worker project)
// ============================================================
builder.Services.AddHostedService<AgentHostService>();
// Add WorkflowResumeHandler if it's a separate service:
// builder.Services.AddScoped<IWorkflowResumeHandler, WorkflowResumeHandler>();

// ============================================================
// BUILD APP
// ============================================================
var app = builder.Build();

// ============================================================
// MIDDLEWARE PIPELINE
// ============================================================

// Development middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    // Swagger UI in development only
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "PRFactory API v1"));
}

// Serilog request logging
app.UseSerilogRequestLogging();

// Static files & routing
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// CORS (NEW - must be before Authentication)
app.UseCors();

// Jira webhook authentication middleware (NEW - from Api)
app.UseJiraWebhookAuthentication();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// ============================================================
// ENDPOINT MAPPING
// ============================================================

// Health checks
app.MapHealthChecks("/health");

// API Controllers (NEW)
app.MapControllers();

// Blazor & SignalR (existing)
app.MapBlazorHub();
app.MapHub<TicketHub>("/hubs/tickets");
app.MapFallbackToPage("/_Host");

// ============================================================
// RUN APP
// ============================================================
app.Run();
```

**Important notes**:
- Verify all `using` statements at the top resolve correctly
- Verify all service interfaces exist (ITicketService, IRepositoryService, etc.)
- Adjust service registrations if your project has different names
- Keep existing Blazor services that aren't listed here

#### 7.3 Verify Middleware Extension Exists

Ensure `UseJiraWebhookAuthentication()` extension method exists:

```bash
# Should exist in copied Middleware directory
ls C:/code/github/PRFactory/src/PRFactory.Web/Middleware/
```

If the extension method doesn't exist, you'll need to create it or register the middleware directly:
```csharp
app.UseMiddleware<JiraWebhookAuthenticationMiddleware>();
```

---

### Step 8: Create Unified Dockerfile (1 hour)

#### 8.1 Create New Dockerfile

Create `C:/code/github/PRFactory/src/PRFactory.Web/Dockerfile`:

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["PRFactory.sln", "./"]
COPY ["src/PRFactory.Domain/PRFactory.Domain.csproj", "src/PRFactory.Domain/"]
COPY ["src/PRFactory.Infrastructure/PRFactory.Infrastructure.csproj", "src/PRFactory.Infrastructure/"]
COPY ["src/PRFactory.Web/PRFactory.Web.csproj", "src/PRFactory.Web/"]

# Restore dependencies
RUN dotnet restore "src/PRFactory.Web/PRFactory.Web.csproj"

# Copy source code
COPY . .

# Build and publish
WORKDIR "/src/src/PRFactory.Web"
RUN dotnet publish "PRFactory.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install git (for LibGit2Sharp operations)
RUN apt-get update && apt-get install -y git && rm -rf /var/lib/apt/lists/*

# Create app user
RUN groupadd -r app && useradd -r -g app -u 1654 app

# Create directories for workspace and data
RUN mkdir -p /var/prfactory/workspace/checkpoints \
             /var/prfactory/workspace/repositories \
             /var/prfactory/data \
    && chown -R app:app /var/prfactory

# Copy published application
COPY --from=build /app/publish .

# Switch to non-root user
USER app

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl --fail http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "PRFactory.Web.dll"]
```

#### 8.2 Verify Dockerfile Builds

```bash
# Test Docker build
cd C:/code/github/PRFactory
docker build -f src/PRFactory.Web/Dockerfile -t prfactory:test .
```

**If build fails**: Check for missing dependencies or incorrect file paths.

---

### Step 9: Update docker-compose.yml (30 minutes)

#### 9.1 Backup Current docker-compose.yml

```bash
cp C:/code/github/PRFactory/docker-compose.yml \
   C:/code/github/PRFactory/docker-compose.yml.backup
```

#### 9.2 Replace with Simplified Version

Update `C:/code/github/PRFactory/docker-compose.yml`:

```yaml
version: '3.8'

services:
  prfactory:
    image: prfactory:latest
    build:
      context: .
      dockerfile: src/PRFactory.Web/Dockerfile
    container_name: prfactory
    ports:
      - "5000:8080"   # API endpoint
      - "5003:8080"   # Blazor UI (same port, different external mapping)
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:8080
      - ConnectionStrings__DefaultConnection=Data Source=/data/prfactory.db
      - Logging__LogLevel__Default=Information
    volumes:
      - ./data:/data
      - ./logs:/var/prfactory/logs
      - ./workspace:/var/prfactory/workspace
    networks:
      - prfactory-network
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 3s
      retries: 3
      start_period: 10s

networks:
  prfactory-network:
    driver: bridge

volumes:
  data:
    driver: local
  logs:
    driver: local
  workspace:
    driver: local
```

**Key changes**:
- Single `prfactory` service (was 3 separate services)
- Ports 5000 and 5003 both map to container port 8080
- All volumes (data, logs, workspace) mounted
- Health check configured

#### 9.3 Test Docker Compose

```bash
cd C:/code/github/PRFactory
docker-compose up --build
```

**Verify**:
- Container starts successfully
- Blazor UI accessible at http://localhost:5003
- API accessible at http://localhost:5000/swagger (in dev mode)
- Health check passes: `curl http://localhost:5000/health`

---

### Step 10: Update CI/CD Pipeline (1 hour)

#### 10.1 Locate CI/CD Configuration

Find your CI/CD configuration file:
- GitHub Actions: `.github/workflows/build.yml`
- Azure Pipelines: `azure-pipelines.yml`
- GitLab CI: `.gitlab-ci.yml`

#### 10.2 Update Build Jobs

**Before** (3 projects):
```yaml
jobs:
  build:
    steps:
      - run: dotnet build src/PRFactory.Api/PRFactory.Api.csproj
      - run: dotnet build src/PRFactory.Worker/PRFactory.Worker.csproj
      - run: dotnet build src/PRFactory.Web/PRFactory.Web.csproj
```

**After** (1 project):
```yaml
jobs:
  build:
    steps:
      - run: dotnet build src/PRFactory.Web/PRFactory.Web.csproj
```

#### 10.3 Update Docker Build Jobs

**Before**:
```yaml
  docker-build:
    steps:
      - run: docker build -f src/PRFactory.Api/Dockerfile -t prfactory-api .
      - run: docker build -f src/PRFactory.Worker/Dockerfile -t prfactory-worker .
```

**After**:
```yaml
  docker-build:
    steps:
      - run: docker build -f src/PRFactory.Web/Dockerfile -t prfactory .
```

#### 10.4 Update Test Jobs

Tests should still run for all projects (Domain, Infrastructure, Web):
```yaml
  test:
    steps:
      - run: dotnet test --no-build
```

---

### Step 11: Testing (2-3 hours) 🔴 **CRITICAL**

#### 11.1 Build & Unit Tests

```bash
source /tmp/dotnet-proxy-setup.sh

# Clean build
dotnet clean
dotnet build

# Run all tests
dotnet test

# Verify formatting
dotnet format --verify-no-changes
```

**Expected**: All tests pass, no build errors.

#### 11.2 Integration Testing - Start Consolidated App

```bash
# Start the consolidated app
cd C:/code/github/PRFactory/src/PRFactory.Web
source /tmp/dotnet-proxy-setup.sh
dotnet run
```

**Verify startup logs show**:
- Blazor Server starting
- SignalR hub registered
- API controllers mapped
- Background service (AgentHostService) starting
- Health check endpoint available

#### 11.3 Manual Smoke Tests

**Test 1: Blazor UI**
- Navigate to `http://localhost:5003`
- Verify dashboard loads
- Verify SignalR connection (check browser console for "SignalR connected")
- Click through a few pages
- Verify no JavaScript errors in console

**Test 2: API Endpoints**
- Navigate to `http://localhost:5000/swagger` (dev mode only)
- Verify Swagger UI loads
- Verify controllers listed:
  - `/api/webhooks/jira`
  - `/api/ticket-updates`
  - `/api/agent-prompt-templates`

**Test 3: Health Check**
```bash
curl http://localhost:5000/health
```
**Expected**: 200 OK with health status

**Test 4: OAuth Login**
- Navigate to `http://localhost:5003/auth/login`
- Click "Login with Google" (or Microsoft)
- Verify redirect to OAuth provider
- Complete login and verify redirect back to app

**Test 5: Jira Webhook** (if you have test webhook payload)
```bash
curl -X POST http://localhost:5000/api/webhooks/jira \
  -H "Content-Type: application/json" \
  -H "X-Hub-Signature: sha256=test" \
  -d @test-webhook.json
```
**Expected**: 200 OK (or appropriate response based on webhook secret validation)

**Test 6: Background Worker**
- Create a ticket in Jira (or via UI)
- Watch logs for AgentHostService activity
- Verify workflow executes
- Check database for TicketUpdate records

#### 11.4 Docker Testing

```bash
# Stop local app first (Ctrl+C)

# Build and start Docker container
docker-compose up --build

# Run same manual tests as above
# - Blazor UI: http://localhost:5003
# - API: http://localhost:5000/swagger
# - Health: http://localhost:5000/health

# Check logs
docker logs prfactory

# Stop container
docker-compose down
```

---

### Step 12: Documentation Updates (1-2 hours)

#### 12.1 Update ARCHITECTURE.md

File: `C:/code/github/PRFactory/docs/ARCHITECTURE.md`

**Changes needed**:
- Update project structure diagram (remove Api/Worker, show consolidated Web)
- Update deployment model (1 container instead of 3)
- Update development workflow (single `dotnet run`)

**Find and replace**:
- "PRFactory.Api", "PRFactory.Worker", "PRFactory.Web" → "PRFactory.Web (consolidated)"
- "3 separate projects" → "single unified project"
- References to docker-compose with 3 services → single service

#### 12.2 Update SETUP.md

File: `C:/code/github/PRFactory/docs/SETUP.md`

**Changes needed**:
- Update "Running Locally" section (single `dotnet run` command)
- Update Docker instructions (simplified docker-compose)
- Update Azure deployment instructions (single App Service)
- Remove references to separate Api/Worker deployments

**Old instructions**:
```bash
# Terminal 1
cd src/PRFactory.Api && dotnet run

# Terminal 2
cd src/PRFactory.Worker && dotnet run

# Terminal 3
cd src/PRFactory.Web && dotnet run
```

**New instructions**:
```bash
# Single terminal
cd src/PRFactory.Web && dotnet run
```

#### 12.3 Update README.md

File: `C:/code/github/PRFactory/README.md`

**Changes needed**:
- Update project structure section
- Update "Quick Start" instructions
- Update deployment section

#### 12.4 Update CLAUDE.md

File: `C:/code/github/PRFactory/CLAUDE.md`

**Changes needed**:
- Update project structure documentation
- Note that Api controllers are now in Web project
- Note that background services are now in Web project
- Keep warning about NOT using HTTP calls within Blazor Server (still applies)

**Add note**:
```markdown
## Project Consolidation (Post-EPIC-08)

As of [date], PRFactory uses a single consolidated project:
- `PRFactory.Web` contains Blazor UI, API controllers, and background services
- All functionality runs in a single process
- Single `dotnet run` starts everything
- Single Docker container for deployment

**File locations**:
- API Controllers: `/src/PRFactory.Web/Controllers/`
- Background Services: `/src/PRFactory.Web/BackgroundServices/`
- Blazor Pages: `/src/PRFactory.Web/Pages/`
- UI Components: `/src/PRFactory.Web/UI/`
```

#### 12.5 Update IMPLEMENTATION_STATUS.md

File: `C:/code/github/PRFactory/docs/IMPLEMENTATION_STATUS.md`

**Changes needed**:
- Update "What Works Today" section
- Add note about project consolidation
- Update deployment model description

---

### Step 13: Cleanup (1 hour)

#### 13.1 Remove Old Project Directories

**CRITICAL**: Only do this AFTER all tests pass and consolidation is verified working.

```bash
# Verify everything works first!
# Run all tests, manual tests, Docker tests

# Then remove old projects
cd C:/code/github/PRFactory/src
rm -rf PRFactory.Api/
rm -rf PRFactory.Worker/

# Remove old Dockerfiles
rm -f PRFactory.Api/Dockerfile
rm -f PRFactory.Worker/Dockerfile
```

#### 13.2 Update Solution File

Edit `C:/code/github/PRFactory/PRFactory.sln`:

**Remove these project references**:
```xml
Project("{...}") = "PRFactory.Api", "src\PRFactory.Api\PRFactory.Api.csproj", "{...}"
EndProject
Project("{...}") = "PRFactory.Worker", "src\PRFactory.Worker\PRFactory.Worker.csproj", "{...}"
EndProject
```

**Verify solution still builds**:
```bash
dotnet build PRFactory.sln
```

#### 13.3 Commit Changes

```bash
git add .
git commit -m "feat(epic-08): consolidate Api/Worker/Web into single project

BREAKING CHANGE: Merged three projects into PRFactory.Web

- Move API controllers from Api to Web/Controllers/
- Move background services from Worker to Web/BackgroundServices/
- Move middleware from Api to Web/Middleware/
- Merge appsettings.json from all 3 projects
- Update Program.cs with consolidated service registration
- Create unified Dockerfile
- Simplify docker-compose.yml (3 services → 1 service)
- Update CI/CD pipeline for single project build
- Update documentation (ARCHITECTURE.md, SETUP.md, README.md)
- Remove old Api and Worker project directories

Closes Phase 1 of EPIC 08"
```

---

## Validation Checklist

Before marking Phase 1 complete, verify:

### Build & Test
- [ ] `dotnet build` succeeds with no errors
- [ ] `dotnet test` passes 100% of tests (check count matches baseline)
- [ ] `dotnet format --verify-no-changes` passes
- [ ] No compiler warnings introduced

### Functional Validation
- [ ] Single `dotnet run` starts all services (UI + API + Worker)
- [ ] Blazor UI loads at http://localhost:5003
- [ ] API accessible at http://localhost:5000
- [ ] Swagger UI accessible at http://localhost:5000/swagger (dev mode)
- [ ] Health check endpoint returns 200 OK
- [ ] OAuth login works (Google/Microsoft)
- [ ] Jira webhook receiver works
- [ ] Background agent execution works (workflow processes tickets)
- [ ] SignalR connections work (check browser console)

### Docker Validation
- [ ] Docker build succeeds: `docker build -f src/PRFactory.Web/Dockerfile -t prfactory:test .`
- [ ] Docker Compose starts successfully: `docker-compose up --build`
- [ ] All manual tests pass in Docker environment
- [ ] Container health check passes
- [ ] Logs show all services starting correctly

### Documentation
- [ ] ARCHITECTURE.md updated (project structure, deployment model)
- [ ] SETUP.md updated (running locally, Docker instructions)
- [ ] README.md updated (project structure, quick start)
- [ ] CLAUDE.md updated (project consolidation note)
- [ ] IMPLEMENTATION_STATUS.md updated

### CI/CD
- [ ] CI/CD pipeline updated (build jobs, Docker jobs)
- [ ] Pipeline runs successfully
- [ ] All pipeline tests pass

### Cleanup
- [ ] Old Api and Worker project directories removed
- [ ] Solution file updated (project references removed)
- [ ] Solution still builds: `dotnet build PRFactory.sln`
- [ ] No dangling references to old projects

### Git
- [ ] All changes committed with clear commit message
- [ ] Commit message follows conventional commit format
- [ ] Branch ready to merge to main

---

## Rollback Procedure

If critical issues are discovered:

### Option 1: Fix Forward (Preferred)
```bash
# Continue on same branch, fix issues
git checkout epic/08-architecture-cleanup
# Make fixes
git commit -m "fix: address Phase 1 issues - [description]"
# Re-validate
```

### Option 2: Rollback to Backup
```bash
# Reset to backup tag
git reset --hard pre-consolidation-backup

# Delete failed attempt branch
git branch -D epic/08-architecture-cleanup

# Create new branch and start over
git checkout -b epic/08-architecture-cleanup-v2
```

### Option 3: Revert Old Projects
```bash
# If you need to temporarily restore old projects
git checkout pre-consolidation-backup -- src/PRFactory.Api/
git checkout pre-consolidation-backup -- src/PRFactory.Worker/
git commit -m "revert: temporarily restore Api and Worker projects"
```

---

## Common Issues & Solutions

### Issue 1: "Type or namespace could not be found"

**Cause**: Missing package reference in PRFactory.Web.csproj

**Solution**: Add missing package from Api or Worker project
```bash
# Find package in old project
grep -r "PackageReference" src/PRFactory.Api/PRFactory.Api.csproj
# Add to Web project
```

### Issue 2: "Controller not found" (404 on API endpoints)

**Cause**: Controllers not registered or namespace incorrect

**Solution**: Verify Program.cs has `app.MapControllers()` and namespace is `PRFactory.Web.Controllers`

### Issue 3: Background service not starting

**Cause**: AgentHostService not registered in DI

**Solution**: Verify Program.cs has:
```csharp
builder.Services.AddHostedService<AgentHostService>();
```

### Issue 4: Middleware not found

**Cause**: Middleware extension method missing or not imported

**Solution**: Verify `using PRFactory.Web.Middleware;` in Program.cs

### Issue 5: Docker build fails with "project not found"

**Cause**: Dockerfile paths incorrect after consolidation

**Solution**: Verify Dockerfile COPY paths match new structure

---

## Success Metrics

**Project Consolidation Metrics** (measure after completion):

| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| Project count | 3 | 1 | -66% |
| Terminals to run locally | 3 | 1 | -66% |
| Docker containers | 2-3 | 1 | -66% |
| Configuration files | 9 (3x3 envs) | 3 (1x3 envs) | -66% |
| CI/CD build time | ~5 min | ~2 min | -60% |
| Docker images to maintain | 2 | 1 | -50% |

---

## Phase 1 Complete!

Once all validation checks pass, Phase 1 is complete. Proceed to Phase 2 (CSS Isolation) after:

1. ✅ Merging Phase 1 to main branch
2. ✅ Tagging release: `git tag phase-1-complete`
3. ✅ Completing Phase 1 retrospective (see README.md)
4. ✅ Verifying production deployment works (if applicable)

**Estimated time saved for developers**: 30-40% reduction in daily workflow complexity.

**Next**: Proceed to `PHASE_02_CSS_ISOLATION.md`
