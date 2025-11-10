using Moq;
using PRFactory.Infrastructure.Agents;
using PRFactory.Infrastructure.Agents.Graphs;
using Xunit;

namespace PRFactory.Tests.Helpers;

/// <summary>
/// Custom xUnit assertion extensions for common test patterns
/// </summary>
public static class AssertExtensions
{
    /// <summary>
    /// Asserts that a string contains all expected substrings in order
    /// </summary>
    public static void AssertContainsInOrder(string actual, params string[] expected)
    {
        if (string.IsNullOrEmpty(actual))
        {
            Assert.Fail("Actual string is null or empty");
        }

        int lastIndex = -1;
        foreach (var expectedSubstring in expected)
        {
            int index = actual.IndexOf(expectedSubstring, StringComparison.OrdinalIgnoreCase);

            if (index == -1)
            {
                Assert.Fail($"Expected substring '{expectedSubstring}' not found in actual string");
            }

            if (index <= lastIndex)
            {
                Assert.Fail($"Expected substring '{expectedSubstring}' found at index {index}, but it should appear after index {lastIndex}");
            }

            lastIndex = index;
        }
    }

    /// <summary>
    /// Asserts that a collection contains all expected items (order doesn't matter)
    /// </summary>
    public static void AssertContainsAll<T>(IEnumerable<T> actual, params T[] expected)
    {
        var actualList = actual.ToList();

        foreach (var expectedItem in expected)
        {
            Assert.Contains(expectedItem, actualList);
        }
    }

    /// <summary>
    /// Asserts that a collection contains exactly the expected items (order doesn't matter)
    /// </summary>
    public static void AssertContainsExactly<T>(IEnumerable<T> actual, params T[] expected)
    {
        var actualList = actual.ToList();

        Assert.Equal(expected.Length, actualList.Count);

        foreach (var expectedItem in expected)
        {
            Assert.Contains(expectedItem, actualList);
        }
    }

    /// <summary>
    /// Asserts that an event was published with the specified ticket ID
    /// </summary>
    public static void AssertEventPublished<TEvent>(Mock<IEventPublisher> eventPublisher, Guid ticketId)
        where TEvent : class
    {
        eventPublisher.Verify(
            x => x.PublishAsync(
                It.Is<TEvent>(e => GetTicketIdFromEvent(e) == ticketId)),
            Times.Once,
            $"Expected event {typeof(TEvent).Name} to be published for ticket {ticketId}");
    }

    /// <summary>
    /// Asserts that an event was published at least once
    /// </summary>
    public static void AssertEventPublishedAtLeastOnce<TEvent>(Mock<IEventPublisher> eventPublisher)
        where TEvent : class
    {
        eventPublisher.Verify(
            x => x.PublishAsync(It.IsAny<TEvent>()),
            Times.AtLeastOnce,
            $"Expected event {typeof(TEvent).Name} to be published at least once");
    }

    /// <summary>
    /// Asserts that an event was NOT published
    /// </summary>
    public static void AssertEventNotPublished<TEvent>(Mock<IEventPublisher> eventPublisher)
        where TEvent : class
    {
        eventPublisher.Verify(
            x => x.PublishAsync(It.IsAny<TEvent>()),
            Times.Never,
            $"Expected event {typeof(TEvent).Name} to NOT be published");
    }

    /// <summary>
    /// Asserts that a date is approximately equal to another date (within tolerance)
    /// </summary>
    public static void AssertDateTimeApproximatelyEqual(DateTime expected, DateTime actual, int toleranceSeconds = 1)
    {
        var difference = Math.Abs((expected - actual).TotalSeconds);
        Assert.True(
            difference <= toleranceSeconds,
            $"Expected datetime {expected:O} to be approximately equal to {actual:O} within {toleranceSeconds} seconds, but difference was {difference} seconds");
    }

    /// <summary>
    /// Asserts that a string matches a pattern (simple wildcard matching with *)
    /// </summary>
    public static void AssertMatchesPattern(string pattern, string actual)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            Assert.Fail("Pattern is null or empty");
        }

        if (string.IsNullOrEmpty(actual))
        {
            Assert.Fail("Actual string is null or empty");
        }

        // Convert simple wildcard pattern to regex
        var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";

        var regex = new System.Text.RegularExpressions.Regex(regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        Assert.True(
            regex.IsMatch(actual),
            $"Expected string '{actual}' to match pattern '{pattern}'");
    }

    /// <summary>
    /// Asserts that a Guid is not empty
    /// </summary>
    public static void AssertNotEmptyGuid(Guid guid)
    {
        Assert.NotEqual(Guid.Empty, guid);
    }

    /// <summary>
    /// Asserts that all items in a collection satisfy a condition
    /// </summary>
    public static void AssertAll<T>(IEnumerable<T> collection, Func<T, bool> condition, string message = "")
    {
        var items = collection.ToList();
        var failedItems = items.Where(item => !condition(item)).ToList();

        if (failedItems.Any())
        {
            var failureMessage = string.IsNullOrEmpty(message)
                ? $"{failedItems.Count} out of {items.Count} items failed the condition"
                : message;

            Assert.Fail(failureMessage);
        }
    }

    /// <summary>
    /// Helper method to extract ticket ID from events using reflection
    /// </summary>
    private static Guid GetTicketIdFromEvent(object eventObj)
    {
        var ticketIdProperty = eventObj.GetType().GetProperty("TicketId");
        if (ticketIdProperty != null)
        {
            var value = ticketIdProperty.GetValue(eventObj);
            if (value is Guid guidValue)
            {
                return guidValue;
            }
        }
        return Guid.Empty;
    }
}
