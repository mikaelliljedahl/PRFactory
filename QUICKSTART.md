# PRFactory Infrastructure Quick Start Guide

## Prerequisites

- .NET 8.0 SDK
- SQLite (or SQL Server / PostgreSQL for production)
- An IDE (Visual Studio, VS Code, or Rider)

## Step 1: Generate Encryption Key

First, generate a secure encryption key for sensitive data:

```bash
cd src/PRFactory.Infrastructure
dotnet run --project ../PRFactory.Api -- generate-key
```

Or use this C# snippet:
```csharp
using PRFactory.Infrastructure.Persistence.Encryption;

var key = EncryptionKeyGenerator.GenerateKey();
Console.WriteLine($"Encryption Key: {key}");
```

**Important**: Save this key securely! You'll need it for configuration.

## Step 2: Configure Application

Create or update `appsettings.json` in your API project:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=prfactory.db"
  },
  "Encryption": {
    "Key": "YOUR_BASE64_KEY_FROM_STEP_1"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore": "Warning"
    },
    "EnableSensitiveDataLogging": false,
    "EnableDetailedErrors": true
  }
}
```

For development, you can also use environment variables:
```bash
export Encryption__Key="YOUR_BASE64_KEY_HERE"
export ConnectionStrings__DefaultConnection="Data Source=prfactory.db"
```

## Step 3: Apply Database Migration

Run the initial migration to create the database:

```bash
cd src/PRFactory.Infrastructure
dotnet ef database update
```

This will create `prfactory.db` in the Infrastructure project directory with all tables, indexes, and constraints.

## Step 4: Register Services

In your `Program.cs` or `Startup.cs`:

```csharp
using PRFactory.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Register Infrastructure services
builder.Services.AddInfrastructure(builder.Configuration);

// Register your application services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

## Step 5: Seed Initial Data (Optional)

Create a seed data script to populate initial tenants and repositories:

```csharp
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var tenantRepo = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
        var repoRepo = scope.ServiceProvider.GetRequiredService<IRepositoryRepository>();

        // Check if already seeded
        var tenants = await tenantRepo.GetAllAsync();
        if (tenants.Any())
            return;

        // Create a test tenant
        var tenant = Tenant.Create(
            "Acme Corporation",
            "https://acme.atlassian.net",
            "jira-api-token-here",
            "claude-api-key-here"
        );
        await tenantRepo.AddAsync(tenant);

        // Create a test repository
        var repository = Repository.Create(
            tenant.Id,
            "acme-web-app",
            "GitHub",
            "https://github.com/acme/web-app.git",
            "github-pat-token-here"
        );
        await repoRepo.AddAsync(repository);

        Console.WriteLine("Database seeded successfully!");
    }
}

// In Program.cs, after building the app:
using (var scope = app.Services.CreateScope())
{
    await DatabaseSeeder.SeedAsync(scope.ServiceProvider);
}
```

## Step 6: Use Repositories in Your Code

### Example: Create a Ticket Service

```csharp
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Domain.ValueObjects;

public class TicketService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IRepositoryRepository _repositoryRepository;
    private readonly ILogger<TicketService> _logger;

    public TicketService(
        ITicketRepository ticketRepository,
        ITenantRepository tenantRepository,
        IRepositoryRepository repositoryRepository,
        ILogger<TicketService> logger)
    {
        _ticketRepository = ticketRepository;
        _tenantRepository = tenantRepository;
        _repositoryRepository = repositoryRepository;
        _logger = logger;
    }

    public async Task<Ticket> CreateTicketAsync(
        string ticketKey,
        Guid tenantId,
        Guid repositoryId)
    {
        // Validate tenant and repository exist
        var tenant = await _tenantRepository.GetByIdAsync(tenantId);
        if (tenant == null || !tenant.IsActive)
            throw new InvalidOperationException("Tenant not found or inactive");

        var repository = await _repositoryRepository.GetByIdAsync(repositoryId);
        if (repository == null)
            throw new InvalidOperationException("Repository not found");

        // Create ticket
        var ticket = Ticket.Create(ticketKey, tenantId, repositoryId);
        await _ticketRepository.AddAsync(ticket);

        _logger.LogInformation("Created ticket {TicketKey} for tenant {TenantId}",
            ticketKey, tenantId);

        return ticket;
    }

    public async Task TransitionTicketAsync(Guid ticketId, WorkflowState newState)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId);
        if (ticket == null)
            throw new InvalidOperationException("Ticket not found");

        // Transition will throw if invalid
        var result = ticket.TransitionTo(newState);
        if (!result.IsSuccess)
            throw new InvalidOperationException(result.ErrorMessage);

        await _ticketRepository.UpdateAsync(ticket);

        _logger.LogInformation("Transitioned ticket {TicketId} to {State}",
            ticketId, newState);
    }

    public async Task<List<Ticket>> GetActiveTicketsAsync(Guid tenantId)
    {
        return await _ticketRepository.GetActiveTicketsAsync(tenantId);
    }
}
```

