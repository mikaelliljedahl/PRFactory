using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Core.Application.LLM;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Infrastructure.Application;
using Xunit;

namespace PRFactory.Tests.Application;

/// <summary>
/// Unit tests for PlanValidationService
/// </summary>
public class PlanValidationServiceTests
{
    private readonly Mock<IPlanService> _mockPlanService;
    private readonly Mock<IAgentPromptTemplateRepository> _mockPromptTemplateRepo;
    private readonly Mock<ILlmProviderFactory> _mockLlmProviderFactory;
    private readonly Mock<ILlmProvider> _mockLlmProvider;
    private readonly Mock<ILogger<PlanValidationService>> _mockLogger;
    private readonly PlanValidationService _service;

    public PlanValidationServiceTests()
    {
        _mockPlanService = new Mock<IPlanService>();
        _mockPromptTemplateRepo = new Mock<IAgentPromptTemplateRepository>();
        _mockLlmProviderFactory = new Mock<ILlmProviderFactory>();
        _mockLlmProvider = new Mock<ILlmProvider>();
        _mockLogger = new Mock<ILogger<PlanValidationService>>();

        // Setup default LLM provider factory
        _mockLlmProviderFactory
            .Setup(f => f.GetDefaultProvider())
            .Returns(_mockLlmProvider.Object);

        _service = new PlanValidationService(
            _mockPlanService.Object,
            _mockPromptTemplateRepo.Object,
            _mockLlmProviderFactory.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ValidatePlan_WithSecurityCheck_ReturnsValidationResult()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var planContent = "# Implementation Plan\n\nCreate a user authentication system.";
        var llmResponse = @"Risk Level: Medium

Findings:
- Missing rate limiting on login attempts
- No password complexity requirements specified

Recommendations:
- Implement rate limiting (5 attempts per 15 minutes)
- Enforce minimum password complexity (8 chars, uppercase, lowercase, number)

Score: 75";

        SetupValidationMocks(ticketId, planContent, "plan-security-check", llmResponse);

        // Act
        var result = await _service.ValidatePlanAsync(ticketId, "security");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ticketId, result.TicketId);
        Assert.Equal("security", result.CheckType);
        Assert.Equal(75, result.Score);
        Assert.Equal("Medium", result.RiskLevel);
        Assert.Equal(2, result.Findings.Count);
        Assert.Contains("Missing rate limiting on login attempts", result.Findings);
        Assert.Equal(2, result.Recommendations.Count);
        Assert.Contains("Implement rate limiting (5 attempts per 15 minutes)", result.Recommendations);
    }

    [Fact]
    public async Task ValidatePlan_WithCompletenessCheck_ReturnsValidationResult()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var planContent = "# Implementation Plan\n\nBuild a REST API.";
        var llmResponse = @"Completeness Score: 60

Findings:
- No error handling strategy defined
- Missing API versioning approach
- No deployment steps specified

Recommendations:
- Add comprehensive error handling with proper HTTP status codes
- Define API versioning strategy (URL path or header-based)
- Document deployment pipeline and infrastructure requirements

Summary:
The plan covers basic functionality but lacks critical operational details.";

        SetupValidationMocks(ticketId, planContent, "plan-completeness-check", llmResponse);

        // Act
        var result = await _service.ValidatePlanAsync(ticketId, "completeness");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(60, result.Score);
        Assert.Equal(3, result.Findings.Count);
        Assert.Contains("No error handling strategy defined", result.Findings);
        Assert.Equal(3, result.Recommendations.Count);
    }

    [Fact]
    public async Task ValidatePlan_WithPerformanceCheck_ReturnsValidationResult()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var planContent = "# Implementation Plan\n\nCreate a data export feature.";
        var llmResponse = @"Performance Risk Level: High

Findings:
- No pagination for large datasets
- Synchronous processing of file generation
- Missing database query optimization

Recommendations:
- Implement cursor-based pagination for exports
- Use background jobs for large file generation
- Add database indexes for frequently queried fields

