#!/bin/bash
set -e

echo "🚀 PRFactory SessionStart Hook - Installing .NET SDK 10..."

# Define SDK version - read from global.json if available
DOTNET_INSTALL_DIR="$HOME/.dotnet"
DOTNET_INSTALL_SCRIPT="/tmp/dotnet-install.sh"

# Try to extract specific version from global.json
GLOBAL_JSON_PATH="${CLAUDE_PROJECT_DIR:-$(pwd)}/global.json"
if [ -f "$GLOBAL_JSON_PATH" ]; then
    # Use sed for better portability (works without Perl regex support)
    SPECIFIC_VERSION=$(sed -n 's/.*"version"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' "$GLOBAL_JSON_PATH" | head -1)
    if [ -n "$SPECIFIC_VERSION" ]; then
        echo "📋 Found specific .NET SDK version in global.json: $SPECIFIC_VERSION"
        DOTNET_VERSION="$SPECIFIC_VERSION"
        USE_SPECIFIC_VERSION=true
    else
        echo "⚠️  Could not extract version from global.json, using channel approach"
        DOTNET_VERSION="10.0"
        USE_SPECIFIC_VERSION=false
    fi
else
    echo "ℹ️  No global.json found, using channel approach"
    DOTNET_VERSION="10.0"
    USE_SPECIFIC_VERSION=false
fi

# Check if .NET SDK 10 is already installed
if command -v dotnet &> /dev/null; then
    CURRENT_VERSION=$(dotnet --version 2>/dev/null || echo "none")

    # Check if the correct version is installed
    VERSION_MATCH=false
    if [ "$USE_SPECIFIC_VERSION" = true ] && [ "$CURRENT_VERSION" = "$DOTNET_VERSION" ]; then
        VERSION_MATCH=true
    elif [[ "$CURRENT_VERSION" == 10.* ]]; then
        VERSION_MATCH=true
    fi

    if [ "$VERSION_MATCH" = true ]; then
        echo "✅ .NET SDK 10 is already installed (version: $CURRENT_VERSION)"
        echo "📍 .NET location: $(which dotnet)"
        dotnet --info

        # Ensure environment variables are persisted for Claude Code
        if [ -n "$CLAUDE_ENV_FILE" ]; then
            echo "export DOTNET_ROOT=\"$DOTNET_INSTALL_DIR\"" >> "$CLAUDE_ENV_FILE"
            echo "export PATH=\"$DOTNET_INSTALL_DIR:\$PATH\"" >> "$CLAUDE_ENV_FILE"
        fi
        exit 0
    else
        if [ "$USE_SPECIFIC_VERSION" = true ]; then
            echo "⚠️  Found .NET version $CURRENT_VERSION, but need version $DOTNET_VERSION (from global.json)"
        else
            echo "⚠️  Found .NET version $CURRENT_VERSION, but need version 10.x"
        fi
    fi
fi

echo "📥 Downloading .NET installation script..."
# Try GitHub first (dotnet.microsoft.com redirects to blocked builds.dotnet.microsoft.com)
if curl -sSL https://raw.githubusercontent.com/dotnet/install-scripts/main/src/dotnet-install.sh -o "$DOTNET_INSTALL_SCRIPT" 2>/dev/null; then
    echo "✅ Downloaded from GitHub"
else
    echo "⚠️ GitHub download failed, trying dotnet.microsoft.com..."
    curl -sSL https://dotnet.microsoft.com/download/dotnet/scripts/v1/dotnet-install.sh -o "$DOTNET_INSTALL_SCRIPT"
fi
chmod +x "$DOTNET_INSTALL_SCRIPT"

if [ "$USE_SPECIFIC_VERSION" = true ]; then
    echo "🔧 Installing .NET SDK version $DOTNET_VERSION (from global.json)..."
    "$DOTNET_INSTALL_SCRIPT" --version "$DOTNET_VERSION" --install-dir "$DOTNET_INSTALL_DIR" --verbose
