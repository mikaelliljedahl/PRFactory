# Saturn Tools & Multi-Agent Architecture Analysis

## Executive Summary

Saturn implements a sophisticated **plugin-based tool architecture** with a **managed multi-agent system** designed for coordinated AI agent orchestration. The system uses C# with OpenRouter for LLM communication and provides a comprehensive toolkit for file operations, command execution, and multi-agent coordination.

**Key Architectural Insight**: Saturn separates concerns into three distinct layers:
1. **Tool System** - Pluggable capabilities available to agents
2. **Agent System** - Individual AI entities with execution context
3. **Orchestration System** - Manages agent lifecycle and inter-agent communication

---

## Part 1: Tools Architecture

### 1.1 Core Tool System Design

#### ITool Interface - The Contract

**Location**: `Tools/Core/ITool.cs`

```csharp
interface ITool
{
    // Identity
    string Name { get; }                    // Unique tool identifier
    string Description { get; }             // Purpose explanation
    
    // Discovery
    Dictionary<string, object> GetParameters();     // Available parameters with types
    
    // Execution
    Task<ToolResult> ExecuteAsync(/* parameters */);  // Async execution
    
    // Display
    string GetDisplaySummary(/* parameters */);     // Human-readable summary
}
```

**Key Design Decision**: Tools are discovered via reflection at startup, enabling zero-registration dynamic plugin loading.

#### ToolBase - Common Implementation

**Location**: `Tools/Core/ToolBase.cs`

Provides template method pattern with:

```csharp
public abstract class ToolBase : ITool
{
    // Derived classes implement:
    public abstract string Name { get; }
    public abstract string Description { get; }
    protected abstract Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters);
    protected abstract Dictionary<string, object>? GetParameterProperties();
    protected abstract List<string> GetRequiredParameters();
    
    // Base class provides:
    public Dictionary<string, object> GetParameters()
    {
        // Builds parameter schema: { "type": "object", "properties": {...}, "required": [...] }
        // Used by agents to understand available parameters
    }
    
    protected T GetParameter<T>(Dictionary<string, object> parameters, string name, T defaultValue)
    {
        // Safe typed parameter retrieval with fallback
    }
    
    // Result formatting
    protected ToolResult CreateSuccessResult(object data)
    {
        // Formats as indented JSON
        return new ToolResult { 
            Success = true,
            FormattedOutput = JsonConvert.SerializeObject(data, Formatting.Indented)
        };
    }
    
    protected ToolResult CreateErrorResult(string message)
    {
        return new ToolResult { Success = false, Error = message };
    }
    
    // Formatting utilities
    protected string FormatPath(string path)           // Truncates to 50 chars, preserves filename
    protected string FormatByteSize(long bytes)        // B, KB, MB, GB, TB
    protected string TruncateString(string s, int len) // With ellipsis
}
```

**Pattern**: Template Method - subclasses provide specifics, base class handles ceremony.

### 1.2 Tool Registration & Discovery

#### ToolRegistry - Singleton Registry

**Location**: `Tools/Core/ToolRegistry.cs`

```csharp
public class ToolRegistry
{
    private static Lazy<ToolRegistry> _instance = new(() => new ToolRegistry());
    public static ToolRegistry Instance => _instance.Value;
    
    private Dictionary<string, ITool> _tools;
    
    public ToolRegistry()
    {
        // AUTO-DISCOVERY: Scan assembly for ITool implementations
        var toolTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(ITool).IsAssignableFrom(t));
        
        // Instantiate each tool via reflection
        foreach (var toolType in toolTypes)
        {
            var instance = Activator.CreateInstance(toolType) as ITool;
            _tools[instance.Name] = instance; // Case-insensitive
        }
    }
    
    // Retrieval
    public ITool? Get(string name) => _tools.TryGetValue(name, out var tool) ? tool : null;
    public List<ITool> GetAll() => _tools.Values.ToList();
    public List<string> GetAllNames() => _tools.Keys.ToList();
    
    // OpenRouter export - creates tool definitions for agent use
    public List<ToolDefinition> GetOpenRouterToolDefinitions()
    {
        return _tools.Values.Select(tool => new ToolDefinition 
        {
            Name = tool.Name,
            Description = tool.Description,
            InputSchema = new JsonSchema 
            {
                Type = "object",
                Properties = tool.GetParameters(),
                Required = tool.GetRequiredParameters()
            }
        }).ToList();
    }
}
```

**Key Design**: 
- Automatic reflection-based discovery (no registration boilerplate)
- Tools discoverable at runtime via `GetOpenRouterToolDefinitions()`
- Used by agents to know which tools are available

### 1.3 Tool Categories & Implementation Patterns

#### Category 1: File System Tools

**Tools**:
- `ReadFileTool` - Read with line ranges, encoding, metadata
- `WriteFileTool` - Atomic write with temp files, overwrite protection
- `DeleteFileTool` - Recursive deletion with dry-run, force options
- `ListFilesTool` - Directory tree with filtering, depth limits

**Pattern Example - ReadFileTool**:

```csharp
public class ReadFileTool : ToolBase
{
    public override string Name => "read_file";
    public override string Description => "Read file contents with optional line range";
    
    protected override List<string> GetRequiredParameters() => new() { "path" };
    
    protected override Dictionary<string, object>? GetParameterProperties() => new()
    {
        { "path", "File path (required)" },
        { "startLine", "1-based line number" },
        { "endLine", "Inclusive end line" },
        { "encoding", "utf8|utf16|utf32|ascii|unicode" },
        { "includeLineNumbers", "Default: true" },
        { "includeMetadata", "Default: true" }
    };
    
    protected override async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var path = GetParameter<string>(parameters, "path", "");
        var startLine = GetParameter<int>(parameters, "startLine", 0);
        var endLine = GetParameter<int>(parameters, "endLine", int.MaxValue);
        var encoding = GetParameter<string>(parameters, "encoding", "utf8");
        var includeLineNumbers = GetParameter<bool>(parameters, "includeLineNumbers", true);
        var includeMetadata = GetParameter<bool>(parameters, "includeMetadata", true);
        
        // Validate path security
        ValidatePathSecurity(path);
        
        // Resolve encoding
        var enc = encoding.ToLower() switch
        {
            "utf16" => Encoding.Unicode,
            "utf32" => Encoding.UTF32,
            "ascii" => Encoding.ASCII,
            _ => Encoding.UTF8
        };
        
        // Read file line-by-line, apply filters
        var lines = new List<string>();
        int lineNum = 1;
        using (var reader = new StreamReader(path, enc))
        {
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (lineNum >= startLine && lineNum <= endLine)
                {
                    if (includeLineNumbers)
                        lines.Add($"{lineNum.ToString().PadLeft(6)}: {line}");
                    else
                        lines.Add(line);
                }
                lineNum++;
            }
        }
        
        // Format result with metadata
        var fileInfo = new FileInfo(path);
        return CreateSuccessResult(new
        {
            File = FormatPath(path),
            Size = FormatByteSize(fileInfo.Length),
            Modified = fileInfo.LastWriteTimeUtc,
            Encoding = enc.EncodingName,
            LineCount = lineNum - 1,
            Content = string.Join(Environment.NewLine, lines)
        });
    }
}
```

