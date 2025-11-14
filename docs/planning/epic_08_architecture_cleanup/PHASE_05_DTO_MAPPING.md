# Phase 5: DTO Mapping with Mapperly

**Duration**: 1 week
**Risk Level**: 🟡 Medium
**Agent Type**: `code-implementation-specialist`
**Dependencies**: Phase 4 merged to main

---

## Objective

Centralize entity-to-DTO mapping using Mapperly (compile-time source generation) to eliminate manual mapping code scattered across services.

**Why this matters**: Manual mapping code is duplicated in multiple places (web services, application services, pages), making maintenance difficult and error-prone. Mapperly generates mapping code at compile time for zero runtime overhead.

---

## Success Criteria

- ✅ Mapperly NuGet package installed
- ✅ Mapper interfaces created for all entities (Ticket, Repository, WorkflowEvent, Tenant, etc.)
- ✅ Mappers registered in DI container
- ✅ Services updated to use mappers (no manual mapping)
- ✅ All tests pass
- ✅ Compile-time mapping validation works

---

## Why Mapperly Over AutoMapper?

| Feature | Mapperly | AutoMapper |
|---------|----------|------------|
| **Performance** | Zero runtime overhead (source-generated) | Runtime reflection |
| **Compile-time safety** | ✅ Errors at compile time | ❌ Errors at runtime |
| **Debugging** | ✅ Generated code visible | ❌ Black box |
| **Setup complexity** | Simple (attribute + partial class) | Complex (profiles, configuration) |

---

## Implementation Steps

### Step 1: Install Mapperly (30 minutes)

#### 1.1 Add NuGet Package

Edit `src/PRFactory.Infrastructure/PRFactory.Infrastructure.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="Riok.Mapperly" Version="3.6.0" />
</ItemGroup>
```

Or via CLI:
```bash
cd src/PRFactory.Infrastructure
source /tmp/dotnet-proxy-setup.sh
dotnet add package Riok.Mapperly
```

#### 1.2 Verify Installation

```bash
dotnet restore
dotnet build
```

---

### Step 2: Create Mapper Interfaces (2-3 hours)

#### 2.1 TicketMapper

Create `src/PRFactory.Infrastructure/Mapping/TicketMapper.cs`:

```csharp
using Riok.Mapperly.Abstractions;
using PRFactory.Domain.Entities;
using PRFactory.Core.DTOs;

namespace PRFactory.Infrastructure.Mapping;

[Mapper]
public partial class TicketMapper
{
    // Entity to DTO
    public partial TicketDto ToDto(Ticket entity);
    public partial List<TicketDto> ToDtoList(List<Ticket> entities);

    // DTO to Entity (for creation)
    public partial Ticket ToEntity(CreateTicketDto dto);

    // Update existing entity from DTO
    public partial void UpdateEntity(UpdateTicketDto dto, Ticket entity);

    // Custom mapping for complex properties (if needed)
    [MapProperty(nameof(Ticket.Repository), nameof(TicketDto.RepositoryName))]
    private string MapRepositoryName(Repository repository) => repository.Name;
}
```

**Key points**:
- `[Mapper]` attribute tells Mapperly to generate code
- `partial` class allows Mapperly to add generated code
- `partial` methods will be implemented by source generator
- Custom mappings handled with `[MapProperty]` or private methods

#### 2.2 RepositoryMapper

Create `src/PRFactory.Infrastructure/Mapping/RepositoryMapper.cs`:

```csharp
using Riok.Mapperly.Abstractions;
using PRFactory.Domain.Entities;
using PRFactory.Core.DTOs;

namespace PRFactory.Infrastructure.Mapping;

[Mapper]
public partial class RepositoryMapper
{
    public partial RepositoryDto ToDto(Repository entity);
    public partial List<RepositoryDto> ToDtoList(List<Repository> entities);

    public partial Repository ToEntity(CreateRepositoryDto dto);
    public partial void UpdateEntity(UpdateRepositoryDto dto, Repository entity);
}
```

#### 2.3 WorkflowEventMapper

Create `src/PRFactory.Infrastructure/Mapping/WorkflowEventMapper.cs`:

```csharp
using Riok.Mapperly.Abstractions;
using PRFactory.Domain.Entities;
using PRFactory.Core.DTOs;

namespace PRFactory.Infrastructure.Mapping;

[Mapper]
public partial class WorkflowEventMapper
{
    public partial WorkflowEventDto ToDto(WorkflowEvent entity);
    public partial List<WorkflowEventDto> ToDtoList(List<WorkflowEvent> entities);
}
```

#### 2.4 TenantMapper, ErrorMapper, etc.

Create similar mappers for:
- `TenantMapper`
- `ErrorMapper`
- `AgentPromptMapper`
- `UserMapper` (if applicable)

**Pattern**:
```csharp
[Mapper]
public partial class [Entity]Mapper
{
    public partial [Entity]Dto ToDto([Entity] entity);
    public partial List<[Entity]Dto> ToDtoList(List<[Entity]> entities);
    // Add Create/Update mappings if needed
}
```

---

### Step 3: Register Mappers in DI (30 minutes)

