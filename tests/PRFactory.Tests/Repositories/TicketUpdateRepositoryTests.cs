using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Domain.Entities;
using PRFactory.Domain.ValueObjects;
using PRFactory.Infrastructure.Persistence;
using PRFactory.Infrastructure.Persistence.Repositories;
using PRFactory.Tests.Builders;

namespace PRFactory.Tests.Repositories;

/// <summary>
/// Comprehensive tests for TicketUpdateRepository operations
/// </summary>
public class TicketUpdateRepositoryTests : TestBase
{
    private readonly TicketUpdateRepository _repository;
    private readonly Mock<ILogger<TicketUpdateRepository>> _mockLogger;

    public TicketUpdateRepositoryTests()
    {
        _mockLogger = new Mock<ILogger<TicketUpdateRepository>>();
        _repository = new TicketUpdateRepository(DbContext, _mockLogger.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingUpdate_ReturnsUpdate()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository = new RepositoryBuilder().ForTenant(tenant.Id).Build();
        var ticket = new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .Build();

        var successCriteria = new List<SuccessCriterion>
        {
            SuccessCriterion.CreateMustHave(SuccessCriterionCategory.Functional, "Must work")
        };

        var ticketUpdate = TicketUpdate.Create(
            ticket.Id,
            "Updated Title",
            "Updated Description",
            successCriteria,
            "Acceptance criteria");

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.Add(repository);
        DbContext.Tickets.Add(ticket);
        DbContext.TicketUpdates.Add(ticketUpdate);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(ticketUpdate.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ticketUpdate.Id, result.Id);
        Assert.NotNull(result.Ticket);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentUpdate_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByTicketIdAsync_WithMultipleUpdates_ReturnsAllOrderedByGeneratedAtDescending()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository = new RepositoryBuilder().ForTenant(tenant.Id).Build();
        var ticket = new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .Build();

        var successCriteria = new List<SuccessCriterion>
        {
            SuccessCriterion.CreateMustHave(SuccessCriterionCategory.Functional, "Must work")
        };

        var update1 = TicketUpdate.Create(ticket.Id, "Title 1", "Desc 1", successCriteria, "AC 1", 1);
        await Task.Delay(10);
        var update2 = TicketUpdate.Create(ticket.Id, "Title 2", "Desc 2", successCriteria, "AC 2", 2);

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.Add(repository);
        DbContext.Tickets.Add(ticket);
        DbContext.TicketUpdates.AddRange(update1, update2);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTicketIdAsync(ticket.Id);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(update2.Id, result[0].Id); // Most recent first
        Assert.Equal(update1.Id, result[1].Id);
    }

    [Fact]
    public async Task GetLatestDraftByTicketIdAsync_WithMultipleDrafts_ReturnsHighestVersion()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository = new RepositoryBuilder().ForTenant(tenant.Id).Build();
        var ticket = new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .Build();

        var successCriteria = new List<SuccessCriterion>
        {
            SuccessCriterion.CreateMustHave(SuccessCriterionCategory.Functional, "Must work")
        };

        var update1 = TicketUpdate.Create(ticket.Id, "Title 1", "Desc 1", successCriteria, "AC 1", 1);
        var update2 = TicketUpdate.Create(ticket.Id, "Title 2", "Desc 2", successCriteria, "AC 2", 2);
        var update3 = TicketUpdate.Create(ticket.Id, "Title 3", "Desc 3", successCriteria, "AC 3", 3);

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.Add(repository);
        DbContext.Tickets.Add(ticket);
        DbContext.TicketUpdates.AddRange(update1, update2, update3);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetLatestDraftByTicketIdAsync(ticket.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Version);
        Assert.Equal("Title 3", result.UpdatedTitle);
    }

    [Fact]
    public async Task GetLatestDraftByTicketIdAsync_WithNoDrafts_ReturnsNull()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository = new RepositoryBuilder().ForTenant(tenant.Id).Build();
        var ticket = new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .Build();

        var successCriteria = new List<SuccessCriterion>
        {
            SuccessCriterion.CreateMustHave(SuccessCriterionCategory.Functional, "Must work")
        };