**Key Patterns**:
- Path validation for security
- Streaming for large files
- Metadata included in results
- Consistent error handling

#### Category 2: Search & Pattern Tools

**Tools**:
- `GrepTool` - Regex search with recursive, case-sensitivity options
- `GlobTool` - File pattern matching using Microsoft.Extensions.FileSystemGlobbing
- `SearchAndReplaceTool` - Regex search/replace with dry-run mode

**Pattern Example - GlobTool**:

```csharp
public class GlobTool : ToolBase
{
    public override string Name => "glob";
    public override string Description => "Find files matching patterns";
    
    protected override async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var patterns = GetParameter<string>(parameters, "patterns", "");
        var path = GetParameter<string>(parameters, "path", ".");
        var caseSensitive = GetParameter<bool>(parameters, "caseSensitive", false);
        var maxDepth = GetParameter<int>(parameters, "maxDepth", int.MaxValue);
        var maxResults = GetParameter<int>(parameters, "maxResults", 1000);
        
        // Parse patterns: comma/semicolon separated, support "!" negation
        var patternList = patterns.Split(new[] { ',', ';' }, StringSplitOptions.TrimEntries);
        
        var matcher = new Matcher(
            caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase
        );
        
        // Add positive and negative patterns
        foreach (var pattern in patternList)
        {
            if (pattern.StartsWith("!"))
                matcher.AddExclude(pattern.Substring(1));
            else
                matcher.AddInclude(pattern);
        }
        
        // Execute matching
        var dirWrapper = new DirectoryInfoWrapper(new DirectoryInfo(path));
        var matchResult = matcher.Execute(dirWrapper);
        
        // Process results with depth, symlink, and size filters
        var results = matchResult.Files
            .Where(f => CountDepth(f.Path) <= maxDepth)
            .Take(maxResults)
            .Select(f => new
            {
                Path = f.Path,
                Size = FormatByteSize(new FileInfo(f.Path).Length),
                Modified = new FileInfo(f.Path).LastWriteTimeUtc
            })
            .OrderBy(r => r.Path)
            .ToList();
        
        return CreateSuccessResult(new
        {
            Matches = results.Count,
            Files = results
        });
    }
}
```

**Key Patterns**:
- Supports multiple wildcards: `*` (path segment), `**` (recursive), `?` (single char)
- Negation patterns (exclusions) via `!` prefix
- Sorting and result limiting
- Integration with Microsoft's globbing library

#### Category 3: Command Execution Tool

**Pattern Example - ExecuteCommandTool**:

```csharp
public class ExecuteCommandTool : ToolBase
{
    public override string Name => "execute";
    public override string Description => "Execute shell commands";
    
    protected override async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var command = GetParameter<string>(parameters, "command", "");
        var workingDirectory = GetParameter<string>(parameters, "workingDirectory", ".");
        var timeoutMs = GetParameter<int>(parameters, "timeout", 30000);
        var requireApproval = GetParameter<bool>(parameters, "requireApproval", true);
        var captureOutput = GetParameter<bool>(parameters, "captureOutput", true);
        
        // Security check - optional approval
        if (requireApproval && AgentContext.RequireCommandApproval)
        {
            var approved = await _approvalService.RequestApprovalAsync(command);
            if (!approved)
                return CreateErrorResult($"Command execution denied: {command}");
        }
        
        // Platform detection for shell
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var shellCommand = isWindows 
            ? $"cmd.exe /c {command}"
            : $"/bin/sh -c {command}";
        
        // Process setup
        var psi = new ProcessStartInfo
        {
            FileName = isWindows ? "cmd.exe" : "/bin/sh",
            Arguments = isWindows ? $"/c {command}" : $"-c {command}",
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = captureOutput,
            RedirectStandardError = captureOutput,
            CreateNoWindow = true
        };
        
        // Async execution with timeout
        using (var process = Process.Start(psi))
        {
            var cts = new CancellationTokenSource(timeoutMs);
            var output = captureOutput 
                ? await process.StandardOutput.ReadToEndAsync(cts.Token)
                : "Output not captured";
            var error = captureOutput
                ? await process.StandardError.ReadToEndAsync(cts.Token)
                : "";
            
            var completedInTime = await process.WaitForExitAsync(cts.Token);
            
            if (!completedInTime)
            {
                process.Kill();
                return CreateErrorResult($"Command timeout after {timeoutMs}ms");
            }
            
            return CreateSuccessResult(new
            {
                ExitCode = process.ExitCode,
                Output = TruncateString(output, 1_000_000), // Max 1MB
                Error = TruncateString(error, 1_000_000),
                Duration = $"{sw.ElapsedMilliseconds}ms"
            });
        }
    }
}
```

**Key Patterns**:
- Cross-platform shell detection (cmd.exe vs /bin/sh)
- Optional approval before execution (security gate)
- Timeout enforcement with process killing
- Output truncation (max 1MB per stream)
- Exit code and error capture

#### Category 4: Web Access

**Pattern Example - WebFetchTool**:

```csharp
public class WebFetchTool : ToolBase
{
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(30) };
    private static readonly Dictionary<string, CachedContent> _cache = new();
    
    public override string Name => "web_fetch";
    public override string Description => "Fetch and parse web content";
    
    protected override async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var url = GetParameter<string>(parameters, "url", "");
        var mode = GetParameter<string>(parameters, "mode", "full"); // full|article|text|markdown
        var headers = GetParameter<Dictionary<string, string>>(parameters, "headers", new());
        
        // Security: Block dangerous schemes
        if (url.StartsWith("file://") || url.StartsWith("ftp://"))
            return CreateErrorResult("Dangerous URL scheme");
        
        // Security: Block localhost/private IPs
        var uri = new Uri(url);
        var ipAddress = Dns.GetHostAddresses(uri.Host).FirstOrDefault();
        if (ipAddress?.IsPrivate() == true)
            return CreateErrorResult("Access to private IP blocked");
        
        // Check cache (5 minute TTL)
        if (_cache.TryGetValue(url, out var cached) && cached.Age < TimeSpan.FromMinutes(5))
            return ProcessContent(cached.Content, mode);
        
        // Fetch with browser headers
        using (var request = new HttpRequestMessage(HttpMethod.Get, url))
        {
            request.Headers.Add("User-Agent", "Mozilla/5.0...");
            foreach (var (key, value) in headers)
                request.Headers.Add(key, value);
            
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            
            var html = await response.Content.ReadAsStringAsync();
            _cache[url] = new CachedContent { Content = html, CachedAt = DateTime.UtcNow };
            
            return ProcessContent(html, mode);
        }
    }
    
    private ToolResult ProcessContent(string html, string mode)
    {
        // Parse HTML
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        
        // Remove unwanted elements
        doc.DocumentNode
            .SelectNodes("//script | //style | //iframe")
            .ForEach(n => n.Remove());
        
        return mode switch
        {
            "full" => ConvertToMarkdown(html),              // Full HTML to markdown
            "article" => ExtractMainContent(doc),           // Main article only
            "text" => ExtractPlainText(doc),               // Plain text
            "markdown" => ConvertToMarkdown(html),          // Markdown format
            _ => CreateErrorResult($"Unknown mode: {mode}")
        };
    }
}
```

