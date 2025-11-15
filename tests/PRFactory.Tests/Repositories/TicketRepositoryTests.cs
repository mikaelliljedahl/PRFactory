using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Domain.DTOs;
using PRFactory.Domain.Entities;
using PRFactory.Domain.ValueObjects;
using PRFactory.Infrastructure.Persistence;
using PRFactory.Infrastructure.Persistence.Repositories;
using PRFactory.Tests.Builders;
using Xunit;

namespace PRFactory.Tests.Repositories;

/// <summary>
/// Comprehensive tests for TicketRepository operations
/// </summary>
public class TicketRepositoryTests : TestBase
{
    private readonly TicketRepository _repository;
    private readonly Mock<ILogger<TicketRepository>> _mockLogger;

    public TicketRepositoryTests()
    {
        _mockLogger = new Mock<ILogger<TicketRepository>>();
        _repository = new TicketRepository(DbContext, _mockLogger.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingTicket_ReturnsTicket()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository = new RepositoryBuilder().ForTenant(tenant.Id).Build();
        var ticket = new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .WithTicketKey("TEST-001")
            .WithTitle("Test Ticket")
            .Build();

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.Add(repository);
        DbContext.Tickets.Add(ticket);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(ticket.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ticket.Id, result.Id);
        Assert.Equal("TEST-001", result.TicketKey);
        Assert.NotNull(result.Repository);
        Assert.NotNull(result.Tenant);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentTicket_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByTicketKeyAsync_WithExistingKey_ReturnsTicket()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository = new RepositoryBuilder().ForTenant(tenant.Id).Build();
        var ticket = new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .WithTicketKey("PROJ-123")
            .Build();

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.Add(repository);
        DbContext.Tickets.Add(ticket);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTicketKeyAsync("PROJ-123");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("PROJ-123", result.TicketKey);
        Assert.NotNull(result.Repository);
        Assert.NotNull(result.Tenant);
    }

    [Fact]
    public async Task GetByTicketKeyAsync_WithNonExistentKey_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByTicketKeyAsync("NONEXISTENT-999");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByTenantIdAsync_WithMultipleTickets_ReturnsAllTicketsForTenant()
    {
        // Arrange
        var tenant1 = new TenantBuilder().WithName("Tenant 1").Build();
        var tenant2 = new TenantBuilder().WithName("Tenant 2").Build();
        var repository = new RepositoryBuilder().ForTenant(tenant1.Id).Build();

        var ticket1 = new TicketBuilder()
            .WithTenantId(tenant1.Id)
            .WithRepositoryId(repository.Id)
            .WithTicketKey("T1-001")
            .Build();
        var ticket2 = new TicketBuilder()
            .WithTenantId(tenant1.Id)
            .WithRepositoryId(repository.Id)
            .WithTicketKey("T1-002")
            .Build();
        var ticket3 = new TicketBuilder()
            .WithTenantId(tenant2.Id)
            .WithRepositoryId(repository.Id)
            .WithTicketKey("T2-001")
            .Build();

        DbContext.Tenants.AddRange(tenant1, tenant2);
        DbContext.Repositories.Add(repository);
        DbContext.Tickets.AddRange(ticket1, ticket2, ticket3);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTenantIdAsync(tenant1.Id);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, t => Assert.Equal(tenant1.Id, t.TenantId));
        Assert.Contains(result, t => t.TicketKey == "T1-001");
        Assert.Contains(result, t => t.TicketKey == "T1-002");
    }

    [Fact]
    public async Task GetByTenantIdAsync_OrdersByCreatedAtDescending()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository = new RepositoryBuilder().ForTenant(tenant.Id).Build();

        var ticket1 = new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .WithTicketKey("FIRST")
            .Build();

        await Task.Delay(10); // Ensure different timestamps

