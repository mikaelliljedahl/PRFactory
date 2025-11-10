# Dependency Injection (DI) Registration Validation Test Plan

## Overview

This plan outlines a comprehensive testing strategy for validating that all dependency injection registrations in PRFactory are correctly configured, preventing runtime resolution errors and ensuring all services are properly wired.

---

## Current State Analysis

### Service Registration Points

PRFactory has **3 main application entry points** with distinct service registrations:

#### 1. **PRFactory.Web** (Blazor Server)
- **File**: `/src/PRFactory.Web/Program.cs`
- **Services Registered**:
  - Infrastructure services via `AddInfrastructure(builder.Configuration)`
  - Web layer services (Facades):
    - `ITicketService` → `TicketService`
    - `IRepositoryService` → `RepositoryService`
    - `IWorkflowEventService` → `WorkflowEventService`
    - `IAgentPromptService` → `AgentPromptService`
    - `ITenantService` → `TenantService`
    - `IErrorService` → `ErrorService`
    - `IToastService` → `ToastService`
  - SignalR services
  - Blazor server-side services

#### 2. **PRFactory.Api** (REST API)
- **File**: `/src/PRFactory.Api/Program.cs`
- **Current State**: Basic setup with TODOs for service registration
- **Services Registered**:
  - Controllers with JSON options
  - Swagger/OpenAPI
  - CORS
  - Health checks
  - **Status**: Infrastructure services NOT YET registered (commented out)

#### 3. **PRFactory.Worker** (Background Service)
- **File**: `/src/PRFactory.Worker/Program.cs`
- **Services Registered**:
  - Serilog logging
  - `IWorkflowResumeHandler` → `WorkflowResumeHandler`
  - Configuration binding for `AgentHostOptions`
  - **Status**: Infrastructure services NOT YET registered (commented out)

### Service Registration Extension Methods

#### Primary Extension Method
- **File**: `/src/PRFactory.Infrastructure/DependencyInjection.cs`
- **Method**: `AddInfrastructure(IServiceCollection services, IConfiguration configuration)`
- **Registers**: 50+ services including repositories, application services, agents, and infrastructure components

#### Secondary Extension Methods
- **File**: `/src/PRFactory.Infrastructure/Agents/Configuration/ServiceCollectionExtensions.cs`
  - `AddAgentFramework(IServiceCollection, IConfiguration)`
  - `AddAgentFramework(IServiceCollection, Action<AgentConfiguration>)`
  - **Purpose**: Configures agent framework, middleware, and checkpoint storage

- **File**: `/src/PRFactory.Infrastructure/Git/GitServiceCollectionExtensions.cs`
  - `AddGitPlatformIntegration()`
  - `AddGitPlatformIntegrationWithRepository<TRepository>()`
  - **Purpose**: Registers Git platform providers (GitHub, Bitbucket, Azure DevOps)

- **File**: `/src/PRFactory.Infrastructure/Agents/AgentRegistry.cs`
  - `AddAgentRegistry()`
  - **Purpose**: Registers all agent types

### Service Lifetimes Across Projects

**Singleton** (shared across all requests):
- `IEncryptionService`
- `LoggingMiddleware`
- `ErrorHandlingMiddleware`
- `RetryMiddleware`
- `ICheckpointRepository` (InMemory)
- Middleware services

**Scoped** (one per request/operation):
- All repositories (ITenantRepository, IRepositoryRepository, etc.)
- Application services (ITicketApplicationService, etc.)
- Configuration services
- DbContext (ApplicationDbContext)
- Web facade services
- Context builders
- Orchestrators and graph services

**Transient** (new instance every time):
- All agents (TriggerAgent, RepositoryCloneAgent, etc.)
- Process executors

---

## Test Strategy

### Test Project Location
- **Primary Location**: `/tests/PRFactory.Tests/DependencyInjection/` (NEW)
- **Test Framework**: xUnit (already in use)
- **Mocking Library**: Moq (already in use)
- **Supporting Tools**: Microsoft.Extensions.DependencyInjection.Abstractions