### Example: Create an API Controller

```csharp
using Microsoft.AspNetCore.Mvc;
using PRFactory.Domain.ValueObjects;

[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly TicketService _ticketService;

    public TicketsController(TicketService ticketService)
    {
        _ticketService = ticketService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTicket(
        [FromBody] CreateTicketRequest request)
    {
        try
        {
            var ticket = await _ticketService.CreateTicketAsync(
                request.TicketKey,
                request.TenantId,
                request.RepositoryId);

            return CreatedAtAction(
                nameof(GetTicket),
                new { id = ticket.Id },
                ticket);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTicket(Guid id)
    {
        var ticket = await _ticketRepository.GetByIdAsync(id);
        if (ticket == null)
            return NotFound();

        return Ok(ticket);
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveTickets(
        [FromQuery] Guid tenantId)
    {
        var tickets = await _ticketService.GetActiveTicketsAsync(tenantId);
        return Ok(tickets);
    }

    [HttpPost("{id}/transition")]
    public async Task<IActionResult> TransitionTicket(
        Guid id,
        [FromBody] TransitionRequest request)
    {
        try
        {
            await _ticketService.TransitionTicketAsync(id, request.NewState);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public record CreateTicketRequest(
    string TicketKey,
    Guid TenantId,
    Guid RepositoryId);

public record TransitionRequest(WorkflowState NewState);
```

## Step 7: Run and Test

1. **Start the application**:
   ```bash
   dotnet run --project src/PRFactory.Api
   ```

2. **Test with Swagger**: Navigate to `https://localhost:5001/swagger`

3. **Create a tenant**:
   ```bash
   curl -X POST https://localhost:5001/api/tenants \
     -H "Content-Type: application/json" \
     -d '{
       "name": "Test Tenant",
       "jiraUrl": "https://test.atlassian.net",
       "jiraApiToken": "test-token",
       "claudeApiKey": "test-key"
     }'
   ```

4. **Create a ticket**:
   ```bash
   curl -X POST https://localhost:5001/api/tickets \
     -H "Content-Type: application/json" \
     -d '{
       "ticketKey": "PROJ-123",
       "tenantId": "tenant-guid-here",
       "repositoryId": "repo-guid-here"
     }'
   ```

## Common Tasks

### View Database Schema
```bash
sqlite3 prfactory.db ".schema"
```

### Query Database
```bash
sqlite3 prfactory.db
SELECT * FROM Tenants;
SELECT * FROM Tickets WHERE State = 'Analyzing';
.quit
```

### Create a New Migration
```bash
cd src/PRFactory.Infrastructure
dotnet ef migrations add AddNewFeature
dotnet ef database update
```

### Rollback a Migration
```bash
dotnet ef database update PreviousMigrationName
dotnet ef migrations remove
```

### Switch to SQL Server
Update `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=PRFactory;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

Update `PRFactory.Infrastructure.csproj`:
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
```

Update `DependencyInjection.cs`:
```csharp
services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    options.UseSqlServer(connectionString);
});
```

Recreate migrations:
```bash
dotnet ef migrations remove
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## Troubleshooting

### Error: "Encryption key not configured"
- Ensure `Encryption:Key` is set in `appsettings.json` or environment variables
- Generate a new key with `EncryptionKeyGenerator.GenerateKey()`

### Error: "Unable to open database file"
- Check file permissions on `prfactory.db`
- Ensure the directory exists and is writable

### Error: "Invalid transition from X to Y"
- Check `WorkflowStateTransitions.cs` for valid state transitions
- Ensure you're following the proper workflow sequence

### Slow Queries
- Check that indexes are being used: `EXPLAIN QUERY PLAN SELECT ...`
- Use `.AsNoTracking()` for read-only queries
- Consider pagination for large result sets

### Encryption/Decryption Errors
- Ensure the encryption key hasn't changed
- Verify the key is exactly 256 bits (32 bytes) encoded as Base64
- Check for special characters or whitespace in the key

## Next Steps

1. **Security**: Move encryption key to Azure Key Vault or AWS Secrets Manager
2. **Monitoring**: Add Application Insights or similar for telemetry
3. **Caching**: Implement caching for frequently accessed data
4. **Background Jobs**: Set up Hangfire for workflow processing
5. **API Authentication**: Add JWT or OAuth2 authentication
6. **Rate Limiting**: Implement rate limiting for API endpoints
7. **Documentation**: Generate API documentation with Swashbuckle

## Additional Resources

- [EF Core Documentation](https://learn.microsoft.com/en-us/ef/core/)
- [Repository Pattern](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Domain-Driven Design](https://martinfowler.com/bliki/DomainDrivenDesign.html)

## Support

For issues or questions:
1. Check the logs for detailed error messages
2. Review the documentation in `src/PRFactory.Infrastructure/README.md`
3. Examine the database schema in `docs/database-schema.md`
4. Review entity configurations in `src/PRFactory.Infrastructure/Persistence/Configurations/`