        var ticket2 = new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .WithTicketKey("SECOND")
            .Build();

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.Add(repository);
        DbContext.Tickets.AddRange(ticket1, ticket2);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTenantIdAsync(tenant.Id);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("SECOND", result[0].TicketKey); // Most recent first
        Assert.Equal("FIRST", result[1].TicketKey);
    }

    [Fact]
    public async Task GetByRepositoryIdAsync_WithMultipleTickets_ReturnsTicketsForRepository()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository1 = new RepositoryBuilder().ForTenant(tenant.Id).WithName("repo1").Build();
        var repository2 = new RepositoryBuilder().ForTenant(tenant.Id).WithName("repo2").Build();

        var ticket1 = new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository1.Id)
            .WithTicketKey("R1-001")
            .Build();
        var ticket2 = new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository1.Id)
            .WithTicketKey("R1-002")
            .Build();
        var ticket3 = new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository2.Id)
            .WithTicketKey("R2-001")
            .Build();

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.AddRange(repository1, repository2);
        DbContext.Tickets.AddRange(ticket1, ticket2, ticket3);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByRepositoryIdAsync(repository1.Id);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, t => Assert.Equal(repository1.Id, t.RepositoryId));
        Assert.Contains(result, t => t.TicketKey == "R1-001");
        Assert.Contains(result, t => t.TicketKey == "R1-002");
    }

    [Fact]
    public async Task GetByStateAsync_WithVariousStates_ReturnsMatchingTickets()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository = new RepositoryBuilder().ForTenant(tenant.Id).Build();

        var ticket1 = new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .WithTicketKey("TRIGGERED-001")
            .WithState(WorkflowState.Triggered)
            .Build();
        var ticket2 = new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .WithTicketKey("PLANNING-001")
            .WithState(WorkflowState.Planning)
            .Build();

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.Add(repository);
        DbContext.Tickets.AddRange(ticket1, ticket2);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStateAsync(WorkflowState.Triggered);

        // Assert
        Assert.Single(result);
        Assert.Equal("TRIGGERED-001", result[0].TicketKey);
        Assert.Equal(WorkflowState.Triggered, result[0].State);
    }

    [Fact]
    public async Task GetByStatesAsync_WithMultipleStates_ReturnsMatchingTickets()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository = new RepositoryBuilder().ForTenant(tenant.Id).Build();

        var ticket1 = new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .WithTicketKey("TRIGGERED")
            .WithState(WorkflowState.Triggered)
            .Build();
        var ticket2 = new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .WithTicketKey("PLANNING")
            .WithState(WorkflowState.Planning)
            .Build();
        var ticket3 = new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .WithTicketKey("COMPLETED")
            .WithState(WorkflowState.Completed)
            .Build();

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.Add(repository);
        DbContext.Tickets.AddRange(ticket1, ticket2, ticket3);
        await DbContext.SaveChangesAsync();

        // Act
        var states = new[] { WorkflowState.Triggered, WorkflowState.Planning };
        var result = await _repository.GetByStatesAsync(states);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, t => t.TicketKey == "TRIGGERED");
        Assert.Contains(result, t => t.TicketKey == "PLANNING");
        Assert.DoesNotContain(result, t => t.TicketKey == "COMPLETED");
    }

    [Fact]
    public async Task GetStaleAwaitingAnswersAsync_WithStaleTickets_ReturnsOnlyStaleOnes()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository = new RepositoryBuilder().ForTenant(tenant.Id).Build();

        // Create a stale ticket (updated more than threshold ago)
        var staleTicket = Ticket.Create("STALE-001", tenant.Id, repository.Id);
        staleTicket.TransitionTo(WorkflowState.Analyzing);
        staleTicket.TransitionTo(WorkflowState.TicketUpdateGenerated);
        staleTicket.TransitionTo(WorkflowState.TicketUpdateUnderReview);
        staleTicket.TransitionTo(WorkflowState.TicketUpdateApproved);
        staleTicket.TransitionTo(WorkflowState.TicketUpdatePosted);
        staleTicket.TransitionTo(WorkflowState.QuestionsPosted);
        staleTicket.TransitionTo(WorkflowState.AwaitingAnswers);
        // Manually set UpdatedAt to simulate old ticket
        typeof(Ticket).GetProperty("UpdatedAt")!.SetValue(staleTicket, DateTime.UtcNow.AddHours(-25));

        // Create a recent ticket
        var recentTicket = Ticket.Create("RECENT-001", tenant.Id, repository.Id);
        recentTicket.TransitionTo(WorkflowState.Analyzing);
        recentTicket.TransitionTo(WorkflowState.TicketUpdateGenerated);
        recentTicket.TransitionTo(WorkflowState.TicketUpdateUnderReview);
        recentTicket.TransitionTo(WorkflowState.TicketUpdateApproved);
        recentTicket.TransitionTo(WorkflowState.TicketUpdatePosted);
        recentTicket.TransitionTo(WorkflowState.QuestionsPosted);
        recentTicket.TransitionTo(WorkflowState.AwaitingAnswers);

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.Add(repository);
        DbContext.Tickets.AddRange(staleTicket, recentTicket);
        await DbContext.SaveChangesAsync();

        // Act
        var threshold = TimeSpan.FromHours(24);
        var result = await _repository.GetStaleAwaitingAnswersAsync(threshold);

        // Assert
        Assert.Single(result);
        Assert.Equal("STALE-001", result[0].TicketKey);
    }

    [Fact]
    public async Task GetRetryableFailedTicketsAsync_WithFailedTickets_ReturnsOnlyRetryable()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository = new RepositoryBuilder().ForTenant(tenant.Id).Build();

        // Create ticket with retry count below max
        var retryableTicket = Ticket.Create("RETRYABLE-001", tenant.Id, repository.Id);
        retryableTicket.TransitionTo(WorkflowState.Analyzing);
        retryableTicket.TransitionTo(WorkflowState.Failed);
        retryableTicket.RecordError("First error");

        // Create ticket that has exceeded max retries
        var maxedOutTicket = Ticket.Create("MAXED-001", tenant.Id, repository.Id);
        maxedOutTicket.TransitionTo(WorkflowState.Analyzing);
        maxedOutTicket.TransitionTo(WorkflowState.Failed);
        for (int i = 0; i < 5; i++)
        {
            maxedOutTicket.RecordError($"Error {i}");
        }

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.Add(repository);
        DbContext.Tickets.AddRange(retryableTicket, maxedOutTicket);
        await DbContext.SaveChangesAsync();

        // Act
        var maxRetries = 3;
        var result = await _repository.GetRetryableFailedTicketsAsync(maxRetries);

        // Assert
        Assert.Single(result);
        Assert.Equal("RETRYABLE-001", result[0].TicketKey);
    }

    [Fact]
    public async Task GetActiveTicketsAsync_ExcludesTerminalStates()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository = new RepositoryBuilder().ForTenant(tenant.Id).Build();

        var activeTicket = new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .WithTicketKey("ACTIVE-001")
            .WithState(WorkflowState.Planning)
            .Build();

        var completedTicket = new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .WithTicketKey("COMPLETED-001")
            .WithState(WorkflowState.Completed)
            .Build();

        var failedTicket = Ticket.Create("FAILED-001", tenant.Id, repository.Id);
        failedTicket.TransitionTo(WorkflowState.Analyzing);
        failedTicket.TransitionTo(WorkflowState.Failed);

        var cancelledTicket = Ticket.Create("CANCELLED-001", tenant.Id, repository.Id);
        cancelledTicket.TransitionTo(WorkflowState.Analyzing);
        cancelledTicket.TransitionTo(WorkflowState.Cancelled);

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.Add(repository);
        DbContext.Tickets.AddRange(activeTicket, completedTicket, failedTicket, cancelledTicket);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveTicketsAsync(tenant.Id);

        // Assert
        Assert.Single(result);
        Assert.Equal("ACTIVE-001", result[0].TicketKey);
    }

    [Fact]
    public async Task GetByIdWithEventsAsync_IncludesEvents()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository = new RepositoryBuilder().ForTenant(tenant.Id).Build();
        var ticket = new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .WithTicketKey("EVENT-001")
            .Build();

        ticket.TransitionTo(WorkflowState.Analyzing);

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.Add(repository);
        DbContext.Tickets.Add(ticket);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdWithEventsAsync(ticket.Id);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Events);
    }

    [Fact]
    public async Task GetByDateRangeAsync_WithinRange_ReturnsMatchingTickets()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository = new RepositoryBuilder().ForTenant(tenant.Id).Build();

        var ticket1 = new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .WithTicketKey("IN-RANGE-001")
            .Build();

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.Add(repository);
        DbContext.Tickets.Add(ticket1);
        await DbContext.SaveChangesAsync();

        // Act
        var startDate = DateTime.UtcNow.AddDays(-1);
        var endDate = DateTime.UtcNow.AddDays(1);
        var result = await _repository.GetByDateRangeAsync(startDate, endDate);

        // Assert
        Assert.Single(result);
        Assert.Equal("IN-RANGE-001", result[0].TicketKey);
    }

    [Fact]
    public async Task AddAsync_CreatesNewTicket()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository = new RepositoryBuilder().ForTenant(tenant.Id).Build();

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.Add(repository);
        await DbContext.SaveChangesAsync();

        var ticket = Ticket.Create("NEW-001", tenant.Id, repository.Id);

        // Act
        var result = await _repository.AddAsync(ticket);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);

        var savedTicket = await DbContext.Tickets.FindAsync(result.Id);
        Assert.NotNull(savedTicket);
        Assert.Equal("NEW-001", savedTicket.TicketKey);
    }

    [Fact]
    public async Task UpdateAsync_ModifiesExistingTicket()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository = new RepositoryBuilder().ForTenant(tenant.Id).Build();
        var ticket = new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .WithTicketKey("UPDATE-001")
            .WithTitle("Original Title")
            .Build();

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.Add(repository);
        DbContext.Tickets.Add(ticket);
        await DbContext.SaveChangesAsync();

        // Act
        ticket.UpdateTicketInfo("Updated Title", "Updated Description");
        await _repository.UpdateAsync(ticket);

        // Assert
        var updatedTicket = await DbContext.Tickets.FindAsync(ticket.Id);
        Assert.NotNull(updatedTicket);
        Assert.Equal("Updated Title", updatedTicket.Title);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTicket()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository = new RepositoryBuilder().ForTenant(tenant.Id).Build();
        var ticket = new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .WithTicketKey("DELETE-001")
            .Build();

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.Add(repository);
        DbContext.Tickets.Add(ticket);
        await DbContext.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(ticket.Id);

        // Assert
        var deletedTicket = await DbContext.Tickets.FindAsync(ticket.Id);
        Assert.Null(deletedTicket);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_DoesNotThrow()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        await _repository.DeleteAsync(nonExistentId);

        // Assert - No exception thrown, operation completes successfully
        var count = await DbContext.Tickets.CountAsync();
        Assert.True(count >= 0);
    }

    [Fact]
    public async Task GetStateCountsAsync_ReturnsCorrectCounts()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository = new RepositoryBuilder().ForTenant(tenant.Id).Build();

        var ticket1 = new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .WithTicketKey("T1")
            .WithState(WorkflowState.Triggered)
            .Build();
        var ticket2 = new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .WithTicketKey("T2")
            .WithState(WorkflowState.Triggered)
            .Build();
        var ticket3 = new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .WithTicketKey("T3")
            .WithState(WorkflowState.Planning)
            .Build();

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.Add(repository);
        DbContext.Tickets.AddRange(ticket1, ticket2, ticket3);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetStateCountsAsync(tenant.Id);

        // Assert
        Assert.Equal(2, result[WorkflowState.Triggered]);
        Assert.Equal(1, result[WorkflowState.Planning]);
    }

    [Fact]
    public async Task ExistsAsync_WithExistingTicketKey_ReturnsTrue()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository = new RepositoryBuilder().ForTenant(tenant.Id).Build();
        var ticket = new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .WithTicketKey("EXISTS-001")
            .Build();

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.Add(repository);
        DbContext.Tickets.Add(ticket);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.ExistsAsync("EXISTS-001");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentTicketKey_ReturnsFalse()
    {
        // Act
        var result = await _repository.ExistsAsync("NONEXISTENT-999");

        // Assert
        Assert.False(result);
    }

    #region Pagination Tests

    [Fact]
    public async Task GetTicketsPagedAsync_WithDefaultParams_ReturnsFirstPage()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository = new RepositoryBuilder().ForTenant(tenant.Id).Build();

        // Create 25 tickets
        var tickets = new List<Ticket>();
        for (int i = 1; i <= 25; i++)
        {
            tickets.Add(new TicketBuilder()
                .WithTenantId(tenant.Id)
                .WithRepositoryId(repository.Id)
                .WithTicketKey($"TICKET-{i:D3}")
                .WithTitle($"Ticket {i}")
                .Build());
        }

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.Add(repository);
        DbContext.Tickets.AddRange(tickets);
        await DbContext.SaveChangesAsync();

        var paginationParams = new PaginationParams
        {
            Page = 1,
            PageSize = 10,
            SortBy = "created",
            Descending = true
        };

        // Act
        var result = await _repository.GetTicketsPagedAsync(paginationParams);

        // Assert
        Assert.Equal(10, result.Items.Count);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(3, result.TotalPages);
        Assert.False(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
    }

    [Fact]
    public async Task GetTicketsPagedAsync_WithPageTwo_ReturnsSecondPage()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository = new RepositoryBuilder().ForTenant(tenant.Id).Build();

        // Create 25 tickets
        for (int i = 1; i <= 25; i++)
        {
            DbContext.Tickets.Add(new TicketBuilder()
                .WithTenantId(tenant.Id)
                .WithRepositoryId(repository.Id)
                .WithTicketKey($"TICKET-{i:D3}")
                .Build());
        }

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.Add(repository);
        await DbContext.SaveChangesAsync();

        var paginationParams = new PaginationParams
        {
            Page = 2,
            PageSize = 10
        };

        // Act
        var result = await _repository.GetTicketsPagedAsync(paginationParams);

        // Assert
        Assert.Equal(10, result.Items.Count);
        Assert.Equal(2, result.Page);
        Assert.True(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
    }

    [Fact]
    public async Task GetTicketsPagedAsync_WithLastPage_ReturnsRemainingItems()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository = new RepositoryBuilder().ForTenant(tenant.Id).Build();

        // Create 25 tickets
        for (int i = 1; i <= 25; i++)
        {
            DbContext.Tickets.Add(new TicketBuilder()
                .WithTenantId(tenant.Id)
                .WithRepositoryId(repository.Id)
                .WithTicketKey($"TICKET-{i:D3}")
                .Build());
        }

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.Add(repository);
        await DbContext.SaveChangesAsync();

        var paginationParams = new PaginationParams
        {
            Page = 3,
            PageSize = 10
        };

        // Act
        var result = await _repository.GetTicketsPagedAsync(paginationParams);

        // Assert
        Assert.Equal(5, result.Items.Count); // Only 5 items on last page
        Assert.Equal(3, result.Page);
        Assert.True(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public async Task GetTicketsPagedAsync_WithSearchQuery_FiltersResults()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository = new RepositoryBuilder().ForTenant(tenant.Id).Build();

        DbContext.Tickets.Add(new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .WithTicketKey("SEARCH-001")
            .WithTitle("Important feature")
            .Build());
        DbContext.Tickets.Add(new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .WithTicketKey("SEARCH-002")
            .WithTitle("Another task")
            .Build());
        DbContext.Tickets.Add(new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .WithTicketKey("SEARCH-003")
            .WithTitle("Important bug fix")
            .Build());

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.Add(repository);
        await DbContext.SaveChangesAsync();

        var paginationParams = new PaginationParams
        {
            Page = 1,
            PageSize = 10,
            SearchQuery = "important"
        };

        // Act
        var result = await _repository.GetTicketsPagedAsync(paginationParams);

        // Assert
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, t => Assert.Contains("important", t.Title, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetTicketsPagedAsync_WithStateFilter_FiltersResults()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository = new RepositoryBuilder().ForTenant(tenant.Id).Build();

        DbContext.Tickets.Add(new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .WithTicketKey("STATE-001")
            .WithState(WorkflowState.Triggered)
            .Build());
        DbContext.Tickets.Add(new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .WithTicketKey("STATE-002")
            .WithState(WorkflowState.Planning)
            .Build());
        DbContext.Tickets.Add(new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .WithTicketKey("STATE-003")
            .WithState(WorkflowState.Triggered)
            .Build());

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.Add(repository);
        await DbContext.SaveChangesAsync();

        var paginationParams = new PaginationParams
        {
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _repository.GetTicketsPagedAsync(paginationParams, WorkflowState.Triggered);

        // Assert
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, t => Assert.Equal(WorkflowState.Triggered, t.State));
    }

    [Fact]
    public async Task GetTicketsPagedAsync_SortByTitle_ReturnsSortedResults()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository = new RepositoryBuilder().ForTenant(tenant.Id).Build();

        DbContext.Tickets.Add(new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .WithTicketKey("SORT-001")
            .WithTitle("Zebra")
            .Build());
        DbContext.Tickets.Add(new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .WithTicketKey("SORT-002")
            .WithTitle("Apple")
            .Build());
        DbContext.Tickets.Add(new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .WithTicketKey("SORT-003")
            .WithTitle("Mango")
            .Build());

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.Add(repository);
        await DbContext.SaveChangesAsync();

        var paginationParams = new PaginationParams
        {
            Page = 1,
            PageSize = 10,
            SortBy = "title",
            Descending = false
        };

        // Act
        var result = await _repository.GetTicketsPagedAsync(paginationParams);

        // Assert
        Assert.Equal(3, result.Items.Count);
        Assert.Equal("Apple", result.Items[0].Title);
        Assert.Equal("Mango", result.Items[1].Title);
        Assert.Equal("Zebra", result.Items[2].Title);
    }

    [Fact]
    public async Task GetTicketsPagedAsync_SortByTitleDescending_ReturnsSortedResults()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository = new RepositoryBuilder().ForTenant(tenant.Id).Build();

        DbContext.Tickets.Add(new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .WithTicketKey("SORT-001")
            .WithTitle("Zebra")
            .Build());
        DbContext.Tickets.Add(new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .WithTicketKey("SORT-002")
            .WithTitle("Apple")
            .Build());
        DbContext.Tickets.Add(new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .WithTicketKey("SORT-003")
            .WithTitle("Mango")
            .Build());

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.Add(repository);
        await DbContext.SaveChangesAsync();

        var paginationParams = new PaginationParams
        {
            Page = 1,
            PageSize = 10,
            SortBy = "title",
            Descending = true
        };

        // Act
        var result = await _repository.GetTicketsPagedAsync(paginationParams);

        // Assert
        Assert.Equal(3, result.Items.Count);
        Assert.Equal("Zebra", result.Items[0].Title);
        Assert.Equal("Mango", result.Items[1].Title);
        Assert.Equal("Apple", result.Items[2].Title);
    }

    [Fact]
    public async Task GetTicketsPagedAsync_WithEmptyResults_ReturnsEmptyPage()
    {
        // Arrange
        var paginationParams = new PaginationParams
        {
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _repository.GetTicketsPagedAsync(paginationParams);

        // Assert
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
        Assert.False(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public async Task GetTicketsPagedAsync_WithSearchAndFilter_CombinesFilters()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository = new RepositoryBuilder().ForTenant(tenant.Id).Build();

        DbContext.Tickets.Add(new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .WithTicketKey("COMBO-001")
            .WithTitle("Important feature")
            .WithState(WorkflowState.Planning)
            .Build());
        DbContext.Tickets.Add(new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .WithTicketKey("COMBO-002")
            .WithTitle("Important bug")
            .WithState(WorkflowState.Triggered)
            .Build());
        DbContext.Tickets.Add(new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .WithTicketKey("COMBO-003")
            .WithTitle("Other task")
            .WithState(WorkflowState.Planning)
            .Build());

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.Add(repository);
        await DbContext.SaveChangesAsync();

        var paginationParams = new PaginationParams
        {
            Page = 1,
            PageSize = 10,
            SearchQuery = "important"
        };

        // Act
        var result = await _repository.GetTicketsPagedAsync(paginationParams, WorkflowState.Planning);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal("COMBO-001", result.Items[0].TicketKey);
    }

    #endregion
}