### Test File Organization

```
/tests/PRFactory.Tests/
├── DependencyInjection/                    (NEW FOLDER)
│   ├── DIValidationTestBase.cs             (Base class for all DI tests)
│   ├── InfrastructureServiceRegistrationTests.cs
│   ├── WebServiceRegistrationTests.cs
│   ├── ApiServiceRegistrationTests.cs
│   ├── WorkerServiceRegistrationTests.cs
│   └── ServiceLifetimeValidationTests.cs
```

---

## Detailed Test Plans

### 1. Base Test Class: `DIValidationTestBase.cs`

**Purpose**: Provides common utilities and helper methods for all DI validation tests

**Key Responsibilities**:
1. Create service collection and build service provider
2. Validate service registration without building (to catch errors early)
3. Assert service can be resolved with correct lifetime
4. Get all registered services of a type
5. Validate chains of dependencies

**Key Methods**:

```csharp
public abstract class DIValidationTestBase
{
    // Helper to build service collection
    protected IServiceCollection CreateServiceCollection()
    
    // Helper to validate a service is registered
    protected void AssertServiceRegistered<TInterface>(IServiceProvider provider)
    
    // Helper to validate service can be resolved
    protected void AssertServiceResolvable<TInterface>(IServiceProvider provider)
    
    // Helper to validate service lifetime
    protected void AssertServiceLifetime<TInterface>(
        IServiceCollection services, 
        ServiceLifetime expectedLifetime)
    
    // Helper to validate dependency chain
    protected void AssertDependencyChainResolvable<TInterface>(IServiceProvider provider)
    
    // Helper to test parameterized factory registrations
    protected void AssertFactoryRegistration<TInterface>(IServiceProvider provider)
    
    // Helper to validate configuration is accessible
    protected void AssertConfigurationRegistered(IServiceProvider provider)
}
```

---

### 2. Test Class: `InfrastructureServiceRegistrationTests.cs`

**Purpose**: Validate all infrastructure services registered via `DependencyInjection.AddInfrastructure()`

**Coverage Areas**:

#### A. Repository Registration Tests
```csharp
[Fact]
public void AddInfrastructure_RegistersAllRepositories()
{
    // Validates:
    // - ITenantRepository → TenantRepository
    // - IRepositoryRepository → RepositoryRepository
    // - ITicketRepository → TicketRepository
    // - ITicketUpdateRepository → TicketUpdateRepository
    // - IWorkflowEventRepository → WorkflowEventRepository
    // - ICheckpointRepository → CheckpointRepository
    // - IAgentPromptTemplateRepository → AgentPromptTemplateRepository
    // - IErrorRepository → ErrorRepository
    // - IUserRepository → UserRepository
    // - IPlanReviewRepository → PlanReviewRepository
    // - IReviewCommentRepository → ReviewCommentRepository
}

[Fact]
public void AddInfrastructure_RepositoriesAreScoped()
{
    // Validates all repositories are Scoped (correct for DbContext per-request access)
}
```

#### B. Application Service Registration Tests
```csharp
[Fact]
public void AddInfrastructure_RegistersAllApplicationServices()
{
    // Validates:
    // - ITicketUpdateService → TicketUpdateService
    // - ITicketApplicationService → TicketApplicationService
    // - IRepositoryApplicationService → RepositoryApplicationService
    // - ITenantApplicationService → TenantApplicationService
    // - IErrorApplicationService → ErrorApplicationService
    // - IQuestionApplicationService → QuestionApplicationService
    // - IWorkflowEventApplicationService → WorkflowEventApplicationService
    // - IPlanService → PlanService
    // - IUserService → UserService
    // - IPlanReviewService → PlanReviewService
}

[Fact]
public void AddInfrastructure_ApplicationServicesAreScoped()
{
    // Validates all application services are Scoped
}
```

