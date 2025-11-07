# PRFactory Setup Guide

Complete guide for installing, configuring, and running PRFactory.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Configuration](#configuration)
- [Database Setup](#database-setup)
- [Running the Application](#running-the-application)
- [Configuring Integrations](#configuring-integrations)
- [Troubleshooting](#troubleshooting)

## Prerequisites

### Required Software

- **.NET 8 SDK** or later
  - Download: https://dotnet.microsoft.com/download/dotnet/8.0
  - Verify: `dotnet --version`

- **Git** (version 2.x or later)
  - Download: https://git-scm.com/downloads
  - Verify: `git --version`

### Optional Software

- **Docker** and Docker Compose (recommended for containerized deployment)
  - Download: https://www.docker.com/get-started
  - Verify: `docker --version` and `docker-compose --version`

### External Services

You'll need accounts and API credentials for:

1. **Jira Cloud**
   - API token from https://id.atlassian.com/manage-profile/security/api-tokens
   - Webhook secret (you'll configure this)

2. **GitHub** (or GitLab/Bitbucket)
   - Personal Access Token with repo permissions
   - From https://github.com/settings/tokens

3. **Anthropic Claude**
   - API key from https://console.anthropic.com/
   - Access to Claude Sonnet 4.5 model

## Installation

### 1. Clone the Repository

```bash
git clone <repository-url>
cd PRFactory
```

### 2. Verify Build

```bash
# Restore NuGet packages
dotnet restore

# Build the solution
dotnet build

# Verify all projects compile
dotnet build -c Release
```

You should see output indicating all 5 projects built successfully:
- PRFactory.Domain
- PRFactory.Infrastructure
- PRFactory.Api
- PRFactory.Worker
- PRFactory.Tests

## Configuration

PRFactory supports multiple configuration sources (in order of precedence):

1. Environment variables
2. User secrets (for development)
3. appsettings.json
4. appsettings.Development.json (for development)

### Option 1: Using appsettings.json

Create or modify `src/PRFactory.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=prfactory.db"
  },
  "Encryption": {
    "Key": "YOUR_BASE64_ENCRYPTION_KEY"
  },
  "Jira": {
    "BaseUrl": "https://yourcompany.atlassian.net",
    "WebhookSecret": "your-webhook-secret-here"
  },
  "Claude": {
    "ApiKey": "sk-ant-api03-...",
    "Model": "claude-sonnet-4-5-20250929",
    "MaxTokens": 8000
  },
  "GitHub": {
    "Token": "ghp_...",
    "BaseUrl": "https://api.github.com"
  },
  "Workspace": {
    "BasePath": "./workspace"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

**Important:** Never commit real API keys to source control!

### Option 2: Using User Secrets (Recommended for Development)

```bash
# Navigate to API project
cd src/PRFactory.Api

# Set secrets
dotnet user-secrets init
dotnet user-secrets set "Encryption:Key" "your-base64-key"
dotnet user-secrets set "Jira:BaseUrl" "https://yourcompany.atlassian.net"
dotnet user-secrets set "Jira:WebhookSecret" "your-webhook-secret"
dotnet user-secrets set "Claude:ApiKey" "sk-ant-..."
dotnet user-secrets set "GitHub:Token" "ghp_..."

# Repeat for Worker project
cd ../PRFactory.Worker
dotnet user-secrets init
dotnet user-secrets set "Encryption:Key" "your-base64-key"
# ... (set other secrets)
```

### Option 3: Using Environment Variables

```bash
# Linux/Mac
export ConnectionStrings__DefaultConnection="Data Source=prfactory.db"
export Encryption__Key="your-base64-key"
export Jira__BaseUrl="https://yourcompany.atlassian.net"
export Jira__WebhookSecret="your-webhook-secret"
export Claude__ApiKey="sk-ant-..."
export GitHub__Token="ghp_..."

# Windows (PowerShell)
$env:ConnectionStrings__DefaultConnection="Data Source=prfactory.db"
$env:Encryption__Key="your-base64-key"
# ... (set other variables)
```

### Generating Encryption Key

The encryption key is used to securely store API tokens in the database.

```bash
# Using PowerShell (cross-platform)
dotnet run --project src/PRFactory.Api -- generate-key

# Or use this C# snippet:
# var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
# Console.WriteLine(key);
```

Save the generated key securely - you'll need it for all deployments.

## Database Setup

PRFactory uses Entity Framework Core with SQLite by default (can be switched to SQL Server or PostgreSQL).

### Apply Migrations

```bash
cd src/PRFactory.Api

# Apply database migrations
dotnet ef database update

# Verify database was created
ls prfactory.db  # Should exist
```

### View Database Schema

```bash
# Using SQLite CLI
sqlite3 prfactory.db

# View tables
.tables

# View schema
.schema

# Query data
SELECT * FROM Tenants;
SELECT * FROM Tickets;

# Exit
.quit
```

### Seed Initial Data (Optional)

You can manually add a test tenant using the API, or create a seed script:

```bash
# Start the API first (see next section)
# Then use curl or Swagger UI to create a tenant

curl -X POST http://localhost:5000/api/tenants \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test Company",
    "jiraBaseUrl": "https://test.atlassian.net",
    "jiraApiToken": "encrypted-token",
    "claudeApiKey": "encrypted-key"
  }'
```

## Running the Application

### Option 1: Docker Compose (Recommended)

This runs the API and Worker in containers.

```bash
# Build and start all services
docker-compose up --build

# Or run in detached mode
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down
```

**Services:**
- API: http://localhost:5000
- Swagger UI: http://localhost:5000/swagger
- Worker: Background job processing

### Option 2: Run Locally

#### Start the API

```bash
cd src/PRFactory.Api
dotnet run
```

The API will start on:
- HTTP: http://localhost:5000
- HTTPS: https://localhost:5001
- Swagger: http://localhost:5000/swagger

#### Start the Worker (in a separate terminal)

```bash
cd src/PRFactory.Worker
dotnet run
```

The worker will continuously poll for tickets to process.

### Option 3: Visual Studio / Rider

1. Open `PRFactory.sln`
2. Set multiple startup projects:
   - PRFactory.Api
   - PRFactory.Worker
3. Press F5 to start debugging

## Configuring Integrations

### Jira Webhook Setup

1. **Go to Jira Settings** → System → Webhooks
2. **Create a new webhook**:
   - URL: `https://your-prfactory-domain.com/api/webhooks/jira`
   - Events:
     - Issue → created
     - Issue → updated
     - Comment → created
   - JQL Filter (optional): `labels = "Claude" OR text ~ "@claude"`
3. **Configure HMAC Secret**:
   - Generate a random secret: `openssl rand -hex 32`
   - Add to webhook configuration in Jira
   - Set the same secret in PRFactory config (`Jira:WebhookSecret`)

### GitHub Repository Access

1. **Create Personal Access Token**:
   - Go to https://github.com/settings/tokens
   - Click "Generate new token (classic)"
   - Scopes needed:
     - `repo` (full control of private repositories)
     - `workflow` (if updating GitHub Actions)
2. **Add to PRFactory Config**:
   - Set `GitHub:Token` in configuration
   - PRFactory encrypts and stores per-tenant credentials

### Claude API Setup

1. **Get API Key**:
   - Sign up at https://console.anthropic.com/
   - Navigate to API Keys
   - Create a new key
2. **Configure in PRFactory**:
   - Set `Claude:ApiKey` in configuration
   - Optionally adjust `Claude:MaxTokens` for response length

### Multi-Tenant Configuration

For each customer/tenant, create a tenant record via the API:

```bash
curl -X POST http://localhost:5000/api/tenants \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Acme Corp",
    "jiraBaseUrl": "https://acme.atlassian.net",
    "jiraApiToken": "your-jira-token",
    "claudeApiKey": "your-claude-key"
  }'
```

Then register repositories for that tenant:

```bash
curl -X POST http://localhost:5000/api/repositories \
  -H "Content-Type: application/json" \
  -d '{
    "tenantId": "tenant-guid-here",
    "name": "my-project",
    "provider": "GitHub",
    "cloneUrl": "https://github.com/acme/my-project.git",
    "accessToken": "ghp_..."
  }'
```

## Troubleshooting

### Build Issues

**Error: `dotnet: command not found`**
- Solution: Install .NET 8 SDK from https://dotnet.microsoft.com/download

**Error: Package restore failed**
- Solution: Clear NuGet cache
  ```bash
  dotnet nuget locals all --clear
  dotnet restore
  ```

**Error: Project doesn't support framework .NET 8**
- Solution: Update global.json or install .NET 8 SDK

### Database Issues

**Error: `Unable to open database file`**
- Check file permissions on `prfactory.db`
- Ensure the directory exists and is writable
- On Linux: `chmod 644 prfactory.db`

**Error: `No such table: Tickets`**
- Migrations not applied
- Solution: `dotnet ef database update`

**Error: `Encryption key not configured`**
- Solution: Generate and set encryption key in config
  ```bash
  dotnet run --project src/PRFactory.Api -- generate-key
  ```

### Runtime Issues

**Error: `Invalid Jira webhook signature`**
- HMAC secret mismatch between Jira and PRFactory
- Solution: Ensure `Jira:WebhookSecret` matches webhook config in Jira

**Error: `GitHub API rate limit exceeded`**
- Using unauthenticated requests or token has low limits
- Solution: Verify `GitHub:Token` is set correctly

**Error: `Claude API authentication failed`**
- Invalid or expired API key
- Solution: Verify `Claude:ApiKey` at https://console.anthropic.com/

**Error: `Repository clone failed`**
- Check git credentials and repository URL
- Ensure access token has correct permissions
- Solution: Test manually: `git clone https://token@github.com/user/repo.git`

**Worker not processing tickets**
- Check Worker service is running
- Check database connection
- View logs for errors: `docker-compose logs worker` or check console output

### Docker Issues

**Error: Port already in use**
- Solution: Stop conflicting services or change ports in `docker-compose.yml`
  ```bash
  lsof -i :5000  # Find process using port 5000
  ```

**Error: Docker build fails**
- Clear Docker cache:
  ```bash
  docker-compose build --no-cache
  ```

**Container exits immediately**
- Check logs: `docker-compose logs api`
- Usually due to missing configuration or database issues

### Performance Issues

**Slow repository cloning**
- Large repositories take time on first clone
- Solution: Repository caching will help on subsequent runs
- Consider shallow clones for very large repos

**High memory usage**
- Claude API responses can be large
- Solution: Adjust `Claude:MaxTokens` to lower value
- Ensure workspace cleanup is running (old repos deleted)

**Database growing large**
- Old tickets and state not being cleaned up
- Solution: Implement retention policy (archive tickets after N days)

## Development Tips

### Debugging

**Enable detailed EF Core logging**:
```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore": "Information"
    },
    "EnableSensitiveDataLogging": true
  }
}
```

**Use Swagger for API testing**:
- Navigate to http://localhost:5000/swagger
- Test endpoints directly in browser

**View Hangfire dashboard** (if configured):
- Add Hangfire dashboard in Program.cs
- Navigate to /hangfire

### Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true

# Run specific test project
dotnet test tests/PRFactory.Tests/
```

### Database Migrations

**Create new migration**:
```bash
cd src/PRFactory.Api
dotnet ef migrations add AddNewFeature
```

**Rollback migration**:
```bash
dotnet ef database update PreviousMigrationName
dotnet ef migrations remove
```

**Switch to SQL Server**:
1. Update connection string in appsettings.json
2. Change DbContext provider in DependencyInjection.cs
3. Delete existing migrations
4. Create new migration: `dotnet ef migrations add InitialCreate`

## Production Deployment

### Security Checklist

- [ ] Use HTTPS (configure SSL certificates)
- [ ] Store secrets in Azure Key Vault or AWS Secrets Manager
- [ ] Enable authentication on API endpoints
- [ ] Configure CORS policies
- [ ] Set up firewall rules (only allow required ports)
- [ ] Rotate encryption keys periodically
- [ ] Enable audit logging
- [ ] Configure backup for database
- [ ] Use SQL Server or PostgreSQL instead of SQLite

### Azure App Service Deployment

```bash
# Create App Service
az webapp create --resource-group MyResourceGroup \
  --plan MyPlan --name prfactory-api --runtime "DOTNETCORE:8.0"

# Deploy
dotnet publish -c Release
cd src/PRFactory.Api/bin/Release/net8.0/publish
zip -r deploy.zip .
az webapp deployment source config-zip --resource-group MyResourceGroup \
  --name prfactory-api --src deploy.zip
```

### Docker Production Build

```bash
# Build optimized images
docker build -f src/PRFactory.Api/Dockerfile -t prfactory-api:latest .
docker build -f src/PRFactory.Worker/Dockerfile -t prfactory-worker:latest .

# Tag and push to registry
docker tag prfactory-api:latest myregistry.azurecr.io/prfactory-api:latest
docker push myregistry.azurecr.io/prfactory-api:latest
```

## Next Steps

- Review the [Architecture Documentation](ARCHITECTURE.md)
- Understand the [Workflow Details](WORKFLOW.md)
- Explore the [Database Schema](database-schema.md)
- Check component-specific READMEs in src/ folders

## Getting Help

- Check the logs (Serilog writes to console and files)
- Review the [FAQ](../README.md#support)
- Open an issue on GitHub
- Check Jira webhook logs for webhook failures
