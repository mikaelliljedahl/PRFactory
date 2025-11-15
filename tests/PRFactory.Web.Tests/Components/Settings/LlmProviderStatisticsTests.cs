using AngleSharp.Dom;
using Bunit;
using Xunit;
using PRFactory.Web.Components.Settings;

namespace PRFactory.Web.Tests.Components.Settings;

/// <summary>
/// Tests for the LlmProviderStatistics component.
/// Verifies statistics display, card rendering, and data formatting.
/// </summary>
public class LlmProviderStatisticsTests : TestContext
{
    [Fact]
    public void Render_DisplaysFourStatisticCards()
    {
        // Arrange & Act
        var cut = RenderComponent<LlmProviderStatistics>(parameters => parameters
            .Add(p => p.TotalRequests, 100)
            .Add(p => p.SuccessfulRequests, 95)
            .Add(p => p.FailedRequests, 5)
            .Add(p => p.AverageResponseTimeMs, 500));

        // Assert
        var cards = cut.FindAll(".card");
        Assert.Equal(4, cards.Count);
    }

    [Fact]
    public void Render_DisplaysTotalRequestsCard()
    {
        // Arrange & Act
        var cut = RenderComponent<LlmProviderStatistics>(parameters => parameters
            .Add(p => p.TotalRequests, 100)
            .Add(p => p.SuccessfulRequests, 95)
            .Add(p => p.FailedRequests, 5)
            .Add(p => p.AverageResponseTimeMs, 500));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Total Requests", markup);
        Assert.Contains("100", markup);
    }

    [Fact]
    public void Render_DisplaysSuccessfulRequestsCard()
    {
        // Arrange & Act
        var cut = RenderComponent<LlmProviderStatistics>(parameters => parameters
            .Add(p => p.TotalRequests, 100)
            .Add(p => p.SuccessfulRequests, 95)
            .Add(p => p.FailedRequests, 5)
            .Add(p => p.AverageResponseTimeMs, 500));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Successful", markup);
        Assert.Contains("95", markup);
    }

    [Fact]
    public void Render_DisplaysFailedRequestsCard()
    {
        // Arrange & Act
        var cut = RenderComponent<LlmProviderStatistics>(parameters => parameters
            .Add(p => p.TotalRequests, 100)
            .Add(p => p.SuccessfulRequests, 95)
            .Add(p => p.FailedRequests, 5)
            .Add(p => p.AverageResponseTimeMs, 500));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Failed", markup);
        Assert.Contains("5", markup);
    }

    [Fact]
    public void Render_DisplaysAverageResponseTimeCard()
    {
        // Arrange & Act
        var cut = RenderComponent<LlmProviderStatistics>(parameters => parameters
            .Add(p => p.TotalRequests, 100)
            .Add(p => p.SuccessfulRequests, 95)
            .Add(p => p.FailedRequests, 5)
            .Add(p => p.AverageResponseTimeMs, 500));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Avg Response Time", markup);
        Assert.Contains("500", markup);
        Assert.Contains("ms", markup);
    }

    [Fact]
    public void Render_SuccessfulCard_DisplaysGreenColor()
    {
        // Arrange & Act
        var cut = RenderComponent<LlmProviderStatistics>(parameters => parameters
            .Add(p => p.TotalRequests, 100)
            .Add(p => p.SuccessfulRequests, 95)
            .Add(p => p.FailedRequests, 5)
            .Add(p => p.AverageResponseTimeMs, 500));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("text-success", markup);
    }

    [Fact]
    public void Render_FailedCard_DisplaysRedColor()
    {
        // Arrange & Act
        var cut = RenderComponent<LlmProviderStatistics>(parameters => parameters
            .Add(p => p.TotalRequests, 100)
            .Add(p => p.SuccessfulRequests, 95)
            .Add(p => p.FailedRequests, 5)
            .Add(p => p.AverageResponseTimeMs, 500));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("text-danger", markup);
    }