        var update = TicketUpdate.Create(ticket.Id, "Title", "Desc", successCriteria, "AC", 1);
        update.Approve(); // No longer a draft

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.Add(repository);
        DbContext.Tickets.Add(ticket);
        DbContext.TicketUpdates.Add(update);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetLatestDraftByTicketIdAsync(ticket.Id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestApprovedByTicketIdAsync_WithApprovedUpdate_ReturnsLatest()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository = new RepositoryBuilder().ForTenant(tenant.Id).Build();
        var ticket = new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .Build();

        var successCriteria = new List<SuccessCriterion>
        {
            SuccessCriterion.CreateMustHave(SuccessCriterionCategory.Functional, "Must work")
        };

        var update1 = TicketUpdate.Create(ticket.Id, "Title 1", "Desc 1", successCriteria, "AC 1", 1);
        update1.Approve();

        var update2 = TicketUpdate.Create(ticket.Id, "Title 2", "Desc 2", successCriteria, "AC 2", 2);
        update2.Approve();

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.Add(repository);
        DbContext.Tickets.Add(ticket);
        DbContext.TicketUpdates.AddRange(update1, update2);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetLatestApprovedByTicketIdAsync(ticket.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Version);
        Assert.True(result.IsApproved);
    }

    [Fact]
    public async Task GetVersionHistoryAsync_ReturnsAllVersionsOrdered()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository = new RepositoryBuilder().ForTenant(tenant.Id).Build();
        var ticket = new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .Build();

        var successCriteria = new List<SuccessCriterion>
        {
            SuccessCriterion.CreateMustHave(SuccessCriterionCategory.Functional, "Must work")
        };

