using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PRFactory.Core.Application.Services;
using PRFactory.Core.Application.DTOs;
using PRFactory.Tests.Blazor;
using PRFactory.Web.Components.Plans;
using Xunit;

namespace PRFactory.Tests.Web.Components;

public class PlanRevisionHistoryTests : ComponentTestBase
{
    private Mock<IPlanService> _mockPlanService = null!;
    private Guid _testTicketId;
    private List<PlanRevisionDto> _testRevisions = null!;
    private PlanRevisionComparisonDto _testComparison = null!;

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        _mockPlanService = new Mock<IPlanService>();
        services.AddSingleton(_mockPlanService.Object);

        // Setup test data
        _testTicketId = Guid.NewGuid();

        _testRevisions = new List<PlanRevisionDto>
        {
            new PlanRevisionDto
            {
                Id = Guid.NewGuid(),
                TicketId = _testTicketId,
                RevisionNumber = 1,
                BranchName = "feature/test-branch",
                MarkdownPath = "docs/plan.md",
                CommitHash = "abc123def456",
                Content = "# Initial Plan\n\nThis is the initial plan.",
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                CreatedByUserId = null,
                CreatedByName = "AI Generated",
                Reason = "Initial"
            },
            new PlanRevisionDto
            {
                Id = Guid.NewGuid(),
                TicketId = _testTicketId,
                RevisionNumber = 2,
                BranchName = "feature/test-branch",
                MarkdownPath = "docs/plan.md",
                CommitHash = "def456ghi789",
                Content = "# Refined Plan\n\nThis is the refined plan.",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                CreatedByUserId = Guid.NewGuid(),
                CreatedByName = "John Doe",
                Reason = "Refined"
            }
        };

        _testComparison = new PlanRevisionComparisonDto
        {
            Revision1 = _testRevisions[0],
            Revision2 = _testRevisions[1],
            DiffLines = new List<DiffLine>
            {
                new DiffLine { LineNumber = 1, Type = DiffLineType.Unchanged, Content = "# Plan" },
                new DiffLine { LineNumber = 2, Type = DiffLineType.Removed, Content = "Old content" },
                new DiffLine { LineNumber = 3, Type = DiffLineType.Added, Content = "New content" }
            }
        };

        // Setup default mock behavior
        _mockPlanService
            .Setup(s => s.GetPlanRevisionsAsync(_testTicketId))
            .ReturnsAsync(_testRevisions);

        _mockPlanService
            .Setup(s => s.GetPlanRevisionAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => _testRevisions.FirstOrDefault(r => r.Id == id));

        _mockPlanService
            .Setup(s => s.CompareRevisionsAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(_testComparison);
    }

    [Fact]
    public async Task OnInitialized_LoadsRevisions()
    {
        // Act
        var cut = RenderComponent<PlanRevisionHistory>(parameters => parameters
            .Add(p => p.TicketId, _testTicketId));
        await Task.Delay(100); // Allow async initialization to complete

        // Assert
        _mockPlanService.Verify(
            s => s.GetPlanRevisionsAsync(_testTicketId),
            Times.Once);
    }

    [Fact]
    public async Task OnInitialized_DisplaysRevisions()
    {
        // Act
        var cut = RenderComponent<PlanRevisionHistory>(parameters => parameters
            .Add(p => p.TicketId, _testTicketId));
        await Task.Delay(100); // Allow async initialization to complete

        // Assert
        Assert.Contains("Revision #1", cut.Markup);
        Assert.Contains("Revision #2", cut.Markup);
        Assert.Contains("Initial", cut.Markup);
        Assert.Contains("Refined", cut.Markup);
    }

    [Fact]
    public async Task OnInitialized_DisplaysEmptyState_WhenNoRevisions()
    {
        // Arrange
        _mockPlanService
            .Setup(s => s.GetPlanRevisionsAsync(_testTicketId))
            .ReturnsAsync(new List<PlanRevisionDto>());

        // Act
        var cut = RenderComponent<PlanRevisionHistory>(parameters => parameters
            .Add(p => p.TicketId, _testTicketId));
        await Task.Delay(100); // Allow async initialization to complete

        // Assert
        Assert.Contains("No revision history available", cut.Markup);
    }

    [Fact]
    public async Task LoadRevisions_WithException_SetsErrorMessage()
    {
        // Arrange
        _mockPlanService
            .Setup(s => s.GetPlanRevisionsAsync(_testTicketId))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var cut = RenderComponent<PlanRevisionHistory>(parameters => parameters
            .Add(p => p.TicketId, _testTicketId));
        await Task.Delay(100); // Allow async initialization to complete

        // Assert
        Assert.Contains("Failed to load revision history", cut.Markup);
        Assert.Contains("Test exception", cut.Markup);
    }

