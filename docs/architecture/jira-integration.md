# Jira Integration Architecture

## Overview

The Jira integration handles bidirectional communication with Jira Cloud:
- **Inbound**: Webhooks for ticket events (created, updated, commented)
- **Outbound**: REST API calls to post comments, update fields, transition tickets

## Key Responsibilities

- Receive and validate webhook events from Jira
- Parse comments to detect @claude mentions and user responses
- Post AI-generated questions and plans to Jira as comments
- Update ticket status and custom fields
- Link tickets to PRs
- Maintain audit trail in Jira

## Architecture Components

```
┌─────────────────────────────────────────────────────────┐
│                    Jira Cloud                           │
│  ┌─────────────┐  ┌──────────────┐  ┌───────────────┐  │
│  │   Webhook   │  │  REST API    │  │ Custom Fields │  │
│  └──────┬──────┘  └──────▲───────┘  └───────▲───────┘  │
│         │                │                   │          │
└─────────┼────────────────┼───────────────────┼──────────┘
          │                │                   │
          │ HTTPS          │ HTTPS             │
          ▼                │                   │
┌─────────────────────────────────────────────────────────┐
│              PRFactory.Api                              │
│  ┌──────────────────────────────────────────────────┐   │
│  │      JiraWebhookController                       │   │
│  │  • POST /api/webhooks/jira                       │   │
│  │  • Validates HMAC signature                      │   │
│  │  • Routes to handlers                            │   │
│  └────────────┬─────────────────────────────────────┘   │
│               │                                         │
│  ┌────────────▼─────────────────────────────────────┐   │
│  │      JiraWebhookProcessor                        │   │
│  │  • Parses event payload                          │   │
│  │  • Detects triggers (@claude, labels)            │   │
│  │  • Publishes commands via MediatR                │   │
│  └──────────────────────────────────────────────────┘   │
│                                                         │
│  ┌──────────────────────────────────────────────────┐   │
│  │      JiraCommentParser                           │   │
│  │  • Extracts @claude mentions                     │   │
│  │  • Parses user responses                         │   │
│  │  • Detects approvals/rejections                  │   │
│  └──────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────┐
│         PRFactory.Infrastructure.Jira                   │
│  ┌──────────────────────────────────────────────────┐   │
│  │      IJiraClient (Refit)                         │   │
│  │  • GetIssue(key)                                 │   │
│  │  • AddComment(key, body)                         │   │
│  │  • UpdateField(key, field, value)                │   │
│  │  • TransitionIssue(key, transition)              │   │
│  │  • AddRemoteLink(key, url, title)                │   │
│  └────────────────────┬─────────────────────────────┘   │
│                       │                                 │
│  ┌────────────────────▼─────────────────────────────┐   │
│  │      JiraService                                 │   │
│  │  • High-level operations                         │   │
│  │  • Retry logic (Polly)                           │   │
│  │  • Logging & metrics                             │   │
│  └──────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
```

## Webhook Configuration

### Register Webhook in Jira

**Endpoint URL:**
```
https://your-prfactory-instance.com/api/webhooks/jira
```

**Events to Subscribe:**
- `jira:issue_created`
- `jira:issue_updated`
- `comment_created`
- `comment_updated`

**JQL Filter (optional):**
```jql
labels = "claude-ai" OR description ~ "@claude" OR comment ~ "@claude"
```

### Webhook Secret

Store a shared secret in both Jira and PRFactory for HMAC validation.

**Jira Configuration:**
```json
{
  "name": "PRFactory Webhook",
  "url": "https://your-instance.com/api/webhooks/jira",
  "events": ["jira:issue_created", "jira:issue_updated", "comment_created"],
  "filters": {
    "issue-related-events-section": "labels = claude-ai"
  },
  "excludeBody": false
}
```

## Implementation Details

### Webhook Controller

