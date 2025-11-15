using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.ValueObjects;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Agents.Planning;
using Xunit;

namespace PRFactory.Tests.Agents.Planning;

public class ArchitectDbSchemaAgentTests
{
    private readonly Mock<ICliAgent> _mockCliAgent;
    private readonly Mock<IArchitectureContextService> _mockArchContextService;
    private readonly Mock<ILogger<ArchitectDbSchemaAgent>> _mockLogger;
    private readonly ArchitectDbSchemaAgent _agent;

    public ArchitectDbSchemaAgentTests()
    {
        _mockCliAgent = new Mock<ICliAgent>();
        _mockArchContextService = new Mock<IArchitectureContextService>();
        _mockLogger = new Mock<ILogger<ArchitectDbSchemaAgent>>();

        // Setup Epic 07 service mocks
        _mockArchContextService.Setup(x => x.GetArchitecturePatternsAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Clean Architecture patterns...");

        _mockArchContextService.Setup(x => x.GetTechnologyStack())
            .Returns(".NET 10, Blazor Server...");

        _mockArchContextService.Setup(x => x.GetCodeStyleGuidelines())
            .Returns("UTF-8 without BOM, file-scoped namespaces...");

        _mockArchContextService.Setup(x => x.GetRelevantCodeSnippetsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CodeSnippet>
            {
                new() { FilePath = "src/Example.cs", Language = "csharp", Code = "public class Example {}" }
            });

        _agent = new ArchitectDbSchemaAgent(_mockLogger.Object, _mockCliAgent.Object, _mockArchContextService.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ValidContext_ReturnsDatabaseSchema()
    {
        // Arrange
        var context = CreateValidContext();
        context.State["UserStories"] = "# User Stories\n## Story 1";

        var mockResponse = CreateMockDatabaseSchemaResponse();

        _mockCliAgent
            .Setup(x => x.ExecuteWithProjectContextAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        Assert.True(context.State.ContainsKey("DatabaseSchema"));
        Assert.NotNull(context.State["DatabaseSchema"]);

        var dbSchema = context.State["DatabaseSchema"] as string;
        Assert.Contains("CREATE TABLE", dbSchema);
    }

    [Fact]
    public async Task ExecuteAsync_MissingUserStories_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = CreateValidContext();
        // UserStories not added to context

        // Act & Assert
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);
        Assert.Equal(AgentStatus.Failed, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_CliAgentFails_ReturnsFailedStatus()
    {
        // Arrange
        var context = CreateValidContext();
        context.State["UserStories"] = "# User Stories";

        var failedResponse = new CliAgentResponse
        {
            Success = false,
            ErrorMessage = "Schema generation failed"
        };

        _mockCliAgent
            .Setup(x => x.ExecuteWithProjectContextAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedResponse);

        // Act
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Failed, result.Status);
        Assert.Contains("Schema generation failed", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_ContainsDangerousDropDatabase_ReturnsFailedStatus()
    {
        // Arrange
        var context = CreateValidContext();
        context.State["UserStories"] = "# User Stories";

        var dangerousResponse = new CliAgentResponse
        {
            Success = true,
            Content = "DROP DATABASE mydb; CREATE TABLE Users (Id INT);"
        };

        _mockCliAgent
            .Setup(x => x.ExecuteWithProjectContextAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(dangerousResponse);

        // Act
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Failed, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_MissingCreateAlterStatements_ReturnsFailedStatus()
    {
        // Arrange
        var context = CreateValidContext();
        context.State["UserStories"] = "# User Stories";

        var invalidResponse = new CliAgentResponse
        {
            Success = true,
            Content = "Just some commentary about the schema"
        };

        _mockCliAgent
            .Setup(x => x.ExecuteWithProjectContextAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(invalidResponse);

        // Act
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Failed, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_ExtractsSqlFromCodeBlock()
    {
        // Arrange
        var context = CreateValidContext();
        context.State["UserStories"] = "# User Stories";

        var responseWithCodeBlock = new CliAgentResponse
        {
            Success = true,
            Content = @"Here is the database schema:

```sql
CREATE TABLE Users (
    Id INT PRIMARY KEY,
    Email VARCHAR(255) NOT NULL,
    CreatedAt DATETIME NOT NULL
);
```"
        };

        _mockCliAgent
            .Setup(x => x.ExecuteWithProjectContextAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseWithCodeBlock);

        // Act
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        var dbSchema = context.State["DatabaseSchema"] as string;
        Assert.Contains("CREATE TABLE Users", dbSchema);
        Assert.DoesNotContain("Here is the database schema:", dbSchema);
        Assert.DoesNotContain("```sql", dbSchema);
    }

    [Fact]
    public async Task ExecuteAsync_CountsTablesCorrectly()
    {
        // Arrange
        var context = CreateValidContext();
        context.State["UserStories"] = "# User Stories";

        var mockResponse = CreateMockDatabaseSchemaResponse();

        _mockCliAgent
            .Setup(x => x.ExecuteWithProjectContextAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        Assert.True(result.Output.ContainsKey("TableCount"));
        Assert.True((int)result.Output["TableCount"] > 0);
    }

    private AgentContext CreateValidContext()
    {
        var tenant = Tenant.Create("Test Tenant", "Personal", "test-id", "https://test.atlassian.net", "test-token", "test-key");
        var repository = Repository.Create(
            tenant.Id,
            "Test Repo",
            "GitHub",
            "https://github.com/test/repo.git",
            "access-token");

        var ticket = Ticket.Create("PROJ-123", tenant.Id, repository.Id);
        ticket.UpdateTicketInfo("Add user authentication", "Implement OAuth 2.0 authentication");

        return new AgentContext
        {
            TicketId = ticket.Id.ToString(),
            TenantId = tenant.Id.ToString(),
            RepositoryId = repository.Id.ToString(),
            Ticket = ticket,
            Tenant = tenant,
            Repository = repository,
            RepositoryPath = "/tmp/repo",
            State = new Dictionary<string, object>()
        };
    }

    private CliAgentResponse CreateMockDatabaseSchemaResponse()
    {
        return new CliAgentResponse
        {
            Success = true,
            Content = @"CREATE TABLE Users (
    Id uniqueidentifier PRIMARY KEY,
    Email VARCHAR(255) NOT NULL UNIQUE,
    GoogleId VARCHAR(255),
    DisplayName VARCHAR(255),
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME
);

CREATE TABLE AuthTokens (
    Id uniqueidentifier PRIMARY KEY,
    UserId uniqueidentifier NOT NULL,
    Token VARCHAR(500) NOT NULL,
    ExpiresAt DATETIME NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE INDEX IX_AuthTokens_UserId ON AuthTokens(UserId);
CREATE INDEX IX_AuthTokens_ExpiresAt ON AuthTokens(ExpiresAt);",
            Metadata = new Dictionary<string, object>
            {
                ["tokens_used"] = 1800
            }
        };
    }
}