**Key Patterns**:
- SSRF protection (scheme/IP blocking)
- Caching (5-minute TTL reduces redundant requests)
- Multiple output formats
- HTML cleaning (remove scripts/styles)
- Markdown conversion via ReverseMarkdown

#### Category 5: Diff/Patch Application

**Pattern Example - ApplyDiffTool**:

```csharp
public class ApplyDiffTool : ToolBase
{
    private static readonly HashSet<string> FileLocks = new();
    
    public override string Name => "apply_diff";
    public override string Description => "Apply patch files to modify code";
    
    protected override async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var patchText = GetParameter<string>(parameters, "patch", "");
        var dryRun = GetParameter<bool>(parameters, "dryRun", false);
        var workingDirectory = GetParameter<string>(parameters, "workingDirectory", ".");
        
        // Validate all files referenced in patch
        var filesToModify = ExtractFilesFromPatch(patchText);
        foreach (var file in filesToModify)
        {
            if (!ValidatePathSecurity(file))
                return CreateErrorResult($"Invalid path: {file}");
            
            var info = new FileInfo(file);
            if (info.Length > 100_000_000) // 100MB limit
                return CreateErrorResult($"File too large: {file}");
        }
        
        // Acquire locks to prevent concurrent modifications
        if (!filesToModify.All(f => FileLocks.Add(f)))
            return CreateErrorResult("Files locked by another operation");
        
        try
        {
            // Parse patch into operations
            var operations = ParsePatchOperations(patchText);
            
            var statistics = new PatchStatistics();
            var commit = new Commit();
            
            foreach (var op in operations)
            {
                var result = await ApplyOperation(op, workingDirectory, dryRun);
                if (!result.Success)
                    return CreateErrorResult(result.Error);
                
                statistics.AddedLines += result.AddedCount;
                statistics.RemovedLines += result.RemovedCount;
                statistics.AffectedFiles++;
            }
            
            if (!dryRun)
            {
                // Apply changes and preserve line endings
                foreach (var file in filesToModify)
                {
                    await File.WriteAllBytesAsync(file, commit.GetFileBytes(file));
                }
            }
            
            return CreateSuccessResult(statistics);
        }
        finally
        {
            // Release locks
            foreach (var file in filesToModify)
                FileLocks.Remove(file);
        }
    }
    
    private PatchOperation[] ParsePatchOperations(string patch)
    {
        // Recognizes:
        // *** Add File: path
        // *** Update File: path
        // *** Delete File: path
        // With context/diff markers (+, -, space)
        
        var operations = new List<PatchOperation>();
        var lines = patch.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("*** Add File:"))
            {
                var path = lines[i].Substring("*** Add File: ".Length).Trim();
                var content = ExtractContentBlock(lines, ref i);
                operations.Add(new PatchOperation 
                { 
                    Type = OperationType.Add,
                    FilePath = path,
                    Content = content
                });
            }
            else if (lines[i].StartsWith("*** Update File:"))
            {
                var path = lines[i].Substring("*** Update File: ".Length).Trim();
                var hunks = ExtractHunks(lines, ref i);
                operations.Add(new PatchOperation
                {
                    Type = OperationType.Update,
                    FilePath = path,
                    Hunks = hunks
                });
            }
            else if (lines[i].StartsWith("*** Delete File:"))
            {
                var path = lines[i].Substring("*** Delete File: ".Length).Trim();
                operations.Add(new PatchOperation
                {
                    Type = OperationType.Delete,
                    FilePath = path
                });
            }
        }
        
        return operations.ToArray();
    }
}
```

**Key Patterns**:
- Patch format: `*** Add/Update/Delete File: path`
- Hunk context markers for line-accurate application
- File locking to prevent concurrent modifications
- Dry-run mode for validation
- Line ending preservation (CRLF vs LF)
- Pre-flight validation before modification

### 1.4 Tool Result Objects

**Location**: `Tools/Objects/ToolResult.cs`

```csharp
public class ToolResult
{
    public bool Success { get; set; }
    public string FormattedOutput { get; set; }  // Human-readable (JSON formatted)
    public object? RawData { get; set; }          // Structured data for agents
    public string? Error { get; set; }            // Error message if failed
}
```

**Design**: Dual output format
- `FormattedOutput` for human consumption (indented JSON)
- `RawData` for agent consumption (structured object)

---

## Part 2: Multi-Agent System

### 2.1 Agent Execution Model

#### AgentBase - Core Agent Implementation

**Location**: `Agents/Core/AgentBase.cs`

