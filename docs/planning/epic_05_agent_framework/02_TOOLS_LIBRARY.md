# 02: PRFactory.AgentTools Class Library

**Document Purpose:** Detailed specification for the `PRFactory.AgentTools` class library, including tool interfaces, implementations, security patterns, and testing strategies.

**Last Updated:** 2025-11-13

---

## Table of Contents

- [Project Structure](#project-structure)
- [Core Interfaces (from Saturn)](#core-interfaces-from-saturn)
- [Tool Categories](#tool-categories)
- [Tool Implementations](#tool-implementations)
- [Security Patterns](#security-patterns)
- [Testing Strategy](#testing-strategy)
- [Registration & Discovery](#registration--discovery)

---

## Project Structure

```
/PRFactory.AgentTools/
├── Core/
│   ├── ITool.cs                      # Tool interface (from Saturn)
│   ├── ToolBase.cs                   # Base implementation with security
│   ├── ToolRegistry.cs               # Auto-discovery + DI
│   ├── ToolExecutionContext.cs       # Execution context with tenant
│   ├── ToolExecutionResult.cs        # Standardized result format
│   ├── ToolDescriptionAttribute.cs   # Metadata for Agent Framework
│   └── ToolParameterAttribute.cs     # Parameter metadata
│
├── FileSystem/
│   ├── ReadFileTool.cs               # Read file contents
│   ├── WriteFileTool.cs              # Write file with atomic operations
│   ├── ListFilesTool.cs              # List directory contents
│   ├── DeleteFileTool.cs             # Delete file (with safety checks)
│   └── ApplyDiffTool.cs              # Apply unified diff patch
│
├── Search/
│   ├── GrepTool.cs                   # Search file contents (regex)
│   ├── GlobTool.cs                   # Find files by pattern
│   └── SearchReplaceTool.cs          # Find and replace in files
│
├── Git/
│   ├── CommitTool.cs                 # Git commit changes
│   ├── CreateBranchTool.cs           # Create new branch
│   ├── GetDiffTool.cs                # Get diff between commits
│   ├── CreatePullRequestTool.cs      # Create PR via platform API
│   └── GetBranchesTool.cs            # List branches
│
├── Jira/
│   ├── GetTicketTool.cs              # Get Jira ticket details
│   ├── AddCommentTool.cs             # Add comment to ticket
│   ├── TransitionTicketTool.cs       # Change ticket status
│   └── GetTicketHistoryTool.cs       # Get ticket history
│
├── Analysis/
│   ├── CodeSearchTool.cs             # Search code with semantic understanding
│   ├── DependencyMapTool.cs          # Analyze dependencies
│   ├── ParseASTTool.cs               # Parse code to AST (future)
│   └── FindReferencesTool.cs         # Find symbol references
│
├── Command/
│   ├── ExecuteShellTool.cs           # Execute shell command (whitelisted)
│   ├── RunTestsTool.cs               # Run test suite
│   └── BuildProjectTool.cs           # Build project (dotnet build, npm build, etc.)
│
├── Web/
│   ├── WebFetchTool.cs               # Fetch URL with SSRF protection
│   └── ApiCallTool.cs                # Make authenticated API call
│
├── Security/
│   ├── PathValidator.cs              # Path validation utilities
│   ├── SsrfProtection.cs             # SSRF mitigation
│   └── ResourceLimits.cs             # Size/time limits
│
├── Extensions/
│   ├── ServiceCollectionExtensions.cs  # DI registration
│   └── AgentFrameworkExtensions.cs     # AF integration helpers
│
└── PRFactory.AgentTools.csproj
```

---

## Core Interfaces (from Saturn)

### ITool Interface

**Purpose:** Standard contract for all tools. Enables auto-discovery and uniform execution.

**Reference:** Saturn's `ITool` pattern (SATURN_TOOLS_ANALYSIS.md)

```csharp
namespace PRFactory.AgentTools.Core;

/// <summary>
/// Base interface for all agent tools.
/// Tools provide capabilities to agents (file I/O, git, Jira, analysis, etc.)
/// </summary>
public interface ITool
{
    /// <summary>
    /// Unique tool name (used for whitelisting and invocation)
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Human-readable description for LLM (what the tool does)
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Execute the tool with given context and parameters
    /// </summary>
    /// <param name="context">Execution context (tenant, ticket, workspace)</param>
    /// <returns>Tool execution result (output, success, metadata)</returns>
    Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context);

    /// <summary>
    /// Convert to Agent Framework AIFunction for registration
    /// </summary>
    AIFunction ToAIFunction();
}
```

---

### ToolBase Abstract Class

**Purpose:** Provides common functionality for all tools (security, logging, error handling).

```csharp
namespace PRFactory.AgentTools.Core;

/// <summary>
/// Base class for all tools. Implements template method pattern with
/// security validations, logging, and error handling.
/// </summary>
public abstract class ToolBase : ITool
{
    protected readonly ILogger<ToolBase> _logger;
    protected readonly ITenantContext _tenantContext;

    public abstract string Name { get; }
    public abstract string Description { get; }

    protected ToolBase(
        ILogger<ToolBase> logger,
        ITenantContext tenantContext)
    {
        _logger = logger;
        _tenantContext = tenantContext;
    }

    // Template method (public interface)
    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 1. Validate execution context
            ValidateContext(context);

            // 2. Validate tenant isolation
            ValidateTenantContext(context);

            // 3. Validate input parameters
            await ValidateInputAsync(context);

            // 4. Execute tool-specific logic (subclass implements this)
            var output = await ExecuteToolAsync(context);

            stopwatch.Stop();

            // 5. Log success
            _logger.LogInformation(
                "Tool {ToolName} executed successfully for tenant {TenantId} in {Duration}ms",
                Name, context.TenantId, stopwatch.ElapsedMilliseconds);

            return ToolExecutionResult.Success(output, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Log failure
            _logger.LogError(ex,
                "Tool {ToolName} failed for tenant {TenantId} after {Duration}ms: {Error}",
                Name, context.TenantId, stopwatch.ElapsedMilliseconds, ex.Message);

            return ToolExecutionResult.Failure(ex.Message, stopwatch.Elapsed);
        }
    }

    // Subclasses implement this
    protected abstract Task<string> ExecuteToolAsync(ToolExecutionContext context);

    // Subclasses can override for custom validation
    protected virtual Task ValidateInputAsync(ToolExecutionContext context)
    {
        return Task.CompletedTask;
    }

    private void ValidateContext(ToolExecutionContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        if (context.TenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required", nameof(context));

        if (string.IsNullOrEmpty(context.WorkspacePath))
            throw new ArgumentException("WorkspacePath is required", nameof(context));
    }

    private void ValidateTenantContext(ToolExecutionContext context)
    {
        // Ensure current tenant context matches execution context
        if (_tenantContext.TenantId != context.TenantId)
        {
            throw new SecurityException(
                $"Tenant context mismatch: expected {_tenantContext.TenantId}, " +
                $"got {context.TenantId}");
        }
    }

    // Helper: Execute with timeout
    protected async Task<TResult> ExecuteWithTimeoutAsync<TResult>(
        Func<Task<TResult>> operation,
        TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            return await operation().WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new ToolTimeoutException(Name, timeout);
        }
    }

    // Convert to Agent Framework AIFunction
    public AIFunction ToAIFunction()
    {
        return AIFunctionFactory.Create(
            name: Name,
            description: Description,
            implementation: async (context) =>
            {
                var toolContext = ToolExecutionContext.FromDictionary(context);
                var result = await ExecuteAsync(toolContext);
                return result.Output;
            },
            parameters: GetParameterSchema());
    }

    protected abstract AIFunctionParameterSchema GetParameterSchema();
}
```

---

### ToolExecutionContext

**Purpose:** Encapsulates execution context for tool invocation.

```csharp
namespace PRFactory.AgentTools.Core;

/// <summary>
/// Context for tool execution. Contains tenant info, workspace path, and parameters.
/// </summary>
public class ToolExecutionContext
{
    public Guid TenantId { get; set; }
    public Guid TicketId { get; set; }
    public string WorkspacePath { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();

    // Helper: Get typed parameter
    public T GetParameter<T>(string name)
    {
        if (!Parameters.TryGetValue(name, out var value))
            throw new ArgumentException($"Parameter '{name}' is required");

        if (value is JsonElement jsonElement)
        {
            return JsonSerializer.Deserialize<T>(jsonElement.GetRawText())
                ?? throw new InvalidOperationException($"Failed to deserialize parameter '{name}'");
        }

        return (T)Convert.ChangeType(value, typeof(T));
    }

    // Helper: Get optional parameter
    public T? GetOptionalParameter<T>(string name, T? defaultValue = default)
    {
        return Parameters.TryGetValue(name, out var value)
            ? GetParameter<T>(name)
            : defaultValue;
    }

    // Factory: Create from Agent Framework parameters
    public static ToolExecutionContext FromDictionary(
        IDictionary<string, object> parameters)
    {
        return new ToolExecutionContext
        {
            TenantId = GetParameter<Guid>(parameters, "tenantId"),
            TicketId = GetParameter<Guid>(parameters, "ticketId"),
            WorkspacePath = GetParameter<string>(parameters, "workspacePath"),
            Parameters = parameters.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        };
    }

    private static T GetParameter<T>(IDictionary<string, object> dict, string key)
    {
        return dict.TryGetValue(key, out var value)
            ? (T)Convert.ChangeType(value, typeof(T))
            : default!;
    }
}
```

---

### ToolExecutionResult

**Purpose:** Standardized result format for all tools.

```csharp
namespace PRFactory.AgentTools.Core;

/// <summary>
/// Result of tool execution. Includes output, success status, and metadata.
/// </summary>
public class ToolExecutionResult
{
    public string Output { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan Duration { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();

    public static ToolExecutionResult Success(string output, TimeSpan duration)
    {
        return new ToolExecutionResult
        {
            Output = output,
            Success = true,
            Duration = duration
        };
    }

    public static ToolExecutionResult Failure(string errorMessage, TimeSpan duration)
    {
        return new ToolExecutionResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            Duration = duration
        };
    }
}
```

---

## Tool Categories

### 1. File System Tools

**Purpose:** File I/O operations with security validations.

**Security Considerations:**
- Path validation (no directory traversal)
- Workspace boundary enforcement
- File size limits
- Atomic writes (temp file + rename pattern)

---

#### ReadFileTool

**Description:** Read the contents of a file within the workspace.

**Implementation:**
```csharp
namespace PRFactory.AgentTools.FileSystem;

[ToolDescription("Read the contents of a file")]
public class ReadFileTool : ToolBase
{
    public override string Name => "ReadFile";
    public override string Description => "Read the contents of a file from the workspace";

    private const long MaxFileSize = 10 * 1024 * 1024;  // 10MB

    public ReadFileTool(ILogger<ReadFileTool> logger, ITenantContext tenantContext)
        : base(logger, tenantContext) { }

    protected override async Task<string> ExecuteToolAsync(ToolExecutionContext context)
    {
        var filePath = context.GetParameter<string>("filePath");

        // 1. Validate path
        var fullPath = PathValidator.ValidateAndResolve(
            filePath, context.WorkspacePath);

        // 2. Check file exists
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {filePath}");

        // 3. Check file size
        var fileInfo = new FileInfo(fullPath);
        if (fileInfo.Length > MaxFileSize)
        {
            throw new FileTooLargeException(
                $"File size {fileInfo.Length} bytes exceeds limit {MaxFileSize} bytes");
        }

        // 4. Read file
        var content = await File.ReadAllTextAsync(fullPath);

        _logger.LogDebug(
            "Read file {FilePath} ({Size} bytes) for tenant {TenantId}",
            filePath, fileInfo.Length, context.TenantId);

        return content;
    }

    protected override AIFunctionParameterSchema GetParameterSchema()
    {
        return new AIFunctionParameterSchema
        {
            Properties = new Dictionary<string, AIFunctionParameterPropertySchema>
            {
                ["filePath"] = new AIFunctionParameterPropertySchema
                {
                    Type = "string",
                    Description = "Relative path to file within workspace",
                    Required = true
                }
            }
        };
    }
}
```

---

#### WriteFileTool

**Description:** Write content to a file with atomic operations.

**Implementation:**
```csharp
namespace PRFactory.AgentTools.FileSystem;

[ToolDescription("Write content to a file")]
public class WriteFileTool : ToolBase
{
    public override string Name => "WriteFile";
    public override string Description => "Write content to a file (atomic operation)";

    private const long MaxFileSize = 1 * 1024 * 1024;  // 1MB

    public WriteFileTool(ILogger<WriteFileTool> logger, ITenantContext tenantContext)
        : base(logger, tenantContext) { }

    protected override async Task<string> ExecuteToolAsync(ToolExecutionContext context)
    {
        var filePath = context.GetParameter<string>("filePath");
        var content = context.GetParameter<string>("content");

        // 1. Validate path
        var fullPath = PathValidator.ValidateAndResolve(
            filePath, context.WorkspacePath);

        // 2. Check content size
        if (Encoding.UTF8.GetByteCount(content) > MaxFileSize)
        {
            throw new FileTooLargeException(
                $"Content size exceeds limit {MaxFileSize} bytes");
        }

        // 3. Atomic write (temp file + rename)
        var directory = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(directory);

        var tempPath = Path.Combine(directory, $".{Guid.NewGuid()}.tmp");

        try
        {
            // Write to temp file
            await File.WriteAllTextAsync(tempPath, content);

            // Rename to target (atomic operation)
            File.Move(tempPath, fullPath, overwrite: true);

            _logger.LogInformation(
                "Wrote file {FilePath} ({Size} bytes) for tenant {TenantId}",
                filePath, content.Length, context.TenantId);

            return $"Successfully wrote {content.Length} bytes to {filePath}";
        }
        finally
        {
            // Cleanup temp file if still exists
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    protected override AIFunctionParameterSchema GetParameterSchema()
    {
        return new AIFunctionParameterSchema
        {
            Properties = new Dictionary<string, AIFunctionParameterPropertySchema>
            {
                ["filePath"] = new AIFunctionParameterPropertySchema
                {
                    Type = "string",
                    Description = "Relative path to file within workspace",
                    Required = true
                },
                ["content"] = new AIFunctionParameterPropertySchema
                {
                    Type = "string",
                    Description = "Content to write to file",
                    Required = true
                }
            }
        };
    }
}
```

---

### 2. Search Tools

**Purpose:** Search code and files.

---

#### GrepTool

**Description:** Search for pattern in files (regex support).

**Implementation:**
```csharp
namespace PRFactory.AgentTools.Search;

[ToolDescription("Search for a pattern in files")]
public class GrepTool : ToolBase
{
    public override string Name => "Grep";
    public override string Description => "Search for a regex pattern in files";

    private const int MaxResultLines = 1000;
    private const int MaxFileSize = 10 * 1024 * 1024;  // 10MB

    public GrepTool(ILogger<GrepTool> logger, ITenantContext tenantContext)
        : base(logger, tenantContext) { }

    protected override async Task<string> ExecuteToolAsync(ToolExecutionContext context)
    {
        var pattern = context.GetParameter<string>("pattern");
        var directory = context.GetOptionalParameter<string>("directory", ".");
        var filePattern = context.GetOptionalParameter<string>("filePattern", "*.*");
        var caseSensitive = context.GetOptionalParameter<bool>("caseSensitive", false);

        // 1. Validate directory
        var fullPath = PathValidator.ValidateAndResolve(
            directory, context.WorkspacePath);

        // 2. Compile regex
        var regexOptions = caseSensitive
            ? RegexOptions.None
            : RegexOptions.IgnoreCase;
        var regex = new Regex(pattern, regexOptions);

        // 3. Search files
        var results = new List<string>();
        var files = Directory.GetFiles(fullPath, filePattern, SearchOption.AllDirectories);

        foreach (var file in files)
        {
            // Skip large files
            if (new FileInfo(file).Length > MaxFileSize)
                continue;

            var lines = await File.ReadAllLinesAsync(file);
            var relativePath = Path.GetRelativePath(context.WorkspacePath, file);

            for (int i = 0; i < lines.Length; i++)
            {
                if (regex.IsMatch(lines[i]))
                {
                    results.Add($"{relativePath}:{i + 1}:{lines[i]}");

                    if (results.Count >= MaxResultLines)
                    {
                        results.Add($"... (truncated, max {MaxResultLines} results)");
                        goto Done;
                    }
                }
            }
        }

    Done:
        _logger.LogInformation(
            "Grep found {Count} matches for pattern '{Pattern}' in {FileCount} files",
            results.Count, pattern, files.Length);

        return string.Join(Environment.NewLine, results);
    }

    protected override AIFunctionParameterSchema GetParameterSchema()
    {
        return new AIFunctionParameterSchema
        {
            Properties = new Dictionary<string, AIFunctionParameterPropertySchema>
            {
                ["pattern"] = new AIFunctionParameterPropertySchema
                {
                    Type = "string",
                    Description = "Regex pattern to search for",
                    Required = true
                },
                ["directory"] = new AIFunctionParameterPropertySchema
                {
                    Type = "string",
                    Description = "Directory to search (default: workspace root)",
                    Required = false
                },
                ["filePattern"] = new AIFunctionParameterPropertySchema
                {
                    Type = "string",
                    Description = "File pattern (e.g., '*.cs', '*.ts')",
                    Required = false
                },
                ["caseSensitive"] = new AIFunctionParameterPropertySchema
                {
                    Type = "boolean",
                    Description = "Case-sensitive search (default: false)",
                    Required = false
                }
            }
        };
    }
}
```

---

#### GlobTool

**Description:** Find files matching a pattern.

**Implementation:**
```csharp
namespace PRFactory.AgentTools.Search;

[ToolDescription("Find files matching a pattern")]
public class GlobTool : ToolBase
{
    public override string Name => "Glob";
    public override string Description => "Find files matching a glob pattern (e.g., '**/*.cs')";

    private const int MaxResults = 1000;

    public GlobTool(ILogger<GlobTool> logger, ITenantContext tenantContext)
        : base(logger, tenantContext) { }

    protected override async Task<string> ExecuteToolAsync(ToolExecutionContext context)
    {
        var pattern = context.GetParameter<string>("pattern");
        var directory = context.GetOptionalParameter<string>("directory", ".");

        // 1. Validate directory
        var fullPath = PathValidator.ValidateAndResolve(
            directory, context.WorkspacePath);

        // 2. Use Matcher for glob patterns
        var matcher = new Microsoft.Extensions.FileSystemGlobbing.Matcher();
        matcher.AddInclude(pattern);

        // 3. Execute glob
        var result = matcher.Execute(
            new DirectoryInfoWrapper(new DirectoryInfo(fullPath)));

        var files = result.Files
            .Take(MaxResults)
            .Select(f => Path.GetRelativePath(context.WorkspacePath, f.Path))
            .ToList();

        if (result.Files.Count() > MaxResults)
            files.Add($"... (truncated, {result.Files.Count()} total files)");

        _logger.LogInformation(
            "Glob found {Count} files matching pattern '{Pattern}'",
            files.Count, pattern);

        return string.Join(Environment.NewLine, files);
    }

    protected override AIFunctionParameterSchema GetParameterSchema()
    {
        return new AIFunctionParameterSchema
        {
            Properties = new Dictionary<string, AIFunctionParameterPropertySchema>
            {
                ["pattern"] = new AIFunctionParameterPropertySchema
                {
                    Type = "string",
                    Description = "Glob pattern (e.g., '**/*.cs', 'src/**/*.ts')",
                    Required = true
                },
                ["directory"] = new AIFunctionParameterPropertySchema
                {
                    Type = "string",
                    Description = "Directory to search (default: workspace root)",
                    Required = false
                }
            }
        };
    }
}
```

---

### 3. Git Tools

**Purpose:** Git operations using LibGit2Sharp.

**Security:** Uses existing `ILocalGitService` (already tenant-aware).

---

#### CommitTool

**Description:** Commit changes to git repository.

**Implementation:**
```csharp
namespace PRFactory.AgentTools.Git;

[ToolDescription("Commit changes to git repository")]
public class CommitTool : ToolBase
{
    private readonly ILocalGitService _gitService;

    public override string Name => "GitCommit";
    public override string Description => "Commit staged changes with a message";

    public CommitTool(
        ILocalGitService gitService,
        ILogger<CommitTool> logger,
        ITenantContext tenantContext)
        : base(logger, tenantContext)
    {
        _gitService = gitService;
    }

    protected override async Task<string> ExecuteToolAsync(ToolExecutionContext context)
    {
        var message = context.GetParameter<string>("message");
        var stageAll = context.GetOptionalParameter<bool>("stageAll", true);

        // Workspace path is git repository root
        var repoPath = context.WorkspacePath;

        // Stage all changes if requested
        if (stageAll)
        {
            await _gitService.StageAllAsync(repoPath);
        }

        // Commit
        var commitHash = await _gitService.CommitAsync(repoPath, message);

        _logger.LogInformation(
            "Committed changes to {RepoPath} with message '{Message}' (commit: {Commit})",
            repoPath, message, commitHash);

        return $"Committed: {commitHash.Substring(0, 7)} - {message}";
    }

    protected override AIFunctionParameterSchema GetParameterSchema()
    {
        return new AIFunctionParameterSchema
        {
            Properties = new Dictionary<string, AIFunctionParameterPropertySchema>
            {
                ["message"] = new AIFunctionParameterPropertySchema
                {
                    Type = "string",
                    Description = "Commit message",
                    Required = true
                },
                ["stageAll"] = new AIFunctionParameterPropertySchema
                {
                    Type = "boolean",
                    Description = "Stage all changes before committing (default: true)",
                    Required = false
                }
            }
        };
    }
}
```

---

### 4. Jira Tools

**Purpose:** Jira API operations.

**Security:** Uses existing `IJiraService` (already tenant-aware).

---

#### GetTicketTool

**Description:** Get Jira ticket details.

**Implementation:**
```csharp
namespace PRFactory.AgentTools.Jira;

[ToolDescription("Get Jira ticket details")]
public class GetTicketTool : ToolBase
{
    private readonly IJiraService _jiraService;

    public override string Name => "GetJiraTicket";
    public override string Description => "Get details of a Jira ticket by key";

    public GetTicketTool(
        IJiraService jiraService,
        ILogger<GetTicketTool> logger,
        ITenantContext tenantContext)
        : base(logger, tenantContext)
    {
        _jiraService = jiraService;
    }

    protected override async Task<string> ExecuteToolAsync(ToolExecutionContext context)
    {
        var ticketKey = context.GetParameter<string>("ticketKey");

        // Fetch ticket
        var ticket = await _jiraService.GetTicketAsync(ticketKey);

        if (ticket == null)
            throw new NotFoundException($"Jira ticket '{ticketKey}' not found");

        // Format as structured output
        var output = new
        {
            ticket.Key,
            ticket.Summary,
            ticket.Description,
            ticket.Status,
            ticket.Assignee,
            ticket.Priority,
            ticket.CreatedAt,
            ticket.UpdatedAt
        };

        _logger.LogInformation(
            "Retrieved Jira ticket {TicketKey} for tenant {TenantId}",
            ticketKey, context.TenantId);

        return JsonSerializer.Serialize(output, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    protected override AIFunctionParameterSchema GetParameterSchema()
    {
        return new AIFunctionParameterSchema
        {
            Properties = new Dictionary<string, AIFunctionParameterPropertySchema>
            {
                ["ticketKey"] = new AIFunctionParameterPropertySchema
                {
                    Type = "string",
                    Description = "Jira ticket key (e.g., 'PROJ-123')",
                    Required = true
                }
            }
        };
    }
}
```

---

### 5. Analysis Tools

**Purpose:** Code analysis and semantic search.

---

#### CodeSearchTool

**Description:** Search code with semantic understanding.

**Implementation:**
```csharp
namespace PRFactory.AgentTools.Analysis;

[ToolDescription("Search code with semantic understanding")]
public class CodeSearchTool : ToolBase
{
    public override string Name => "CodeSearch";
    public override string Description => "Search for code symbols, classes, methods, etc.";

    private const int MaxResults = 50;

    public CodeSearchTool(ILogger<CodeSearchTool> logger, ITenantContext tenantContext)
        : base(logger, tenantContext) { }

    protected override async Task<string> ExecuteToolAsync(ToolExecutionContext context)
    {
        var query = context.GetParameter<string>("query");
        var language = context.GetOptionalParameter<string>("language", "csharp");

        var results = new List<string>();

        // For C#, search for class/method/interface definitions
        if (language.ToLower() == "csharp")
        {
            var patterns = new[]
            {
                $@"class\s+{query}",           // class Foo
                $@"interface\s+{query}",       // interface IFoo
                $@"public\s+\w+\s+{query}\(",  // public void Foo(
                $@"private\s+\w+\s+{query}\(", // private void Foo(
            };

            foreach (var pattern in patterns)
            {
                var grepContext = new ToolExecutionContext
                {
                    TenantId = context.TenantId,
                    TicketId = context.TicketId,
                    WorkspacePath = context.WorkspacePath,
                    Parameters = new Dictionary<string, object>
                    {
                        ["pattern"] = pattern,
                        ["filePattern"] = "*.cs",
                        ["caseSensitive"] = false
                    }
                };

                var grepTool = new GrepTool(_logger as ILogger<GrepTool>, _tenantContext);
                var grepResult = await grepTool.ExecuteAsync(grepContext);

                if (!string.IsNullOrEmpty(grepResult.Output))
                    results.Add(grepResult.Output);
            }
        }

        _logger.LogInformation(
            "CodeSearch for '{Query}' found {Count} results",
            query, results.Count);

        return results.Any()
            ? string.Join(Environment.NewLine + "---" + Environment.NewLine, results)
            : $"No results found for '{query}'";
    }

    protected override AIFunctionParameterSchema GetParameterSchema()
    {
        return new AIFunctionParameterSchema
        {
            Properties = new Dictionary<string, AIFunctionParameterPropertySchema>
            {
                ["query"] = new AIFunctionParameterPropertySchema
                {
                    Type = "string",
                    Description = "Symbol name to search for (class, method, interface, etc.)",
                    Required = true
                },
                ["language"] = new AIFunctionParameterPropertySchema
                {
                    Type = "string",
                    Description = "Programming language (default: 'csharp')",
                    Required = false
                }
            }
        };
    }
}
```

---

## Security Patterns

### PathValidator Utility

**Purpose:** Validate file paths to prevent directory traversal attacks.

```csharp
namespace PRFactory.AgentTools.Security;

public static class PathValidator
{
    /// <summary>
    /// Validate and resolve a file path within workspace.
    /// Throws SecurityException if path is outside workspace.
    /// </summary>
    public static string ValidateAndResolve(string filePath, string workspacePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path is required", nameof(filePath));

        if (string.IsNullOrWhiteSpace(workspacePath))
            throw new ArgumentException("Workspace path is required", nameof(workspacePath));

        // 1. Prevent obvious attacks
        if (filePath.Contains(".."))
            throw new SecurityException("Path contains '..' which is not allowed");

        if (Path.IsPathRooted(filePath))
            throw new SecurityException("Absolute paths are not allowed");

        // 2. Combine and normalize
        var fullPath = Path.GetFullPath(Path.Combine(workspacePath, filePath));
        var normalizedWorkspace = Path.GetFullPath(workspacePath);

        // 3. Ensure within workspace
        if (!fullPath.StartsWith(normalizedWorkspace, StringComparison.OrdinalIgnoreCase))
        {
            throw new SecurityException(
                $"Access denied: Path '{filePath}' resolves to '{fullPath}' " +
                $"which is outside workspace '{normalizedWorkspace}'");
        }

        return fullPath;
    }
}
```

---

### SSRF Protection Utility

**Purpose:** Prevent Server-Side Request Forgery attacks.

```csharp
namespace PRFactory.AgentTools.Security;

public static class SsrfProtection
{
    private static readonly string[] BlockedHosts = {
        "localhost", "127.0.0.1", "::1", "0.0.0.0",
        "169.254.169.254",          // AWS metadata
        "metadata.google.internal", // GCP metadata
        "metadata.azure.com"        // Azure metadata
    };

    private static readonly IPAddress[] PrivateRanges = {
        IPAddress.Parse("10.0.0.0"),
        IPAddress.Parse("172.16.0.0"),
        IPAddress.Parse("192.168.0.0")
    };

    /// <summary>
    /// Validate URL is not targeting internal/private resources.
    /// Throws SecurityException if URL is blocked.
    /// </summary>
    public static void ValidateUrl(string url)
    {
        var uri = new Uri(url);

        // 1. Check blocked hosts
        if (BlockedHosts.Any(h =>
            uri.Host.Equals(h, StringComparison.OrdinalIgnoreCase)))
        {
            throw new SecurityException(
                $"Access to '{uri.Host}' is blocked for security reasons");
        }

        // 2. Resolve DNS and check for private IPs
        var addresses = Dns.GetHostAddresses(uri.Host);
        foreach (var ip in addresses)
        {
            if (IsPrivateOrLoopback(ip))
            {
                throw new SecurityException(
                    $"Access to private/loopback IP '{ip}' is blocked");
            }
        }
    }

    private static bool IsPrivateOrLoopback(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip))
            return true;

        var bytes = ip.GetAddressBytes();

        // Check common private ranges
        return (bytes[0] == 10) ||
               (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
               (bytes[0] == 192 && bytes[1] == 168);
    }
}
```

---

## Testing Strategy

### Unit Testing

**Test all tools in isolation with mocked dependencies.**

**Example:**
```csharp
namespace PRFactory.AgentTools.Tests.FileSystem;

public class ReadFileToolTests
{
    private readonly Mock<ILogger<ReadFileTool>> _mockLogger;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly ReadFileTool _tool;
    private readonly string _tempWorkspace;

    public ReadFileToolTests()
    {
        _mockLogger = new Mock<ILogger<ReadFileTool>>();
        _mockTenantContext = new Mock<ITenantContext>();
        _mockTenantContext.Setup(t => t.TenantId).Returns(Guid.NewGuid());

        _tool = new ReadFileTool(_mockLogger.Object, _mockTenantContext.Object);

        _tempWorkspace = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempWorkspace);
    }

    [Fact]
    public async Task ExecuteAsync_ValidFile_ReturnsContent()
    {
        // Arrange
        var filePath = "test.txt";
        var fullPath = Path.Combine(_tempWorkspace, filePath);
        await File.WriteAllTextAsync(fullPath, "Hello World");

        var context = new ToolExecutionContext
        {
            TenantId = _mockTenantContext.Object.TenantId,
            TicketId = Guid.NewGuid(),
            WorkspacePath = _tempWorkspace,
            Parameters = new Dictionary<string, object>
            {
                ["filePath"] = filePath
            }
        };

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Hello World", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_PathTraversal_ThrowsSecurityException()
    {
        // Arrange
        var context = new ToolExecutionContext
        {
            TenantId = _mockTenantContext.Object.TenantId,
            TicketId = Guid.NewGuid(),
            WorkspacePath = _tempWorkspace,
            Parameters = new Dictionary<string, object>
            {
                ["filePath"] = "../../../etc/passwd"
            }
        };

        // Act & Assert
        var result = await _tool.ExecuteAsync(context);
        Assert.False(result.Success);
        Assert.Contains("security", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_FileTooLarge_ThrowsFileTooLargeException()
    {
        // Arrange
        var filePath = "large.txt";
        var fullPath = Path.Combine(_tempWorkspace, filePath);

        // Create 11MB file
        await File.WriteAllTextAsync(fullPath, new string('x', 11 * 1024 * 1024));

        var context = new ToolExecutionContext
        {
            TenantId = _mockTenantContext.Object.TenantId,
            TicketId = Guid.NewGuid(),
            WorkspacePath = _tempWorkspace,
            Parameters = new Dictionary<string, object>
            {
                ["filePath"] = filePath
            }
        };

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("exceeds limit", result.ErrorMessage);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempWorkspace))
            Directory.Delete(_tempWorkspace, recursive: true);
    }
}
```

**Coverage Target:** 80%+ for all tools

---

### Integration Testing

**Test tools with real dependencies (file system, git, Jira sandbox).**

**Example:**
```csharp
namespace PRFactory.AgentTools.Tests.Integration;

public class FileSystemToolsIntegrationTests
{
    [Fact]
    public async Task WriteFile_ReadFile_RoundTrip_Success()
    {
        // Arrange
        var workspace = CreateTempWorkspace();
        var context = CreateContext(workspace);

        var writeTool = CreateWriteTool();
        var readTool = CreateReadTool();

        // Act: Write file
        context.Parameters = new Dictionary<string, object>
        {
            ["filePath"] = "test.txt",
            ["content"] = "Integration test content"
        };

        var writeResult = await writeTool.ExecuteAsync(context);

        // Act: Read file
        context.Parameters = new Dictionary<string, object>
        {
            ["filePath"] = "test.txt"
        };

        var readResult = await readTool.ExecuteAsync(context);

        // Assert
        Assert.True(writeResult.Success);
        Assert.True(readResult.Success);
        Assert.Equal("Integration test content", readResult.Output);
    }
}
```

---

## Registration & Discovery

### Service Registration

**Extension method for DI registration:**

```csharp
namespace PRFactory.AgentTools.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register all agent tools in DI container.
    /// Tools are auto-discovered via reflection.
    /// </summary>
    public static IServiceCollection AddAgentTools(this IServiceCollection services)
    {
        // Register ToolRegistry
        services.AddSingleton<IToolRegistry, ToolRegistry>();

        // Auto-discover all ITool implementations
        var toolTypes = typeof(ITool).Assembly.GetTypes()
            .Where(t => typeof(ITool).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);

        foreach (var toolType in toolTypes)
        {
            services.AddTransient(typeof(ITool), toolType);
        }

        return services;
    }
}
```

**Usage in Startup/Program.cs:**
```csharp
// PRFactory.Worker/Program.cs
builder.Services.AddAgentTools();
```

---

## Next Steps

1. **Create PRFactory.AgentTools project** - Class library scaffolding
2. **Implement core interfaces** - ITool, ToolBase, ToolExecutionContext
3. **Port security utilities** - PathValidator, SsrfProtection
4. **Implement file tools** - ReadFile, WriteFile, Grep, Glob (Phase 1)
5. **Write unit tests** - 80%+ coverage before integration
6. **Implement git/Jira tools** - Using existing services (Phase 2)
7. **Integration testing** - End-to-end tool execution

**See:** `03_AGENT_ROLES.md` for specialized agent role specifications.