#### C. Agent Registration Tests
```csharp
[Fact]
public void AddInfrastructure_RegistersAllAgents()
{
    // Validates all agent types are registered:
    // - TriggerAgent, RepositoryCloneAgent, AnalysisAgent, etc. (14+ agents)
}

[Theory]
[InlineData(typeof(TriggerAgent))]
[InlineData(typeof(RepositoryCloneAgent))]
[InlineData(typeof(AnalysisAgent))]
[InlineData(typeof(QuestionGenerationAgent))]
[InlineData(typeof(JiraPostAgent))]
[InlineData(typeof(HumanWaitAgent))]
[InlineData(typeof(AnswerProcessingAgent))]
[InlineData(typeof(PlanningAgent))]
[InlineData(typeof(GitPlanAgent))]
[InlineData(typeof(ImplementationAgent))]
[InlineData(typeof(GitCommitAgent))]
[InlineData(typeof(PullRequestAgent))]
[InlineData(typeof(CompletionAgent))]
[InlineData(typeof(ApprovalCheckAgent))]
[InlineData(typeof(ErrorHandlingAgent))]
public void AddInfrastructure_AgentIsRegisteredAsTransient(Type agentType)
{
    // Validates agent is Transient (new instance per execution)
}
```

#### D. Encryption Service Tests
```csharp
[Fact]
public void AddInfrastructure_RegistersEncryptionService_WhenKeyConfigured()
{
    // Validates IEncryptionService is registered as Singleton when encryption key exists
}

[Fact]
public void AddInfrastructure_ThrowsInvalidOperationException_WhenEncryptionKeyMissing()
{
    // Validates registration fails gracefully when "Encryption:Key" not in config
}
```

#### E. DbContext Registration Tests
```csharp
[Fact]
public void AddInfrastructure_RegistersApplicationDbContext()
{
    // Validates ApplicationDbContext is registered
}

[Fact]
public void AddInfrastructure_DbContextIsScoped()
{
    // Validates DbContext is Scoped (required for per-request isolation)
}

[Fact]
public void AddInfrastructure_DbContextIsConfiguredWithSqlite()
{
    // Validates DbContext is configured to use SQLite
}
```

#### F. Infrastructure Service Dependencies Tests
```csharp
[Fact]
public void AddInfrastructure_CanResolveTicketService_WithAllDependencies()
{
    // Tests full dependency chain for ITicketApplicationService
    // Validates it can resolve all repositories, context, etc.
}

[Fact]
public void AddInfrastructure_CanResolvePlanningAgent_WithAllDependencies()
{
    // Tests full dependency chain for PlanningAgent
    // Validates it has access to needed services
}
```

#### G. Configuration Service Tests
```csharp
[Fact]
public void AddInfrastructure_RegistersTenantConfigurationService()
{
    // Validates ITenantConfigurationService is registered
}

[Fact]
public void AddInfrastructure_RegistersContextBuilder()
{
    // Validates Claude.IContextBuilder → Claude.ContextBuilder
}
```

#### H. Checkpoint & State Store Tests
```csharp
[Fact]
public void AddInfrastructure_RegistersCheckpointStoreAdapter()
{
    // Validates WorkflowCheckpointStore → GraphCheckpointStoreAdapter
}

[Fact]
public void AddInfrastructure_RegistersWorkflowStateStore()
{
    // Validates Agents.Graphs.IWorkflowStateStore
}

[Fact]
public void AddInfrastructure_RegistersEventPublisher()
{
    // Validates Agents.Graphs.IEventPublisher
}
```

---

### 3. Test Class: `WebServiceRegistrationTests.cs`

**Purpose**: Validate Web layer service registrations and Blazor-specific configurations

**Coverage Areas**:

#### A. Web Facade Service Tests
```csharp
[Theory]
[InlineData(typeof(ITicketService), typeof(TicketService))]
[InlineData(typeof(IRepositoryService), typeof(RepositoryService))]
[InlineData(typeof(IWorkflowEventService), typeof(WorkflowEventService))]
[InlineData(typeof(IAgentPromptService), typeof(AgentPromptService))]
[InlineData(typeof(ITenantService), typeof(TenantService))]
[InlineData(typeof(IErrorService), typeof(ErrorService))]
[InlineData(typeof(IToastService), typeof(ToastService))]
public void WebProgram_RegistersWebFacadeService(Type serviceType, Type implementationType)
{
    // Validates each web facade service is registered correctly
}

[Fact]
public void WebProgram_AllWebFacadeServicesAreScoped()
{
    // Validates web facade services are Scoped (per-request)
}
```

#### B. SignalR and Blazor Tests
```csharp
[Fact]
public void WebProgram_RegistersSignalREventBroadcaster()
{
    // Validates PRFactory.Infrastructure.Events.IEventBroadcaster
    //           → SignalREventBroadcaster
}

[Fact]
public void WebProgram_CanResolveSignalRServices()
{
    // Validates SignalR is properly configured
}
```

#### C. Database Seeder Tests
```csharp
[Fact]
public void WebProgram_RegistersDbSeeder()
{
    // Validates DbSeeder is registered
}

[Fact]
public void WebProgram_CanResolveDbSeederWithDependencies()
{
    // Validates DbSeeder can resolve its dependencies (repositories, etc.)
}
```

#### D. Web Layer Dependency Chain Tests
```csharp
[Fact]
public void WebProgram_CanResolveTicketService_WithAllInfrastructureDependencies()
{
    // Tests ITicketService can resolve:
    // - ITicketApplicationService
    // - ITicketUpdateService
    // - IQuestionApplicationService
    // - IWorkflowEventApplicationService
    // - IPlanService
    // - ITenantContext
    // - ITicketRepository
    // - IPlanReviewService
    // - ICurrentUserService
}
```

---

### 4. Test Class: `ApiServiceRegistrationTests.cs`

**Purpose**: Validate API service registrations

**Coverage Areas**:

#### A. Current State Tests
```csharp
[Fact]
public void ApiProgram_HasBasicServiceRegistrations()
{
    // Validates: Controllers, Swagger, CORS, HealthChecks are registered
}

[Fact]
public void ApiProgram_HasPlaceholderForInfrastructureServices()
{
    // Documents that infrastructure services registration is TODO
    // (Services commented out in Program.cs)
}
```

#### B. When Infrastructure Services Are Enabled (Future)
```csharp
[Fact(Skip = "Infrastructure services registration in Api is pending")]
public void ApiProgram_RegistersInfrastructureServices_WhenEnabled()
{
    // Will validate: ITicketRepository, IRepositoryRepository, etc.
}
```

---

### 5. Test Class: `WorkerServiceRegistrationTests.cs`

**Purpose**: Validate Worker service registrations for background processing

**Coverage Areas**:

#### A. Current Registrations
```csharp
[Fact]
public void WorkerProgram_RegistersWorkflowResumeHandler()
{
    // Validates IWorkflowResumeHandler → WorkflowResumeHandler
}

[Fact]
public void WorkerProgram_ConfiguresAgentHostOptions()
{
    // Validates AgentHostOptions is configured from "AgentHost" section
}

[Fact]
public void WorkerProgram_RegistersHostedService()
{
    // Validates AgentHostService is registered as IHostedService
}
```

#### B. When Infrastructure Services Are Enabled (Future)
```csharp
[Fact(Skip = "Infrastructure services registration in Worker is pending")]
public void WorkerProgram_RegistersInfrastructureServices_WhenEnabled()
{
    // Will validate infrastructure service registration
}
```

---

### 6. Test Class: `ServiceLifetimeValidationTests.cs`

**Purpose**: Validate that service lifetimes are appropriate for their use cases

**Coverage Areas**:

#### A. Singleton Lifetime Tests
```csharp
[Fact]
public void ServiceLifetimes_EncryptionServiceIsSingleton()
{
    // Validates: Encryption service (expensive to initialize) is Singleton
}

[Fact]
public void ServiceLifetimes_MiddlewareIsSingleton()
{
    // Validates: Agent middleware is Singleton
}
```

#### B. Scoped Lifetime Tests
```csharp
[Fact]
public void ServiceLifetimes_RepositoriesAreScoped()
{
    // Validates: All repositories are Scoped (required for DbContext)
    // Ensures data isolation per request
}

[Fact]
public void ServiceLifetimes_ApplicationServicesAreScoped()
{
    // Validates: All application services are Scoped
}

[Fact]
public void ServiceLifetimes_DbContextIsScoped()
{
    // Validates: DbContext is Scoped (required for Entity Framework)
}

[Fact]
public void ServiceLifetimes_WebFacadesAreScoped()
{
    // Validates: Web layer services are Scoped
}
```

#### C. Transient Lifetime Tests
```csharp
[Theory]
[InlineData(typeof(TriggerAgent))]
[InlineData(typeof(PlanningAgent))]
[InlineData(typeof(ImplementationAgent))]
public void ServiceLifetimes_AgentsAreTransient(Type agentType)
{
    // Validates: Agents are Transient (new per execution)
    // Ensures no state bleeding between executions
}
```

#### D. Lifetime Appropriateness Tests
```csharp
[Fact]
public void ServiceLifetimes_NoIncorrectSingletonUsingScoped()
{
    // Validates: No Singleton service depends on Scoped services
    // This would cause scoped dependencies to live longer than intended
}

[Fact]
public void ServiceLifetimes_NoIncorrectSingletonUsingTransient()
{
    // Validates: No Singleton service depends on Transient services
    // (Usually OK, but worth validating)
}
```

---

### 7. Test Class: `AgentFrameworkServiceRegistrationTests.cs`

**Purpose**: Validate services registered by `AddAgentFramework()` and agent registry

**Coverage Areas**:

#### A. Agent Registry Tests
```csharp
[Fact]
public void AddAgentFramework_RegistersAgentRegistry()
{
    // Validates agent registry is configured
}

[Fact]
public void AddAgentFramework_DiscoversAllAgentTypes()
{
    // Validates all agent types are discovered and registered
}
```

#### B. Middleware Registration Tests
```csharp
[Fact]
public void AddAgentFramework_RegistersLoggingMiddleware_WhenEnabled()
{
    // Validates LoggingMiddleware is registered when config enables it
}

[Fact]
public void AddAgentFramework_RegistersErrorHandlingMiddleware_WhenEnabled()
{
    // Validates ErrorHandlingMiddleware is registered when config enables it
}

[Fact]
public void AddAgentFramework_RegistersRetryMiddleware_WhenEnabled()
{
    // Validates RetryMiddleware is registered when config enables it
}
```

#### C. Checkpoint Store Tests
```csharp
[Fact]
public void AddAgentFramework_RegistersInMemoryCheckpointRepository()
{
    // Validates ICheckpointRepository → InMemoryCheckpointRepository
}
```

---

### 8. Test Class: `GitPlatformServiceRegistrationTests.cs`

**Purpose**: Validate Git platform provider registrations

**Coverage Areas**:

#### A. Provider Registration Tests
```csharp
[Fact]
public void AddGitPlatformIntegration_RegistersGitHubProvider()
{
    // Validates IGitPlatformProvider → GitHubProvider
}

[Fact]
public void AddGitPlatformIntegration_RegistersAzureDevOpsProvider()
{
    // Validates IGitPlatformProvider → AzureDevOpsProvider
}

[Fact]
public void AddGitPlatformIntegration_RegistersBitbucketProvider()
{
    // Validates IGitPlatformProvider → BitbucketProvider
}

[Fact]
public void AddGitPlatformIntegration_AllProvidersAreScoped()
{
    // Validates all providers are Scoped
}
```

