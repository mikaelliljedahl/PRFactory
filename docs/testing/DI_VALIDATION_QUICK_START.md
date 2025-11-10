# DI Validation Test Implementation Quick Start

## Overview
This is a quick reference guide for implementing Dependency Injection validation tests. See `DI_VALIDATION_TEST_PLAN.md` for the complete detailed plan.

---

## Quick Facts

**Service Registration Locations:**
- Primary: `/src/PRFactory.Infrastructure/DependencyInjection.cs` → `AddInfrastructure()` (50+ services)
- Agent Framework: `/src/PRFactory.Infrastructure/Agents/Configuration/ServiceCollectionExtensions.cs` → `AddAgentFramework()`
- Git Platforms: `/src/PRFactory.Infrastructure/Git/GitServiceCollectionExtensions.cs` → `AddGitPlatformIntegration()`
- Programs: Web, Api, Worker in `/src/*/Program.cs`

**Service Categories:**
- **Repositories** (11): ITenantRepository, IRepositoryRepository, ITicketRepository, etc. → Scoped
- **Application Services** (10): ITicketApplicationService, IPlanService, etc. → Scoped
- **Agents** (15): TriggerAgent, PlanningAgent, etc. → Transient
- **Infrastructure** (15+): IEncryptionService, DbContext, configuration, middleware → Mixed

**Total Services to Validate:** 50+

---

## Test File Structure

Create these files in `/tests/PRFactory.Tests/DependencyInjection/`:

```
DependencyInjection/
├── DIValidationTestBase.cs              (Base class - 40 lines)
├── TestConfigurationBuilder.cs          (Helper - 30 lines)
├── DIAssertions.cs                      (Helper assertions - 40 lines)
├── InfrastructureServiceRegistrationTests.cs    (8-10 test methods)
├── WebServiceRegistrationTests.cs               (4-5 test methods)
├── ApiServiceRegistrationTests.cs               (2-3 test methods)
├── WorkerServiceRegistrationTests.cs            (3-4 test methods)
├── ServiceLifetimeValidationTests.cs            (8-10 test methods)
├── AgentFrameworkServiceRegistrationTests.cs    (5-6 test methods)
└── GitPlatformServiceRegistrationTests.cs       (6-7 test methods)
```

---

## Minimal Test Template

```csharp
using Microsoft.Extensions.DependencyInjection;
using PRFactory.Infrastructure;
using Xunit;

namespace PRFactory.Tests.DependencyInjection;

public class YourServiceRegistrationTests
{
    private IConfiguration CreateTestConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Encryption:Key", "test-encryption-key" },
                { "ConnectionStrings:DefaultConnection", "Data Source=:memory:" }
            })
            .Build();
    }

    [Fact]
    public void AddInfrastructure_RegistersRepository()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = CreateTestConfiguration();

        // Act
        services.AddInfrastructure(config);
        var provider = services.BuildServiceProvider();

        // Assert
        var repo = provider.GetService<ITicketRepository>();
        Assert.NotNull(repo);
        Assert.IsType<TicketRepository>(repo);
    }

    [Fact]
    public void AddInfrastructure_RepositoryIsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = CreateTestConfiguration();

        // Act
        services.AddInfrastructure(config);

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ITicketRepository));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor!.Lifetime);
    }
}
```

---

## Implementation Checklist

### Step 1: Create Test Infrastructure (1-2 hours)
- [ ] Create `DIValidationTestBase.cs` with helper methods
- [ ] Create `TestConfigurationBuilder.cs` for test configs
- [ ] Create `DIAssertions.cs` for common assertion patterns
- [ ] Test that basic service registration works

### Step 2: Infrastructure Tests (2-3 hours)
- [ ] Implement `InfrastructureServiceRegistrationTests.cs`
- [ ] Create tests for each repository (11 tests)
- [ ] Create tests for each application service (10 tests)
- [ ] Create tests for each agent (15 tests with @Theory)
- [ ] Create tests for special services (encryption, DbContext, etc.)
- [ ] Run and fix any failures

### Step 3: Web Layer Tests (1-2 hours)
- [ ] Implement `WebServiceRegistrationTests.cs`
- [ ] Test web facade services (7 services)
- [ ] Test SignalR and Blazor configuration
- [ ] Test dependency chain for critical services
- [ ] Run and verify all pass

### Step 4: Agent & Git Tests (1-2 hours)
- [ ] Implement `AgentFrameworkServiceRegistrationTests.cs`
- [ ] Implement `GitPlatformServiceRegistrationTests.cs`
- [ ] Test agent registry and middleware
- [ ] Test all platform providers
- [ ] Run and verify all pass

### Step 5: Lifetime Validation (1-2 hours)
- [ ] Implement `ServiceLifetimeValidationTests.cs`
- [ ] Validate all Scoped services are indeed Scoped
- [ ] Validate all Transient agents are Transient
- [ ] Check for inappropriate Singleton→Scoped dependencies
- [ ] Run and fix any lifetime violations

