using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.ValueObjects;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Agents.Planning;
using Xunit;

namespace PRFactory.Tests.Agents.Planning;

public class QaTestCasesAgentTests
{
    private readonly Mock<ICliAgent> _mockCliAgent;
    private readonly Mock<ILogger<QaTestCasesAgent>> _mockLogger;
    private readonly QaTestCasesAgent _agent;

    public QaTestCasesAgentTests()
    {
        _mockCliAgent = new Mock<ICliAgent>();
        _mockLogger = new Mock<ILogger<QaTestCasesAgent>>();
        _agent = new QaTestCasesAgent(_mockLogger.Object, _mockCliAgent.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ValidContext_ReturnsTestCases()
    {
        // Arrange
        var context = CreateValidContext();
        context.State["UserStories"] = "# User Stories";
        context.State["ApiDesign"] = "openapi: 3.0.0";
        context.State["DatabaseSchema"] = "CREATE TABLE Users";

        var mockResponse = CreateMockTestCasesResponse();

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
        Assert.True(context.State.ContainsKey("TestCases"));
        Assert.NotNull(context.State["TestCases"]);

        var testCases = context.State["TestCases"] as string;
        Assert.Contains("# Test Cases", testCases);
        Assert.Contains("### Test Case", testCases);
    }

    [Fact]
    public async Task ExecuteAsync_MissingUserStories_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = CreateValidContext();
        context.State["ApiDesign"] = "openapi: 3.0.0";
        context.State["DatabaseSchema"] = "CREATE TABLE Users";

        // Act & Assert
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);
        Assert.Equal(AgentStatus.Failed, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_MissingApiDesign_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = CreateValidContext();
        context.State["UserStories"] = "# User Stories";
        context.State["DatabaseSchema"] = "CREATE TABLE Users";

        // Act & Assert
        var result = await _agent.ExecuteWithMiddlewareAsync(context, CancellationToken.None);
        Assert.Equal(AgentStatus.Failed, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_MissingDatabaseSchema_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = CreateValidContext();
        context.State["UserStories"] = "# User Stories";
        context.State["ApiDesign"] = "openapi: 3.0.0";

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
        context.State["ApiDesign"] = "openapi: 3.0.0";
        context.State["DatabaseSchema"] = "CREATE TABLE Users";

        var failedResponse = new CliAgentResponse
        {
            Success = false,
            ErrorMessage = "Test case generation failed"
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
        Assert.Contains("Test case generation failed", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidTestCasesFormat_ReturnsFailedStatus()
    {
        // Arrange
        var context = CreateValidContext();
        context.State["UserStories"] = "# User Stories";
        context.State["ApiDesign"] = "openapi: 3.0.0";
        context.State["DatabaseSchema"] = "CREATE TABLE Users";

        var invalidResponse = new CliAgentResponse
        {
            Success = true,
            Content = "Invalid content without test cases format"
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
    public async Task ExecuteAsync_ExtractsTestCasesFromMarkdownCodeBlock()
    {
        // Arrange
        var context = CreateValidContext();
        context.State["UserStories"] = "# User Stories";
        context.State["ApiDesign"] = "openapi: 3.0.0";
        context.State["DatabaseSchema"] = "CREATE TABLE Users";

        var responseWithCodeBlock = new CliAgentResponse
        {
            Success = true,
            Content = @"Here are the test cases:

```markdown
# Test Cases

### Test Case 1: User Login Success
**Description**: Test successful user login
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
        var testCases = context.State["TestCases"] as string;
        Assert.Contains("# Test Cases", testCases);
        Assert.DoesNotContain("Here are the test cases:", testCases);
        Assert.DoesNotContain("```markdown", testCases);
    }

    [Fact]
    public async Task ExecuteAsync_CountsTestCasesCorrectly()
    {
        // Arrange
        var context = CreateValidContext();
        context.State["UserStories"] = "# User Stories";
        context.State["ApiDesign"] = "openapi: 3.0.0";
        context.State["DatabaseSchema"] = "CREATE TABLE Users";

        var mockResponse = CreateMockTestCasesResponse();

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
        Assert.True(result.Output.ContainsKey("TestCaseCount"));
        Assert.True((int)result.Output["TestCaseCount"] > 0);
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

    private CliAgentResponse CreateMockTestCasesResponse()
    {
        return new CliAgentResponse
        {
            Success = true,
            Content = @"# Test Cases

## Unit Tests

### Test Case 1: User Registration - Valid Data
**Category**: Unit Test
**Priority**: High
**Description**: Verify user can register with valid email and OAuth credentials

**Preconditions**:
- Database is accessible
- OAuth provider is configured

**Test Steps**:
1. Call registration endpoint with valid user data
2. Verify user is created in database
3. Verify JWT token is returned

**Expected Result**:
- HTTP 201 Created status
- User record exists in Users table
- Valid JWT token returned

### Test Case 2: User Registration - Duplicate Email
**Category**: Unit Test
**Priority**: High
**Description**: Verify system rejects duplicate email registrations

**Preconditions**:
- User with test@example.com already exists

**Test Steps**:
1. Attempt to register with email test@example.com
2. Verify error response

**Expected Result**:
- HTTP 409 Conflict status
- Error message indicates duplicate email

## Integration Tests

### Test Case 3: End-to-End OAuth Flow
**Category**: Integration Test
**Priority**: High
**Description**: Verify complete OAuth authentication flow

**Test Steps**:
1. Initiate OAuth flow
2. Simulate OAuth provider callback
3. Verify JWT token generation
4. Verify user session creation

**Expected Result**:
- User successfully authenticated
- Valid session created
- JWT token contains correct claims",
            Metadata = new Dictionary<string, object>
            {
                ["tokens_used"] = 2200
            }
        };
    }
}
