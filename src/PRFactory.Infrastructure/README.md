# PRFactory.Infrastructure

This project contains the infrastructure layer for PRFactory, including:

## Entity Framework Core Setup

### Database Context
- `ApplicationDbContext`: Main EF Core DbContext with DbSets for all entities
- Includes encryption for sensitive fields (API tokens, access tokens)
- Supports SQLite database

### Entity Configurations
Located in `Persistence/Configurations/`:
- `TenantConfiguration`: Configuration for Tenant entities
- `RepositoryConfiguration`: Configuration for Repository entities
- `TicketConfiguration`: Configuration for Ticket entities
- `WorkflowEventConfiguration`: Configuration for WorkflowEvent entities (TPH inheritance)

### Repositories
Located in `Persistence/Repositories/`:
- `TenantRepository`: CRUD operations for tenants
- `RepositoryRepository`: CRUD operations for repositories
- `TicketRepository`: CRUD operations for tickets with workflow state queries

### Encryption
Located in `Persistence/Encryption/`:
- `IEncryptionService`: Interface for encryption operations
- `AesEncryptionService`: AES-256-GCM implementation for encrypting sensitive data at rest

## Configuration

### Required Configuration Settings

Add these to your `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=prfactory.db"
  },
  "Encryption": {
    "Key": "YOUR_BASE64_ENCRYPTION_KEY_HERE"
  },
  "Logging": {
    "EnableSensitiveDataLogging": false,
    "EnableDetailedErrors": false
  }
}
```

### Generating an Encryption Key

To generate a secure encryption key, run this code once:

```csharp
using PRFactory.Infrastructure;

var key = DependencyInjection.GenerateEncryptionKey();
Console.WriteLine($"Encryption Key: {key}");
```

Or use the provided helper:

```csharp
using PRFactory.Infrastructure.Persistence.Encryption;

var key = EncryptionKeyGenerator.GenerateKey();
Console.WriteLine($"Encryption Key: {key}");
```

**Important**: Store this key securely (e.g., Azure Key Vault, AWS Secrets Manager). Never commit it to source control.

## Database Migrations

### Initial Migration

The initial migration has been created: `20251104000000_InitialCreate.cs`

To apply the migration to create the database:

```bash
cd src/PRFactory.Infrastructure
dotnet ef database update
```

### Creating New Migrations

After modifying entities or configurations:

```bash
dotnet ef migrations add MigrationName
dotnet ef database update
```

### Removing the Last Migration

If you need to remove the last migration (before applying it):

```bash
dotnet ef migrations remove
```

## Dependency Injection

Register all infrastructure services in your `Program.cs` or `Startup.cs`:

```csharp
using PRFactory.Infrastructure;

// Register infrastructure services
builder.Services.AddInfrastructure(builder.Configuration);
```

This registers:
- `ApplicationDbContext`
- `IEncryptionService` → `AesEncryptionService`
- `ITenantRepository` → `TenantRepository`
- `IRepositoryRepository` → `RepositoryRepository`
- `ITicketRepository` → `TicketRepository`

## Usage Examples

### Using Repositories

```csharp
public class MyService
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITicketRepository _ticketRepository;

    public MyService(
        ITenantRepository tenantRepository,
        ITicketRepository ticketRepository)
    {
        _tenantRepository = tenantRepository;
        _ticketRepository = ticketRepository;
    }

    public async Task ProcessTicket(string ticketKey)
    {
        var ticket = await _ticketRepository.GetByTicketKeyAsync(ticketKey);
        if (ticket == null)
            throw new NotFoundException("Ticket not found");

        // Process ticket...
        ticket.TransitionTo(WorkflowState.Analyzing);

        await _ticketRepository.UpdateAsync(ticket);
    }
}
```

### Working with Encrypted Fields

Encryption/decryption is handled automatically by EF Core value converters:

```csharp
// Creating a tenant - token is stored encrypted
var tenant = Tenant.Create(
    "Acme Corp",
    "https://acme.atlassian.net",
    "jira-api-token-here",  // Will be encrypted in DB
    "claude-api-key-here"    // Will be encrypted in DB
);

await _tenantRepository.AddAsync(tenant);

// Reading a tenant - token is decrypted automatically
var tenant = await _tenantRepository.GetByIdAsync(tenantId);
var jiraToken = tenant.JiraApiToken;  // Automatically decrypted
```

## Database Schema

### Tables
- `Tenants`: Customer tenants with encrypted credentials
- `Repositories`: Git repositories with encrypted access tokens
- `Tickets`: Tickets being processed through the workflow
- `WorkflowEvents`: Event history for ticket state transitions

### Indexes
Optimized indexes for common queries:
- Ticket lookups by key, state, tenant, repository
- Repository lookups by tenant, platform, URL
- Tenant lookups by name, active status
- Event lookups by ticket and timestamp

## Security Considerations

1. **Encryption Keys**: Store encryption keys securely (Key Vault, Secrets Manager)
2. **Connection Strings**: Use secure connection strings in production
3. **Sensitive Data Logging**: Disable in production
4. **Access Control**: Ensure proper authentication/authorization at API layer
5. **Regular Key Rotation**: Implement key rotation strategy for long-term security

## Testing

For unit tests, you can use in-memory database:

```csharp
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseInMemoryDatabase(databaseName: "TestDb")
    .Options;

var encryptionService = new Mock<IEncryptionService>();
var logger = new Mock<ILogger<ApplicationDbContext>>();

var context = new ApplicationDbContext(options, encryptionService.Object, logger.Object);
```

## Troubleshooting

### Migration Errors

If you get errors about missing migrations:
```bash
dotnet ef database update
```

### Encryption Errors

If you get decryption errors, ensure:
1. The encryption key in configuration matches the key used to encrypt the data
2. The key is a valid base64-encoded 256-bit (32 byte) key
3. No special characters or whitespace in the key

### Performance Issues

For large datasets:
1. Review query patterns and add appropriate indexes
2. Use `.AsNoTracking()` for read-only queries
3. Consider pagination for list queries
4. Use `.Include()` strategically to avoid N+1 queries
