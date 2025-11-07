#!/bin/bash
set -e

echo "🚀 PRFactory SessionStart Hook - Installing .NET SDK 10..."

# Define SDK version
DOTNET_VERSION="10.0"
DOTNET_INSTALL_DIR="$HOME/.dotnet"
DOTNET_INSTALL_SCRIPT="$HOME/dotnet-install.sh"

# Check if .NET SDK 10 is already installed
if command -v dotnet &> /dev/null; then
    CURRENT_VERSION=$(dotnet --version 2>/dev/null || echo "none")
    if [[ "$CURRENT_VERSION" == 10.* ]]; then
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
        echo "⚠️  Found .NET version $CURRENT_VERSION, but need version 10.x"
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

echo "🔧 Installing .NET SDK $DOTNET_VERSION (including preview/RC versions)..."
"$DOTNET_INSTALL_SCRIPT" --channel "$DOTNET_VERSION" --quality preview --install-dir "$DOTNET_INSTALL_DIR" --verbose

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