    [Fact]
    public async Task ViewRevision_LoadsRevisionDetails()
    {
        // Arrange
        var cut = RenderComponent<PlanRevisionHistory>(parameters => parameters
            .Add(p => p.TicketId, _testTicketId));
        await Task.Delay(100); // Allow async initialization to complete

        var revisionId = _testRevisions[0].Id;

        // Act
        await cut.InvokeAsync(async () =>
        {
            var instance = cut.Instance;
            var method = typeof(PlanRevisionHistory).GetMethod("ViewRevision",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                await (Task)method.Invoke(instance, new object[] { revisionId })!;
            }
        });

        // Assert
        _mockPlanService.Verify(
            s => s.GetPlanRevisionAsync(revisionId),
            Times.Once);
    }

    [Fact]
    public async Task ViewRevision_WithException_SetsErrorMessage()
    {
        // Arrange
        var revisionId = _testRevisions[0].Id;
        _mockPlanService
            .Setup(s => s.GetPlanRevisionAsync(revisionId))
            .ThrowsAsync(new Exception("Failed to load revision"));

        var cut = RenderComponent<PlanRevisionHistory>(parameters => parameters
            .Add(p => p.TicketId, _testTicketId));
        await Task.Delay(100); // Allow async initialization to complete

        // Act
        await cut.InvokeAsync(async () =>
        {
            var instance = cut.Instance;
            var method = typeof(PlanRevisionHistory).GetMethod("ViewRevision",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                await (Task)method.Invoke(instance, new object[] { revisionId })!;
            }
        });

        // Assert
        Assert.Contains("Failed to load revision", cut.Markup);
    }