```csharp
// PRFactory.Api/Controllers/WebhooksController.cs
[ApiController]
[Route("api/webhooks")]
public class JiraWebhookController : ControllerBase
{
    private readonly ITicketService _ticketService;
    private readonly ITicketRepository _ticketRepository;
    private readonly IJiraWebhookValidator _validator;
    private readonly IJiraCommentParser _commentParser;
    private readonly ILogger<JiraWebhookController> _logger;

    [HttpPost("jira")]
    public async Task<IActionResult> ReceiveJiraWebhook(
        [FromBody] JiraWebhookPayload payload,
        [FromHeader(Name = "X-Hub-Signature")] string signature,
        CancellationToken ct)
    {
        // 1. Validate signature
        var rawBody = await ReadRawBodyAsync();
        if (!_validator.ValidateSignature(rawBody, signature))
        {
            _logger.LogWarning("Invalid webhook signature from Jira");
            return Unauthorized("Invalid signature");
        }

        // 2. Log webhook receipt
        _logger.LogInformation("Received Jira webhook: {EventType} for {IssueKey}",
            payload.WebhookEvent, payload.Issue?.Key);

        // 3. Process webhook
        await ProcessWebhookAsync(payload, ct);

        return Ok();
    }

    private async Task ProcessWebhookAsync(JiraWebhookPayload payload, CancellationToken ct)
    {
        switch (payload.WebhookEvent)
        {
            case "jira:issue_created" when HasClaudeTrigger(payload.Issue):
                await _ticketService.TriggerTicketAsync(
                    payload.Issue.Key,
                    GetTenantId(payload),
                    GetRepositoryId(payload),
                    ct
                );
                break;

            case "comment_created" when payload.Comment?.Body?.Contains("@claude") == true:
                await ProcessCommentAsync(
                    payload.Issue.Key,
                    payload.Comment.Id,
                    payload.Comment.Body,
                    ct
                );
                break;
        }
    }

    private async Task ProcessCommentAsync(string issueKey, string commentId, string commentBody, CancellationToken ct)
    {
        var ticket = await _ticketRepository.GetByTicketKeyAsync(issueKey, ct);
        if (ticket == null) return;

        // Parse comment based on current state
        if (ticket.State == WorkflowState.AwaitingAnswers)
        {
            var answers = _commentParser.ParseAnswers(commentBody, ticket.Questions);
            if (answers != null)
            {
                await _ticketService.SubmitAnswersAsync(ticket.Id, answers, ct);
            }
        }
        else if (ticket.State == WorkflowState.PlanUnderReview)
        {
            var approval = _commentParser.ParseApproval(commentBody);
            if (approval == ApprovalStatus.Approved)
            {
                await _ticketService.ApprovePlanAsync(ticket.Id, ct);
            }
            else if (approval == ApprovalStatus.Rejected)
            {
                await _ticketService.RejectPlanAsync(ticket.Id, commentBody, ct);
            }
        }
    }

    private bool HasClaudeTrigger(JiraIssue issue)
    {
        return issue.Fields.Labels?.Contains("claude-ai") == true ||
               issue.Fields.Description?.Contains("@claude") == true;
    }

    private async Task<string> ReadRawBodyAsync()
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        Request.Body.Position = 0;
        return body;
    }
}
```

### Webhook Validator

```csharp
// PRFactory.Infrastructure/Jira/JiraWebhookValidator.cs
public interface IJiraWebhookValidator
{
    bool ValidateSignature(string rawBody, string signature);
}

public class JiraWebhookValidator : IJiraWebhookValidator
{
    private readonly IConfiguration _configuration;

    public bool ValidateSignature(string rawBody, string signature)
    {
        var secret = _configuration["Jira:WebhookSecret"];
        if (string.IsNullOrEmpty(secret))
        {
            throw new InvalidOperationException("Jira webhook secret not configured");
        }

        var expectedSignature = ComputeHmacSha256(rawBody, secret);
        return signature.Equals(expectedSignature, StringComparison.OrdinalIgnoreCase);
    }

    private string ComputeHmacSha256(string data, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var dataBytes = Encoding.UTF8.GetBytes(data);

        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(dataBytes);
        return "sha256=" + Convert.ToHexString(hashBytes).ToLower();
    }
}
```

### Comment Parser