```csharp
public abstract class AgentBase : IDisposable
{
    public string Id { get; } = Guid.NewGuid().ToString();
    public string Name { get; }
    public string SystemPrompt { get; }
    
    private List<ChatMessage> ChatHistory { get; } = new();
    private string CurrentSessionId { get; set; }
    
    // EXECUTION MODELS
    
    // Synchronous execution with tools
    public T Execute<T>(string prompt) where T : class
    {
        // Add user message
        ChatHistory.Add(new ChatMessage 
        { 
            Role = "user", 
            Content = prompt 
        });
        
        // Get LLM response (with tool calling support)
        var response = Client.CreateMessage(new CreateMessageRequest
        {
            Model = Configuration.Model,
            SystemPrompt = SystemPrompt,
            Messages = ChatHistory,
            Tools = ToolRegistry.Instance.GetOpenRouterToolDefinitions(), // Available tools
            Temperature = Configuration.Temperature,
            MaxTokens = Configuration.MaxTokens
        });
        
        ChatHistory.Add(new ChatMessage { Role = "assistant", Content = response.Content });
        
        // Handle tool calls
        if (response.StopReason == "tool_use")
        {
            var toolResults = HandleToolCalls(response.ToolCalls);
            ChatHistory.AddRange(toolResults);
            
            // Continue with tool results
            return Execute<T>(null); // Recursive call with tool context
        }
        
        return JsonConvert.DeserializeObject<T>(response.Content);
    }
    
    // Streaming execution for real-time response
    public async IAsyncEnumerable<string> ExecuteStreamAsync(string prompt)
    {
        ChatHistory.Add(new ChatMessage { Role = "user", Content = prompt });
        
        await foreach (var chunk in Client.StreamMessageAsync(new StreamRequest
        {
            Model = Configuration.Model,
            SystemPrompt = SystemPrompt,
            Messages = ChatHistory,
            Tools = ToolRegistry.Instance.GetOpenRouterToolDefinitions()
        }))
        {
            yield return chunk;
            
            // Handle tool calls in streaming context
            if (chunk.StopReason == "tool_use")
            {
                var toolResults = HandleToolCalls(chunk.ToolCalls);
                foreach (var result in toolResults)
                    yield return result.Content;
            }
        }
    }
    
    // TOOL HANDLING
    
    private List<ChatMessage> HandleToolCalls(List<ToolCall> toolCalls)
    {
        var results = new List<ChatMessage>();
        
        foreach (var toolCall in toolCalls)
        {
            try
            {
                var tool = ToolRegistry.Instance.Get(toolCall.Name);
                if (tool == null)
                    throw new InvalidOperationException($"Unknown tool: {toolCall.Name}");
                
                // Execute tool with parameters
                var toolResult = await tool.ExecuteAsync(toolCall.Input);
                
                // Record tool call and result
                PersistToolCallAsync(toolCall.Id, toolCall.Name, toolCall.Input);
                
                results.Add(new ChatMessage
                {
                    Role = "user",
                    Content = toolResult.FormattedOutput,
                    ToolUseId = toolCall.Id,
                    ToolName = toolCall.Name
                });
            }
            catch (Exception ex)
            {
                results.Add(new ChatMessage
                {
                    Role = "user",
                    Content = $"Error executing {toolCall.Name}: {ex.Message}",
                    ToolUseId = toolCall.Id
                });
            }
        }
        
        return results;
    }
    
    // HISTORY MANAGEMENT
    
    public List<ChatMessage> GetHistory() => new(ChatHistory);
    
    public void ClearHistory()
    {
        ChatHistory.Clear();
        CurrentSessionId = Guid.NewGuid().ToString();
    }
    
    public void TrimHistory()
    {
        // Keep only most recent N messages per configuration
        if (ChatHistory.Count > Configuration.MaxHistoryMessages)
        {
            ChatHistory.RemoveRange(0, ChatHistory.Count - Configuration.MaxHistoryMessages);
        }
    }
    
    // PERSISTENCE
    
    public async Task PersistMessageAsync(ChatMessage message)
    {
        // Save to database for audit trail
        await MessageRepository.InsertAsync(new StoredMessage
        {
            AgentId = Id,
            SessionId = CurrentSessionId,
            Role = message.Role,
            Content = message.Content,
            Timestamp = DateTime.UtcNow
        });
    }
    
    public async Task FlushPendingMessagesAsync()
    {
        // Batch save messages since last flush
        var pendingMessages = ChatHistory.Where(m => !m.Persisted);
        foreach (var msg in pendingMessages)
        {
            await PersistMessageAsync(msg);
            msg.Persisted = true;
        }
    }
}
```

**Key Design Patterns**:
1. **Template Method** - Subclasses provide configuration, base handles execution
2. **Tool Integration** - Agents discover tools from registry at execution time
3. **Chat History** - Maintains context across multiple turns
4. **Dual Execution Models** - Sync and streaming for different use cases
5. **Auto-Tool-Calling** - LLM directs tool usage, agent handles execution

### 2.2 Agent Configuration

**Location**: `Agents/Core/AgentConfiguration.cs`

```csharp
public class AgentConfiguration
{
    // REQUIRED
    public required string Name { get; set; }
    public required string SystemPrompt { get; set; }
    public required OpenRouterClient Client { get; set; }
    
    // MODEL PARAMETERS
    public string Model { get; set; } = "anthropic/claude-sonnet-4";
    public double Temperature { get; set; } = 1.0;          // 0-2 randomness
    public int MaxTokens { get; set; } = 16000;
    public double TopP { get; set; } = 1.0;                 // Nucleus sampling
    public double FrequencyPenalty { get; set; } = 0.0;
    public double PresencePenalty { get; set; } = 0.0;
    public List<string>? StopSequences { get; set; }
    
    // BEHAVIOR
    public bool MaintainHistory { get; set; } = true;       // Keep chat context
    public int MaxHistoryMessages { get; set; } = 20;       // Retention limit
    public bool EnableTools { get; set; } = true;           // Use tool calling
    public List<string> ToolNames { get; set; } = new();    // Specific tools only
    public bool EnableStreaming { get; set; } = false;      // Real-time output
    public int StreamBufferSize { get; set; } = 1024;       // Chunk size
    public bool RequireCommandApproval { get; set; } = true; // Gate dangerous ops
    public bool EnableUserRules { get; set; } = true;       // Apply constraints
    public string? CurrentModeId { get; set; }              // Operating mode
    
    // FACTORY
    public static AgentConfiguration FromMode(Mode mode, AgentConfigurationOverrides? overrides = null)
    {
        var config = new AgentConfiguration
        {
            Name = overrides?.Name ?? mode.Name,
            SystemPrompt = overrides?.SystemPrompt ?? mode.SystemPrompt,
            Model = overrides?.Model ?? mode.Model,
            Temperature = overrides?.Temperature ?? mode.Temperature,
            // ... all other properties
        };
        return config;
    }
}
```

**Design Pattern**: Configuration Object with factory methods for different operating modes.

### 2.3 Multi-Agent Orchestration

#### AgentManager - Central Coordination

**Location**: `Agents/MultiAgent/AgentManager.cs`