    [Fact]
    public async Task SelectForComparison_SelectsFirstRevision()
    {
        // Arrange
        var cut = RenderComponent<PlanRevisionHistory>(parameters => parameters
            .Add(p => p.TicketId, _testTicketId));
        await Task.Delay(100); // Allow async initialization to complete

        var revisionId = _testRevisions[0].Id;

        // Act
        await cut.InvokeAsync(() =>
        {
            var instance = cut.Instance;
            var method = typeof(PlanRevisionHistory).GetMethod("SelectForComparison",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(instance, new object[] { revisionId });
        });

        // Assert
        var selectedRevision1 = typeof(PlanRevisionHistory)
            .GetField("selectedRevision1", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(cut.Instance);

        Assert.Equal(revisionId, selectedRevision1);
    }

    [Fact]
    public async Task SelectForComparison_SelectsSecondRevision()
    {
        // Arrange
        var cut = RenderComponent<PlanRevisionHistory>(parameters => parameters
            .Add(p => p.TicketId, _testTicketId));
        await Task.Delay(100); // Allow async initialization to complete

        var revisionId1 = _testRevisions[0].Id;
        var revisionId2 = _testRevisions[1].Id;

        // Act - Select first revision
        await cut.InvokeAsync(() =>
        {
            var instance = cut.Instance;
            var method = typeof(PlanRevisionHistory).GetMethod("SelectForComparison",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(instance, new object[] { revisionId1 });
        });

        // Act - Select second revision
        await cut.InvokeAsync(() =>
        {
            var instance = cut.Instance;
            var method = typeof(PlanRevisionHistory).GetMethod("SelectForComparison",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(instance, new object[] { revisionId2 });
        });

        // Assert
        var selectedRevision2 = typeof(PlanRevisionHistory)
            .GetField("selectedRevision2", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(cut.Instance);

        Assert.Equal(revisionId2, selectedRevision2);
    }

    [Fact]
    public async Task CompareSelected_WithTwoSelected_LoadsComparison()
    {
        // Arrange
        var cut = RenderComponent<PlanRevisionHistory>(parameters => parameters
            .Add(p => p.TicketId, _testTicketId));
        await Task.Delay(100); // Allow async initialization to complete

        var revisionId1 = _testRevisions[0].Id;
        var revisionId2 = _testRevisions[1].Id;

        // Select two revisions
        await cut.InvokeAsync(() =>
        {
            var instance = cut.Instance;
            var selectMethod = typeof(PlanRevisionHistory).GetMethod("SelectForComparison",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            selectMethod?.Invoke(instance, new object[] { revisionId1 });
            selectMethod?.Invoke(instance, new object[] { revisionId2 });
        });

        // Act
        await cut.InvokeAsync(async () =>
        {
            var instance = cut.Instance;
            var method = typeof(PlanRevisionHistory).GetMethod("CompareSelected",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                await (Task)method.Invoke(instance, new object[] { })!;
            }
        });

        // Assert
        _mockPlanService.Verify(
            s => s.CompareRevisionsAsync(revisionId1, revisionId2),
            Times.Once);
    }

    [Fact]
    public async Task CompareSelected_WithLessThanTwo_SetsError()
    {
        // Arrange
        var cut = RenderComponent<PlanRevisionHistory>(parameters => parameters
            .Add(p => p.TicketId, _testTicketId));
        await Task.Delay(100); // Allow async initialization to complete

        // Act - Compare without selecting any revisions
        await cut.InvokeAsync(async () =>
        {
            var instance = cut.Instance;
            var method = typeof(PlanRevisionHistory).GetMethod("CompareSelected",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                await (Task)method.Invoke(instance, new object[] { })!;
            }
        });

        // Assert
        Assert.Contains("Please select two revisions to compare", cut.Markup);
    }

    [Fact]
    public async Task CompareSelected_WithException_SetsErrorMessage()
    {
        // Arrange
        var revisionId1 = _testRevisions[0].Id;
        var revisionId2 = _testRevisions[1].Id;

        _mockPlanService
            .Setup(s => s.CompareRevisionsAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ThrowsAsync(new Exception("Failed to compare"));

        var cut = RenderComponent<PlanRevisionHistory>(parameters => parameters
            .Add(p => p.TicketId, _testTicketId));
        await Task.Delay(100); // Allow async initialization to complete

        // Select two revisions
        await cut.InvokeAsync(() =>
        {
            var instance = cut.Instance;
            var selectMethod = typeof(PlanRevisionHistory).GetMethod("SelectForComparison",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            selectMethod?.Invoke(instance, new object[] { revisionId1 });
            selectMethod?.Invoke(instance, new object[] { revisionId2 });
        });

        // Act
        await cut.InvokeAsync(async () =>
        {
            var instance = cut.Instance;
            var method = typeof(PlanRevisionHistory).GetMethod("CompareSelected",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                await (Task)method.Invoke(instance, new object[] { })!;
            }
        });

        // Assert
        Assert.Contains("Failed to compare revisions", cut.Markup);
    }

    [Fact]
    public async Task ClearSelection_ClearsSelectedRevisions()
    {
        // Arrange
        var cut = RenderComponent<PlanRevisionHistory>(parameters => parameters
            .Add(p => p.TicketId, _testTicketId));
        await Task.Delay(100); // Allow async initialization to complete

        var revisionId1 = _testRevisions[0].Id;
        var revisionId2 = _testRevisions[1].Id;

        // Select two revisions
        await cut.InvokeAsync(() =>
        {
            var instance = cut.Instance;
            var selectMethod = typeof(PlanRevisionHistory).GetMethod("SelectForComparison",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            selectMethod?.Invoke(instance, new object[] { revisionId1 });
            selectMethod?.Invoke(instance, new object[] { revisionId2 });
        });

        // Act
        await cut.InvokeAsync(() =>
        {
            var instance = cut.Instance;
            var method = typeof(PlanRevisionHistory).GetMethod("ClearSelection",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(instance, new object[] { });
        });

        // Assert
        var selectedRevision1 = typeof(PlanRevisionHistory)
            .GetField("selectedRevision1", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(cut.Instance);
        var selectedRevision2 = typeof(PlanRevisionHistory)
            .GetField("selectedRevision2", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(cut.Instance);

        Assert.Null(selectedRevision1);
        Assert.Null(selectedRevision2);
    }

    [Fact]
    public async Task CloseViewer_ClearsViewingRevision()
    {
        // Arrange
        var cut = RenderComponent<PlanRevisionHistory>(parameters => parameters
            .Add(p => p.TicketId, _testTicketId));
        await Task.Delay(100); // Allow async initialization to complete

        // Set viewing revision
        await cut.InvokeAsync(async () =>
        {
            var instance = cut.Instance;
            var viewMethod = typeof(PlanRevisionHistory).GetMethod("ViewRevision",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (viewMethod != null)
            {
                await (Task)viewMethod.Invoke(instance, new object[] { _testRevisions[0].Id })!;
            }
        });

        // Act
        await cut.InvokeAsync(() =>
        {
            var instance = cut.Instance;
            var method = typeof(PlanRevisionHistory).GetMethod("CloseViewer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(instance, new object[] { });
        });

        // Assert
        var viewingRevision = typeof(PlanRevisionHistory)
            .GetField("viewingRevision", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(cut.Instance);

        Assert.Null(viewingRevision);
    }

    [Fact]
    public async Task CloseComparison_ClearsComparisonAndSelection()
    {
        // Arrange
        var cut = RenderComponent<PlanRevisionHistory>(parameters => parameters
            .Add(p => p.TicketId, _testTicketId));
        await Task.Delay(100); // Allow async initialization to complete

        var revisionId1 = _testRevisions[0].Id;
        var revisionId2 = _testRevisions[1].Id;

        // Select and compare
        await cut.InvokeAsync(async () =>
        {
            var instance = cut.Instance;
            var selectMethod = typeof(PlanRevisionHistory).GetMethod("SelectForComparison",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            selectMethod?.Invoke(instance, new object[] { revisionId1 });
            selectMethod?.Invoke(instance, new object[] { revisionId2 });

            var compareMethod = typeof(PlanRevisionHistory).GetMethod("CompareSelected",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (compareMethod != null)
            {
                await (Task)compareMethod.Invoke(instance, new object[] { })!;
            }
        });

        // Act
        await cut.InvokeAsync(() =>
        {
            var instance = cut.Instance;
            var method = typeof(PlanRevisionHistory).GetMethod("CloseComparison",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(instance, new object[] { });
        });

        // Assert
        var comparison = typeof(PlanRevisionHistory)
            .GetField("comparison", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(cut.Instance);
        var selectedRevision1 = typeof(PlanRevisionHistory)
            .GetField("selectedRevision1", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(cut.Instance);

        Assert.Null(comparison);
        Assert.Null(selectedRevision1);
    }
}
