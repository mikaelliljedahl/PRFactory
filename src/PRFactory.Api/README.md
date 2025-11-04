# PRFactory.Api

ASP.NET Core Web API for PRFactory - Automated development workflows triggered by Jira tickets.

## Overview

PRFactory.Api provides REST endpoints for:
- Receiving Jira webhooks for ticket events
- Querying ticket status and workflow progress
- Manual approval/rejection of implementation plans
- Listing and filtering tickets by tenant

## Features

- **Webhook Integration**: Secure HMAC-validated Jira webhook receiver
- **Async Processing**: Returns 200 OK immediately, processes workflows asynchronously
- **OpenTelemetry**: Full distributed tracing with Jaeger exporter
- **Serilog Logging**: Structured logging to console and file
- **Swagger/OpenAPI**: Interactive API documentation at `/swagger`
- **Health Checks**: Health endpoint at `/health`
- **CORS Support**: Configurable cross-origin resource sharing

## Architecture

```
┌─────────────────┐
│  Jira Webhook   │
└────────┬────────┘
         │
         ▼
┌─────────────────┐         ┌──────────────────┐
│ WebhookController│────────▶│ Agent Framework  │
└─────────────────┘         └──────────────────┘
                                     │
         ┌───────────────────────────┼───────────────────────────┐
         │                           │                           │
         ▼                           ▼                           ▼
┌──────────────┐          ┌──────────────────┐      ┌──────────────────┐
│ Analysis     │          │ Planning         │      │ Implementation   │
│ Agent Graph  │          │ Agent Graph      │      │ Agent Graph      │
└──────────────┘          └──────────────────┘      └──────────────────┘
         │                           │                           │
         └───────────────────────────┴───────────────────────────┘
                                     │
                                     ▼
                          ┌──────────────────┐
                          │  TicketController │
                          └──────────────────┘
```

## Endpoints

### Webhooks

#### `POST /api/webhooks/jira`
Receives Jira webhook events and queues workflows.

**Request Headers:**
- `X-Hub-Signature`: HMAC-SHA256 signature (sha256=<hash>)
- `Content-Type`: application/json

**Request Body:**
```json
{
  "webhookEvent": "jira:issue_created",
  "issue": {
    "key": "PROJ-123",
    "fields": {
      "summary": "Implement user authentication",
      "description": "Add OAuth2 authentication..."
    }
  }
}
```

**Response:** `200 OK`
```json
{
  "success": true,
  "message": "Webhook received and queued for processing",
  "activityId": "00-abc123...",
  "issueKey": "PROJ-123",
  "workflowType": "trigger"
}
```

### Tickets

#### `GET /api/tickets/{id}`
Get current status of a ticket.

**Response:** `200 OK`
```json
{
  "ticketId": "PROJ-123",
  "currentState": "awaiting_approval",
  "tenantId": "acme-corp",
  "repositoryName": "acme/webapp",
  "branchName": "feature/PROJ-123-user-auth",
  "implementationPlan": "## Implementation Plan\n...",
  "awaitingHumanInput": true,
  "lastUpdated": "2025-11-04T20:00:00Z",
  "events": [...]
}
```

#### `GET /api/tickets`
List tickets with optional filtering.

**Query Parameters:**
- `state` (optional): Filter by workflow state
- `repository` (optional): Filter by repository name
- `page` (default: 1): Page number
- `pageSize` (default: 20): Items per page

**Response:** `200 OK`
```json
{
  "tickets": [...],
  "totalCount": 42,
  "page": 1,
  "pageSize": 20,
  "totalPages": 3
}
```

#### `POST /api/tickets/{id}/approve-plan`
Approve an implementation plan.

**Request Body:**
```json
{
  "approved": true,
  "comments": "Looks good, proceed with implementation",
  "approvedBy": "john.doe@example.com"
}
```

**Response:** `200 OK`
```json
{
  "success": true,
  "message": "Plan approved. Implementation workflow will continue.",
  "ticketStatus": {...}
}
```

#### `POST /api/tickets/{id}/reject-plan`
Reject an implementation plan.

**Request Body:**
```json
{
  "reason": "Missing error handling for edge cases",
  "restartPlanning": true,
  "rejectedBy": "john.doe@example.com"
}
```

**Response:** `200 OK`
```json
{
  "success": true,
  "message": "Plan rejected. Planning workflow will restart with the provided feedback.",
  "ticketStatus": {...}
}
```

## Configuration

Configuration is managed through `appsettings.json` and environment variables.

### Required Settings

```json
{
  "ConnectionStrings": {
    "Database": "Data Source=/var/prfactory/data/prfactory.db"
  },
  "Jira": {
    "BaseUrl": "https://your-domain.atlassian.net",
    "ApiToken": "your-jira-api-token",
    "WebhookSecret": "your-webhook-secret",
    "UserEmail": "bot@example.com"
  },
  "Claude": {
    "ApiKey": "your-anthropic-api-key",
    "Model": "claude-sonnet-4-5-20250929"
  },
  "Workspace": {
    "BasePath": "/var/prfactory/workspace"
  },
  "Git": {
    "GitHub": {
      "Token": "your-github-token"
    }
  }
}
```

### Environment Variables

You can override any configuration using environment variables with double underscore (`__`) separator:

```bash
export ConnectionStrings__Database="Data Source=/path/to/db.sqlite"
export Jira__WebhookSecret="your-secret"
export Claude__ApiKey="sk-ant-..."
export Git__GitHub__Token="ghp_..."
```

## Development

### Prerequisites

- .NET 8 SDK
- SQLite (for local development)
- Git

### Running Locally

```bash
cd src/PRFactory.Api
dotnet restore
dotnet run
```

The API will start on `http://localhost:5000` with Swagger UI at the root.

### Testing Webhooks Locally

Use ngrok or similar to expose your local API:

```bash
ngrok http 5000
```

Configure Jira webhook to point to: `https://your-ngrok-url.ngrok.io/api/webhooks/jira`

## Docker

### Build

```bash
docker build -t prfactory-api:latest -f src/PRFactory.Api/Dockerfile .
```

### Run

```bash
docker run -d \
  -p 5000:80 \
  -v /var/prfactory:/var/prfactory \
  -e Jira__WebhookSecret="your-secret" \
  -e Claude__ApiKey="sk-ant-..." \
  --name prfactory-api \
  prfactory-api:latest
```

## Monitoring

### Logs

Logs are written to:
- Console (structured JSON in production)
- `logs/prfactory-YYYYMMDD.log` (rolling daily files)

### Traces

OpenTelemetry traces are exported to Jaeger at `http://localhost:16686` (configurable).

### Health Check

```bash
curl http://localhost:5000/health
```

## Security

- **HMAC Validation**: All webhook requests are validated using HMAC-SHA256 signatures
- **HTTPS**: Use HTTPS in production (configured in hosting environment)
- **Secrets**: Store sensitive configuration in environment variables or secret managers

## Error Handling

All errors return consistent JSON responses:

```json
{
  "error": "Error message",
  "type": "ExceptionType",
  "statusCode": 400,
  "timestamp": "2025-11-04T20:00:00Z"
}
```

## License

Proprietary - PRFactory Team
