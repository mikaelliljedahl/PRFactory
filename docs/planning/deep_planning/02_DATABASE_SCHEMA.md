# Database Schema Changes Implementation Plan

**Epic:** Deep Planning Phase (Epic 3)
**Component:** Database Schema for Multi-Artifact Storage
**Estimated Effort:** 2-3 days
**Dependencies:** None (can be developed in parallel with agents)

---

## Overview

This implementation plan covers the database schema changes required to store multiple planning artifacts (user stories, API design, database schema, test cases, implementation steps) with versioning support.

---

## Schema Design

### Current State

**Plans Table (existing):**
```sql
CREATE TABLE Plans (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    TicketId UNIQUEIDENTIFIER NOT NULL,
    Content NVARCHAR(MAX),  -- Single-file plan (legacy)
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2,
    CONSTRAINT FK_Plans_Tickets FOREIGN KEY (TicketId) REFERENCES Tickets(Id)
);
```

### Target State

**Enhanced Plans Table:**
```sql
CREATE TABLE Plans (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    TicketId UNIQUEIDENTIFIER NOT NULL,

    -- Legacy single-file plan (backward compatibility)
    Content NVARCHAR(MAX),

    -- Multi-artifact fields
    UserStories NVARCHAR(MAX),
    ApiDesign NVARCHAR(MAX),
    DatabaseSchema NVARCHAR(MAX),
    TestCases NVARCHAR(MAX),
    ImplementationSteps NVARCHAR(MAX),

    Version INT NOT NULL DEFAULT 1,

    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2,

    CONSTRAINT FK_Plans_Tickets FOREIGN KEY (TicketId) REFERENCES Tickets(Id)
);
```

**New PlanVersions Table (optional, for versioning):**
```sql
CREATE TABLE PlanVersions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PlanId UNIQUEIDENTIFIER NOT NULL,
    Version INT NOT NULL,

    -- Snapshot of artifacts at this version
    UserStories NVARCHAR(MAX),
    ApiDesign NVARCHAR(MAX),
    DatabaseSchema NVARCHAR(MAX),
    TestCases NVARCHAR(MAX),
    ImplementationSteps NVARCHAR(MAX),

    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy NVARCHAR(256),
    RevisionReason NVARCHAR(1000),

    CONSTRAINT FK_PlanVersions_Plans FOREIGN KEY (PlanId) REFERENCES Plans(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_PlanVersions_PlanId_Version UNIQUE (PlanId, Version)
);

CREATE INDEX IX_PlanVersions_PlanId ON PlanVersions(PlanId);
CREATE INDEX IX_PlanVersions_PlanId_Version ON PlanVersions(PlanId, Version);
```

---

## Implementation Steps

### Step 1: Update Domain Entities

**File:** `/src/PRFactory.Domain/Entities/Plan.cs`