```csharp
// PRFactory.Infrastructure/Jira/JiraCommentParser.cs
public interface IJiraCommentParser
{
    bool IsMentioningClaude(string commentBody);
    Dictionary<string, string>? ParseAnswers(string commentBody, List<Question> questions);
    ApprovalStatus ParseApproval(string commentBody);
}

public class JiraCommentParser : IJiraCommentParser
{
    public bool IsMentioningClaude(string commentBody)
    {
        return commentBody.Contains("@claude", StringComparison.OrdinalIgnoreCase);
    }

    public Dictionary<string, string>? ParseAnswers(string commentBody, List<Question> questions)
    {
        // Strategy: Look for patterns like:
        // 1. Answer: ...
        // 2. Answer: ...
        // OR
        // Q: ... A: ...

        var answers = new Dictionary<string, string>();

        // Pattern 1: Numbered answers
        var numberPattern = @"(\d+)\.\s*(?:Answer:?\s*)?(.+?)(?=\d+\.|$)";
        var matches = Regex.Matches(commentBody, numberPattern, RegexOptions.Singleline);

        if (matches.Count == questions.Count)
        {
            for (int i = 0; i < matches.Count; i++)
            {
                var answerText = matches[i].Groups[2].Value.Trim();
                answers[questions[i].Id] = answerText;
            }
            return answers;
        }

        // Pattern 2: Q&A pairs
        var qaPattern = @"Q:\s*(.+?)\s*A:\s*(.+?)(?=Q:|$)";
        matches = Regex.Matches(commentBody, qaPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            var questionSnippet = match.Groups[1].Value.Trim();
            var answer = match.Groups[2].Value.Trim();

            // Find matching question by partial text match
            var question = questions.FirstOrDefault(q =>
                q.Text.Contains(questionSnippet, StringComparison.OrdinalIgnoreCase) ||
                questionSnippet.Contains(q.Text[..Math.Min(50, q.Text.Length)], StringComparison.OrdinalIgnoreCase)
            );

            if (question != null)
            {
                answers[question.Id] = answer;
            }
        }

        return answers.Any() ? answers : null;
    }

    public ApprovalStatus ParseApproval(string commentBody)
    {
        var lower = commentBody.ToLower();

        if (lower.Contains("approve") || lower.Contains("looks good") || lower.Contains("lgtm"))
            return ApprovalStatus.Approved;

        if (lower.Contains("reject") || lower.Contains("needs changes"))
            return ApprovalStatus.Rejected;

        return ApprovalStatus.None;
    }
}

public enum ApprovalStatus
{
    None,
    Approved,
    Rejected
}
```

### Jira REST API Client (Refit)

```csharp
// PRFactory.Infrastructure/Jira/IJiraClient.cs
public interface IJiraClient
{
    [Get("/rest/api/3/issue/{issueKey}")]
    Task<JiraIssue> GetIssueAsync(string issueKey);

    [Post("/rest/api/3/issue/{issueKey}/comment")]
    Task<JiraComment> AddCommentAsync(
        string issueKey,
        [Body] AddCommentRequest request
    );

    [Put("/rest/api/3/issue/{issueKey}")]
    Task UpdateIssueAsync(
        string issueKey,
        [Body] UpdateIssueRequest request
    );

    [Post("/rest/api/3/issue/{issueKey}/transitions")]
    Task TransitionIssueAsync(
        string issueKey,
        [Body] TransitionRequest request
    );

    [Post("/rest/api/3/issue/{issueKey}/remotelink")]
    Task AddRemoteLinkAsync(
        string issueKey,
        [Body] RemoteLinkRequest request
    );
}

// DTOs
public class AddCommentRequest
{
    [JsonPropertyName("body")]
    public JiraContent Body { get; set; }
}

public class JiraContent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "doc";

    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    [JsonPropertyName("content")]
    public List<JiraContentNode> Content { get; set; }
}

public class JiraContentNode
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("content")]
    public List<JiraContentNode>? Content { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    // For markdown support
    public static JiraContentNode Paragraph(string text) => new()
    {
        Type = "paragraph",
        Content = new List<JiraContentNode>
        {
            new() { Type = "text", Text = text }
        }
    };
}
```

### Jira Service (High-Level Wrapper)