        var update1 = TicketUpdate.Create(ticket.Id, "Title 1", "Desc 1", successCriteria, "AC 1", 1);
        var update2 = TicketUpdate.Create(ticket.Id, "Title 2", "Desc 2", successCriteria, "AC 2", 2);
        var update3 = TicketUpdate.Create(ticket.Id, "Title 3", "Desc 3", successCriteria, "AC 3", 3);

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.Add(repository);
        DbContext.Tickets.Add(ticket);
        DbContext.TicketUpdates.AddRange(update3, update1, update2); // Add out of order
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetVersionHistoryAsync(ticket.Id);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(1, result[0].Version);
        Assert.Equal(2, result[1].Version);
        Assert.Equal(3, result[2].Version);
    }

    [Fact]
    public async Task GetPendingPostsAsync_ReturnsApprovedButNotPostedUpdates()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository = new RepositoryBuilder().ForTenant(tenant.Id).Build();
        var ticket1 = new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .WithTicketKey("T1")
            .Build();
        var ticket2 = new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .WithTicketKey("T2")
            .Build();

        var successCriteria = new List<SuccessCriterion>
        {
            SuccessCriterion.CreateMustHave(SuccessCriterionCategory.Functional, "Must work")
        };

        var pendingUpdate = TicketUpdate.Create(ticket1.Id, "Title 1", "Desc 1", successCriteria, "AC 1");
        pendingUpdate.Approve();

        var postedUpdate = TicketUpdate.Create(ticket2.Id, "Title 2", "Desc 2", successCriteria, "AC 2");
        postedUpdate.Approve();
        postedUpdate.MarkAsPosted();

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.Add(repository);
        DbContext.Tickets.AddRange(ticket1, ticket2);
        DbContext.TicketUpdates.AddRange(pendingUpdate, postedUpdate);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetPendingPostsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal(pendingUpdate.Id, result[0].Id);
        Assert.True(result[0].IsApproved);
        Assert.Null(result[0].PostedAt);
    }

    [Fact]
    public async Task GetDraftsAsync_ReturnsOnlyDraftUpdates()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository = new RepositoryBuilder().ForTenant(tenant.Id).Build();
        var ticket = new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .Build();

        var successCriteria = new List<SuccessCriterion>
        {
            SuccessCriterion.CreateMustHave(SuccessCriterionCategory.Functional, "Must work")
        };

        var draftUpdate = TicketUpdate.Create(ticket.Id, "Draft", "Draft Desc", successCriteria, "AC");
        var approvedUpdate = TicketUpdate.Create(ticket.Id, "Approved", "Approved Desc", successCriteria, "AC");
        approvedUpdate.Approve();

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.Add(repository);
        DbContext.Tickets.Add(ticket);
        DbContext.TicketUpdates.AddRange(draftUpdate, approvedUpdate);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetDraftsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal(draftUpdate.Id, result[0].Id);
        Assert.True(result[0].IsDraft);
    }

    [Fact]
    public async Task GetRejectedAsync_ReturnsOnlyRejectedUpdates()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository = new RepositoryBuilder().ForTenant(tenant.Id).Build();
        var ticket = new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .Build();

        var successCriteria = new List<SuccessCriterion>
        {
            SuccessCriterion.CreateMustHave(SuccessCriterionCategory.Functional, "Must work")
        };

        var rejectedUpdate = TicketUpdate.Create(ticket.Id, "Rejected", "Rejected Desc", successCriteria, "AC");
        rejectedUpdate.Reject("Not good enough");

        var draftUpdate = TicketUpdate.Create(ticket.Id, "Draft", "Draft Desc", successCriteria, "AC");

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.Add(repository);
        DbContext.Tickets.Add(ticket);
        DbContext.TicketUpdates.AddRange(rejectedUpdate, draftUpdate);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetRejectedAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal(rejectedUpdate.Id, result[0].Id);
        Assert.NotNull(result[0].RejectionReason);
    }

    [Fact]
    public async Task GetByDateRangeAsync_WithinRange_ReturnsMatchingUpdates()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository = new RepositoryBuilder().ForTenant(tenant.Id).Build();
        var ticket = new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .Build();

        var successCriteria = new List<SuccessCriterion>
        {
            SuccessCriterion.CreateMustHave(SuccessCriterionCategory.Functional, "Must work")
        };

        var update = TicketUpdate.Create(ticket.Id, "Title", "Desc", successCriteria, "AC");

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.Add(repository);
        DbContext.Tickets.Add(ticket);
        DbContext.TicketUpdates.Add(update);
        await DbContext.SaveChangesAsync();

        // Act
        var startDate = DateTime.UtcNow.AddDays(-1);
        var endDate = DateTime.UtcNow.AddDays(1);
        var result = await _repository.GetByDateRangeAsync(startDate, endDate);

        // Assert
        Assert.Single(result);
        Assert.Equal(update.Id, result[0].Id);
    }

    [Fact]
    public async Task CreateAsync_CreatesNewUpdate()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository = new RepositoryBuilder().ForTenant(tenant.Id).Build();
        var ticket = new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .Build();

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.Add(repository);
        DbContext.Tickets.Add(ticket);
        await DbContext.SaveChangesAsync();

        var successCriteria = new List<SuccessCriterion>
        {
            SuccessCriterion.CreateMustHave(SuccessCriterionCategory.Functional, "Must work")
        };

        var ticketUpdate = TicketUpdate.Create(ticket.Id, "New Title", "New Desc", successCriteria, "AC");

        // Act
        var result = await _repository.CreateAsync(ticketUpdate);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);

        var savedUpdate = await DbContext.TicketUpdates.FindAsync(result.Id);
        Assert.NotNull(savedUpdate);
        Assert.Equal("New Title", savedUpdate.UpdatedTitle);
    }

    [Fact]
    public async Task UpdateAsync_ModifiesExistingUpdate()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository = new RepositoryBuilder().ForTenant(tenant.Id).Build();
        var ticket = new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .Build();

        var successCriteria = new List<SuccessCriterion>
        {
            SuccessCriterion.CreateMustHave(SuccessCriterionCategory.Functional, "Must work")
        };

        var ticketUpdate = TicketUpdate.Create(ticket.Id, "Original", "Original Desc", successCriteria, "AC");

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.Add(repository);
        DbContext.Tickets.Add(ticket);
        DbContext.TicketUpdates.Add(ticketUpdate);
        await DbContext.SaveChangesAsync();

        // Act
        ticketUpdate.Approve();
        await _repository.UpdateAsync(ticketUpdate);

        // Assert
        var updatedUpdate = await DbContext.TicketUpdates.FindAsync(ticketUpdate.Id);
        Assert.NotNull(updatedUpdate);
        Assert.True(updatedUpdate.IsApproved);
    }

    [Fact]
    public async Task DeleteAsync_RemovesUpdate()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository = new RepositoryBuilder().ForTenant(tenant.Id).Build();
        var ticket = new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .Build();

        var successCriteria = new List<SuccessCriterion>
        {
            SuccessCriterion.CreateMustHave(SuccessCriterionCategory.Functional, "Must work")
        };

        var ticketUpdate = TicketUpdate.Create(ticket.Id, "To Delete", "Desc", successCriteria, "AC");

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.Add(repository);
        DbContext.Tickets.Add(ticket);
        DbContext.TicketUpdates.Add(ticketUpdate);
        await DbContext.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(ticketUpdate.Id);

        // Assert
        var deletedUpdate = await DbContext.TicketUpdates.FindAsync(ticketUpdate.Id);
        Assert.Null(deletedUpdate);
    }

    [Fact]
    public async Task DeleteByTicketIdAsync_RemovesAllUpdatesForTicket()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository = new RepositoryBuilder().ForTenant(tenant.Id).Build();
        var ticket = new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .Build();

        var successCriteria = new List<SuccessCriterion>
        {
            SuccessCriterion.CreateMustHave(SuccessCriterionCategory.Functional, "Must work")
        };

        var update1 = TicketUpdate.Create(ticket.Id, "Title 1", "Desc 1", successCriteria, "AC 1");
        var update2 = TicketUpdate.Create(ticket.Id, "Title 2", "Desc 2", successCriteria, "AC 2");

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.Add(repository);
        DbContext.Tickets.Add(ticket);
        DbContext.TicketUpdates.AddRange(update1, update2);
        await DbContext.SaveChangesAsync();

        // Act
        await _repository.DeleteByTicketIdAsync(ticket.Id);

        // Assert
        var remainingUpdates = await DbContext.TicketUpdates
            .Where(tu => tu.TicketId == ticket.Id)
            .ToListAsync();
        Assert.Empty(remainingUpdates);
    }

    [Fact]
    public async Task HasApprovedUpdateAsync_WithApprovedUpdate_ReturnsTrue()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository = new RepositoryBuilder().ForTenant(tenant.Id).Build();
        var ticket = new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .Build();

        var successCriteria = new List<SuccessCriterion>
        {
            SuccessCriterion.CreateMustHave(SuccessCriterionCategory.Functional, "Must work")
        };

        var ticketUpdate = TicketUpdate.Create(ticket.Id, "Title", "Desc", successCriteria, "AC");
        ticketUpdate.Approve();

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.Add(repository);
        DbContext.Tickets.Add(ticket);
        DbContext.TicketUpdates.Add(ticketUpdate);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.HasApprovedUpdateAsync(ticket.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GetStatusCountsAsync_ReturnsCorrectCounts()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository = new RepositoryBuilder().ForTenant(tenant.Id).Build();
        var ticket = new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .Build();

        var successCriteria = new List<SuccessCriterion>
        {
            SuccessCriterion.CreateMustHave(SuccessCriterionCategory.Functional, "Must work")
        };

        var draft = TicketUpdate.Create(ticket.Id, "Draft", "Desc", successCriteria, "AC");
        var approved = TicketUpdate.Create(ticket.Id, "Approved", "Desc", successCriteria, "AC");
        approved.Approve();
        var posted = TicketUpdate.Create(ticket.Id, "Posted", "Desc", successCriteria, "AC");
        posted.Approve();
        posted.MarkAsPosted();

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.Add(repository);
        DbContext.Tickets.Add(ticket);
        DbContext.TicketUpdates.AddRange(draft, approved, posted);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetStatusCountsAsync(ticket.Id);

        // Assert
        Assert.Equal(3, result["Total"]);
        Assert.Equal(1, result["Draft"]);
        Assert.Equal(2, result["Approved"]);
        Assert.Equal(1, result["Posted"]);
        Assert.Equal(1, result["PendingPost"]);
    }

    [Fact]
    public async Task GetLatestVersionNumberAsync_ReturnsHighestVersion()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository = new RepositoryBuilder().ForTenant(tenant.Id).Build();
        var ticket = new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .Build();

        var successCriteria = new List<SuccessCriterion>
        {
            SuccessCriterion.CreateMustHave(SuccessCriterionCategory.Functional, "Must work")
        };

        var update1 = TicketUpdate.Create(ticket.Id, "V1", "Desc", successCriteria, "AC", 1);
        var update2 = TicketUpdate.Create(ticket.Id, "V2", "Desc", successCriteria, "AC", 2);
        var update3 = TicketUpdate.Create(ticket.Id, "V3", "Desc", successCriteria, "AC", 3);

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.Add(repository);
        DbContext.Tickets.Add(ticket);
        DbContext.TicketUpdates.AddRange(update1, update2, update3);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetLatestVersionNumberAsync(ticket.Id);

        // Assert
        Assert.Equal(3, result);
    }

    [Fact]
    public async Task GetLatestVersionNumberAsync_WithNoUpdates_ReturnsZero()
    {
        // Arrange
        var tenant = new TenantBuilder().Build();
        var repository = new RepositoryBuilder().ForTenant(tenant.Id).Build();
        var ticket = new TicketBuilder()
            .WithTenantId(tenant.Id)
            .WithRepositoryId(repository.Id)
            .Build();

        DbContext.Tenants.Add(tenant);
        DbContext.Repositories.Add(repository);
        DbContext.Tickets.Add(ticket);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetLatestVersionNumberAsync(ticket.Id);

        // Assert
        Assert.Equal(0, result);
    }
}