```csharp
using System;
using System.Collections.Generic;

namespace PRFactory.Domain.Entities;

/// <summary>
/// Represents an implementation plan for a ticket.
/// Supports both legacy single-file plans and new multi-artifact plans.
/// </summary>
public class Plan : BaseEntity
{
    public Guid TicketId { get; set; }
    public virtual Ticket Ticket { get; set; } = null!;

    // Legacy single-file plan (kept for backward compatibility)
    public string? Content { get; set; }

    // Multi-artifact fields (new)
    public string? UserStories { get; set; }
    public string? ApiDesign { get; set; }
    public string? DatabaseSchema { get; set; }
    public string? TestCases { get; set; }
    public string? ImplementationSteps { get; set; }

    /// <summary>
    /// Version number (incremented on each revision)
    /// </summary>
    public int Version { get; set; } = 1;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public virtual ICollection<PlanVersion> Versions { get; set; } = new List<PlanVersion>();

    /// <summary>
    /// Determines if this plan uses multi-artifact format
    /// </summary>
    public bool HasMultipleArtifacts =>
        !string.IsNullOrEmpty(UserStories) ||
        !string.IsNullOrEmpty(ApiDesign) ||
        !string.IsNullOrEmpty(DatabaseSchema) ||
        !string.IsNullOrEmpty(TestCases) ||
        !string.IsNullOrEmpty(ImplementationSteps);

    /// <summary>
    /// Creates a snapshot of current artifacts as a new version
    /// </summary>
    public PlanVersion CreateVersion(string? createdBy = null, string? revisionReason = null)
    {
        return new PlanVersion
        {
            PlanId = Id,
            Version = Version,
            UserStories = UserStories,
            ApiDesign = ApiDesign,
            DatabaseSchema = DatabaseSchema,
            TestCases = TestCases,
            ImplementationSteps = ImplementationSteps,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
            RevisionReason = revisionReason
        };
    }

    /// <summary>
    /// Updates artifacts and increments version
    /// </summary>
    public void UpdateArtifacts(
        string? userStories = null,
        string? apiDesign = null,
        string? databaseSchema = null,
        string? testCases = null,
        string? implementationSteps = null,
        string? createdBy = null,
        string? revisionReason = null)
    {
        // Create version snapshot before updating
        var version = CreateVersion(createdBy, revisionReason);
        Versions.Add(version);

        // Update artifacts (only update non-null values)
        if (userStories != null) UserStories = userStories;
        if (apiDesign != null) ApiDesign = apiDesign;
        if (databaseSchema != null) DatabaseSchema = databaseSchema;
        if (testCases != null) TestCases = testCases;
        if (implementationSteps != null) ImplementationSteps = implementationSteps;

        // Increment version
        Version++;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

**New File:** `/src/PRFactory.Domain/Entities/PlanVersion.cs`

```csharp
using System;

namespace PRFactory.Domain.Entities;

/// <summary>
/// Represents a historical version of a plan's artifacts.
/// Created whenever a plan is revised.
/// </summary>
public class PlanVersion : BaseEntity
{
    public Guid PlanId { get; set; }
    public virtual Plan Plan { get; set; } = null!;

    public int Version { get; set; }

    // Snapshot of artifacts at this version
    public string? UserStories { get; set; }
    public string? ApiDesign { get; set; }
    public string? DatabaseSchema { get; set; }
    public string? TestCases { get; set; }
    public string? ImplementationSteps { get; set; }

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? RevisionReason { get; set; }
}
```

---

### Step 2: Update Entity Configuration

**File:** `/src/PRFactory.Infrastructure/Data/Configurations/PlanConfiguration.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRFactory.Domain.Entities;

namespace PRFactory.Infrastructure.Data.Configurations;

public class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("Plans");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.TicketId)
            .IsRequired();

        // Legacy field
        builder.Property(p => p.Content)
            .HasColumnType("nvarchar(max)");

        // Multi-artifact fields
        builder.Property(p => p.UserStories)
            .HasColumnType("nvarchar(max)");

        builder.Property(p => p.ApiDesign)
            .HasColumnType("nvarchar(max)");

        builder.Property(p => p.DatabaseSchema)
            .HasColumnType("nvarchar(max)");

        builder.Property(p => p.TestCases)
            .HasColumnType("nvarchar(max)");

        builder.Property(p => p.ImplementationSteps)
            .HasColumnType("nvarchar(max)");

        builder.Property(p => p.Version)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.UpdatedAt);

        // Relationships
        builder.HasOne(p => p.Ticket)
            .WithOne()
            .HasForeignKey<Plan>(p => p.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Versions)
            .WithOne(v => v.Plan)
            .HasForeignKey(v => v.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(p => p.TicketId)
            .IsUnique();

        // Ignore computed property
        builder.Ignore(p => p.HasMultipleArtifacts);
    }
}
```

**New File:** `/src/PRFactory.Infrastructure/Data/Configurations/PlanVersionConfiguration.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRFactory.Domain.Entities;