### Step 6: API & Worker Tests (1 hour)
- [ ] Implement `ApiServiceRegistrationTests.cs` (with Skip attributes for now)
- [ ] Implement `WorkerServiceRegistrationTests.cs` (with Skip attributes for now)
- [ ] Document TODO items

### Step 7: Documentation & Cleanup (30 mins)
- [ ] Add XML documentation to test methods
- [ ] Add helpful comments explaining what each test validates
- [ ] Update this quick start with actual results
- [ ] Run full test suite to confirm all pass

**Total Time Estimate:** 7-10 hours of focused work

---

## Key Test Assertions

```csharp
// Service is registered
var service = provider.GetService<IInterface>();
Assert.NotNull(service);

// Service is correct type
Assert.IsType<Implementation>(service);

// Service is Scoped
var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IInterface));
Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);

// Service can be resolved (dependency chain works)
var service = provider.GetRequiredService<IInterface>();
Assert.NotNull(service);

// Multiple implementations available
var providers = provider.GetRequiredService<IEnumerable<IGitPlatformProvider>>();
Assert.NotEmpty(providers);
Assert.True(providers.Any(p => p is GitHubProvider));
```

---

## Common Pitfalls to Avoid

1. **Forgetting to add logging to test config**
   ```csharp
   // This will fail - services need ILogger
   var config = new ConfigurationBuilder().Build();
   services.AddInfrastructure(config);  // Missing logging!
   
   // Correct:
   var services = new ServiceCollection().AddLogging();
   services.AddInfrastructure(config);
   ```

2. **Not building service provider before resolving**
   ```csharp
   // Wrong:
   var service = services.GetService<IInterface>();  // IServiceCollection has no GetService!
   
   // Correct:
   var provider = services.BuildServiceProvider();
   var service = provider.GetService<IInterface>();
   ```

3. **Forgetting to dispose provider**
   ```csharp
   // Wrong - may leak resources:
   var provider = services.BuildServiceProvider();
   var service = provider.GetService<IInterface>();
   
   // Better:
   using var provider = services.BuildServiceProvider();
   var service = provider.GetService<IInterface>();
   
   // Or for scoped:
   using var scope = provider.CreateScope();
   var service = scope.ServiceProvider.GetRequiredService<IInterface>();
   ```

4. **Checking lifetime on wrong thing**
   ```csharp
   // Wrong - checking service, not registration:
   var service = provider.GetService<IInterface>();
   // Can't get lifetime from resolved instance
   
   // Correct - check descriptor before building:
   var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IInterface));
   Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
   ```

---

## Service Categories for Reference

### Always Scoped
- All repositories (DbContext pattern)
- All application services
- DbContext itself
- Web facade services

### Always Transient
- All agents
- Event handlers
- Command handlers

### Usually Singleton
- Encryption service
- Configuration services
- Middleware
- Memory cache

### DbContext Setup
```csharp
// In config for tests:
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("TestDb"));

// Or in memory:
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
```

---

## Running Tests

```bash
# Run all DI tests
dotnet test tests/PRFactory.Tests/DependencyInjection/

# Run specific test class
dotnet test tests/PRFactory.Tests/DependencyInjection/InfrastructureServiceRegistrationTests.cs

# Run with verbose output
dotnet test tests/PRFactory.Tests/DependencyInjection/ -v

# Run and stop on first failure
dotnet test tests/PRFactory.Tests/DependencyInjection/ --no-build -x

# Generate coverage report
dotnet test /p:CollectCoverage=true /p:CoverageFileName=di-coverage.xml
```

---

## Expected Results

| Test Category | Expected Count | Status |
|--------------|---|--------|
| Infrastructure Repositories | 11 | Active |
| Infrastructure App Services | 10 | Active |
| Infrastructure Agents | 15 | Active |
| Infrastructure Special Services | 8 | Active |
| Web Facade Services | 7 | Active |
| Web SignalR/Blazor | 2 | Active |
| Web Dependency Chains | 2 | Active |
| Agent Framework | 5 | Active |
| Git Platform Providers | 6 | Active |
| Service Lifetimes | 10 | Active |
| API Services | 3 | Skipped |
| Worker Services | 3 | Skipped |
| **TOTAL** | **90+** | |

---

## Success Criteria

- All active tests pass
- API and Worker tests skip gracefully with clear messages
- Code coverage > 95% for service registration
- Test execution < 5 seconds
- No failing tests in CI/CD pipeline

---

## References

- Full plan: `docs/testing/DI_VALIDATION_TEST_PLAN.md`
- Service registration: `src/PRFactory.Infrastructure/DependencyInjection.cs`
- Test framework: xUnit 2.9.0
- Mocking: Moq 4.20.70
- Configuration: Microsoft.Extensions.Configuration

---

## Getting Help

1. Check test patterns in `DI_VALIDATION_TEST_PLAN.md` under "Test Implementation Patterns"
2. See existing tests in `/tests/PRFactory.Tests/Services/` for patterns
3. Check `TestBase.cs` for common test setup patterns
4. Review `PRFactory.Tests.csproj` for available packages

