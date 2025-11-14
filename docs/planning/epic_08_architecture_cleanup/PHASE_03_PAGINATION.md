# Phase 3: Server-Side Pagination

**Duration**: 1 week
**Risk Level**: 🟡 Medium
**Agent Type**: `code-implementation-specialist`
**Dependencies**: Phase 2 merged to main

---

## Objective

Replace in-memory filtering/pagination with server-side implementation for scalability.

**Why this matters**: Currently `Tickets/Index.razor` loads ALL tickets into memory then filters locally. This doesn't scale beyond 1000+ tickets and wastes memory/network bandwidth.

---

## Success Criteria

- ✅ `PagedResult<T>` and `PaginationParams` DTOs created
- ✅ Repository layer supports server-side filtering/pagination
- ✅ Service layer uses pagination methods
- ✅ `Tickets/Index.razor` refactored to use server-side pagination
- ✅ Page load time <500ms for 1000+ tickets
- ✅ Memory usage reduced (no loading all records)
- ✅ All tests pass

---

## Current Problem

**File**: `src/PRFactory.Web/Pages/Tickets/Index.razor.cs` (line ~88)

```csharp
// TODO: This loads ALL tickets and filters in memory - should be server-side
var allTickets = await _ticketService.GetAllTicketsAsync(ct);

// Client-side filtering (bad for scale)
if (!string.IsNullOrEmpty(searchQuery))
{
    filteredTickets = allTickets
        .Where(t => t.Title.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
        .ToList();
}

// Client-side pagination (bad for scale)
paginatedTickets = filteredTickets.Skip((currentPage - 1) * pageSize).Take(pageSize);
```

**Problems**:
- Loads ALL tickets (could be 10,000+)
- Network overhead
- Memory consumption
- Slow page load

---

## Implementation Steps

### Step 1: Create Pagination Infrastructure (1-2 hours)

#### 1.1 Create PagedResult DTO

Create `src/PRFactory.Core/DTOs/PagedResult.cs`:

```csharp
namespace PRFactory.Core.DTOs;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
```

#### 1.2 Create PaginationParams DTO

Create `src/PRFactory.Core/DTOs/PaginationParams.cs`:

```csharp
namespace PRFactory.Core.DTOs;

public class PaginationParams
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SearchQuery { get; set; }
    public string? SortBy { get; set; }
    public bool Descending { get; set; } = true;
}
```

---

### Step 2: Update Repository Layer (2-3 hours)

#### 2.1 Add Interface Method

File: `src/PRFactory.Core/Repositories/ITicketRepository.cs`

```csharp
Task<PagedResult<Ticket>> GetTicketsPagedAsync(
    PaginationParams paginationParams,
    TicketStatus? statusFilter = null,
    CancellationToken ct = default);
```

#### 2.2 Implement Repository Method

File: `src/PRFactory.Infrastructure/Repositories/TicketRepository.cs`

```csharp
public async Task<PagedResult<Ticket>> GetTicketsPagedAsync(
    PaginationParams paginationParams,
    TicketStatus? statusFilter = null,
    CancellationToken ct = default)
{
    var query = _context.Tickets.AsQueryable();

    // Apply status filter
    if (statusFilter.HasValue)
    {
        query = query.Where(t => t.Status == statusFilter.Value);
    }

    // Apply search filter
    if (!string.IsNullOrEmpty(paginationParams.SearchQuery))
    {
        query = query.Where(t =>
            t.Title.Contains(paginationParams.SearchQuery) ||
            t.Description.Contains(paginationParams.SearchQuery));
    }

    // Get total count BEFORE pagination
    var totalCount = await query.CountAsync(ct);

    // Apply sorting
    query = paginationParams.SortBy?.ToLower() switch
    {
        "title" => paginationParams.Descending
            ? query.OrderByDescending(t => t.Title)
            : query.OrderBy(t => t.Title),
        "status" => paginationParams.Descending
            ? query.OrderByDescending(t => t.Status)
            : query.OrderBy(t => t.Status),
        "created" or _ => paginationParams.Descending
            ? query.OrderByDescending(t => t.CreatedAt)
            : query.OrderBy(t => t.CreatedAt)
    };

    // Apply pagination
    var items = await query
        .Skip((paginationParams.Page - 1) * paginationParams.PageSize)
        .Take(paginationParams.PageSize)
        .ToListAsync(ct);

    return new PagedResult<Ticket>
    {
        Items = items,
        TotalCount = totalCount,
        Page = paginationParams.Page,
        PageSize = paginationParams.PageSize
    };
}
```

---

### Step 3: Update Application Service Layer (1 hour)

File: `src/PRFactory.Infrastructure/Application/TicketApplicationService.cs`

```csharp
public async Task<PagedResult<Ticket>> GetTicketsPagedAsync(
    PaginationParams paginationParams,
    TicketStatus? statusFilter = null,
    CancellationToken ct = default)
{
    return await _ticketRepository.GetTicketsPagedAsync(
        paginationParams,
        statusFilter,
        ct);
}
```

---