namespace PRFactory.Infrastructure.Data.Configurations;

public class PlanVersionConfiguration : IEntityTypeConfiguration<PlanVersion>
{
    public void Configure(EntityTypeBuilder<PlanVersion> builder)
    {
        builder.ToTable("PlanVersions");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.PlanId)
            .IsRequired();

        builder.Property(v => v.Version)
            .IsRequired();

        builder.Property(v => v.UserStories)
            .HasColumnType("nvarchar(max)");

        builder.Property(v => v.ApiDesign)
            .HasColumnType("nvarchar(max)");

        builder.Property(v => v.DatabaseSchema)
            .HasColumnType("nvarchar(max)");

        builder.Property(v => v.TestCases)
            .HasColumnType("nvarchar(max)");

        builder.Property(v => v.ImplementationSteps)
            .HasColumnType("nvarchar(max)");

        builder.Property(v => v.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(v => v.CreatedBy)
            .HasMaxLength(256);

        builder.Property(v => v.RevisionReason)
            .HasMaxLength(1000);

        // Relationships
        builder.HasOne(v => v.Plan)
            .WithMany(p => p.Versions)
            .HasForeignKey(v => v.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(v => v.PlanId);

        builder.HasIndex(v => new { v.PlanId, v.Version })
            .IsUnique()
            .HasDatabaseName("UQ_PlanVersions_PlanId_Version");
    }
}
```

---

### Step 3: Update DbContext

**File:** `/src/PRFactory.Infrastructure/Data/ApplicationDbContext.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using PRFactory.Domain.Entities;

namespace PRFactory.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    // Existing DbSets...
    public DbSet<Ticket> Tickets { get; set; } = null!;
    public DbSet<Plan> Plans { get; set; } = null!;

    // New DbSet
    public DbSet<PlanVersion> PlanVersions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations
        modelBuilder.ApplyConfiguration(new TicketConfiguration());
        modelBuilder.ApplyConfiguration(new PlanConfiguration());
        modelBuilder.ApplyConfiguration(new PlanVersionConfiguration());  // New

        // ... other configurations ...
    }
}
```

---

### Step 4: Create EF Core Migration

**Command:**
```bash
cd /home/user/PRFactory/src/PRFactory.Infrastructure

# Source .NET proxy setup (for Claude Code web environments)
source /tmp/dotnet-proxy-setup.sh

# Create migration
dotnet ef migrations add AddPlanArtifactsAndVersioning \
    --startup-project ../PRFactory.Api/PRFactory.Api.csproj \
    --context ApplicationDbContext \
    --output-dir Data/Migrations
```

**Generated Migration File:** `/src/PRFactory.Infrastructure/Data/Migrations/YYYYMMDDHHMMSS_AddPlanArtifactsAndVersioning.cs`

```csharp
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRFactory.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanArtifactsAndVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new artifact columns to Plans table
            migrationBuilder.AddColumn<string>(
                name: "UserStories",
                table: "Plans",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApiDesign",
                table: "Plans",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DatabaseSchema",
                table: "Plans",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TestCases",
                table: "Plans",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImplementationSteps",
                table: "Plans",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Plans",
                type: "int",
                nullable: false,
                defaultValue: 1);

            // Create PlanVersions table
            migrationBuilder.CreateTable(
                name: "PlanVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    UserStories = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApiDesign = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DatabaseSchema = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TestCases = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImplementationSteps = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RevisionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanVersions_Plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlanVersions_PlanId",
                table: "PlanVersions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "UQ_PlanVersions_PlanId_Version",
                table: "PlanVersions",
                columns: new[] { "PlanId", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlanVersions");

            migrationBuilder.DropColumn(
                name: "UserStories",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "ApiDesign",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "DatabaseSchema",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "TestCases",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "ImplementationSteps",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Plans");
        }
    }
}
```

---

### Step 5: Apply Migration

**Development:**
```bash
cd /home/user/PRFactory/src/PRFactory.Api

