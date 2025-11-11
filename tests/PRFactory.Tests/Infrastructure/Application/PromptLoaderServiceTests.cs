using Microsoft.Extensions.Configuration;
using Moq;
using PRFactory.Infrastructure.Application;
using Xunit;

namespace PRFactory.Tests.Infrastructure.Application;

/// <summary>
/// Comprehensive tests for PromptLoaderService covering:
/// - Prompt template loading from file system
/// - Handlebars template rendering with variables
/// - Custom helpers (code, truncate, filesize)
/// - Template variable substitution
/// - Error handling for missing templates
/// </summary>
public class PromptLoaderServiceTests : IDisposable
{
    private readonly string _testPromptsPath;
    private readonly IConfiguration _configuration;

    public PromptLoaderServiceTests()
    {
        // Create temporary directory for test prompts
        _testPromptsPath = Path.Combine(Path.GetTempPath(), "prfactory-test-prompts-" + Guid.NewGuid());
        Directory.CreateDirectory(_testPromptsPath);

        // Configure to use test prompts path
        var configDict = new Dictionary<string, string?>
        {
            { "Prompts:BasePath", _testPromptsPath }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        // Create test prompt files
        SetupTestPromptFiles();
    }

    private void SetupTestPromptFiles()
    {
        // Create test directory structure: /prompts/code-review/anthropic/
        var anthropicDir = Path.Combine(_testPromptsPath, "code-review", "anthropic");
        Directory.CreateDirectory(anthropicDir);

        // Create system prompt
        var systemPrompt = "You are a code review expert.";
        File.WriteAllText(Path.Combine(anthropicDir, "system.txt"), systemPrompt);

        // Create user template with Handlebars variables
        var userTemplate = @"Review the following PR:

Ticket: {{ticket_number}}
Title: {{ticket_title}}
Files changed: {{files_changed_count}}

{{#each file_changes}}
- {{this.path}} (+{{this.lines_added}} -{{this.lines_deleted}})
{{/each}}

{{#if has_tests}}
Tests included: Yes
{{else}}
Tests included: No
{{/if}}";

        File.WriteAllText(Path.Combine(anthropicDir, "user_template.hbs"), userTemplate);
    }

    private PromptLoaderService CreateService()
    {
        return new PromptLoaderService(_configuration);
    }

    public void Dispose()
    {
        // Clean up test prompts directory
        if (Directory.Exists(_testPromptsPath))
        {
            Directory.Delete(_testPromptsPath, recursive: true);
        }
    }

    #region LoadPrompt Tests

    [Fact]
    public void LoadPrompt_WithValidPath_ReturnsPromptContent()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.LoadPrompt("code-review", "anthropic", "system");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("You are a code review expert.", result);
    }

    [Fact]
    public void LoadPrompt_WithInvalidPath_ThrowsFileNotFoundException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        var exception = Assert.Throws<FileNotFoundException>(
            () => service.LoadPrompt("nonexistent-agent", "anthropic", "system"));

        Assert.Contains("Prompt template not found", exception.Message);
        Assert.Contains("nonexistent-agent", exception.Message);
    }

    [Fact]
    public void LoadPrompt_CaseInsensitive_ReturnsContent()
    {
        // Arrange
        var service = CreateService();

        // Act - Use uppercase agent and provider names
        var result = service.LoadPrompt("CODE-REVIEW", "ANTHROPIC", "system");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("You are a code review expert.", result);
    }

    #endregion

    #region RenderTemplate Tests

    [Fact]
    public void RenderTemplate_WithSimpleVariables_SubstitutesCorrectly()
    {
        // Arrange
        var service = CreateService();
        var variables = new
        {
            ticket_number = "TEST-123",
            ticket_title = "Implement feature X",
            files_changed_count = 5,
            file_changes = new List<object>(),
            has_tests = false
        };

        // Act
        var result = service.RenderTemplate("code-review", "anthropic", "user_template", variables);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Ticket: TEST-123", result);
        Assert.Contains("Title: Implement feature X", result);
        Assert.Contains("Files changed: 5", result);
        Assert.Contains("Tests included: No", result);
    }

    [Fact]
    public void RenderTemplate_WithArrayIteration_RendersEachItem()
    {
        // Arrange
        var service = CreateService();
        var variables = new
        {
            ticket_number = "TEST-123",
            ticket_title = "Implement feature X",
            files_changed_count = 3,
            file_changes = new[]
            {
                new { path = "src/Service.cs", lines_added = 50, lines_deleted = 10 },
                new { path = "src/Controller.cs", lines_added = 30, lines_deleted = 5 },
                new { path = "tests/ServiceTests.cs", lines_added = 100, lines_deleted = 0 }
            },
            has_tests = true
        };

        // Act
        var result = service.RenderTemplate("code-review", "anthropic", "user_template", variables);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("- src/Service.cs (+50 -10)", result);
        Assert.Contains("- src/Controller.cs (+30 -5)", result);
        Assert.Contains("- tests/ServiceTests.cs (+100 -0)", result);
        Assert.Contains("Tests included: Yes", result);
    }

    [Fact]
    public void RenderTemplate_WithConditionalBlocks_RendersCorrectly()
    {
        // Arrange
        var service = CreateService();
        var variablesWithTests = new
        {
            ticket_number = "TEST-123",
            ticket_title = "Implement feature X",
            files_changed_count = 2,
            file_changes = new List<object>(),
            has_tests = true
        };

        var variablesWithoutTests = new
        {
            ticket_number = "TEST-124",
            ticket_title = "Fix bug Y",
            files_changed_count = 1,
            file_changes = new List<object>(),
            has_tests = false
        };

        // Act
        var resultWithTests = service.RenderTemplate("code-review", "anthropic", "user_template", variablesWithTests);
        var resultWithoutTests = service.RenderTemplate("code-review", "anthropic", "user_template", variablesWithoutTests);

        // Assert
        Assert.Contains("Tests included: Yes", resultWithTests);
        Assert.Contains("Tests included: No", resultWithoutTests);
    }

    [Fact]
    public void RenderTemplate_WithMissingTemplate_ThrowsFileNotFoundException()
    {
        // Arrange
        var service = CreateService();
        var variables = new { ticket_number = "TEST-123" };

        // Act & Assert
        var exception = Assert.Throws<FileNotFoundException>(
            () => service.RenderTemplate("nonexistent-agent", "anthropic", "user_template", variables));

        Assert.Contains("Template not found", exception.Message);
    }

    #endregion

    #region Custom Helper Tests

    [Fact]
    public void CustomHelper_Code_FormatsCodeBlock()
    {
        // Arrange
        var service = CreateService();

        // Create a template that uses the code helper
        var templateDir = Path.Combine(_testPromptsPath, "test-agent", "anthropic");
        Directory.CreateDirectory(templateDir);

        var templateContent = @"Example code:
{{code ""csharp"" code_content}}";

        File.WriteAllText(Path.Combine(templateDir, "code_test.hbs"), templateContent);

        var variables = new
        {
            code_content = "public class Example { }"
        };

        // Act
        var result = service.RenderTemplate("test-agent", "anthropic", "code_test", variables);

        // Assert
        Assert.Contains("```csharp", result);
        Assert.Contains("public class Example { }", result);
        Assert.Contains("```", result);
    }

    [Fact]
    public void CustomHelper_Truncate_TruncatesLongText()
    {
        // Arrange
        var service = CreateService();

        // Create a template that uses the truncate helper
        var templateDir = Path.Combine(_testPromptsPath, "test-agent", "anthropic");
        Directory.CreateDirectory(templateDir);

        var templateContent = @"Description: {{truncate description 50}}";

        File.WriteAllText(Path.Combine(templateDir, "truncate_test.hbs"), templateContent);

        var longText = new string('a', 100);
        var variables = new
        {
            description = longText
        };

        // Act
        var result = service.RenderTemplate("test-agent", "anthropic", "truncate_test", variables);

        // Assert
        Assert.Contains("...", result);
        Assert.True(result.Length < 100); // Should be truncated
    }

    [Fact]
    public void CustomHelper_Truncate_DoesNotTruncateShortText()
    {
        // Arrange
        var service = CreateService();

        // Create a template that uses the truncate helper
        var templateDir = Path.Combine(_testPromptsPath, "test-agent", "anthropic");
        Directory.CreateDirectory(templateDir);

        var templateContent = @"Description: {{truncate description 100}}";

        File.WriteAllText(Path.Combine(templateDir, "truncate_test2.hbs"), templateContent);

        var shortText = "This is a short description.";
        var variables = new
        {
            description = shortText
        };

        // Act
        var result = service.RenderTemplate("test-agent", "anthropic", "truncate_test2", variables);

        // Assert
        Assert.Contains(shortText, result);
        Assert.DoesNotContain("...", result);
    }

    [Fact]
    public void CustomHelper_FileSize_FormatsBytes()
    {
        // Arrange
        var service = CreateService();

        // Create a template that uses the filesize helper
        var templateDir = Path.Combine(_testPromptsPath, "test-agent", "anthropic");
        Directory.CreateDirectory(templateDir);

        var templateContent = @"File size: {{filesize size_in_bytes}}";

        File.WriteAllText(Path.Combine(templateDir, "filesize_test.hbs"), templateContent);

        // Test different sizes
        var testCases = new[]
        {
            new { bytes = 500L, expected = "B" },
            new { bytes = 2048L, expected = "KB" },
            new { bytes = 5242880L, expected = "MB" },
            new { bytes = 1073741824L, expected = "GB" }
        };

        foreach (var testCase in testCases)
        {
            var variables = new { size_in_bytes = testCase.bytes };

            // Act
            var result = service.RenderTemplate("test-agent", "anthropic", "filesize_test", variables);

            // Assert
            Assert.Contains(testCase.expected, result);
        }
    }

    #endregion
}
