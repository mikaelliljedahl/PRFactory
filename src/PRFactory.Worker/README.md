# PRFactory Worker Service

The PRFactory Worker Service is a background service that hosts agent graph execution for the PRFactory system. It polls for new agent executions, resumes suspended workflows when webhooks arrive, and manages the lifecycle of agent-based workflows.

## Architecture

The Worker Service is built on the following components:

### 1. AgentHostService

A `BackgroundService` that:
- Polls the execution queue for new agent graph executions
- Resumes suspended workflows when webhook events arrive
- Manages concurrent execution limits using semaphores
- Handles failures and implements retry logic with exponential backoff
- Provides graceful shutdown capabilities

**Key Features:**
- Configurable concurrent execution limit (default: 10)
- Configurable poll interval (default: 5 seconds)
- Automatic retry with exponential backoff
- OpenTelemetry integration for distributed tracing
- Graceful shutdown with configurable timeout

### 2. WorkflowResumeHandler

Handles resumption of suspended workflows:
- Loads checkpoint data from the database
- Validates the checkpoint is from a HumanWaitAgent
- Determines the next agent to execute based on the resume message
- Creates agent context from the checkpoint
- Resumes graph execution from the suspended point

**Supported Resume Events:**
- `answers_received` - User provided answers to questions
- `plan_approved` - User approved the implementation plan
- `plan_rejected` - User rejected the plan (loops back to planning)

### 3. Program.cs

Worker service entry point that:
- Configures services and dependency injection
- Sets up Serilog logging
- Configures OpenTelemetry for tracing and metrics
- Registers the AgentHostService as a hosted service
- Supports running as Windows Service or Linux systemd service

## Configuration

Configuration is managed through `appsettings.json`:

```json
{
  "AgentHost": {
    "MaxConcurrentExecutions": 10,
    "PollIntervalSeconds": 5,
    "BatchSize": 20,
    "MaxRetries": 3,
    "MaxResumeRetries": 5,
    "RetryDelayBaseSeconds": 30,
    "GracefulShutdownTimeoutSeconds": 300
  },
  "AgentFramework": {
    "MaxConcurrentWorkflows": 50,
    "AgentTimeout": "00:15:00",
    "CheckpointRetentionDays": 7,
    "EnableTracing": true
  }
}
```

### Configuration Options

#### AgentHost
- **MaxConcurrentExecutions**: Maximum number of workflows that can execute simultaneously
- **PollIntervalSeconds**: How often to poll for new work (seconds)
- **BatchSize**: Number of executions to fetch per poll
- **MaxRetries**: Maximum retry attempts for failed executions
- **MaxResumeRetries**: Maximum retry attempts for workflow resumption
- **RetryDelayBaseSeconds**: Base delay for exponential backoff
- **GracefulShutdownTimeoutSeconds**: Timeout for graceful shutdown

#### AgentFramework
- **MaxConcurrentWorkflows**: Global workflow limit
- **AgentTimeout**: Maximum time an agent can run
- **CheckpointRetentionDays**: How long to keep old checkpoints
- **EnableTracing**: Enable OpenTelemetry tracing

## Running the Worker

### Development

```bash
cd src/PRFactory.Worker
dotnet run
```

### Production

Build and publish:

```bash
dotnet publish -c Release -o ./publish
```

Run as console application:

```bash
cd publish
./PRFactory.Worker
```

### Running as Windows Service

Install the service:

```powershell
sc.exe create "PRFactory Worker" binPath="C:\path\to\PRFactory.Worker.exe"
sc.exe start "PRFactory Worker"
```

### Running as Linux systemd Service

Create service file `/etc/systemd/system/prfactory-worker.service`:

```ini
[Unit]
Description=PRFactory Worker Service
After=network.target

[Service]
Type=notify
ExecStart=/usr/local/bin/prfactory-worker/PRFactory.Worker
WorkingDirectory=/usr/local/bin/prfactory-worker
Restart=always
RestartSec=10
User=prfactory
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
```

Enable and start:

