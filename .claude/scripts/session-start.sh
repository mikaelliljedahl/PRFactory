#!/bin/bash
set -e

echo "🚀 PRFactory SessionStart Hook - Installing .NET SDK..."

# Define installation directory
DOTNET_INSTALL_DIR="$HOME/.dotnet"

# Check if .NET is already installed
if command -v dotnet &> /dev/null; then
    CURRENT_VERSION=$(dotnet --version 2>/dev/null || echo "none")
    echo "✅ .NET SDK is already installed (version: $CURRENT_VERSION)"
    echo "📍 .NET location: $(which dotnet)"
    dotnet --info

    # Ensure environment variables are persisted
    if [ -n "$CLAUDE_ENV_FILE" ]; then
        echo "export DOTNET_ROOT=\"$DOTNET_INSTALL_DIR\"" >> "$CLAUDE_ENV_FILE"
        echo "export PATH=\"$DOTNET_INSTALL_DIR:\$PATH\"" >> "$CLAUDE_ENV_FILE"
    fi
    exit 0
fi

echo "⚠️  .NET 10 SDK downloads are blocked by Claude Code proxy."
echo "📦 Installing .NET 8 LTS via apt packages (workaround)..."

# Create temp directory for deb packages
TEMP_DIR=$(mktemp -d)
cd "$TEMP_DIR"

# Download .NET 8 packages without sudo
echo "📥 Downloading .NET 8 packages..."
apt-get download dotnet-sdk-8.0 dotnet-runtime-8.0 dotnet-hostfxr-8.0 dotnet-host-8.0 2>&1 | grep -v "Warning:"

# Extract packages to temporary directory
echo "📦 Extracting packages..."
EXTRACT_DIR="$TEMP_DIR/extracted"
mkdir -p "$EXTRACT_DIR"

for deb in *.deb; do
    echo "  Extracting $deb..."
    dpkg-deb -x "$deb" "$EXTRACT_DIR"
done

# Copy .NET to installation directory
echo "📁 Installing to $DOTNET_INSTALL_DIR..."
mkdir -p "$DOTNET_INSTALL_DIR"
cp -r "$EXTRACT_DIR"/usr/lib/dotnet/* "$DOTNET_INSTALL_DIR/"
if [ -f "$EXTRACT_DIR/usr/bin/dotnet" ]; then
    cp "$EXTRACT_DIR/usr/bin/dotnet" "$DOTNET_INSTALL_DIR/"
fi
chmod +x "$DOTNET_INSTALL_DIR/dotnet"

# Clean up
cd /
rm -rf "$TEMP_DIR"

# Add to PATH for current session
export DOTNET_ROOT="$DOTNET_INSTALL_DIR"
export PATH="$DOTNET_INSTALL_DIR:$PATH"

# Persist environment variables
if [ -n "$CLAUDE_ENV_FILE" ]; then
    echo "📝 Persisting environment variables to Claude Code session..."
    echo "export DOTNET_ROOT=\"$DOTNET_INSTALL_DIR\"" >> "$CLAUDE_ENV_FILE"
    echo "export PATH=\"$DOTNET_INSTALL_DIR:\$PATH\"" >> "$CLAUDE_ENV_FILE"
else
    # For local development
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
echo "⚠️  Note: Installed .NET 8 LTS instead of .NET 10 due to proxy restrictions."
echo "    The project may need adjustments to build with .NET 8."
echo ""

# Start NuGet proxy (not needed for apt-installed .NET, but keep for consistency)
echo "🔧 Starting NuGet proxy (for dotnet restore/build)..."
NUGET_PROXY_SCRIPT="${CLAUDE_PROJECT_DIR:-$(pwd)}/.claude/scripts/nuget-proxy.py"
if [ -f "$NUGET_PROXY_SCRIPT" ]; then
    # Kill any existing proxy
    pkill -f nuget-proxy.py 2>/dev/null || true

    # Start proxy in background
    nohup python3 "$NUGET_PROXY_SCRIPT" > /tmp/nuget-proxy.log 2>&1 &
    sleep 2

    if pgrep -f nuget-proxy.py > /dev/null; then
        echo "✅ NuGet proxy started on http://127.0.0.1:8888"
    else
        echo "⚠️  NuGet proxy failed to start (check /tmp/nuget-proxy.log)"
    fi
fi