```csharp
public class AgentManager
{
    private static Lazy<AgentManager> _instance = new(() => new AgentManager());
    public static AgentManager Instance => _instance.Value;
    
    // CONCURRENCY MANAGEMENT
    private const int MaxConcurrentAgents = 25;
    private SemaphoreSlim _agentSemaphore = new(MaxConcurrentAgents);
    private SemaphoreSlim _reviewerSemaphore = new(MaxConcurrentAgents);
    
    // STATE TRACKING
    private ConcurrentDictionary<string, AgentInstance> _runningAgents = new();
    private ConcurrentDictionary<string, AgentTaskResult> _completedTasks = new();
    private ConcurrentDictionary<string, ReviewerInstance> _activeReviewers = new();
    
    // EVENT PUBLISHING
    public event EventHandler<AgentStatusChangedEventArgs>? AgentStatusChanged;
    public event EventHandler<TaskCompletedEventArgs>? TaskCompleted;
    
    // CREATION
    
    public bool TryCreateSubAgent(
        string name,
        string purpose,
        SubAgentPreferences preferences)
    {
        // Check capacity
        if (_runningAgents.Count >= MaxConcurrentAgents)
        {
            var running = _runningAgents.Values.Select(a => a.TaskId).ToList();
            throw new InvalidOperationException(
                $"Max agents ({MaxConcurrentAgents}) reached. " +
                $"Running tasks: {string.Join(", ", running)}");
        }
        
        // Create agent
        var agentId = Guid.NewGuid().ToString();
        var config = new AgentConfiguration
        {
            Name = name,
            SystemPrompt = $"You are {name}. Your purpose: {purpose}",
            Model = preferences.Model,
            Temperature = preferences.Temperature,
            MaxTokens = preferences.MaxTokens,
            EnableTools = preferences.ToolsEnabled
        };
        
        var agent = new Agent(config);
        
        var agentInstance = new AgentInstance
        {
            AgentId = agentId,
            Agent = agent,
            Name = name,
            Purpose = purpose,
            Status = AgentStatus.Idle,
            CreatedAt = DateTime.UtcNow,
            Context = new SubAgentContext { AgentId = agentId, Name = name, Purpose = purpose }
        };
        
        _runningAgents.TryAdd(agentId, agentInstance);
        AgentStatusChanged?.Invoke(this, new AgentStatusChangedEventArgs { AgentId = agentId });
        
        return true;
    }
    
    // TASK DISTRIBUTION
    
    public string HandOffTask(
        string agentId,
        string taskDescription,
        string? context = null)
    {
        if (!_runningAgents.TryGetValue(agentId, out var agentInstance))
            throw new InvalidOperationException($"Agent {agentId} not found");
        
        var taskId = Guid.NewGuid().ToString();
        var task = new AgentTask
        {
            TaskId = taskId,
            AgentId = agentId,
            Description = taskDescription,
            Context = context,
            Status = TaskStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        
        // Execute task asynchronously
        _ = ExecuteTaskAsync(agentInstance, task);
        
        // Return immediately with task ID for parallel execution
        return taskId;
    }
    
    private async Task ExecuteTaskAsync(AgentInstance agentInstance, AgentTask task)
    {
        try
        {
            agentInstance.Status = AgentStatus.Busy;
            agentInstance.CurrentTaskId = task.TaskId;
            AgentStatusChanged?.Invoke(this, new AgentStatusChangedEventArgs { AgentId = agentInstance.AgentId });
            
            // Execute on agent
            var startTime = DateTime.UtcNow;
            var result = await agentInstance.Agent.ExecuteAsync(task.Description);
            var duration = DateTime.UtcNow - startTime;
            
            // Store result
            var taskResult = new AgentTaskResult
            {
                TaskId = task.TaskId,
                AgentId = agentInstance.AgentId,
                Success = result.Success,
                Result = result.Data,
                Duration = duration,
                CompletedAt = DateTime.UtcNow
            };
            
            _completedTasks.TryAdd(task.TaskId, taskResult);
            
            // OPTIONAL: Review phase
            if (_reviewConfiguration.EnableReview)
            {
                await ExecuteReviewPhaseAsync(task, taskResult);
            }
            
            TaskCompleted?.Invoke(this, new TaskCompletedEventArgs { TaskId = task.TaskId });
        }
        catch (Exception ex)
        {
            // Error handling
            var taskResult = new AgentTaskResult
            {
                TaskId = task.TaskId,
                AgentId = agentInstance.AgentId,
                Success = false,
                Error = ex.Message
            };
            _completedTasks.TryAdd(task.TaskId, taskResult);
        }
        finally
        {
            agentInstance.Status = AgentStatus.Idle;
            agentInstance.CurrentTaskId = null;
        }
    }
    
    // REVIEW PHASE (Optional)
    
    private async Task ExecuteReviewPhaseAsync(AgentTask originalTask, AgentTaskResult initialResult)
    {
        await _reviewerSemaphore.WaitAsync();
        
        try
        {
            var reviewerId = Guid.NewGuid().ToString();
            var reviewerInstance = new ReviewerInstance
            {
                ReviewerId = reviewerId,
                OriginalTaskId = originalTask.TaskId,
                Status = ReviewStatus.InProgress,
                CreatedAt = DateTime.UtcNow
            };
            
            _activeReviewers.TryAdd(reviewerId, reviewerInstance);
            
            // Create reviewer agent
            var reviewerConfig = new AgentConfiguration
            {
                Name = "Reviewer",
                SystemPrompt = "You are a code reviewer. Evaluate quality and compliance.",
                Model = "anthropic/claude-sonnet-4"
            };
            var reviewer = new Agent(reviewerConfig);
            
            // Review multiple times if needed
            var currentResult = initialResult;
            int iteration = 0;
            int maxIterations = _reviewConfiguration.MaxReviewIterations ?? 1;
            
            while (iteration < maxIterations)
            {
                var reviewPrompt = $"Review this result:\n{currentResult.Result}\n\nProvide feedback.";
                var reviewDecision = await reviewer.ExecuteAsync<ReviewDecision>(reviewPrompt);
                
                if (reviewDecision.Status == ReviewStatus.Approved)
                {
                    // Approved - update original result
                    initialResult.ReviewApproved = true;
                    break;
                }
                else if (reviewDecision.Status == ReviewStatus.RequestsRevision)
                {
                    // Request changes from original agent
                    var revisionTask = new AgentTask
                    {
                        TaskId = Guid.NewGuid().ToString(),
                        AgentId = originalTask.AgentId,
                        Description = $"Revise based on feedback: {reviewDecision.Feedback}",
                        Status = TaskStatus.Pending
                    };
                    
                    var agentInstance = _runningAgents[originalTask.AgentId];
                    currentResult = await agentInstance.Agent.ExecuteAsync(revisionTask.Description);
                    iteration++;
                }
                else
                {
                    // Rejected - stop review
                    initialResult.ReviewApproved = false;
                    break;
                }
            }
            
            reviewerInstance.Status = ReviewStatus.Completed;
        }
        finally
        {
            _reviewerSemaphore.Release();
        }
    }
    
    // TASK AWAITING
    
    public async Task<AgentTaskResult> WaitForTaskAsync(
        string taskId,
        int timeoutMs = 30000)
    {
        var sw = Stopwatch.StartNew();
        
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            if (_completedTasks.TryGetValue(taskId, out var result))
                return result;
            
            await Task.Delay(100); // Poll every 100ms
        }
        
        throw new TimeoutException($"Task {taskId} did not complete within {timeoutMs}ms");
    }
    
    public async Task<List<AgentTaskResult>> WaitForAllTasksAsync(
        List<string> taskIds,
        int timeoutMs = 30000)
    {
        var sw = Stopwatch.StartNew();
        var results = new List<AgentTaskResult>();
        
        foreach (var taskId in taskIds)
        {
            var remaining = timeoutMs - (int)sw.ElapsedMilliseconds;
            if (remaining <= 0)
                throw new TimeoutException($"Timeout waiting for tasks");
            
            if (_completedTasks.TryGetValue(taskId, out var result))
            {
                results.Add(result);
            }
            else
            {
                await WaitForTaskAsync(taskId, remaining);
                results.Add(_completedTasks[taskId]);
            }
        }
        
        return results;
    }
    
    // STATUS QUERIES
    
    public AgentStatusInfo GetAgentStatus(string agentId)
    {
        if (!_runningAgents.TryGetValue(agentId, out var instance))
            return new AgentStatusInfo { Exists = false };
        
        return new AgentStatusInfo
        {
            AgentId = agentId,
            Name = instance.Name,
            Status = instance.Status.ToString(),
            CurrentTask = instance.CurrentTaskId ?? "None",
            IsIdle = instance.Status == AgentStatus.Idle,
            DurationSeconds = (DateTime.UtcNow - instance.CreatedAt).TotalSeconds
        };
    }
    
    public List<AgentStatusInfo> GetAllAgentStatuses()
    {
        return _runningAgents.Values
            .Select(instance => GetAgentStatus(instance.AgentId))
            .ToList();
    }
    
    // TERMINATION
    
    public void TerminateAgent(string agentId)
    {
        if (_runningAgents.TryRemove(agentId, out var instance))
        {
            instance.Agent.Dispose();
            AgentStatusChanged?.Invoke(this, new AgentStatusChangedEventArgs 
            { 
                AgentId = agentId,
                NewStatus = "Terminated"
            });
        }
    }
    
    // RESULT RETRIEVAL
    
    public AgentTaskResult? GetTaskResult(string taskId)
    {
        _completedTasks.TryGetValue(taskId, out var result);
        return result;
    }
}
```