#### B. Git Service Tests
```csharp
[Fact]
public void AddGitPlatformIntegration_RegistersLocalGitService()
{
    // Validates ILocalGitService → LocalGitService
}

[Fact]
public void AddGitPlatformIntegration_RegistersGitPlatformService()
{
    // Validates IGitPlatformService → GitPlatformService
}
```

#### C. Dependency Tests
```csharp
[Fact]
public void AddGitPlatformIntegration_CanResolveGitPlatformService_WithAllProviders()
{
    // Validates GitPlatformService can resolve IEnumerable<IGitPlatformProvider>
}
```

---

## Test Implementation Patterns

### Pattern 1: Basic Service Registration Validation

```csharp
[Fact]
public void AddInfrastructure_RegistersTicketRepository()
{
    // Arrange
    var services = new ServiceCollection();
    var config = CreateTestConfiguration();
    
    // Act
    services.AddInfrastructure(config);
    var provider = services.BuildServiceProvider();
    
    // Assert
    var repository = provider.GetService<ITicketRepository>();
    Assert.NotNull(repository);
    Assert.IsType<TicketRepository>(repository);
}
```

### Pattern 2: Service Lifetime Validation

```csharp
[Fact]
public void AddInfrastructure_RepositoriesAreScoped()
{
    // Arrange
    var services = new ServiceCollection();
    var config = CreateTestConfiguration();
    
    // Act
    services.AddInfrastructure(config);
    
    // Assert - verify Scoped registration
    var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ITicketRepository));
    Assert.NotNull(descriptor);
    Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
}
```

### Pattern 3: Dependency Chain Resolution

```csharp
[Fact]
public void AddInfrastructure_CanResolveTicketService_WithAllDependencies()
{
    // Arrange
    var services = new ServiceCollection();
    var config = CreateTestConfiguration();
    services.AddInfrastructure(config);
    var provider = services.BuildServiceProvider();
    
    // Act & Assert - should not throw
    var service = provider.GetRequiredService<ITicketApplicationService>();
    Assert.NotNull(service);
    
    // Verify it has its dependencies wired
    using var scope = provider.CreateScope();
    var scopedService = scope.ServiceProvider.GetRequiredService<ITicketApplicationService>();
    Assert.NotNull(scopedService);
}
```

### Pattern 4: Multiple Implementations (Provider Pattern)

```csharp
[Fact]
public void AddGitPlatformIntegration_RegistersAllProviders()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddMemoryCache();
    services.AddLogging();
    
    // Act
    services.AddGitPlatformIntegration();
    var provider = services.BuildServiceProvider();
    
    // Assert
    var providers = provider.GetRequiredService<IEnumerable<IGitPlatformProvider>>();
    Assert.NotEmpty(providers);
    Assert.True(providers.Any(p => p is GitHubProvider));
    Assert.True(providers.Any(p => p is AzureDevOpsProvider));
    Assert.True(providers.Any(p => p is BitbucketProvider));
}
```

### Pattern 5: Configuration-Dependent Registration

```csharp
[Fact]
public void AddInfrastructure_RegistersEncryption_WhenKeyProvided()
{
    // Arrange
    var services = new ServiceCollection();
    var config = CreateTestConfiguration("valid-encryption-key");
    
    // Act
    services.AddInfrastructure(config);
    var provider = services.BuildServiceProvider();
    
    // Assert
    var encryption = provider.GetService<IEncryptionService>();
    Assert.NotNull(encryption);
}

[Fact]
public void AddInfrastructure_ThrowsValidationError_WhenEncryptionKeyMissing()
{
    // Arrange
    var services = new ServiceCollection();
    var config = CreateTestConfiguration(encryptionKey: null);
    
    // Act & Assert
    Assert.Throws<InvalidOperationException>(() =>
    {
        services.AddInfrastructure(config);
    });
}
```

---

## Mock Configuration Helpers

Create a helper class to provide test configurations:

```csharp
public static class TestConfigurationBuilder
{
    public static IConfiguration CreateTestConfiguration(
        string? encryptionKey = "test-key-valid-base64-encoded",
        string? connectionString = null,
        bool enableSensitiveLogging = false,
        bool enableDetailedErrors = false)
    {
        var configDict = new Dictionary<string, string?>
        {
            { "Encryption:Key", encryptionKey },
            { "ConnectionStrings:DefaultConnection", connectionString ?? "Data Source=:memory:" },
            { "Logging:EnableSensitiveDataLogging", enableSensitiveLogging.ToString() },
            { "Logging:EnableDetailedErrors", enableDetailedErrors.ToString() },
            { "AgentHost:Enabled", "true" },
            { "ClaudeCodeCli:Path", "/usr/local/bin/claude" },
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();
    }

    public static IServiceCollection CreateTestServiceCollection()
    {
        return new ServiceCollection()
            .AddLogging();
    }
}
```

---

## Assertions and Validation Methods

Create a helper class for common assertions:

```csharp
public static class DIAssertions
{
    public static void AssertServiceRegistered<TInterface>(
        IServiceCollection services,
        Type? expectedImplementation = null)
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(TInterface));
        Assert.NotNull(descriptor);

        if (expectedImplementation != null)
        {
            Assert.Equal(expectedImplementation, descriptor!.ImplementationType);
        }
    }

    public static void AssertServiceResolvable<TInterface>(IServiceProvider provider)
    {
        var service = provider.GetService<TInterface>();
        Assert.NotNull(service);
    }

    public static void AssertServiceLifetime<TInterface>(
        IServiceCollection services,
        ServiceLifetime expectedLifetime)
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(TInterface));
        Assert.NotNull(descriptor);
        Assert.Equal(expectedLifetime, descriptor!.Lifetime);
    }

    public static void AssertAllProvidersRegistered(
        IServiceProvider provider,
        params Type[] providerTypes)
    {
        var providers = provider.GetRequiredService<IEnumerable<IGitPlatformProvider>>();
        foreach (var type in providerTypes)
        {
            Assert.True(
                providers.Any(p => p.GetType() == type),
                $"Provider {type.Name} not registered");
        }
    }

    public static void AssertNoDependencyCycle(IServiceProvider provider, Type serviceType)
    {
        // Complex check: attempt to resolve and verify no circular dependencies
        Assert.NotThrows(() =>
        {
            using var scope = provider.CreateScope();
            scope.ServiceProvider.GetRequiredService(serviceType);
        });
    }
}
```

---

## Test Execution Strategy

### Phase 1: Infrastructure Services (Foundation)
1. Implement `DIValidationTestBase.cs`
2. Implement `InfrastructureServiceRegistrationTests.cs`
3. Run against current code
4. Fix any failures in DependencyInjection.cs

### Phase 2: Web Application
1. Implement `WebServiceRegistrationTests.cs`
2. Run against Web Program.cs
3. Add infrastructure registration to Web if not present

### Phase 3: Agent Framework and Git Services
1. Implement `AgentFrameworkServiceRegistrationTests.cs`
2. Implement `GitPlatformServiceRegistrationTests.cs`
3. Run against Agents and Git DependencyInjection

### Phase 4: Service Lifetime Validation
1. Implement `ServiceLifetimeValidationTests.cs`
2. Run comprehensive lifetime checks across all services
3. Fix any lifetime violations

### Phase 5: Future Applications (API and Worker)
1. Implement `ApiServiceRegistrationTests.cs`
2. Implement `WorkerServiceRegistrationTests.cs`
3. These will fail initially (by design) since registration is pending
4. Mark with `[Fact(Skip = "...")]` until services are registered

---

## Expected Test Results

### Infrastructure Tests
- **Expected Passes**: ~45 tests
- **Coverage**: All repositories, application services, agents, encryption, DbContext