### Step 4: Update Web Service Layer (1 hour)

File: `src/PRFactory.Web/Services/ITicketService.cs`

```csharp
Task<PagedResult<TicketDto>> GetTicketsPagedAsync(
    PaginationParams paginationParams,
    TicketStatus? statusFilter = null,
    CancellationToken ct = default);
```

File: `src/PRFactory.Web/Services/TicketService.cs`

```csharp
public async Task<PagedResult<TicketDto>> GetTicketsPagedAsync(
    PaginationParams paginationParams,
    TicketStatus? statusFilter = null,
    CancellationToken ct = default)
{
    var pagedResult = await _ticketApplicationService.GetTicketsPagedAsync(
        paginationParams,
        statusFilter,
        ct);

    // Map entities to DTOs
    return new PagedResult<TicketDto>
    {
        Items = pagedResult.Items.Select(MapToDto).ToList(),
        TotalCount = pagedResult.TotalCount,
        Page = pagedResult.Page,
        PageSize = pagedResult.PageSize
    };
}
```

---

### Step 5: Refactor Tickets/Index Page (2-3 hours)

#### 5.1 Update Index.razor.cs

File: `src/PRFactory.Web/Pages/Tickets/Index.razor.cs`

```csharp
private PagedResult<TicketDto>? pagedTickets;
private PaginationParams paginationParams = new()
{
    Page = 1,
    PageSize = 20,
    SortBy = "created",
    Descending = true
};
private TicketStatus? statusFilter = null;

protected override async Task OnInitializedAsync()
{
    await LoadTicketsAsync();
}

private async Task LoadTicketsAsync()
{
    isLoading = true;
    errorMessage = null;

    try
    {
        // Server-side pagination!
        pagedTickets = await _ticketService.GetTicketsPagedAsync(
            paginationParams,
            statusFilter,
            CancellationToken.None);
    }
    catch (Exception ex)
    {
        errorMessage = $"Failed to load tickets: {ex.Message}";
        _logger.LogError(ex, "Failed to load tickets");
    }
    finally
    {
        isLoading = false;
    }
}

private async Task HandlePageChanged(int newPage)
{
    paginationParams.Page = newPage;
    await LoadTicketsAsync();
}

private async Task HandleSearchChanged(string searchQuery)
{
    paginationParams.SearchQuery = searchQuery;
    paginationParams.Page = 1; // Reset to page 1 when searching
    await LoadTicketsAsync();
}

private async Task HandleStatusFilterChanged(TicketStatus? status)
{
    statusFilter = status;
    paginationParams.Page = 1; // Reset to page 1 when filtering
    await LoadTicketsAsync();
}

private async Task HandleSortChanged(string sortBy, bool descending)
{
    paginationParams.SortBy = sortBy;
    paginationParams.Descending = descending;
    await LoadTicketsAsync();
}
```

#### 5.2 Update Index.razor Markup

File: `src/PRFactory.Web/Pages/Tickets/Index.razor`

```razor
@page "/tickets"

<PageHeader Title="Tickets" Icon="ticket" Subtitle="Manage your tickets">
    <Actions>
        <a href="/tickets/create" class="btn btn-primary">
            <i class="bi bi-plus-lg me-2"></i>Create Ticket
        </a>
    </Actions>
</PageHeader>

<!-- Search and filters -->
<div class="mb-3">
    <input type="text" class="form-control" placeholder="Search tickets..."
           @bind="searchQuery" @bind:event="oninput"
           @bind:after="() => HandleSearchChanged(searchQuery)" />
</div>

<!-- Loading spinner -->
<LoadingSpinner Show="@isLoading" Centered="true" Message="Loading tickets..." />

<!-- Error message -->
<AlertMessage Type="AlertType.Error" Message="@errorMessage"
              OnDismiss="() => errorMessage = null" />

<!-- Empty state -->
@if (!isLoading && (pagedTickets == null || !pagedTickets.Items.Any()))
{
    <EmptyState Icon="inbox"
                Title="No tickets found"
                Message="Create your first ticket to get started" />
}
else if (!isLoading && pagedTickets != null)
{
    <Card>
        <Body>
            <!-- Ticket list -->
            @foreach (var ticket in pagedTickets.Items)
            {
                <div class="ticket-item">
                    <a href="/tickets/@ticket.Id">@ticket.Title</a>
                    <StatusBadge Status="@ticket.Status" />
                    <RelativeTime DateTime="@ticket.CreatedAt" />
                </div>
            }

            <!-- Pagination -->
            <Pagination CurrentPage="@pagedTickets.Page"
                       TotalPages="@pagedTickets.TotalPages"
                       OnPageChanged="HandlePageChanged" />

            <!-- Results info -->
            <div class="text-muted small mt-2">
                Showing @pagedTickets.Items.Count of @pagedTickets.TotalCount tickets
            </div>
        </Body>
    </Card>
}
```

---

### Step 6: Testing (2 hours)

#### 6.1 Unit Tests

Create `tests/PRFactory.Infrastructure.Tests/Repositories/TicketRepositoryTests.cs`:

```csharp
[Fact]
public async Task GetTicketsPagedAsync_WithPagination_ReturnsCorrectPage()
{
    // Arrange
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(databaseName: "TestDb")
        .Options;

    using var context = new ApplicationDbContext(options);

    // Seed 50 tickets
    for (int i = 1; i <= 50; i++)
    {
        context.Tickets.Add(new Ticket
        {
            Id = Guid.NewGuid(),
            Title = $"Ticket {i}",
            Status = TicketStatus.Open,
            CreatedAt = DateTime.UtcNow.AddDays(-i)
        });
    }
    await context.SaveChangesAsync();

    var repository = new TicketRepository(context);
    var paginationParams = new PaginationParams { Page = 2, PageSize = 10 };

    // Act
    var result = await repository.GetTicketsPagedAsync(paginationParams);

    // Assert
    Assert.Equal(50, result.TotalCount);
    Assert.Equal(10, result.Items.Count);
    Assert.Equal(2, result.Page);
    Assert.Equal(5, result.TotalPages);
}

[Fact]
public async Task GetTicketsPagedAsync_WithSearch_FiltersCorrectly()
{
    // Arrange
    // ... similar setup

    var paginationParams = new PaginationParams
    {
        Page = 1,
        PageSize = 20,
        SearchQuery = "important"
    };

    // Act
    var result = await repository.GetTicketsPagedAsync(paginationParams);

    // Assert
    Assert.All(result.Items, ticket =>
        Assert.Contains("important", ticket.Title, StringComparison.OrdinalIgnoreCase));
}
```

#### 6.2 Integration Tests

Test the full stack (repository → service → web service → page):

```bash
source /tmp/dotnet-proxy-setup.sh
dotnet test --filter "FullyQualifiedName~TicketPaginationTests"
```

#### 6.3 Performance Tests

Create test database with 10,000 tickets:

```bash
# Run performance test
dotnet run --project tests/PRFactory.PerformanceTests

# Measure:
# - Query time for page 1 (should be <100ms)
# - Query time for page 500 (should be <100ms)
# - Memory usage (should NOT load all 10k records)
```

**Expected results**:
- Page load time: <100ms (vs 2-3s before)
- Memory usage: ~2MB per page (vs 50MB loading all)
- Database queries: Efficient with proper WHERE/ORDER BY/LIMIT

---

### Step 7: Apply to Other Paginated Lists (Optional)

If time permits, apply pagination to other list pages:
- Repositories list (`Pages/Repositories/Index.razor`)
- Workflow events list
- Error list

Follow same pattern as Tickets/Index.

---

## Validation Checklist

- [ ] `PagedResult<T>` and `PaginationParams` created in Core/DTOs
- [ ] Repository method `GetTicketsPagedAsync` implemented
- [ ] Application service method added
- [ ] Web service method added
- [ ] Tickets/Index page refactored
- [ ] Unit tests pass (pagination, filtering, sorting)
- [ ] Integration tests pass
- [ ] Performance tests show improvement
- [ ] Page load time <500ms for 1000+ tickets
- [ ] Memory usage reduced
- [ ] All existing tests still pass

---

## Deliverables

1. **DTOs**
   - `Core/DTOs/PagedResult.cs`
   - `Core/DTOs/PaginationParams.cs`

2. **Repository Layer**
   - Updated `ITicketRepository` interface
   - Implemented `GetTicketsPagedAsync` in `TicketRepository`

3. **Service Layers**
   - Updated `TicketApplicationService`
   - Updated `TicketService` (Web)

4. **Pages**
   - Refactored `Pages/Tickets/Index.razor`
   - Refactored `Pages/Tickets/Index.razor.cs`

5. **Tests**
   - Unit tests for pagination
   - Performance benchmarks

6. **Commit**
   ```bash
   git commit -m "feat(epic-08): implement server-side pagination

   - Create PagedResult and PaginationParams DTOs
   - Add pagination support to TicketRepository
   - Update service layers to use pagination
   - Refactor Tickets/Index to use server-side pagination
   - Add unit and performance tests
   - Improve page load time from 2-3s to <500ms

   Closes Phase 3 of EPIC 08"
   ```

---

## Common Issues & Solutions

### Issue: Total count incorrect after filtering

**Solution**: Ensure `CountAsync()` is called AFTER applying filters but BEFORE applying `Skip/Take`

### Issue: Slow queries with large datasets

**Solution**: Add database indexes:
```sql
CREATE INDEX idx_tickets_created_at ON Tickets(CreatedAt);
CREATE INDEX idx_tickets_status ON Tickets(Status);
CREATE INDEX idx_tickets_title ON Tickets(Title); -- for search
```

### Issue: Search doesn't work for partial matches

**Solution**: Use `EF.Functions.Like()` for better search:
```csharp
query = query.Where(t => EF.Functions.Like(t.Title, $"%{searchQuery}%"));
```

---

## Phase 3 Complete!

**Next**: `PHASE_04_MISSING_UI_COMPONENTS.md`