```csharp
// PRFactory.Infrastructure/Jira/JiraService.cs
public interface IJiraService
{
    Task PostCommentAsync(string issueKey, string markdownText, CancellationToken ct = default);
    Task UpdateCustomFieldAsync(string issueKey, string fieldKey, object value, CancellationToken ct = default);
    Task LinkPullRequestAsync(string issueKey, string prUrl, string prTitle, CancellationToken ct = default);
    Task TransitionToStatusAsync(string issueKey, string statusName, CancellationToken ct = default);
}

public class JiraService : IJiraService
{
    private readonly IJiraClient _client;
    private readonly ILogger<JiraService> _logger;
    private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;

    public JiraService(IJiraClient client, ILogger<JiraService> logger)
    {
        _client = client;
        _logger = logger;
        _retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    public async Task PostCommentAsync(string issueKey, string markdownText, CancellationToken ct = default)
    {
        try
        {
            var request = new AddCommentRequest
            {
                Body = ConvertMarkdownToADF(markdownText)
            };

            await _client.AddCommentAsync(issueKey, request);

            _logger.LogInformation("Posted comment to Jira issue {IssueKey}", issueKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to post comment to {IssueKey}", issueKey);
            throw;
        }
    }

    public async Task LinkPullRequestAsync(string issueKey, string prUrl, string prTitle, CancellationToken ct = default)
    {
        var request = new RemoteLinkRequest
        {
            Object = new RemoteLinkObject
            {
                Url = prUrl,
                Title = prTitle,
                Icon = new RemoteLinkIcon
                {
                    Url16x16 = "https://github.com/favicon.ico",
                    Title = "Pull Request"
                }
            }
        };

        await _client.AddRemoteLinkAsync(issueKey, request);

        _logger.LogInformation("Linked PR {PrUrl} to issue {IssueKey}", prUrl, issueKey);
    }

    private JiraContent ConvertMarkdownToADF(string markdown)
    {
        // Simple markdown to ADF converter
        // For production, use a proper library like Markdig + custom renderer

        var paragraphs = markdown.Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
            .Select(para => JiraContentNode.Paragraph(para.Trim()))
            .ToList();

        return new JiraContent
        {
            Content = paragraphs
        };
    }
}
```

## Configuration

### appsettings.json

```json
{
  "Jira": {
    "BaseUrl": "https://yourcompany.atlassian.net",
    "Email": "automation@yourcompany.com",
    "ApiToken": "ATATT3xF...",
    "WebhookSecret": "your-webhook-secret-key"
  }
}
```

### Service Registration

```csharp
// Program.cs
services.AddRefitClient<IJiraClient>()
    .ConfigureHttpClient(c =>
    {
        c.BaseAddress = new Uri(configuration["Jira:BaseUrl"]);
        var credentials = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{configuration["Jira:Email"]}:{configuration["Jira:ApiToken"]}")
        );
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
    })
    .AddPolicyHandler(ResiliencePolicies.GetHttpRetryPolicy());

services.AddScoped<IJiraService, JiraService>();
services.AddSingleton<IJiraWebhookValidator, JiraWebhookValidator>();
services.AddSingleton<IJiraCommentParser, JiraCommentParser>();
```

## Security Considerations

### HMAC Signature Validation
- Always validate webhook signatures
- Use constant-time comparison to prevent timing attacks
- Rotate webhook secrets periodically

### API Token Storage
- Store Jira API tokens encrypted in database
- Use Azure Key Vault for production
- Per-tenant API tokens (multi-tenancy)

### Rate Limiting
- Jira Cloud has rate limits (typically 10 req/sec)
- Implement backoff and retry with Polly
- Monitor rate limit headers

## Testing

### Webhook Testing

```csharp
[Fact]
public async Task ReceiveWebhook_WithValidSignature_ProcessesEvent()
{
    // Arrange
    var payload = CreateTestPayload();
    var signature = ComputeTestSignature(payload);

    // Act
    var result = await _controller.ReceiveJiraWebhook(payload, signature, CancellationToken.None);

    // Assert
    Assert.IsType<OkResult>(result);
    _mockTicketService.Verify(x => x.TriggerTicketAsync(
        It.IsAny<string>(),
        It.IsAny<Guid>(),
        It.IsAny<Guid>(),
        It.IsAny<CancellationToken>()
    ), Times.Once);
}
```

### Integration Test with Jira

Use Jira's test webhooks feature or set up a test Jira instance.

## Error Handling

### Webhook Failures
- Always return 200 OK quickly (< 5 seconds)
- Process asynchronously via Hangfire
- Jira will retry webhooks on 5xx errors

### API Call Failures
- Retry with exponential backoff (Polly)
- Circuit breaker for sustained failures
- Alert on repeated failures

## Monitoring

### Key Metrics
- Webhook receipt rate
- Comment parsing success rate
- API call success/failure rate
- Average response time to Jira

### Logging
```csharp
_logger.LogInformation("Jira webhook received: {Event} for {Key} in {Duration}ms",
    payload.WebhookEvent,
    payload.Issue?.Key,
    stopwatch.ElapsedMilliseconds);
```

## Next Steps

Review the other integration documents:
- [Git Integration](./git-integration.md) - Multi-platform git operations