### Web Tests
- **Expected Passes**: ~10 tests
- **Coverage**: Web facades, SignalR, DbSeeder, dependency chains

### Agent Framework Tests
- **Expected Passes**: ~8 tests
- **Coverage**: Registry, middleware, checkpoint store

### Git Platform Tests
- **Expected Passes**: ~7 tests
- **Coverage**: All platform providers, local git service, facade

### Service Lifetime Tests
- **Expected Passes**: ~15 tests
- **Coverage**: Appropriate lifetimes across all service types

### API/Worker Tests
- **Expected Passes**: ~5 tests (initially skipped)
- **Status**: Will be enabled when registration is complete

**Total Expected Tests**: ~90+ tests

---

## Benefits of This Approach

1. **Early Detection**: Catches missing or misconfigured registrations at test time, not runtime
2. **Prevents Breaking Changes**: Tests fail immediately if someone removes a registration
3. **Documents Intent**: Tests serve as executable documentation of what services are available
4. **Simplifies Debugging**: If an endpoint returns "service not found" error, tests pinpoint the exact registration
5. **Enables Refactoring**: Safe to refactor service implementations knowing tests validate registrations
6. **Reduces Deployment Risk**: Zero chance of deploying an application with missing registrations
7. **Scalability**: New services added to DI must pass registration tests
8. **Configuration Validation**: Tests verify configuration required services are properly accessible

---

## Future Enhancements

1. **Analyzer Tool**: Create a tool to automatically scan assemblies and generate test stubs
2. **Dependency Visualizer**: Generate dependency graphs showing service chains
3. **Lifetime Validator**: Automatic check for inappropriate lifetime combinations
4. **Integration Tests**: Test actual resolution with real dependencies (not mocks)
5. **Performance Tests**: Ensure service resolution doesn't exceed acceptable thresholds
6. **Configuration Validator**: Validate all configuration sections are accessible and correctly typed

---

## Files to Create/Modify

### New Files
- `/tests/PRFactory.Tests/DependencyInjection/DIValidationTestBase.cs`
- `/tests/PRFactory.Tests/DependencyInjection/InfrastructureServiceRegistrationTests.cs`
- `/tests/PRFactory.Tests/DependencyInjection/WebServiceRegistrationTests.cs`
- `/tests/PRFactory.Tests/DependencyInjection/ApiServiceRegistrationTests.cs`
- `/tests/PRFactory.Tests/DependencyInjection/WorkerServiceRegistrationTests.cs`
- `/tests/PRFactory.Tests/DependencyInjection/ServiceLifetimeValidationTests.cs`
- `/tests/PRFactory.Tests/DependencyInjection/AgentFrameworkServiceRegistrationTests.cs`
- `/tests/PRFactory.Tests/DependencyInjection/GitPlatformServiceRegistrationTests.cs`
- `/tests/PRFactory.Tests/DependencyInjection/TestConfigurationBuilder.cs`
- `/tests/PRFactory.Tests/DependencyInjection/DIAssertions.cs`

### Modified Files
- None required (all tests are additive)

---

## Running the Tests

```bash
# Run all DI validation tests
dotnet test tests/PRFactory.Tests --filter "Category=DependencyInjection"

# Run infrastructure tests only
dotnet test tests/PRFactory.Tests/DependencyInjection/InfrastructureServiceRegistrationTests.cs -v

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Generate coverage report
dotnet test /p:CollectCoverage=true /p:CoverageFileName=di-coverage.xml
```

---

## Success Criteria

- All infrastructure service registrations have corresponding validation tests
- All web application services have corresponding validation tests  
- Service lifetime tests validate no inappropriate singleton/scoped/transient combinations
- Configuration-dependent registrations are validated with both success and error paths
- Dependency chain resolution tests verify complete wiring for critical services
- Test execution time < 5 seconds for full suite (fast feedback)
- Code coverage for service registrations > 95%
- Tests pass on all three application entry points (Web, Api, Worker)

