#!/bin/bash
set -euo pipefail

# PRFactory SessionStart Hook - Install .NET SDK and dependencies
# This hook runs when a Claude Code session starts to ensure all dependencies are available

echo "🚀 PRFactory SessionStart Hook - Initializing .NET 10 environment..."

DOTNET_INSTALL_DIR="$HOME/.dotnet"
DOTNET_INSTALL_SCRIPT="/tmp/dotnet-install.sh"
CLAUDE_PROJECT_DIR="${CLAUDE_PROJECT_DIR:-.}"

# Extract SDK version from global.json
GLOBAL_JSON_PATH="$CLAUDE_PROJECT_DIR/global.json"
SPECIFIC_VERSION=""
if [[ -f "$GLOBAL_JSON_PATH" ]]; then
    SPECIFIC_VERSION=$(sed -n 's/.*"version"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' "$GLOBAL_JSON_PATH" | head -1)
    if [[ -n "$SPECIFIC_VERSION" ]]; then
        echo "📋 Found .NET SDK version in global.json: $SPECIFIC_VERSION"
    fi
fi

DOTNET_VERSION="${SPECIFIC_VERSION:-10.0}"

# Check if correct .NET version is already installed
if command -v dotnet &> /dev/null; then
    CURRENT_VERSION=$(dotnet --version 2>/dev/null || echo "none")
    if [[ "$CURRENT_VERSION" = "$DOTNET_VERSION" ]] || [[ "$CURRENT_VERSION" == 10.* ]]; then
        echo "✅ .NET SDK already installed (version: $CURRENT_VERSION)"
        export DOTNET_ROOT="$DOTNET_INSTALL_DIR"
        export PATH="$DOTNET_INSTALL_DIR:$PATH"

        # Persist for Claude Code session
        if [[ -n "${CLAUDE_ENV_FILE:-}" ]]; then
            echo "export DOTNET_ROOT=\"$DOTNET_INSTALL_DIR\"" >> "$CLAUDE_ENV_FILE"
            echo "export PATH=\"$DOTNET_INSTALL_DIR:\$PATH\"" >> "$CLAUDE_ENV_FILE"
        fi
    else
        echo "⚠️  Found .NET $CURRENT_VERSION, need $DOTNET_VERSION - will reinstall..."
    fi
else
    echo "📥 .NET SDK not found - installing..."
fi

# Download installation script if not already done
if [[ ! -f "$DOTNET_INSTALL_SCRIPT" ]]; then
    echo "📥 Downloading .NET installation script..."
    if ! curl -sSL https://raw.githubusercontent.com/dotnet/install-scripts/main/src/dotnet-install.sh -o "$DOTNET_INSTALL_SCRIPT" 2>/dev/null; then
        echo "⚠️  GitHub download failed, trying dotnet.microsoft.com..."
        curl -sSL https://dotnet.microsoft.com/download/dotnet/scripts/v1/dotnet-install.sh -o "$DOTNET_INSTALL_SCRIPT"
    fi
    chmod +x "$DOTNET_INSTALL_SCRIPT"
fi

# Install .NET SDK if needed
if ! command -v dotnet &> /dev/null || ! (dotnet --version | grep -q "10\."); then
    echo "🔧 Installing .NET SDK $DOTNET_VERSION..."
    if [[ -n "$SPECIFIC_VERSION" ]]; then
        "$DOTNET_INSTALL_SCRIPT" --version "$SPECIFIC_VERSION" --install-dir "$DOTNET_INSTALL_DIR" 2>&1 | grep -E "(Downloaded|Extracting|Installed)" || true
    else
        "$DOTNET_INSTALL_SCRIPT" --channel 10.0 --quality preview --install-dir "$DOTNET_INSTALL_DIR" 2>&1 | grep -E "(Downloaded|Extracting|Installed)" || true
    fi
fi

# Set up environment
export DOTNET_ROOT="$DOTNET_INSTALL_DIR"
export PATH="$DOTNET_INSTALL_DIR:$PATH"

# Persist environment variables for Claude Code
if [[ -n "${CLAUDE_ENV_FILE:-}" ]]; then
    echo "export DOTNET_ROOT=\"$DOTNET_INSTALL_DIR\"" >> "$CLAUDE_ENV_FILE"
    echo "export PATH=\"$DOTNET_INSTALL_DIR:\$PATH\"" >> "$CLAUDE_ENV_FILE"
fi

# Verify installation
echo ""
echo "✅ .NET SDK installed:"
dotnet --version

# Start NuGet proxy for .NET operations (handles Claude Code proxy auth)
echo "🔧 Starting NuGet proxy..."
NUGET_PROXY_SCRIPT="$CLAUDE_PROJECT_DIR/.claude/scripts/nuget-proxy.py"

# Kill any existing proxy
pkill -f nuget-proxy.py 2>/dev/null || true

if [[ -f "$NUGET_PROXY_SCRIPT" ]]; then
    nohup python3 "$NUGET_PROXY_SCRIPT" > /tmp/nuget-proxy.log 2>&1 &
    sleep 2

    if pgrep -f nuget-proxy.py > /dev/null; then
        echo "✅ NuGet proxy running on http://127.0.0.1:8888"

        # Create helper script
        cat > /tmp/dotnet-proxy-setup.sh <<'HELPER'
# Configure .NET to use NuGet proxy (handles Claude Code authentication)
unset http_proxy https_proxy HTTP_PROXY HTTPS_PROXY GLOBAL_AGENT_HTTP_PROXY GLOBAL_AGENT_HTTPS_PROXY
export HTTP_PROXY=http://127.0.0.1:8888
export HTTPS_PROXY=http://127.0.0.1:8888
HELPER
        echo "✅ Helper script created at /tmp/dotnet-proxy-setup.sh"
    else
        echo "⚠️  NuGet proxy failed to start"
    fi
else
    echo "⚠️  NuGet proxy script not found"
fi

echo ""
echo "🎯 Environment ready for PRFactory development!"
echo ""
