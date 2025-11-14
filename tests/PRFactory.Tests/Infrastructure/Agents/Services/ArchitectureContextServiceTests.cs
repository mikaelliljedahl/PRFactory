using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Core.Application.Services;
using PRFactory.Infrastructure.Agents.Services;

namespace PRFactory.Tests.Infrastructure.Agents.Services;

public class ArchitectureContextServiceTests : IDisposable
{
    private readonly Mock<ILogger<ArchitectureContextService>> _mockLogger;
    private readonly ArchitectureContextService _service;
    private readonly string _testRepositoryPath;

    public ArchitectureContextServiceTests()
    {
        _mockLogger = new Mock<ILogger<ArchitectureContextService>>();
        _service = new ArchitectureContextService(_mockLogger.Object);

        // Create a temporary test directory
        _testRepositoryPath = Path.Combine(Path.GetTempPath(), "PRFactory_Tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testRepositoryPath);
    }

    public void Dispose()
    {
        // Clean up test directory
        if (Directory.Exists(_testRepositoryPath))
        {
            Directory.Delete(_testRepositoryPath, recursive: true);
        }
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ArchitectureContextService(null!));
    }

    [Fact]
    public void Constructor_WithValidLogger_CreatesInstance()
    {
        // Act
        var service = new ArchitectureContextService(_mockLogger.Object);

        // Assert
        Assert.NotNull(service);
    }

    #endregion

    #region GetArchitecturePatternsAsync Tests