# Source .NET proxy setup
source /tmp/dotnet-proxy-setup.sh

# Apply migration
dotnet ef database update --context ApplicationDbContext
```

**Production:**
- Migration will be applied automatically on application startup via `DbContext.Database.Migrate()` (if configured)
- OR apply manually using deployment scripts

---

### Step 6: Update Repositories

**File:** `/src/PRFactory.Core/Repositories/IPlanRepository.cs`

Add methods for version management:

```csharp
public interface IPlanRepository : IRepository<Plan>
{
    Task<Plan?> GetByTicketIdAsync(Guid ticketId, CancellationToken cancellationToken = default);

    // New methods for version management
    Task<List<PlanVersion>> GetVersionHistoryAsync(Guid planId, CancellationToken cancellationToken = default);
    Task<PlanVersion?> GetVersionAsync(Guid planId, int version, CancellationToken cancellationToken = default);
}
```

**File:** `/src/PRFactory.Infrastructure/Repositories/PlanRepository.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using PRFactory.Core.Repositories;
using PRFactory.Domain.Entities;
using PRFactory.Infrastructure.Data;

namespace PRFactory.Infrastructure.Repositories;

public class PlanRepository : Repository<Plan>, IPlanRepository
{
    public PlanRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Plan?> GetByTicketIdAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Versions.OrderByDescending(v => v.Version))  // Include version history
            .FirstOrDefaultAsync(p => p.TicketId == ticketId, cancellationToken);
    }

    public async Task<List<PlanVersion>> GetVersionHistoryAsync(
        Guid planId,
        CancellationToken cancellationToken = default)
    {
        return await _context.PlanVersions
            .Where(v => v.PlanId == planId)
            .OrderByDescending(v => v.Version)
            .ToListAsync(cancellationToken);
    }

    public async Task<PlanVersion?> GetVersionAsync(
        Guid planId,
        int version,
        CancellationToken cancellationToken = default)
    {
        return await _context.PlanVersions
            .FirstOrDefaultAsync(
                v => v.PlanId == planId && v.Version == version,
                cancellationToken);
    }
}
```

---

### Step 7: Update DTOs

**File:** `/src/PRFactory.Core/DTOs/PlanDto.cs`

```csharp
using System;
using System.Collections.Generic;

namespace PRFactory.Core.DTOs;

public class PlanDto
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }

    // Legacy field
    public string? Content { get; set; }

    // Multi-artifact fields
    public string? UserStories { get; set; }
    public string? ApiDesign { get; set; }
    public string? DatabaseSchema { get; set; }
    public string? TestCases { get; set; }
    public string? ImplementationSteps { get; set; }

    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Computed property
    public bool HasMultipleArtifacts =>
        !string.IsNullOrEmpty(UserStories) ||
        !string.IsNullOrEmpty(ApiDesign) ||
        !string.IsNullOrEmpty(DatabaseSchema) ||
        !string.IsNullOrEmpty(TestCases) ||
        !string.IsNullOrEmpty(ImplementationSteps);

    // Version history
    public List<PlanVersionDto>? Versions { get; set; }
}

public class PlanVersionDto
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public int Version { get; set; }

    public string? UserStories { get; set; }
    public string? ApiDesign { get; set; }
    public string? DatabaseSchema { get; set; }
    public string? TestCases { get; set; }
    public string? ImplementationSteps { get; set; }

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? RevisionReason { get; set; }
}
```

---

## Testing

### Unit Tests

**File:** `/tests/PRFactory.Domain.Tests/Entities/PlanTests.cs`

```csharp
using PRFactory.Domain.Entities;
using Xunit;

namespace PRFactory.Domain.Tests.Entities;

