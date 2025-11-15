using PRFactory.AgentTools.Core;

namespace PRFactory.AgentTools.Tests.Core;

public class ToolExecutionResultTests
{
    [Fact]
    public void CreateSuccess_CreatesSuccessfulResult()
    {
        // Arrange
        var output = "Test output";
        var duration = TimeSpan.FromSeconds(1.5);

        // Act
        var result = ToolExecutionResult.CreateSuccess(output, duration);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(output, result.Output);
        Assert.Equal(duration, result.Duration);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.Metadata);
        Assert.Empty(result.Metadata);
    }

    [Fact]
    public void CreateFailure_CreatesFailedResult()
    {
        // Arrange
        var errorMessage = "Test error";
        var duration = TimeSpan.FromMilliseconds(250);

        // Act
        var result = ToolExecutionResult.CreateFailure(errorMessage, duration);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(errorMessage, result.ErrorMessage);
        Assert.Equal(duration, result.Duration);
        Assert.Equal(string.Empty, result.Output);
        Assert.NotNull(result.Metadata);
        Assert.Empty(result.Metadata);
    }

    [Fact]
    public void Metadata_CanBePopulated()
    {
        // Arrange
        var result = ToolExecutionResult.CreateSuccess("output", TimeSpan.FromSeconds(1));

        // Act
        result.Metadata["fileSize"] = 1024;
        result.Metadata["lineCount"] = 42;

        // Assert
        Assert.Equal(2, result.Metadata.Count);
        Assert.Equal(1024, result.Metadata["fileSize"]);
        Assert.Equal(42, result.Metadata["lineCount"]);
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var output = "Custom output";
        var errorMessage = "Custom error";
        var duration = TimeSpan.FromMinutes(2);

        // Act
        var result = new ToolExecutionResult
        {
            Output = output,
            Success = true,
            ErrorMessage = errorMessage,
            Duration = duration,
            Metadata = new Dictionary<string, object>
            {
                ["key"] = "value"
            }
        };

        // Assert
        Assert.Equal(output, result.Output);
        Assert.True(result.Success);
        Assert.Equal(errorMessage, result.ErrorMessage);
        Assert.Equal(duration, result.Duration);
        Assert.Single(result.Metadata);
        Assert.Equal("value", result.Metadata["key"]);
    }

    [Fact]
    public void DefaultConstructor_InitializesWithDefaults()
    {
        // Act
        var result = new ToolExecutionResult();

        // Assert
        Assert.False(result.Success);
        Assert.Equal(string.Empty, result.Output);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(TimeSpan.Zero, result.Duration);
        Assert.NotNull(result.Metadata);
        Assert.Empty(result.Metadata);
    }
}
