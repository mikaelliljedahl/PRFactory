namespace PRFactory.Infrastructure.Claude.Models;

/// <summary>
/// Wrapper for streaming responses from Claude API
/// </summary>
public class StreamingResponse
{
    private readonly IAsyncEnumerable<string> _stream;

    public StreamingResponse(IAsyncEnumerable<string> stream)
    {
        _stream = stream;
    }

    /// <summary>
    /// Stream text chunks as they arrive from the API
    /// </summary>
    public async IAsyncEnumerable<string> StreamTextAsync()
    {
        await foreach (var chunk in _stream)
        {
            yield return chunk;
        }
    }
}
