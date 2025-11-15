using PRFactory.AgentTools.Core;

namespace PRFactory.AgentTools.Tests.Core;

public class ToolExecutionContextTests
{
    [Fact]
    public void GetParameter_WhenParameterExists_ReturnsValue()
    {
        // Arrange
        var context = new ToolExecutionContext
        {
            Parameters = new Dictionary<string, object>
            {
                ["testParam"] = "testValue"
            }
        };

        // Act
        var result = context.GetParameter<string>("testParam");

        // Assert
        Assert.Equal("testValue", result);
    }

    [Fact]
    public void GetParameter_WhenParameterMissing_ThrowsArgumentException()
    {
        // Arrange
        var context = new ToolExecutionContext
        {
            Parameters = new Dictionary<string, object>()
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            context.GetParameter<string>("missingParam"));
        Assert.Contains("missingParam", ex.Message);
        Assert.Contains("required", ex.Message);
    }

    [Fact]
    public void GetParameter_WithTypeConversion_ConvertsValue()
    {
        // Arrange
        var context = new ToolExecutionContext
        {
            Parameters = new Dictionary<string, object>
            {
                ["intParam"] = 42,
                ["stringParam"] = "123"
            }
        };

        // Act
        var intResult = context.GetParameter<int>("intParam");
        var stringToIntResult = context.GetParameter<int>("stringParam");

        // Assert
        Assert.Equal(42, intResult);
        Assert.Equal(123, stringToIntResult);
    }

    [Fact]
    public void GetParameter_WithBooleanConversion_ConvertsValue()
    {
        // Arrange
        var context = new ToolExecutionContext
        {
            Parameters = new Dictionary<string, object>
            {
                ["boolParam"] = true,
                ["stringBoolParam"] = "false"
            }
        };

        // Act
        var boolResult = context.GetParameter<bool>("boolParam");
        var stringBoolResult = context.GetParameter<bool>("stringBoolParam");

        // Assert
        Assert.True(boolResult);
        Assert.False(stringBoolResult);
    }

    [Fact]
    public void GetOptionalParameter_WhenParameterExists_ReturnsValue()
    {
        // Arrange
        var context = new ToolExecutionContext
        {
            Parameters = new Dictionary<string, object>
            {
                ["optionalParam"] = "optionalValue"
            }
        };

        // Act
        var result = context.GetOptionalParameter<string>("optionalParam", "default");

        // Assert
        Assert.Equal("optionalValue", result);
    }

    [Fact]
    public void GetOptionalParameter_WhenParameterMissing_ReturnsDefault()
    {
        // Arrange
        var context = new ToolExecutionContext
        {
            Parameters = new Dictionary<string, object>()
        };

        // Act
        var result = context.GetOptionalParameter<string>("missingParam", "defaultValue");

        // Assert
        Assert.Equal("defaultValue", result);
    }

    [Fact]
    public void GetOptionalParameter_WhenParameterMissingAndNoDefault_ReturnsNull()
    {
        // Arrange
        var context = new ToolExecutionContext
        {
            Parameters = new Dictionary<string, object>()
        };

        // Act
        var result = context.GetOptionalParameter<string>("missingParam");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetOptionalParameter_WithTypeConversion_ConvertsValue()
    {
        // Arrange
        var context = new ToolExecutionContext
        {
            Parameters = new Dictionary<string, object>
            {
                ["intParam"] = "456"
            }
        };

        // Act
        var result = context.GetOptionalParameter<int>("intParam", 0);

        // Assert
        Assert.Equal(456, result);
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();
        var workspacePath = "/test/workspace";

        // Act
        var context = new ToolExecutionContext
        {
            TenantId = tenantId,
            TicketId = ticketId,
            WorkspacePath = workspacePath,
            Parameters = new Dictionary<string, object>
            {
                ["key"] = "value"
            }
        };

        // Assert
        Assert.Equal(tenantId, context.TenantId);
        Assert.Equal(ticketId, context.TicketId);
        Assert.Equal(workspacePath, context.WorkspacePath);
        Assert.Single(context.Parameters);
        Assert.Equal("value", context.Parameters["key"]);
    }
}