    [Fact]
    public async Task GetArchitecturePatterns_WithNullPath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetArchitecturePatternsAsync(null!));
    }

    [Fact]
    public async Task GetArchitecturePatterns_WithEmptyPath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetArchitecturePatternsAsync(string.Empty));
    }

    [Fact]
    public async Task GetArchitecturePatterns_WhenArchitectureMdExists_ReturnsFileContent()
    {
        // Arrange
        var docsPath = Path.Combine(_testRepositoryPath, "docs");
        Directory.CreateDirectory(docsPath);

        var archContent = "# Clean Architecture\n\nThis is the architecture documentation.";
        var archFilePath = Path.Combine(docsPath, "ARCHITECTURE.md");
        await File.WriteAllTextAsync(archFilePath, archContent);

        // Act
        var result = await _service.GetArchitecturePatternsAsync(_testRepositoryPath);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Clean Architecture", result);
        Assert.Contains("architecture documentation", result);
    }

    [Fact]
    public async Task GetArchitecturePatterns_WhenArchitectureMdTooLong_TruncatesContent()
    {
        // Arrange
        var docsPath = Path.Combine(_testRepositoryPath, "docs");
        Directory.CreateDirectory(docsPath);

        var longContent = new string('a', 3000); // Longer than 2000 characters
        var archFilePath = Path.Combine(docsPath, "ARCHITECTURE.md");
        await File.WriteAllTextAsync(archFilePath, longContent);

        // Act
        var result = await _service.GetArchitecturePatternsAsync(_testRepositoryPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2003, result.Length); // 2000 + "..." (3 characters)
        Assert.EndsWith("...", result);
    }

    [Fact]
    public async Task GetArchitecturePatterns_WhenArchitectureMdNotFound_ReturnsDefaultPatterns()
    {
        // Act
        var result = await _service.GetArchitecturePatternsAsync(_testRepositoryPath);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Clean Architecture", result);
        Assert.Contains("Domain Layer", result);
        Assert.Contains("Application Layer", result);
        Assert.Contains("Infrastructure Layer", result);
    }

    #endregion

    #region GetTechnologyStack Tests

    [Fact]
    public void GetTechnologyStack_ReturnsStackInformation()
    {
        // Act
        var result = _service.GetTechnologyStack();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains(".NET 10", result);
        Assert.Contains("C# 13", result);
        Assert.Contains("Entity Framework Core", result);
        Assert.Contains("Blazor Server", result);
        Assert.Contains("Radzen.Blazor", result);
    }

    [Fact]
    public void GetTechnologyStack_DoesNotIncludeBlazorWebAssembly()
    {
        // Act
        var result = _service.GetTechnologyStack();

        // Assert - Should clarify it's NOT WebAssembly
        Assert.DoesNotContain("Blazor WebAssembly", result);
        Assert.Contains("Blazor Server", result);
        Assert.Contains("NOT WebAssembly", result);
    }

    #endregion

    #region GetCodeStyleGuidelines Tests

    [Fact]
    public void GetCodeStyleGuidelines_ReturnsGuidelines()
    {
        // Act
        var result = _service.GetCodeStyleGuidelines();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("UTF-8", result);
        Assert.Contains("BOM", result);
        Assert.Contains("File-scoped namespaces", result);
        Assert.Contains("4 spaces", result);
    }

    [Fact]
    public void GetCodeStyleGuidelines_IncludesCriticalRules()
    {
        // Act
        var result = _service.GetCodeStyleGuidelines();

        // Assert
        Assert.Contains("mandatory", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Code-behind pattern", result);
        Assert.Contains("Private setters", result);
    }

    #endregion

    #region GetRelevantCodeSnippetsAsync Tests

    [Fact]
    public async Task GetRelevantCodeSnippets_WithNullPath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetRelevantCodeSnippetsAsync(null!, "test description"));
    }

    [Fact]
    public async Task GetRelevantCodeSnippets_WithEmptyPath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetRelevantCodeSnippetsAsync(string.Empty, "test description"));
    }

    [Fact]
    public async Task GetRelevantCodeSnippets_WhenNoSrcDirectory_ReturnsEmptyList()
    {
        // Act
        var result = await _service.GetRelevantCodeSnippetsAsync(
            _testRepositoryPath,
            "ticket service");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetRelevantCodeSnippets_WithMatchingFiles_ReturnsSnippets()
    {
        // Arrange
        var srcPath = Path.Combine(_testRepositoryPath, "src");
        Directory.CreateDirectory(srcPath);

        var ticketServicePath = Path.Combine(srcPath, "TicketService.cs");
        await File.WriteAllTextAsync(ticketServicePath, @"
public class TicketService
{
    public async Task<Ticket> GetByIdAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }
}");

        // Act
        var result = await _service.GetRelevantCodeSnippetsAsync(
            _testRepositoryPath,
            "ticket service implementation");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Single(result);

        var snippet = result[0];
        Assert.Contains("TicketService.cs", snippet.FilePath);
        Assert.Equal("csharp", snippet.Language);
        Assert.Contains("GetByIdAsync", snippet.Code);
    }

    [Fact]
    public async Task GetRelevantCodeSnippets_LimitsToMaxSnippets()
    {
        // Arrange
        var srcPath = Path.Combine(_testRepositoryPath, "src");
        Directory.CreateDirectory(srcPath);

        // Create 5 ticket-related files
        for (int i = 1; i <= 5; i++)
        {
            var filePath = Path.Combine(srcPath, $"Ticket{i}.cs");
            await File.WriteAllTextAsync(filePath, $"public class Ticket{i} {{ }}");
        }

        // Act
        var result = await _service.GetRelevantCodeSnippetsAsync(
            _testRepositoryPath,
            "ticket management",
            maxSnippets: 3);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count <= 3);
    }

    [Fact]
    public async Task GetRelevantCodeSnippets_SkipsTestFiles()
    {
        // Arrange
        var srcPath = Path.Combine(_testRepositoryPath, "src");
        Directory.CreateDirectory(srcPath);

        var ticketServicePath = Path.Combine(srcPath, "TicketService.cs");
        await File.WriteAllTextAsync(ticketServicePath, "public class TicketService { }");

        var ticketTestPath = Path.Combine(srcPath, "TicketServiceTests.cs");
        await File.WriteAllTextAsync(ticketTestPath, "public class TicketServiceTests { }");

        // Act
        var result = await _service.GetRelevantCodeSnippetsAsync(
            _testRepositoryPath,
            "ticket service");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.DoesNotContain(result, s => s.FilePath.Contains("Tests"));
    }

    [Fact]
    public async Task GetRelevantCodeSnippets_SkipsMigrationFiles()
    {
        // Arrange
        var srcPath = Path.Combine(_testRepositoryPath, "src");
        Directory.CreateDirectory(srcPath);

        var ticketEntityPath = Path.Combine(srcPath, "Ticket.cs");
        await File.WriteAllTextAsync(ticketEntityPath, "public class Ticket { }");

        var migrationPath = Path.Combine(srcPath, "20250114_AddTicketMigration.cs");
        await File.WriteAllTextAsync(migrationPath, "public partial class AddTicketMigration { }");

        // Act
        var result = await _service.GetRelevantCodeSnippetsAsync(
            _testRepositoryPath,
            "ticket entity");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.DoesNotContain(result, s => s.FilePath.Contains("Migration"));
    }

    [Fact]
    public async Task GetRelevantCodeSnippets_LimitsLinesToFifty()
    {
        // Arrange
        var srcPath = Path.Combine(_testRepositoryPath, "src");
        Directory.CreateDirectory(srcPath);

        var lines = new List<string>();
        for (int i = 1; i <= 100; i++)
        {
            lines.Add($"// Line {i}");
        }

        var ticketServicePath = Path.Combine(srcPath, "TicketService.cs");
        await File.WriteAllLinesAsync(ticketServicePath, lines);

        // Act
        var result = await _service.GetRelevantCodeSnippetsAsync(
            _testRepositoryPath,
            "ticket service");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);

        var snippet = result[0];
        var codeLines = snippet.Code.Split('\n');
        Assert.True(codeLines.Length <= 50);
    }

    [Fact]
    public async Task GetRelevantCodeSnippets_PrioritizesDomainEntities()
    {
        // Arrange
        var srcPath = Path.Combine(_testRepositoryPath, "src");
        var domainPath = Path.Combine(srcPath, "Domain", "Entities");
        var appPath = Path.Combine(srcPath, "Application");

        Directory.CreateDirectory(domainPath);
        Directory.CreateDirectory(appPath);

        var ticketEntityPath = Path.Combine(domainPath, "Ticket.cs");
        await File.WriteAllTextAsync(ticketEntityPath, "public class Ticket { }");

        var ticketServicePath = Path.Combine(appPath, "TicketService.cs");
        await File.WriteAllTextAsync(ticketServicePath, "public class TicketService { }");

        // Act
        var result = await _service.GetRelevantCodeSnippetsAsync(
            _testRepositoryPath,
            "ticket implementation",
            maxSnippets: 1);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Contains("Domain", result[0].FilePath);
    }

    [Fact]
    public async Task GetRelevantCodeSnippets_WithNoMatches_ReturnsEmptyList()
    {
        // Arrange
        var srcPath = Path.Combine(_testRepositoryPath, "src");
        Directory.CreateDirectory(srcPath);

        var unrelatedPath = Path.Combine(srcPath, "Unrelated.cs");
        await File.WriteAllTextAsync(unrelatedPath, "public class Unrelated { }");

        // Act
        var result = await _service.GetRelevantCodeSnippetsAsync(
            _testRepositoryPath,
            "ticket service repository");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetRelevantCodeSnippets_ExtractsKeywords()
    {
        // Arrange
        var srcPath = Path.Combine(_testRepositoryPath, "src");
        Directory.CreateDirectory(srcPath);

        var ticketServicePath = Path.Combine(srcPath, "TicketService.cs");
        await File.WriteAllTextAsync(ticketServicePath, "public class TicketService { }");

        var repositoryPath = Path.Combine(srcPath, "TicketRepository.cs");
        await File.WriteAllTextAsync(repositoryPath, "public class TicketRepository { }");

        // Act - Test with keywords in description
        var result = await _service.GetRelevantCodeSnippetsAsync(
            _testRepositoryPath,
            "Create a new ticket using the repository pattern");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.True(result.Count >= 1);
    }

    #endregion

    #region CodeSnippet Model Tests

    [Fact]
    public void CodeSnippet_CanBeCreatedWithDefaults()
    {
        // Act
        var snippet = new CodeSnippet();

        // Assert
        Assert.NotNull(snippet);
        Assert.Equal(string.Empty, snippet.FilePath);
        Assert.Equal("csharp", snippet.Language);
        Assert.Equal(string.Empty, snippet.Code);
        Assert.Null(snippet.Description);
    }

    [Fact]
    public void CodeSnippet_CanSetAllProperties()
    {
        // Arrange
        var snippet = new CodeSnippet
        {
            FilePath = "src/Domain/Ticket.cs",
            Language = "csharp",
            Code = "public class Ticket { }",
            Description = "Domain entity"
        };

        // Assert
        Assert.Equal("src/Domain/Ticket.cs", snippet.FilePath);
        Assert.Equal("csharp", snippet.Language);
        Assert.Equal("public class Ticket { }", snippet.Code);
        Assert.Equal("Domain entity", snippet.Description);
    }

    #endregion
}