    [Fact]
    public void Render_FormatsLargeNumbers()
    {
        // Arrange & Act
        var cut = RenderComponent<LlmProviderStatistics>(parameters => parameters
            .Add(p => p.TotalRequests, 1000000)
            .Add(p => p.SuccessfulRequests, 950000)
            .Add(p => p.FailedRequests, 50000)
            .Add(p => p.AverageResponseTimeMs, 1234));

        // Assert
        var markup = cut.Markup;
        // Numbers should be formatted with thousands separator
        Assert.Contains("1,000,000", markup);
        Assert.Contains("950,000", markup);
        Assert.Contains("50,000", markup);
        Assert.Contains("1,234", markup);
    }

    [Fact]
    public void Render_WithZeroRequests_DisplaysZero()
    {
        // Arrange & Act
        var cut = RenderComponent<LlmProviderStatistics>(parameters => parameters
            .Add(p => p.TotalRequests, 0)
            .Add(p => p.SuccessfulRequests, 0)
            .Add(p => p.FailedRequests, 0)
            .Add(p => p.AverageResponseTimeMs, 0));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("0", markup);
    }

    [Fact]
    public void Render_WithLargeResponseTime_DisplaysCorrectly()
    {
        // Arrange & Act
        var cut = RenderComponent<LlmProviderStatistics>(parameters => parameters
            .Add(p => p.TotalRequests, 100)
            .Add(p => p.SuccessfulRequests, 95)
            .Add(p => p.FailedRequests, 5)
            .Add(p => p.AverageResponseTimeMs, 600000));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("600,000", markup);
        Assert.Contains("ms", markup);
    }

    [Fact]
    public void Render_AllCardsAreInGrid()
    {
        // Arrange & Act
        var cut = RenderComponent<LlmProviderStatistics>(parameters => parameters
            .Add(p => p.TotalRequests, 100)
            .Add(p => p.SuccessfulRequests, 95)
            .Add(p => p.FailedRequests, 5)
            .Add(p => p.AverageResponseTimeMs, 500));

        // Assert
        var gridContainer = cut.Find(".row.g-3");
        Assert.NotNull(gridContainer);

        var columns = cut.FindAll(".col-md-3");
        Assert.Equal(4, columns.Count);
    }

    [Fact]
    public void Render_CardTitles_AreMuted()
    {
        // Arrange & Act
        var cut = RenderComponent<LlmProviderStatistics>(parameters => parameters
            .Add(p => p.TotalRequests, 100)
            .Add(p => p.SuccessfulRequests, 95)
            .Add(p => p.FailedRequests, 5)
            .Add(p => p.AverageResponseTimeMs, 500));

        // Assert
        var mutedTitles = cut.FindAll("h6.text-muted");
        Assert.Equal(4, mutedTitles.Count);
        Assert.Contains("Total Requests", mutedTitles[0].TextContent);
        Assert.Contains("Successful", mutedTitles[1].TextContent);
        Assert.Contains("Failed", mutedTitles[2].TextContent);
        Assert.Contains("Avg Response Time", mutedTitles[3].TextContent);
    }

    [Fact]
    public void Render_CardValues_AreHeadings()
    {
        // Arrange & Act
        var cut = RenderComponent<LlmProviderStatistics>(parameters => parameters
            .Add(p => p.TotalRequests, 100)
            .Add(p => p.SuccessfulRequests, 95)
            .Add(p => p.FailedRequests, 5)
            .Add(p => p.AverageResponseTimeMs, 500));

        // Assert
        var headings = cut.FindAll("h3");
        Assert.Equal(4, headings.Count);
    }

