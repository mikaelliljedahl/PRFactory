# Claude Code Hooks

This directory contains hook documentation for Claude Code.

## SessionStart Hook

The `SessionStart` hook automatically installs .NET SDK 10 when you start a Claude Code session in the cloud environment.

### Configuration

The hook is configured in `.claude/settings.json` and points to the script at `.claude/scripts/session-start.sh`.

For Claude Code on the web, hooks are registered in `.claude/settings.json`:

```json
{
  "hooks": {
    "SessionStart": [
      {
        "matcher": "startup",
        "hooks": [
          {
            "type": "command",
            "command": "\"$CLAUDE_PROJECT_DIR\"/.claude/scripts/session-start.sh"
          }
        ]
      }
    ]
  }
}
```

### What it does:

1. **Checks** if .NET SDK 10 is already installed
2. **Downloads** the official dotnet-install.sh script from Microsoft
3. **Installs** .NET SDK 10.0 to `$HOME/.dotnet`
4. **Configures** environment variables using `CLAUDE_ENV_FILE` (for Claude Code) or shell profiles (local)
5. **Verifies** the installation was successful

### Usage:

The hook runs automatically when you start a Claude Code session. No manual intervention needed!

You can also run it manually:

```bash
./.claude/scripts/session-start.sh
```

### After installation:

Once the hook completes, you can use all .NET CLI commands:

```bash
# Check version
dotnet --version

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test

# Run the API
dotnet run --project src/PRFactory.Api

# Run the Worker
dotnet run --project src/PRFactory.Worker
```

### Environment variables set:

- `DOTNET_ROOT=$HOME/.dotnet`
- `PATH=$DOTNET_ROOT:$PATH`

The script automatically uses `CLAUDE_ENV_FILE` when running in Claude Code on the web to persist these variables for subsequent bash commands.

### Troubleshooting:

If the hook fails, you can manually install .NET SDK 10:

```bash
curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 10.0
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$DOTNET_ROOT:$PATH
```

For more information about Claude Code hooks, see: https://docs.claude.com/en/docs/claude-code