**Key Design Patterns**:

1. **Singleton Pattern** - Single global AgentManager instance
2. **Concurrency Control** - `SemaphoreSlim` for agent capacity (max 25)
3. **Asynchronous Task Distribution** - `HandOffTask` returns immediately with task ID
4. **Concurrent Collections** - Thread-safe tracking of agents and tasks
5. **Event-Driven Updates** - Status changes and completions raise events
6. **Optional Review Phase** - Reviewer agents validate original agent outputs
7. **Timeout Management** - Polling with configurable timeouts for task awaiting
8. **Graceful Degradation** - Provides helpful error messages when capacity exceeded

### 2.4 Multi-Agent Tools

These tools allow agents to coordinate with each other:

#### CreateAgentTool

```csharp
public class CreateAgentTool : ToolBase
{
    public override string Name => "create_agent";
    public override string Description => "Create a new sub-agent for parallel work";
    
    protected override async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var agentName = GetParameter<string>(parameters, "name", "");
        var purpose = GetParameter<string>(parameters, "purpose", "");
        var preferences = SubAgentPreferences.Instance;
        
        try
        {
            var created = AgentManager.Instance.TryCreateSubAgent(agentName, purpose, preferences);
            if (!created)
                throw new InvalidOperationException("Failed to create agent");
            
            var statuses = AgentManager.Instance.GetAllAgentStatuses();
            var current = statuses.Count(s => s.Status == "Running");
            var max = 25; // Max concurrent agents
            
            return CreateSuccessResult(new
            {
                AgentName = agentName,
                Purpose = purpose,
                Status = "Created",
                CurrentAgents = current,
                MaxAgents = max,
                Configuration = new
                {
                    Model = preferences.Model,
                    Temperature = preferences.Temperature,
                    MaxTokens = preferences.MaxTokens,
                    ToolsEnabled = preferences.ToolsEnabled
                }
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Max agents"))
        {
            // Provide helpful guidance when capacity exceeded
            var statuses = AgentManager.Instance.GetAllAgentStatuses();
            var running = statuses.Where(s => s.Status == "Running").Select(s => s.CurrentTask).ToList();
            
            return CreateErrorResult(
                $"Maximum agents ({25}) reached. " +
                $"Current: {statuses.Count}. " +
                $"Running tasks: {string.Join(", ", running)}. " +
                $"Terminate idle agents or wait for tasks to complete.");
        }
    }
}
```

#### HandOffToAgentTool

```csharp
public class HandOffToAgentTool : ToolBase
{
    public override string Name => "handoff_agent";
    public override string Description => "Assign work to another agent";
    
    protected override async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var agentId = GetParameter<string>(parameters, "agent_id", "");
        var taskDescription = GetParameter<string>(parameters, "task", "");
        var context = GetParameter<string>(parameters, "context", "");
        
        var taskId = AgentManager.Instance.HandOffTask(agentId, taskDescription, context);
        
        return CreateSuccessResult(new
        {
            TaskId = taskId,
            AgentId = agentId,
            Status = "Handed off",
            CanWaitFor = true
        });
    }
}
```

#### WaitForAgentTool

```csharp
public class WaitForAgentTool : ToolBase
{
    public override string Name => "wait_for_agent";
    public override string Description => "Wait for agent task completion";
    
    protected override async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var taskIds = GetParameter<List<string>>(parameters, "task_ids", new());
        var timeoutMs = GetParameter<int>(parameters, "timeout", 30000);
        
        var results = await AgentManager.Instance.WaitForAllTasksAsync(taskIds, timeoutMs);
        
        return CreateSuccessResult(new
        {
            TaskCount = results.Count,
            Results = results.Select(r => new
            {
                TaskId = r.TaskId,
                AgentId = r.AgentId,
                Success = r.Success,
                Duration = $"{r.Duration.TotalSeconds}s",
                Result = r.Result
            }).ToList()
        });
    }
}
```

#### GetTaskResultTool

```csharp
public class GetTaskResultTool : ToolBase
{
    public override string Name => "get_task_result";
    public override string Description => "Retrieve task result without waiting";
    
    protected override async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var taskId = GetParameter<string>(parameters, "task_id", "");
        var result = AgentManager.Instance.GetTaskResult(taskId);
        
        if (result == null)
            return CreateErrorResult($"Task {taskId} not found or still running");
        
        return CreateSuccessResult(new
        {
            TaskId = taskId,
            AgentId = result.AgentId,
            Success = result.Success,
            Result = result.Result,
            Duration = $"{result.Duration.TotalSeconds}s",
            CompletedAt = result.CompletedAt
        });
    }
}
```

---

## Part 3: Key Architectural Patterns & Abstractions

### 3.1 Pattern Summary

| Pattern | Location | Purpose |
|---------|----------|---------|
| **Singleton** | ToolRegistry, AgentManager | Global access to tools and agents |
| **Template Method** | ToolBase, AgentBase | Enforce consistent structure |
| **Strategy** | ITool implementations | Pluggable tool behavior |
| **Factory** | AgentConfiguration.FromMode | Create configured agents |
| **Observer** | AgentManager events | Broadcast status changes |
| **Registry** | ToolRegistry | Runtime tool discovery |
| **Concurrent Collections** | AgentManager dictionaries | Thread-safe state tracking |

### 3.2 Configuration System

**AgentContext** provides shared configuration:

```csharp
public static class AgentContext
{
    public static AgentConfiguration? CurrentConfiguration { get; set; }
    public static bool RequireCommandApproval => CurrentConfiguration?.RequireCommandApproval ?? true;
}
```

**SystemPrompt** builder creates dynamic instructions:

```csharp
public static class SystemPrompt
{
    public static async Task<string> Create(
        string basePrompt,
        bool includeDirectory = true,
        bool includeUserRules = true)
    {
        var parts = new List<string> { basePrompt };
        
        if (includeDirectory)
        {
            var dirView = await GenerateDirectoryView();
            parts.Add($"<current_directory>\n{dirView}\n</current_directory>");
        }
        
        if (includeUserRules)
        {
            var rules = await UserRulesManager.LoadUserRules();
            parts.Add($"<user_rules>\n{rules}\n</user_rules>");
        }
        
        return string.Join("\n\n", parts);
    }
}
```

### 3.3 Error Handling Patterns

