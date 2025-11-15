using PRFactory.AgentTools.Security;

namespace PRFactory.AgentTools.Tests.Security;

public class ResourceLimitsTests
{
    [Fact]
    public void MaxFileSize_IsCorrectValue()
    {
        // Assert - 10MB = 10 * 1024 * 1024 bytes
        Assert.Equal(10 * 1024 * 1024, ResourceLimits.MaxFileSize);
    }

    [Fact]
    public void MaxWriteSize_IsCorrectValue()
    {
        // Assert - 1MB = 1 * 1024 * 1024 bytes
        Assert.Equal(1 * 1024 * 1024, ResourceLimits.MaxWriteSize);
    }

    [Fact]
    public void MaxResultLines_IsCorrectValue()
    {
        // Assert
        Assert.Equal(1000, ResourceLimits.MaxResultLines);
    }

    [Fact]
    public void MaxGlobResults_IsCorrectValue()
    {
        // Assert
        Assert.Equal(1000, ResourceLimits.MaxGlobResults);
    }

    [Fact]
    public void DefaultTimeout_IsCorrectValue()
    {
        // Assert - 30 seconds
        Assert.Equal(TimeSpan.FromSeconds(30), ResourceLimits.DefaultTimeout);
    }

    #region ValidateFileSize Tests

    [Fact]
    public void ValidateFileSize_ValidSize_DoesNotThrow()
    {
        // Arrange
        var fileSize = 1024; // 1KB

        // Act & Assert - Should not throw
        ResourceLimits.ValidateFileSize(fileSize);
    }

    [Fact]
    public void ValidateFileSize_ZeroSize_DoesNotThrow()
    {
        // Arrange
        var fileSize = 0;

        // Act & Assert - Should not throw
        ResourceLimits.ValidateFileSize(fileSize);
    }

    [Fact]
    public void ValidateFileSize_MaxSize_DoesNotThrow()
    {
        // Arrange
        var fileSize = ResourceLimits.MaxFileSize;

        // Act & Assert - Should not throw
        ResourceLimits.ValidateFileSize(fileSize);
    }

    [Fact]
    public void ValidateFileSize_ExceedsMaxSize_ThrowsInvalidOperationException()
    {
        // Arrange
        var fileSize = ResourceLimits.MaxFileSize + 1;

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            ResourceLimits.ValidateFileSize(fileSize));