    [Fact]
    public void Render_SuccessfulValue_DisplaysWithoutMargin()
    {
        // Arrange & Act
        var cut = RenderComponent<LlmProviderStatistics>(parameters => parameters
            .Add(p => p.TotalRequests, 100)
            .Add(p => p.SuccessfulRequests, 95)
            .Add(p => p.FailedRequests, 5)
            .Add(p => p.AverageResponseTimeMs, 500));

        // Assert
        var headings = cut.FindAll("h3");
        var successHeading = headings[1]; // Second card is successful
        Assert.Contains("mb-0", successHeading.GetAttribute("class"));
        Assert.Contains("text-success", successHeading.GetAttribute("class"));
    }

    [Fact]
    public void Render_FailedValue_DisplaysWithoutMargin()
    {
        // Arrange & Act
        var cut = RenderComponent<LlmProviderStatistics>(parameters => parameters
            .Add(p => p.TotalRequests, 100)
            .Add(p => p.SuccessfulRequests, 95)
            .Add(p => p.FailedRequests, 5)
            .Add(p => p.AverageResponseTimeMs, 500));

        // Assert
        var headings = cut.FindAll("h3");
        var failedHeading = headings[2]; // Third card is failed
        Assert.Contains("mb-0", failedHeading.GetAttribute("class"));
        Assert.Contains("text-danger", failedHeading.GetAttribute("class"));
    }

    [Fact]
    public void Render_WithMixedStats_DisplaysAllValues()
    {
        // Arrange
        var totalRequests = 1234;
        var successfulRequests = 1100;
        var failedRequests = 134;
        var averageResponseTime = 456;

        // Act
        var cut = RenderComponent<LlmProviderStatistics>(parameters => parameters
            .Add(p => p.TotalRequests, totalRequests)
            .Add(p => p.SuccessfulRequests, successfulRequests)
            .Add(p => p.FailedRequests, failedRequests)
            .Add(p => p.AverageResponseTimeMs, averageResponseTime));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("1,234", markup);
        Assert.Contains("1,100", markup);
        Assert.Contains("134", markup);
        Assert.Contains("456", markup);
    }

    [Fact]
    public void Render_CenterAlignedCards()
    {
        // Arrange & Act
        var cut = RenderComponent<LlmProviderStatistics>(parameters => parameters
            .Add(p => p.TotalRequests, 100)
            .Add(p => p.SuccessfulRequests, 95)
            .Add(p => p.FailedRequests, 5)
            .Add(p => p.AverageResponseTimeMs, 500));

        // Assert
        var cards = cut.FindAll(".card.text-center");
        Assert.Equal(4, cards.Count);
    }

    [Fact]
    public void Render_CardsHaveProperStructure()
    {
        // Arrange & Act
        var cut = RenderComponent<LlmProviderStatistics>(parameters => parameters
            .Add(p => p.TotalRequests, 100)
            .Add(p => p.SuccessfulRequests, 95)
            .Add(p => p.FailedRequests, 5)
            .Add(p => p.AverageResponseTimeMs, 500));

        // Assert
        var cardBodies = cut.FindAll(".card-body");
        Assert.Equal(4, cardBodies.Count);

        foreach (var cardBody in cardBodies)
        {
            // Each card should have a title and a value heading
            var children = cardBody.QuerySelectorAll(":scope > *");
            Assert.True(children.Length >= 2);
        }
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(1, "1")]
    [InlineData(10, "10")]
    [InlineData(100, "100")]
    [InlineData(1000, "1,000")]
    [InlineData(10000, "10,000")]
    [InlineData(100000, "100,000")]
    [InlineData(1000000, "1,000,000")]
    public void Render_FormatsNumbersCorrectly(int input, string expected)
    {
        // Arrange & Act
        var cut = RenderComponent<LlmProviderStatistics>(parameters => parameters
            .Add(p => p.TotalRequests, input)
            .Add(p => p.SuccessfulRequests, 0)
            .Add(p => p.FailedRequests, 0)
            .Add(p => p.AverageResponseTimeMs, 0));

        // Assert
        var markup = cut.Markup;
        Assert.Contains(expected, markup);
    }
}