**Tool-level validation**:
```csharp
// Path security validation
private void ValidatePathSecurity(string path)
{
    var fullPath = Path.GetFullPath(path);
    var workingDir = Directory.GetCurrentDirectory();
    
    if (!fullPath.StartsWith(workingDir))
        throw new InvalidOperationException("Path outside working directory");
    
    if (path.Contains("..") || path.StartsWith("~"))
        throw new InvalidOperationException("Path traversal detected");
}

// Size validation
if (fileSize > 100_000_000) // 100MB
    throw new InvalidOperationException($"File too large: {fileSize} bytes");

// Timeout handling
using var cts = new CancellationTokenSource(timeoutMs);
try
{
    await LongRunningOperation(cts.Token);
}
catch (OperationCanceledException)
{
    process?.Kill();
    throw new TimeoutException($"Operation timeout after {timeoutMs}ms");
}
```

**Agent-level error recovery**:
```csharp
try
{
    var result = await tool.ExecuteAsync(parameters);
    if (!result.Success)
    {
        // Graceful degradation - record error and continue
        ChatHistory.Add(new ChatMessage
        {
            Role = "user",
            Content = $"Tool error: {result.Error}"
        });
    }
}
catch (Exception ex)
{
    // Log and recover
    Logger.LogError($"Tool {toolName} failed: {ex.Message}");
    // Provide error context back to LLM
}
```

---

## Part 4: Integration Considerations for PRFactory

### 4.1 What to Reuse from Saturn

#### 1. **Tool Base Architecture** - HIGHLY RECOMMENDED

Saturn's `ITool` and `ToolBase` pattern is production-proven:
- Reflection-based auto-discovery
- Consistent parameter handling
- Unified result formatting
- Easy to implement new tools

**Recommendation**: Adopt Saturn's tool pattern directly or create compatible interface.

#### 2. **Tool Registry Pattern** - HIGHLY RECOMMENDED

The registry provides:
- Dynamic tool discovery
- OpenRouter format export (for agent use)
- Case-insensitive lookup
- Zero boilerplate registration

**Recommendation**: Implement `IToolRegistry` with similar auto-discovery.

#### 3. **File System Tool Suite** - REUSE CORE LOGIC

Saturn's file tools are production-hardened:
- Path security validation
- Atomic writes (temporary files)
- File locking for concurrent operations
- Size limits and timeout enforcement

**Recommendation**: Port or adapt ReadFileTool, WriteFileTool, DeleteFileTool, ApplyDiffTool.

#### 4. **Multi-Agent Coordination** - CONSIDER WITH MODIFICATIONS

Saturn's `AgentManager` provides:
- Capacity management (max 25 concurrent agents)
- Asynchronous task distribution
- Task result caching
- Status tracking and queries
- Optional review phase for quality gates

**Recommendation**: Consider for parallel agent workflows in phase 3+ roadmap.

### 4.2 What to Do Differently with Microsoft Agent Framework

#### 1. **Use Microsoft Agent Framework Instead of Direct OpenRouter**

**Saturn Approach**:
```csharp
// Direct OpenRouter client
var response = Client.CreateMessage(new CreateMessageRequest
{
    Model = "anthropic/claude-sonnet-4",
    Tools = ToolRegistry.Instance.GetOpenRouterToolDefinitions()
});
```

**PRFactory Approach with Microsoft Agent Framework**:
```csharp
// Use Microsoft's abstraction
var agent = new Agent(
    instructions: systemPrompt,
    model: new AnthropicChatCompletionService("claude-sonnet-4"),
    tools: toolRegistry.GetToolDefinitions()
);

var response = await agent.InvokeAsync(userMessage);
```

**Benefits**:
- Model-agnostic (supports Anthropic, OpenAI, Azure, etc.)
- Standardized tool format
- Better integration with .NET ecosystem
- Future-proof for multi-model support

#### 2. **Integrate with PRFactory's Graph System**

Don't replace the graph architecture:

```csharp
// PRFactory approach - use agents within graphs
public class RefinementGraph : AgentGraphBase
{
    public override async Task<GraphOutput> ExecuteAsync(GraphInput input)
    {
        // Create agents as nodes in the graph
        var refinementAgent = new Agent(
            instructions: SystemPrompt.Create("Refine requirements"),
            model: GetModel(),
            tools: GetRefinementTools()
        );
        
        // Agent is a step, not the graph itself
        var refinedRequirements = await refinementAgent.InvokeAsync(input.Requirements);
        
        // Graph orchestrates: agent execution -> checkpoint -> approval
        return new GraphOutput { Status = "awaiting_approval", Data = refinedRequirements };
    }
}
```

#### 3. **Keep Separation of Concerns**

**Saturn conflates**:
- Agent execution (AgentBase)
- Tool invocation (tool.ExecuteAsync)
- Orchestration (AgentManager)

**PRFactory should maintain**:
- **Agent Layer**: LLM interaction + tool calling (from Microsoft Framework)
- **Tool Layer**: Isolated, testable capabilities (port from Saturn)
- **Graph Layer**: Workflow orchestration (existing PRFactory architecture)
- **Application Layer**: Business logic (TicketUpdateService, etc.)

```
Component Hierarchy:

Graph (RefinementGraph, PlanningGraph, ImplementationGraph)
  ├── Node 1: Create Agent + Invoke
  ├── Node 2: Check Result
  └── Node 3: Checkpoint + Approval Gate

Agent (Microsoft Framework)
  ├── System Prompt
  ├── Model (Claude)
  └── Tools Registry

Tool Registry
  ├── ReadFileTool
  ├── WriteFileTool
  ├── ExecuteCommandTool
  └── Custom PRFactory Tools

Application Services
  ├── ITicketUpdateService
  ├── IGitPlatformProvider
  └── IWorkflowOrchestrator
```

#### 4. **Implement Gradually**

**Phase 1** (Current):
- Port Saturn's core tool interface
- Implement 3-5 essential tools (read, write, apply_diff)
- Manual agent invocation within graphs

**Phase 2** (Roadmap):
- Complete Microsoft Agent Framework integration
- Automated tool calling within agents
- Multi-agent coordination for parallel implementation

**Phase 3+** (Future):
- CodeReviewGraph using reviewer agents
- Parallel implementation A/B testing
- Complex orchestration patterns

### 4.3 Technical Debt to Avoid

#### 1. **Don't Replicate Saturn's Command Approval Pattern**

Saturn requires manual approval for each command - this is too granular.

**Better approach for PRFactory**:
```csharp
// Policy-based approval at the workflow level
public class ExecuteCommandTool : ITool
{
    public async Task<ToolResult> ExecuteAsync(string command)
    {
        // Approval happens at graph level, not tool level
        // Agent can execute all commands it needs to implement approved plan
        
        // Only block:
        // - Known dangerous operations (rm -rf /)
        // - Commands outside expected scope
        // - In specific security contexts (production)
        
        if (IsDangerous(command))
            return CreateErrorResult("Dangerous operation blocked");
        
        return await ExecuteAsync(command);
    }
}
```

#### 2. **Don't Use Polling for Task Awaiting**