else
    echo "🔧 Installing .NET SDK $DOTNET_VERSION (including preview/RC versions)..."
    "$DOTNET_INSTALL_SCRIPT" --channel "$DOTNET_VERSION" --quality preview --install-dir "$DOTNET_INSTALL_DIR" --verbose
fi

# Add to PATH for current session
export DOTNET_ROOT="$DOTNET_INSTALL_DIR"
export PATH="$DOTNET_INSTALL_DIR:$PATH"

# Persist environment variables
# For Claude Code on the web, use CLAUDE_ENV_FILE
if [ -n "$CLAUDE_ENV_FILE" ]; then
    echo "📝 Persisting environment variables to Claude Code session..."
    echo "export DOTNET_ROOT=\"$DOTNET_INSTALL_DIR\"" >> "$CLAUDE_ENV_FILE"
    echo "export PATH=\"$DOTNET_INSTALL_DIR:\$PATH\"" >> "$CLAUDE_ENV_FILE"
else
    # For local development, persist to shell profile files
    echo "📝 Persisting environment variables to shell profiles..."
    echo "" >> "$HOME/.bashrc"
    echo "# .NET SDK configuration (added by PRFactory SessionStart hook)" >> "$HOME/.bashrc"
    echo "export DOTNET_ROOT=\"$DOTNET_INSTALL_DIR\"" >> "$HOME/.bashrc"
    echo "export PATH=\"\$DOTNET_ROOT:\$PATH\"" >> "$HOME/.bashrc"

    if [ -f "$HOME/.zshrc" ]; then
        echo "" >> "$HOME/.zshrc"
        echo "# .NET SDK configuration (added by PRFactory SessionStart hook)" >> "$HOME/.zshrc"
        echo "export DOTNET_ROOT=\"$DOTNET_INSTALL_DIR\"" >> "$HOME/.zshrc"
        echo "export PATH=\"\$DOTNET_ROOT:\$PATH\"" >> "$HOME/.zshrc"
    fi
fi

# Verify installation
echo ""
echo "✅ .NET SDK installation complete!"
echo "📍 Installation directory: $DOTNET_INSTALL_DIR"
echo "📍 .NET location: $(which dotnet)"
echo ""
echo "🔍 Installed SDK version:"
dotnet --version
echo ""
echo "📦 .NET SDK info:"
dotnet --info
echo ""
echo "🎯 Ready to build PRFactory with .NET 10!"
echo ""

# Start NuGet proxy for .NET restore/build (works around Claude Code proxy auth issues)
echo "🔧 Starting NuGet proxy..."
NUGET_PROXY_SCRIPT="${CLAUDE_PROJECT_DIR:-$(pwd)}/.claude/scripts/nuget-proxy.py"
if [ -f "$NUGET_PROXY_SCRIPT" ]; then
    # Kill any existing proxy
    pkill -f nuget-proxy.py 2>/dev/null || true

    # Start proxy in background
    nohup python3 "$NUGET_PROXY_SCRIPT" > /tmp/nuget-proxy.log 2>&1 &
    sleep 2

    # Check if proxy started
    if pgrep -f nuget-proxy.py > /dev/null; then
        echo "✅ NuGet proxy started on http://127.0.0.1:8888"
        echo ""
        echo "💡 For dotnet restore/build, run:"
        echo "   unset http_proxy https_proxy HTTP_PROXY HTTPS_PROXY GLOBAL_AGENT_HTTP_PROXY GLOBAL_AGENT_HTTPS_PROXY"
        echo "   export HTTP_PROXY=http://127.0.0.1:8888"
        echo "   export HTTPS_PROXY=http://127.0.0.1:8888"
        echo "   dotnet restore"
        echo "   dotnet build"
    else
        echo "⚠️  NuGet proxy failed to start (check /tmp/nuget-proxy.log)"
    fi
else
    echo "⚠️  NuGet proxy script not found at $NUGET_PROXY_SCRIPT"
fi