Score: 45";

        SetupValidationMocks(ticketId, planContent, "plan-performance-check", llmResponse);

        // Act
        var result = await _service.ValidatePlanAsync(ticketId, "performance");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(45, result.Score);
        Assert.Equal("High", result.RiskLevel);
        Assert.Equal(3, result.Findings.Count);
        Assert.Equal(3, result.Recommendations.Count);
    }

    [Fact]
    public async Task ValidatePlan_WithInvalidCheckType_ThrowsException()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var planContent = "# Plan";

        _mockPlanService
            .Setup(s => s.GetPlanAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlanInfo
            {
                BranchName = "plan/test",
                Content = planContent,
                IsApproved = false
            });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.ValidatePlanAsync(ticketId, "invalid-type"));

        Assert.Contains("Unknown check type: invalid-type", exception.Message);
    }

    [Fact]
    public async Task ValidatePlan_WithNonExistentTicket_ThrowsException()
    {
        // Arrange
        var ticketId = Guid.NewGuid();

        _mockPlanService
            .Setup(s => s.GetPlanAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlanInfo?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ValidatePlanAsync(ticketId, "security"));

        Assert.Contains($"No plan found for ticket {ticketId}", exception.Message);
    }

    [Fact]
    public async Task ValidatePlan_WithMissingPromptTemplate_ThrowsException()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var planContent = "# Plan";

        _mockPlanService
            .Setup(s => s.GetPlanAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlanInfo
            {
                BranchName = "plan/test",
                Content = planContent,
                IsApproved = false
            });

        _mockPromptTemplateRepo
            .Setup(r => r.GetByNameAsync("plan-security-check", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AgentPromptTemplate?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ValidatePlanAsync(ticketId, "security"));

        Assert.Contains("Prompt template 'plan-security-check' not found", exception.Message);
    }

    [Fact]
    public async Task ValidatePlanWithCustomPrompt_ReturnsValidationResult()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var planContent = "# Implementation Plan\n\nCustom feature.";
        var customPrompt = "Review this plan for accessibility: {plan_content}";
        var llmResponse = @"Score: 85

Findings:
- Missing ARIA labels for interactive elements
- No keyboard navigation support documented

Recommendations:
- Add proper ARIA labels to all interactive components
- Document keyboard shortcuts and navigation paths";

        _mockPlanService
            .Setup(s => s.GetPlanAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlanInfo
            {
                BranchName = "plan/test",
                Content = planContent,
                IsApproved = false
            });

        _mockLlmProvider
            .Setup(p => p.SendMessageAsync(
                It.Is<string>(s => s.Contains("Custom feature")),
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmResponse
            {
                Success = true,
                Content = llmResponse
            });

        // Act
        var result = await _service.ValidatePlanWithPromptAsync(ticketId, customPrompt);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(85, result.Score);
        Assert.Equal("custom", result.CheckType);
        Assert.Equal(2, result.Findings.Count);
        Assert.Equal(2, result.Recommendations.Count);
    }

    [Fact]
    public async Task ValidateCodeVsPlan_WithApprovedPlan_ReturnsAlignmentResult()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var planContent = "# Plan\n\n1. Add login endpoint\n2. Add authentication middleware";
        var diffContent = @"+++ AuthController.cs
+public async Task<IActionResult> Login(LoginRequest request)
+++ AuthMiddleware.cs
+public class AuthMiddleware";

        var llmResponse = @"✅ Requirements Successfully Implemented:
- Login endpoint added with proper validation
- Authentication middleware implemented

❌ Requirements Missing or Incomplete:
- Password hashing not implemented
- Session management missing

⚠️ Deviations from Plan:
- Using JWT instead of session-based auth (not specified in plan)

❓ Code Not Specified in Plan:
- Added refresh token endpoint (good addition but not planned)

Overall Alignment Score: 75

Summary:
Most requirements implemented but missing some security features.";

        _mockPlanService
            .Setup(s => s.GetPlanAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlanInfo
            {
                BranchName = "plan/test",
                Content = planContent,
                IsApproved = true,
                ApprovedAt = DateTime.UtcNow
            });

        _mockPromptTemplateRepo
            .Setup(r => r.GetByNameAsync("code-plan-validation", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AgentPromptTemplate.CreateSystemTemplate(
                "code-plan-validation",
                "Code validation",
                "Plan: {plan_artifacts}\nCode: {code_diff}",
                "Review",
                "claude-sonnet-4-5-20250929",
                "#000000"));

        _mockLlmProvider
            .Setup(p => p.SendMessageAsync(
                It.IsAny<string>(),
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmResponse
            {
                Success = true,
                Content = llmResponse
            });

        // Act
        var result = await _service.ValidateCodeVsPlanAsync(ticketId, diffContent);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(75, result.AlignmentScore);
        Assert.Equal(2, result.ImplementedRequirements.Count);
        Assert.Contains("Login endpoint added with proper validation", result.ImplementedRequirements);
        Assert.Equal(2, result.MissingRequirements.Count);
        Assert.Contains("Password hashing not implemented", result.MissingRequirements);
        Assert.Single(result.Deviations);
        Assert.Contains("Using JWT instead of session-based auth", result.Deviations[0]);
        Assert.Single(result.UnplannedCode);
    }

    [Fact]
    public async Task ValidateCodeVsPlan_WithUnapprovedPlan_ThrowsException()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var planContent = "# Plan";

        _mockPlanService
            .Setup(s => s.GetPlanAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlanInfo
            {
                BranchName = "plan/test",
                Content = planContent,
                IsApproved = false
            });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ValidateCodeVsPlanAsync(ticketId, "diff content"));

        Assert.Contains("Cannot validate code against unapproved plan", exception.Message);
    }

    [Fact]
    public async Task ValidatePlan_WithLlmFailure_ThrowsException()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var planContent = "# Plan";

        _mockPlanService
            .Setup(s => s.GetPlanAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlanInfo
            {
                BranchName = "plan/test",
                Content = planContent,
                IsApproved = false
            });

        _mockPromptTemplateRepo
            .Setup(r => r.GetByNameAsync("plan-security-check", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AgentPromptTemplate.CreateSystemTemplate(
                "plan-security-check",
                "Security check",
                "Content: {plan_content}",
                "Review",
                "claude-sonnet-4-5-20250929",
                "#000000"));

        _mockLlmProvider
            .Setup(p => p.SendMessageAsync(
                It.IsAny<string>(),
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmResponse
            {
                Success = false,
                ErrorMessage = "API rate limit exceeded"
            });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ValidatePlanAsync(ticketId, "security"));

        Assert.Contains("LLM provider failed", exception.Message);
        Assert.Contains("API rate limit exceeded", exception.Message);
    }

    [Fact]
    public async Task ParseValidationResponse_ExtractsScore_Correctly()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var planContent = "# Plan";
        var llmResponse = @"Analysis complete.

