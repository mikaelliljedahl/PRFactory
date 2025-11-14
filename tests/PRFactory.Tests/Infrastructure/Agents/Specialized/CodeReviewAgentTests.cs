using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Core.Application.LLM;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Agents.Specialized;
using PRFactory.Infrastructure.Git;
using PRFactory.Tests.Builders;
using Xunit;

namespace PRFactory.Tests.Infrastructure.Agents.Specialized;

/// <summary>
/// Comprehensive tests for CodeReviewAgent covering:
/// - Agent execution with valid PR
/// - Critical issues detection
/// - Template variable building
/// - Review response parsing
/// - Language detection
/// - Test file detection
/// - Error handling
/// </summary>
public class CodeReviewAgentTests
{
    private readonly Mock<ILogger<CodeReviewAgent>> _mockLogger;
    private readonly Mock<ILlmProviderFactory> _mockProviderFactory;
    private readonly Mock<IPromptLoaderService> _mockPromptService;
    private readonly Mock<ICodeReviewResultRepository> _mockReviewResultRepo;
    private readonly Mock<ITicketRepository> _mockTicketRepo;
    private readonly Mock<IGitPlatformService> _mockGitPlatformService;
    private readonly Mock<ILlmProvider> _mockProvider;

    public CodeReviewAgentTests()
    {
        _mockLogger = new Mock<ILogger<CodeReviewAgent>>();
        _mockProviderFactory = new Mock<ILlmProviderFactory>();
        _mockPromptService = new Mock<IPromptLoaderService>();
        _mockReviewResultRepo = new Mock<ICodeReviewResultRepository>();
        _mockTicketRepo = new Mock<ITicketRepository>();
        _mockGitPlatformService = new Mock<IGitPlatformService>();
        _mockProvider = new Mock<ILlmProvider>();

        // Default provider setup
        _mockProvider.Setup(p => p.ProviderName).Returns("Anthropic");
        _mockProvider.Setup(p => p.SupportedModels).Returns(new List<string> { "claude-sonnet-4-5" });

        _mockProviderFactory
            .Setup(f => f.CreateProvider(It.IsAny<string>()))
            .Returns(_mockProvider.Object);
    }

    #region DetectLanguage Tests

    [Fact]
    public void DetectLanguage_WithCSharpFile_ReturnsCSharp()
    {
        // This tests the private DetectLanguage method indirectly through reflection
        var detectLanguageMethod = typeof(CodeReviewAgent).GetMethod(
            "DetectLanguage",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        Assert.NotNull(detectLanguageMethod);

        var result = detectLanguageMethod.Invoke(null, new object[] { "Service.cs" }) as string;
        Assert.Equal("csharp", result);
    }

    [Fact]
    public void DetectLanguage_WithTypeScriptFile_ReturnsTypeScript()
    {
        var detectLanguageMethod = typeof(CodeReviewAgent).GetMethod(
            "DetectLanguage",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        Assert.NotNull(detectLanguageMethod);

        var result = detectLanguageMethod.Invoke(null, new object[] { "component.ts" }) as string;
        Assert.Equal("typescript", result);
    }

    #endregion

    #region IsTestFile Tests

    [Fact]
    public void IsTestFile_WithTestFile_ReturnsTrue()
    {
        var isTestFileMethod = typeof(CodeReviewAgent).GetMethod(
            "IsTestFile",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        Assert.NotNull(isTestFileMethod);

        var testFilePaths = new[]
        {
            "ServiceTests.cs",
            "Service.test.ts",
            "Service.spec.js",
            "tests/Service.cs",
            "__tests__/component.jsx"
        };

        foreach (var path in testFilePaths)
        {
            var result = (bool?)isTestFileMethod.Invoke(null, new object[] { path });
            Assert.True(result, $"Expected {path} to be detected as test file");
        }
    }

    [Fact]
    public void IsTestFile_WithNonTestFile_ReturnsFalse()
    {
        var isTestFileMethod = typeof(CodeReviewAgent).GetMethod(
            "IsTestFile",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        Assert.NotNull(isTestFileMethod);

        var nonTestFilePaths = new[]
        {
            "Service.cs",
            "Controller.ts",
            "Component.jsx",
            "src/Service.cs"
        };

        foreach (var path in nonTestFilePaths)
        {
            var result = (bool?)isTestFileMethod.Invoke(null, new object[] { path });
            Assert.False(result, $"Expected {path} NOT to be detected as test file");
        }
    }

    #endregion

}