        Assert.Contains("exceeds limit", ex.Message);
        Assert.Contains(fileSize.ToString(), ex.Message);
        Assert.Contains(ResourceLimits.MaxFileSize.ToString(), ex.Message);
    }

    [Fact]
    public void ValidateFileSize_LargeExcess_ThrowsInvalidOperationException()
    {
        // Arrange
        var fileSize = 100 * 1024 * 1024; // 100MB

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            ResourceLimits.ValidateFileSize(fileSize));

        Assert.Contains("exceeds limit", ex.Message);
    }

    [Fact]
    public void ValidateFileSize_NegativeSize_ThrowsArgumentException()
    {
        // Arrange
        var fileSize = -1;

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            ResourceLimits.ValidateFileSize(fileSize));

        Assert.Equal("fileSize", ex.ParamName);
        Assert.Contains("cannot be negative", ex.Message);
    }

    [Fact]
    public void ValidateFileSize_CustomMaxSize_ValidSize_DoesNotThrow()
    {
        // Arrange
        var fileSize = 512 * 1024; // 512KB
        var customMax = 1024 * 1024; // 1MB

        // Act & Assert - Should not throw
        ResourceLimits.ValidateFileSize(fileSize, customMax);
    }

    [Fact]
    public void ValidateFileSize_CustomMaxSize_ExceedsLimit_ThrowsInvalidOperationException()
    {
        // Arrange
        var fileSize = 2 * 1024 * 1024; // 2MB
        var customMax = 1024 * 1024; // 1MB

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            ResourceLimits.ValidateFileSize(fileSize, customMax));

        Assert.Contains("exceeds limit", ex.Message);
        Assert.Contains(customMax.ToString(), ex.Message);
    }

    #endregion

    #region ValidateContentSize Tests

    [Fact]
    public void ValidateContentSize_ValidContent_DoesNotThrow()
    {
        // Arrange
        var content = "Hello World";

        // Act & Assert - Should not throw
        ResourceLimits.ValidateContentSize(content);
    }

    [Fact]
    public void ValidateContentSize_EmptyString_DoesNotThrow()
    {
        // Arrange
        var content = "";

        // Act & Assert - Should not throw
        ResourceLimits.ValidateContentSize(content);
    }

    [Fact]
    public void ValidateContentSize_LargeValidContent_DoesNotThrow()
    {
        // Arrange - Just under 1MB
        var content = new string('x', 1024 * 1024 - 100);

        // Act & Assert - Should not throw
        ResourceLimits.ValidateContentSize(content);
    }

    [Fact]
    public void ValidateContentSize_MaxSizeContent_DoesNotThrow()
    {
        // Arrange - Exactly MaxWriteSize (1MB in bytes)
        var content = new string('x', (int)ResourceLimits.MaxWriteSize);

        // Act & Assert - Should not throw
        ResourceLimits.ValidateContentSize(content);
    }

    [Fact]
    public void ValidateContentSize_ExceedsMaxSize_ThrowsInvalidOperationException()
    {
        // Arrange - Larger than 1MB
        var content = new string('x', (int)ResourceLimits.MaxWriteSize + 1);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            ResourceLimits.ValidateContentSize(content));

        Assert.Contains("exceeds limit", ex.Message);
        Assert.Contains("bytes", ex.Message);
    }

    [Fact]
    public void ValidateContentSize_VeryLargeContent_ThrowsInvalidOperationException()
    {
        // Arrange - 10MB content
        var content = new string('x', 10 * 1024 * 1024);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            ResourceLimits.ValidateContentSize(content));

        Assert.Contains("exceeds limit", ex.Message);
    }

    [Fact]
    public void ValidateContentSize_NullContent_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            ResourceLimits.ValidateContentSize(null!));

        Assert.Equal("content", ex.ParamName);
    }

    [Fact]
    public void ValidateContentSize_CustomMaxSize_ValidContent_DoesNotThrow()
    {
        // Arrange
        var content = "Hello World";
        var customMax = 1024; // 1KB

        // Act & Assert - Should not throw
        ResourceLimits.ValidateContentSize(content, customMax);
    }

    [Fact]
    public void ValidateContentSize_CustomMaxSize_ExceedsLimit_ThrowsInvalidOperationException()
    {
        // Arrange
        var content = new string('x', 2048); // 2KB
        var customMax = 1024; // 1KB

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            ResourceLimits.ValidateContentSize(content, customMax));

        Assert.Contains("exceeds limit", ex.Message);
        Assert.Contains(customMax.ToString(), ex.Message);
    }

    [Fact]
    public void ValidateContentSize_Utf8MultiByte_CalculatesByteCount()
    {
        // Arrange - Emoji characters are multi-byte in UTF-8
        var emoji = "😀"; // This is typically 4 bytes in UTF-8
        var repeatCount = (int)(ResourceLimits.MaxWriteSize / 4) + 100; // Exceed limit
        var content = string.Concat(Enumerable.Repeat(emoji, repeatCount));

        // Act & Assert - Should throw because byte count exceeds limit
        var ex = Assert.Throws<InvalidOperationException>(() =>
            ResourceLimits.ValidateContentSize(content));

        Assert.Contains("exceeds limit", ex.Message);
    }

    [Fact]
    public void ValidateContentSize_SpecialCharacters_DoesNotThrow()
    {
        // Arrange - Special characters but within limit
        var content = "Hello\nWorld\t!\r\n\"Test\"";

        // Act & Assert - Should not throw
        ResourceLimits.ValidateContentSize(content);
    }

    [Fact]
    public void ValidateContentSize_WhitespaceContent_DoesNotThrow()
    {
        // Arrange
        var content = "   \n\t\r\n   ";

        // Act & Assert - Should not throw
        ResourceLimits.ValidateContentSize(content);
    }

    #endregion
}