Score: 92

Everything looks good!";

        SetupValidationMocks(ticketId, planContent, "plan-security-check", llmResponse);

        // Act
        var result = await _service.ValidatePlanAsync(ticketId, "security");

        // Assert
        Assert.Equal(92, result.Score);
    }

    [Fact]
    public async Task ParseValidationResponse_WithoutScore_UsesDefaultScore()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var planContent = "# Plan";
        var llmResponse = @"Analysis complete.

No major issues found.";

        SetupValidationMocks(ticketId, planContent, "plan-security-check", llmResponse);

        // Act
        var result = await _service.ValidatePlanAsync(ticketId, "security");

        // Assert
        Assert.Equal(70, result.Score); // Default score
    }

    [Fact]
    public async Task ParseAlignmentResponse_ExtractsAllSections_Correctly()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var planContent = "# Plan";
        var diffContent = "diff";
        var llmResponse = @"✅ Requirement 1 done
✅ Requirement 2 done
❌ Missing requirement 1
❌ Missing requirement 2
⚠️ Deviation 1
⚠️ Deviation 2
❓ Unplanned code 1
❓ Unplanned code 2

Overall Alignment Score: 80";

        _mockPlanService
            .Setup(s => s.GetPlanAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlanInfo
            {
                BranchName = "plan/test",
                Content = planContent,
                IsApproved = true
            });

        _mockPromptTemplateRepo
            .Setup(r => r.GetByNameAsync("code-plan-validation", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AgentPromptTemplate.CreateSystemTemplate(
                "code-plan-validation",
                "Code validation",
                "Plan: {plan_artifacts}\nCode: {code_diff}",
                "Review",
                "claude-sonnet-4-5-20250929",
                "#000000"));

        _mockLlmProvider
            .Setup(p => p.SendMessageAsync(
                It.IsAny<string>(),
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmResponse
            {
                Success = true,
                Content = llmResponse
            });

        // Act
        var result = await _service.ValidateCodeVsPlanAsync(ticketId, diffContent);

        // Assert
        Assert.Equal(80, result.AlignmentScore);
        Assert.Equal(2, result.ImplementedRequirements.Count);
        Assert.Equal(2, result.MissingRequirements.Count);
        Assert.Equal(2, result.Deviations.Count);
        Assert.Equal(2, result.UnplannedCode.Count);
    }

    [Fact]
    public async Task ParseValidationResponse_ExtractsFindings_WithBulletPoints()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var planContent = "# Plan";
        var llmResponse = @"Score: 80

Findings:
- Finding with dash
• Finding with bullet
  - Finding with indented dash

Recommendations:
- Recommendation 1
• Recommendation 2";

        SetupValidationMocks(ticketId, planContent, "plan-completeness-check", llmResponse);

        // Act
        var result = await _service.ValidatePlanAsync(ticketId, "completeness");

        // Assert
        Assert.Equal(3, result.Findings.Count);
        Assert.Equal(2, result.Recommendations.Count);
    }

    private void SetupValidationMocks(Guid ticketId, string planContent, string templateName, string llmResponse)
    {
        _mockPlanService
            .Setup(s => s.GetPlanAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlanInfo
            {
                BranchName = "plan/test",
                Content = planContent,
                IsApproved = false
            });

        _mockPromptTemplateRepo
            .Setup(r => r.GetByNameAsync(templateName, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AgentPromptTemplate.CreateSystemTemplate(
                templateName,
                "Test template",
                "Test content: {plan_content}",
                "Review",
                "claude-sonnet-4-5-20250929",
                "#000000"));

        _mockLlmProvider
            .Setup(p => p.SendMessageAsync(
                It.IsAny<string>(),
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmResponse
            {
                Success = true,
                Content = llmResponse
            });
    }
}