Saturn's `WaitForTask` polls every 100ms - this doesn't scale.

**Better approach**:
```csharp
// Event-driven completion
public class AgentManager
{
    private TaskCompletionSource<AgentTaskResult> _taskCompletion;
    
    public Task<AgentTaskResult> WaitForTaskAsync(string taskId)
    {
        var tcs = new TaskCompletionSource<AgentTaskResult>();
        
        TaskCompleted += (s, e) =>
        {
            if (e.TaskId == taskId)
                tcs.SetResult(e.Result);
        };
        
        return tcs.Task;
    }
}
```

#### 3. **Don't Hardcode Tool Availability**

Saturn's agents all have access to all tools - security concern.

**Better approach**:
```csharp
// Tools selected per graph/role
public class ImplementationGraph : AgentGraphBase
{
    public override List<ITool> GetAvailableTools()
    {
        return new List<ITool>
        {
            new ReadFileTool(),      // Can read
            new WriteFileTool(),     // Can write
            new GrepTool(),          // Can search
            new ApplyDiffTool(),     // Can apply patches
            // NO: ExecuteCommandTool - not needed for code implementation
            // NO: WebFetchTool - not needed
        };
    }
}
```

#### 4. **Don't Use Static Configuration Context**

Saturn's `AgentContext.CurrentConfiguration` is static global state.

**Better approach**:
```csharp
// Dependency injection with configuration
public class RefinementGraph : AgentGraphBase
{
    private readonly IAgentFactory _agentFactory;
    
    public RefinementGraph(IAgentFactory agentFactory)
    {
        _agentFactory = agentFactory;
    }
    
    public override async Task<GraphOutput> ExecuteAsync(GraphInput input)
    {
        var agent = _agentFactory.CreateAgent(
            name: "Refinement",
            instructions: SystemPrompt.Create("Refine requirements"),
            tools: GetRefinementTools(),
            config: GetGraphConfiguration()
        );
        
        var result = await agent.InvokeAsync(input.Requirements);
        return new GraphOutput { Data = result };
    }
}
```

### 4.4 What Worked Well in Saturn

1. **Tool Interface** - Simple, clean, easy to implement
2. **Auto-discovery** - Zero registration boilerplate
3. **Atomic Writes** - Temporary file pattern for safety
4. **File Security** - Path validation prevents escapes
5. **Streaming Support** - Real-time output for long operations
6. **Error Context** - Tools provide detailed error messages to agents

### 4.5 What Was Problematic in Saturn

1. **Global Singleton State** - AgentContext static config, ToolRegistry global
2. **Polling-based Coordination** - Inefficient task awaiting
3. **All Tools Available Everywhere** - No security boundaries
4. **Review Phase Complexity** - Iteration loops add latency
5. **No Structured Agents** - Just a wrapper around OpenRouter
6. **Hard to Test** - Dependency on static registry and context
7. **Configuration as Strings** - System prompts are raw strings

---

## Part 5: Implementation Roadmap for PRFactory

### Phase 1: Tool Foundation (NOW)

```
src/PRFactory.Core/Tools/
├── ITool.cs                    # Tool interface
├── ToolBase.cs                 # Base implementation
├── ToolRegistry.cs             # Registry with auto-discovery
├── ToolResult.cs               # Result wrapper

src/PRFactory.Infrastructure/Tools/
├── ReadFileTool.cs             # Port from Saturn
├── WriteFileTool.cs            # Port from Saturn
├── DeleteFileTool.cs           # Port from Saturn
├── ApplyDiffTool.cs            # Port from Saturn
└── GrepTool.cs                 # Port from Saturn
```

**Acceptance Criteria**:
- Tools have tests for happy path and error cases
- Tools are discoverable via reflection
- Results are consistent format
- All paths validated for security

### Phase 2: Agent Framework Integration (Roadmap)

```
src/PRFactory.Core/Agents/
├── IAgent.cs                   # Abstraction over Microsoft Framework
├── AgentFactory.cs             # Creates configured agents
└── AgentConfiguration.cs        # Tool selection + model params

src/PRFactory.Infrastructure/Agents/
├── MicrosoftAgent.cs          # Wraps Microsoft Agent Framework
└── AgentContextProvider.cs     # Provides execution context
```

**Integration Points**:
- RefinementGraph creates agent with refinement tools
- PlanningGraph creates agent with planning tools
- ImplementationGraph creates agent with implementation tools
- Agents invoke tools, graphs manage workflow

### Phase 3: Multi-Agent Coordination (Future Roadmap)

```
src/PRFactory.Core/Agents/
└── IAgentOrchestrator.cs

src/PRFactory.Infrastructure/Agents/
└── AgentOrchestrator.cs        # Manages parallel agent execution
```

**Use Cases**:
- Multiple implementations in parallel (A/B testing)
- Code review agents validate implementations
- Parallel testing agents verify functionality

---

## Summary & Recommendations

### For PRFactory Implementation

**ADOPT FROM SATURN**:
1. Tool interface and base class pattern
2. Tool registry with auto-discovery
3. File system tools (ReadFile, WriteFile, DeleteFile, ApplyDiff)
4. Path security validation patterns
5. Atomic write patterns
6. Error handling patterns

**DO DIFFERENTLY**:
1. Use Microsoft Agent Framework instead of direct OpenRouter
2. Keep tool layer separate from graph/orchestration layer
3. Implement policy-based approval instead of per-command
4. Use event-driven coordination instead of polling
5. Inject configuration instead of static context
6. Make tools optional per graph context

**AVOID SATURN'S LIMITATIONS**:
1. Static singleton state
2. Polling-based task coordination
3. All tools available everywhere
4. Complex review phase iterations
5. Hard to test due to static dependencies

### Key Files to Port

| Saturn File | Status | Priority | Target Location |
|------------|--------|----------|-----------------|
| ITool.cs | Reference | HIGH | PRFactory.Core/Tools/ |
| ToolBase.cs | Reference | HIGH | PRFactory.Core/Tools/ |
| ToolRegistry.cs | Reference | HIGH | PRFactory.Core/Tools/ |
| ReadFileTool.cs | Port | HIGH | PRFactory.Infrastructure/Tools/ |
| WriteFileTool.cs | Port | HIGH | PRFactory.Infrastructure/Tools/ |
| DeleteFileTool.cs | Port | HIGH | PRFactory.Infrastructure/Tools/ |
| ApplyDiffTool.cs | Port | MEDIUM | PRFactory.Infrastructure/Tools/ |
| GrepTool.cs | Port | MEDIUM | PRFactory.Infrastructure/Tools/ |
| AgentManager.cs | Reference | MEDIUM | Future Phase 2-3 |
| ExecuteCommandTool.cs | Reference | MEDIUM | Decide if needed |
| GlobTool.cs | Reference | LOW | Consider for search |
| WebFetchTool.cs | Reference | LOW | For future research |

---

**This analysis provides the foundation for implementing a production-grade tool and agent system in PRFactory while learning from Saturn's successes and avoiding its pitfalls.**