Edit `src/PRFactory.Web/Program.cs`:

```csharp
using PRFactory.Infrastructure.Mapping;

var builder = WebApplication.CreateBuilder(args);

// ... existing service registrations ...

// ============================================================
// MAPPERS (NEW - Phase 5)
// ============================================================
builder.Services.AddSingleton<TicketMapper>();
builder.Services.AddSingleton<RepositoryMapper>();
builder.Services.AddSingleton<WorkflowEventMapper>();
builder.Services.AddSingleton<TenantMapper>();
builder.Services.AddSingleton<ErrorMapper>();
builder.Services.AddSingleton<AgentPromptMapper>();

// ... rest of Program.cs ...
```

**Why Singleton?** Mappers are stateless and can be reused across requests.

---

### Step 4: Update Services to Use Mappers (3-4 hours)

#### 4.1 Update TicketService (Web Layer)

**Before** (manual mapping):
```csharp
public async Task<TicketDto> GetTicketByIdAsync(Guid id, CancellationToken ct = default)
{
    var ticket = await _ticketApplicationService.GetTicketByIdAsync(id, ct);

    // Manual mapping (BAD)
    return new TicketDto
    {
        Id = ticket.Id,
        Title = ticket.Title,
        Description = ticket.Description,
        Status = ticket.Status,
        CreatedAt = ticket.CreatedAt,
        UpdatedAt = ticket.UpdatedAt,
        RepositoryName = ticket.Repository?.Name,
        // ... many more properties
    };
}
```

**After** (using Mapperly):
```csharp
public class TicketService : ITicketService
{
    private readonly ITicketApplicationService _ticketApplicationService;
    private readonly TicketMapper _ticketMapper;

    public TicketService(
        ITicketApplicationService ticketApplicationService,
        TicketMapper ticketMapper)
    {
        _ticketApplicationService = ticketApplicationService;
        _ticketMapper = ticketMapper;
    }

    public async Task<TicketDto> GetTicketByIdAsync(Guid id, CancellationToken ct = default)
    {
        var ticket = await _ticketApplicationService.GetTicketByIdAsync(id, ct);
        return _ticketMapper.ToDto(ticket); // One line!
    }

    public async Task<List<TicketDto>> GetAllTicketsAsync(CancellationToken ct = default)
    {
        var tickets = await _ticketApplicationService.GetAllTicketsAsync(ct);
        return _ticketMapper.ToDtoList(tickets); // One line!
    }
}
```

#### 4.2 Update RepositoryService

Follow same pattern:
```csharp
public class RepositoryService : IRepositoryService
{
    private readonly IRepositoryApplicationService _repoApplicationService;
    private readonly RepositoryMapper _repoMapper;

    public RepositoryService(
        IRepositoryApplicationService repoApplicationService,
        RepositoryMapper repoMapper)
    {
        _repoApplicationService = repoApplicationService;
        _repoMapper = repoMapper;
    }

    public async Task<RepositoryDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var repo = await _repoApplicationService.GetByIdAsync(id, ct);
        return _repoMapper.ToDto(repo);
    }

    // ... repeat for all methods
}
```

#### 4.3 Update Other Services

Repeat for:
- `WorkflowEventService`
- `TenantService`
- `ErrorService`
- `AgentPromptService`
- Any other services with manual mapping

---

### Step 5: Remove Manual Mapping Code (1-2 hours)

#### 5.1 Find All Manual Mappings

```bash
# Search for manual DTO creation
cd src/PRFactory.Web/Services
grep -r "new.*Dto" --include="*.cs" .

# Search for property-by-property mapping
grep -r "= entity\." --include="*.cs" .
```

#### 5.2 Delete Manual Mapping Code

For each service:
1. Remove manual mapping methods
2. Replace with mapper calls
3. Remove unused private mapping methods

**Example - Remove this**:
```csharp
private TicketDto MapToDto(Ticket ticket)
{
    return new TicketDto
    {
        Id = ticket.Id,
        Title = ticket.Title,
        // ... 20 more lines
    };
}
```

**Replace with**:
```csharp
// Just use _ticketMapper.ToDto(ticket) directly where needed
```

---

### Step 6: Testing (2 hours)

#### 6.1 Build and Verify Generated Code

```bash
source /tmp/dotnet-proxy-setup.sh
dotnet build
```

**Check generated files**:
```bash
# Mapperly generates code in obj/ directory
find obj/ -name "*Mapper.g.cs"
```

Inspect generated code to verify correct mapping:
```bash
cat obj/Debug/net10.0/generated/Riok.Mapperly/Riok.Mapperly.TicketMapper.g.cs
```

**Expected**: Generated methods implement mapping logic

#### 6.2 Unit Tests

Create `tests/PRFactory.Infrastructure.Tests/Mapping/TicketMapperTests.cs`:

```csharp
public class TicketMapperTests
{
    private readonly TicketMapper _mapper = new();

    [Fact]
    public void ToDto_MapsAllProperties()
    {
        // Arrange
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            Title = "Test Ticket",
            Description = "Test Description",
            Status = TicketStatus.Open,
            CreatedAt = DateTime.UtcNow,
            Repository = new Repository { Name = "test-repo" }
        };

        // Act
        var dto = _mapper.ToDto(ticket);

        // Assert
        Assert.Equal(ticket.Id, dto.Id);
        Assert.Equal(ticket.Title, dto.Title);
        Assert.Equal(ticket.Description, dto.Description);
        Assert.Equal(ticket.Status, dto.Status);
        Assert.Equal(ticket.CreatedAt, dto.CreatedAt);
        Assert.Equal("test-repo", dto.RepositoryName);
    }

    [Fact]
    public void ToDtoList_MapsAllEntities()
    {
        // Arrange
        var tickets = new List<Ticket>
        {
            new() { Id = Guid.NewGuid(), Title = "Ticket 1" },
            new() { Id = Guid.NewGuid(), Title = "Ticket 2" }
        };

        // Act
        var dtos = _mapper.ToDtoList(tickets);

        // Assert
        Assert.Equal(2, dtos.Count);
        Assert.Equal(tickets[0].Title, dtos[0].Title);
        Assert.Equal(tickets[1].Title, dtos[1].Title);
    }
}
```

#### 6.3 Integration Tests

Verify services still work correctly:
```bash
dotnet test --filter "FullyQualifiedName~ServiceTests"
```

**Expected**: All service tests pass with mappers

---

### Step 7: Update PagedResult Mapping (1 hour)

From Phase 3, update `PagedResult<T>` mapping in services:

**Before**:
```csharp
public async Task<PagedResult<TicketDto>> GetTicketsPagedAsync(...)
{
    var pagedResult = await _ticketApplicationService.GetTicketsPagedAsync(...);

    // Manual mapping
    return new PagedResult<TicketDto>
    {
        Items = pagedResult.Items.Select(MapToDto).ToList(),
        TotalCount = pagedResult.TotalCount,
        Page = pagedResult.Page,
        PageSize = pagedResult.PageSize
    };
}
```

**After**:
```csharp
public async Task<PagedResult<TicketDto>> GetTicketsPagedAsync(...)
{
    var pagedResult = await _ticketApplicationService.GetTicketsPagedAsync(...);

    // Using mapper
    return new PagedResult<TicketDto>
    {
        Items = _ticketMapper.ToDtoList(pagedResult.Items),
        TotalCount = pagedResult.TotalCount,
        Page = pagedResult.Page,
        PageSize = pagedResult.PageSize
    };
}
```

---

## Validation Checklist

- [ ] Mapperly NuGet package installed
- [ ] Mapper classes created for all entities (5-7 mappers)
- [ ] Mappers registered in DI (Program.cs)
- [ ] All web services updated to use mappers
- [ ] Manual mapping code removed
- [ ] Generated mapper code verified (check obj/ directory)
- [ ] Unit tests for mappers pass
- [ ] Integration tests pass
- [ ] All existing tests still pass
- [ ] No runtime mapping errors

---

## Deliverables

1. **Mapper Classes** (in `Infrastructure/Mapping/`)
   - `TicketMapper.cs`
   - `RepositoryMapper.cs`
   - `WorkflowEventMapper.cs`
   - `TenantMapper.cs`
   - `ErrorMapper.cs`
   - `AgentPromptMapper.cs`

2. **Updated Services**
   - `TicketService.cs` (uses TicketMapper)
   - `RepositoryService.cs` (uses RepositoryMapper)
   - `WorkflowEventService.cs` (uses WorkflowEventMapper)
   - `TenantService.cs` (uses TenantMapper)
   - `ErrorService.cs` (uses ErrorMapper)

3. **DI Registration**
   - Updated `Program.cs` with mapper registrations

4. **Tests**
   - Unit tests for all mappers

5. **Removed Code**
   - Manual mapping methods deleted

6. **Commit**
   ```bash
   git commit -m "feat(epic-08): centralize DTO mapping with Mapperly

   - Install Riok.Mapperly for compile-time source generation
   - Create mapper classes for all entities
   - Register mappers in DI container
   - Update all services to use mappers
   - Remove manual mapping code
   - Add unit tests for mappers
   - Zero runtime overhead vs. manual mapping

   Closes Phase 5 of EPIC 08"
   ```

---

## Common Issues & Solutions

### Issue: "Mapper not generating code"

**Solution**: Ensure:
1. `[Mapper]` attribute present
2. Class is `partial`
3. Methods are `partial`
4. Clean and rebuild: `dotnet clean && dotnet build`

### Issue: "Property not mapped"

**Solution**: Add explicit mapping:
```csharp
[MapProperty(nameof(Source.Prop), nameof(Dest.Prop))]
```

Or custom mapping method:
```csharp
private string CustomMapping(ComplexType input) => input.ToString();
```

### Issue: "Circular reference"

**Solution**: Use `[MapperIgnore]` on navigation properties that cause cycles:
```csharp
[Mapper]
public partial class TicketMapper
{
    [MapperIgnore(nameof(Ticket.Repository.Tickets))]
    public partial TicketDto ToDto(Ticket entity);
}
```

---

## Phase 5 Complete!

**Next**: `PHASE_06_FINAL_POLISH.md`