```bash
sudo systemctl daemon-reload
sudo systemctl enable prfactory-worker
sudo systemctl start prfactory-worker
sudo systemctl status prfactory-worker
```

## Monitoring

The Worker Service exposes:

### Logs
- Console output (structured JSON in production)
- File logs in `./logs/worker-YYYY-MM-DD.log`
- Serilog with enrichment (thread ID, machine name)

### Metrics
- `workflow.started` - Counter of workflows started
- `workflow.completed` - Counter of workflows completed
- `workflow.duration` - Histogram of workflow duration
- `agent.executions` - Counter of agent executions by name and status
- `workflow.active` - Gauge of currently active workflows

### Traces
- OpenTelemetry traces for each workflow execution
- Parent-child span relationships showing agent graph flow
- Integration with Jaeger or other OTLP-compatible backends

## Error Handling

The Worker Service implements robust error handling:

### Execution Failures
1. Automatic retry with exponential backoff
2. Maximum retry limit (default: 3)
3. Permanent failure marking after max retries
4. Detailed error logging with context

### Resume Failures
1. Separate retry counter for resume operations
2. Higher retry limit (default: 5) for transient issues
3. Checkpoint validation before resume
4. Graceful degradation on invalid checkpoints

### Graceful Shutdown
1. Stop accepting new work immediately
2. Wait for active executions to complete
3. Configurable timeout (default: 5 minutes)
4. Force shutdown if timeout exceeded

## Integration with API

The Worker Service is designed to work alongside the PRFactory API:

### Shared Components
- Database context (EF Core)
- Domain entities (Ticket, Repository, Tenant)
- Infrastructure services (Jira, Git, Claude clients)
- Agent definitions and messages

### Separation of Concerns
- **API**: Handles webhooks, UI, and synchronous operations
- **Worker**: Handles asynchronous agent graph execution
- **Shared Database**: Coordination through execution queue and checkpoints

### Deployment Options

**Option 1: Separate Processes**
- API and Worker run as separate services
- Scales independently
- Recommended for production

**Option 2: Same Process**
- Worker added as hosted service to API
- Simpler deployment
- Suitable for development or small deployments

## Dependencies

The Worker Service depends on:

- **PRFactory.Domain**: Domain entities and value objects
- **PRFactory.Infrastructure**: Agent framework, external integrations
- **Microsoft.Agents.AI**: Agent Framework for graph execution
- **Entity Framework Core**: Database access
- **Serilog**: Structured logging
- **OpenTelemetry**: Distributed tracing and metrics
- **Polly**: Resilience policies (retry, timeout, circuit breaker)

## Testing

### Unit Tests
Test individual components in isolation:
- WorkflowResumeHandler message parsing
- AgentHostService retry logic
- Configuration validation

### Integration Tests
Test end-to-end workflows:
- Complete workflow execution from trigger to completion
- Checkpoint save and resume
- Error recovery and retry
- Graceful shutdown

### Load Tests
Verify scalability:
- Multiple concurrent workflows
- High-frequency polling
- Checkpoint storage under load

## Troubleshooting

### Worker not processing workflows

Check:
1. Database connection string
2. Execution queue has pending items
3. MaxConcurrentExecutions not reached
4. No critical errors in logs

### Workflows not resuming

Check:
1. Checkpoint exists in database
2. Resume message type is supported
3. Webhook events are being received
4. No validation errors in logs

### High memory usage

Adjust:
1. Reduce MaxConcurrentExecutions
2. Enable checkpoint cleanup
3. Reduce BatchSize
4. Check for memory leaks in agents

## Future Enhancements

- [ ] Distributed execution across multiple workers
- [ ] Priority queue for urgent workflows
- [ ] Dynamic scaling based on queue depth
- [ ] Advanced retry policies (circuit breaker)
- [ ] Workflow pause/resume via API
- [ ] Real-time progress updates via SignalR
- [ ] Workflow scheduling (cron-based triggers)
- [ ] Dead letter queue for permanently failed workflows

## License

Copyright © 2025 PRFactory Team