public class PlanTests
{
    [Fact]
    public void HasMultipleArtifacts_WithUserStories_ReturnsTrue()
    {
        // Arrange
        var plan = new Plan
        {
            UserStories = "Some user stories"
        };

        // Act & Assert
        Assert.True(plan.HasMultipleArtifacts);
    }

    [Fact]
    public void HasMultipleArtifacts_WithoutArtifacts_ReturnsFalse()
    {
        // Arrange
        var plan = new Plan
        {
            Content = "Legacy plan"
        };

        // Act & Assert
        Assert.False(plan.HasMultipleArtifacts);
    }

    [Fact]
    public void UpdateArtifacts_CreatesVersionAndIncrementsVersion()
    {
        // Arrange
        var plan = new Plan
        {
            Id = Guid.NewGuid(),
            UserStories = "Original stories",
            Version = 1
        };

        // Act
        plan.UpdateArtifacts(
            userStories: "Updated stories",
            createdBy: "user@example.com",
            revisionReason: "Fixed requirements");

        // Assert
        Assert.Equal(2, plan.Version);
        Assert.Equal("Updated stories", plan.UserStories);
        Assert.Single(plan.Versions);
        Assert.Equal(1, plan.Versions.First().Version);
        Assert.Equal("Original stories", plan.Versions.First().UserStories);
    }
}
```

### Integration Tests

**File:** `/tests/PRFactory.Infrastructure.Tests/Repositories/PlanRepositoryTests.cs`

```csharp
public class PlanRepositoryTests : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task GetByTicketIdAsync_WithVersionHistory_LoadsVersions()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var plan = new Plan
        {
            TicketId = ticketId,
            UserStories = "V1 stories",
            Version = 2
        };
        plan.Versions.Add(new PlanVersion
        {
            PlanId = plan.Id,
            Version = 1,
            UserStories = "V0 stories",
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        });

        await _repository.AddAsync(plan);

        // Act
        var loaded = await _repository.GetByTicketIdAsync(ticketId);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(2, loaded.Version);
        Assert.Single(loaded.Versions);
        Assert.Equal("V0 stories", loaded.Versions.First().UserStories);
    }
}
```

---

## Acceptance Criteria

- [ ] `Plan` entity updated with 5 artifact fields
- [ ] `PlanVersion` entity created
- [ ] Entity configurations created (Fluent API)
- [ ] `ApplicationDbContext` updated with `PlanVersions` DbSet
- [ ] EF Core migration generated
- [ ] Migration applied successfully (dev environment)
- [ ] Repository methods added for version management
- [ ] DTOs updated to include artifacts and versions
- [ ] Unit tests for domain entities (80% coverage)
- [ ] Integration tests for repository methods
- [ ] Backward compatibility with legacy `Content` field

---

## Rollback Plan

If migration fails in production:

1. **Revert migration:**
   ```bash
   dotnet ef database update PreviousMigrationName
   ```

2. **Remove migration file:**
   ```bash
   dotnet ef migrations remove
   ```

3. **Redeploy previous version**

---

## Performance Considerations

### Storage

- **NVARCHAR(MAX)** columns can grow large (each artifact can be 10-50KB)
- Consider compression for older versions (SQL Server native compression)
- Archive old versions after 90 days (optional)

### Query Optimization

- Index on `TicketId` (unique) for fast lookups
- Composite index on `(PlanId, Version)` for version queries
- Use `.Include(p => p.Versions)` selectively (don't load versions unless needed)

### Recommendations

1. **Lazy loading for versions:** Don't load `Versions` collection by default
2. **Pagination for version history:** If version count grows > 10, paginate in UI
3. **Soft delete for old versions:** Consider archiving instead of hard delete

---

## Next Steps

After completing database schema:
1. Implement `PlanArtifactStorageAgent` (uses repository to save artifacts)
2. Update Web UI to display multi-artifact plans (see `03_WEB_UI.md`)
3. Implement plan revision workflow (see `04_REVISION_WORKFLOW.md`)
