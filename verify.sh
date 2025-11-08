#!/bin/bash

# verify.sh - Build verification script for PRFactory
# This script verifies the project can be built, handling NuGet proxy if needed

set -e  # Exit on error

echo "=== PRFactory Build Verification ==="
echo ""

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Check if .NET SDK is available
if ! command -v dotnet &> /dev/null; then
    # Check if it exists in common locations
    if [ -f "$HOME/.dotnet/dotnet" ]; then
        export DOTNET_ROOT="$HOME/.dotnet"
        export PATH="$HOME/.dotnet:$PATH"
        echo -e "${GREEN}.NET SDK found in $HOME/.dotnet${NC}"
    elif [ -f "/usr/share/dotnet/dotnet" ]; then
        export PATH="/usr/share/dotnet:$PATH"
        echo -e "${GREEN}.NET SDK found in /usr/share/dotnet${NC}"
    elif [ -f "/usr/bin/dotnet" ]; then
        echo -e "${GREEN}.NET SDK found in /usr/bin${NC}"
    else
        echo -e "${RED}===================================================================${NC}"
        echo -e "${RED}ERROR: .NET SDK not found${NC}"
        echo -e "${RED}===================================================================${NC}"
        echo ""
        echo "The .NET SDK is required to build this project."
        echo ""
        echo -e "${BLUE}Quick Install (Ubuntu):${NC}"
        echo "  sudo apt-get update && sudo apt-get install -y dotnet-sdk-8.0"
        echo ""
        echo -e "${BLUE}Alternative Installation Options:${NC}"
        echo ""
        echo "1. Download from: https://dotnet.microsoft.com/download/dotnet/8.0"
        echo "   Extract to ~/.dotnet/ and add to PATH"
        echo ""
        echo "2. Use Docker:"
        echo "   docker-compose up --build"
        echo ""
        exit 1
    fi
fi

# Get dotnet command
DOTNET_CMD=$(command -v dotnet)
echo -e "${GREEN}.NET SDK found: $($DOTNET_CMD --version) at $DOTNET_CMD${NC}"
echo ""

# Check if projects target unsupported .NET version
echo "Checking .NET target framework versions..."
if grep -r "net10.0" src/ --include="*.csproj" > /dev/null 2>&1; then
    echo -e "${YELLOW}WARNING: Projects target .NET 10.0 which doesn't exist yet.${NC}"

    # Check current SDK version
    SDK_VERSION=$($DOTNET_CMD --version | cut -d'.' -f1)

    if [ "$SDK_VERSION" -lt 10 ]; then
        echo -e "${YELLOW}Current SDK version: $($DOTNET_CMD --version) (major version $SDK_VERSION)${NC}"
        echo -e "${YELLOW}Creating global.json to use available SDK...${NC}"

        cat > global.json <<'JSON_EOF'
{
  "sdk": {
    "rollForward": "latestMajor",
    "allowPrerelease": true
  }
}
JSON_EOF
        echo -e "${GREEN}Created global.json with rollForward policy${NC}"
        echo -e "${YELLOW}NOTE: Build may still fail. Consider updating .csproj files from net10.0 to net8.0${NC}"
    fi
fi
echo ""

# Function to create NuGet.config with proxy support
create_nuget_config() {
    local proxy_url="$1"
    cat > NuGet.config <<'XML_EOF'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
XML_EOF

    if [ -n "$proxy_url" ]; then
        cat >> NuGet.config <<XML_PROXY_EOF
  <config>
    <add key="http_proxy" value="$proxy_url" />
    <add key="https_proxy" value="$proxy_url" />
  </config>
XML_PROXY_EOF
    fi

    cat >> NuGet.config <<'XML_END_EOF'
</configuration>
XML_END_EOF
}

# Check for proxy environment variables
PROXY_URL="${HTTP_PROXY:-${http_proxy:-${HTTPS_PROXY:-${https_proxy}}}}"

if [ -n "$PROXY_URL" ]; then
    echo -e "${YELLOW}Proxy detected: $PROXY_URL${NC}"
    echo "Creating NuGet.config with proxy settings..."
    create_nuget_config "$PROXY_URL"
    echo -e "${GREEN}NuGet.config created with proxy settings${NC}"
else
    echo "No proxy detected. Using default NuGet configuration..."
    create_nuget_config ""
    echo -e "${GREEN}NuGet.config created${NC}"
fi

echo ""
echo "=== Building PRFactory ==="
echo ""

# Restore dependencies
echo "Restoring NuGet packages..."
if $DOTNET_CMD restore PRFactory.sln 2>&1 | tee restore.log; then
    echo -e "${GREEN}NuGet restore successful${NC}"
else
    echo -e "${RED}NuGet restore failed${NC}"
    echo ""
    echo "Check restore.log for details."
    exit 1
fi

echo ""

# Build the solution
echo "Building solution..."
if $DOTNET_CMD build PRFactory.sln -c Release --no-restore 2>&1 | tee build.log; then
    echo ""
    echo -e "${GREEN}===================================================================${NC}"
    echo -e "${GREEN}BUILD SUCCESSFUL${NC}"
    echo -e "${GREEN}===================================================================${NC}"
    echo ""

    # Show build artifacts
    echo "Build artifacts:"
    find . -name "*.dll" -path "*/bin/Release/*" -type f | head -10
    echo ""

    # Show what was created/modified
    if [ -f NuGet.config ]; then
        echo -e "${BLUE}Files created:${NC}"
        echo "  - NuGet.config (proxy configuration for NuGet)"
        if [ -f global.json ]; then
            echo "  - global.json (SDK version rollForward policy)"
        fi
        echo ""
        echo -e "${YELLOW}TIP: You can commit these files if needed${NC}"
        echo "     To remove: rm NuGet.config global.json"
    fi

    exit 0
else
    echo ""
    echo -e "${RED}===================================================================${NC}"
    echo -e "${RED}BUILD FAILED${NC}"
    echo -e "${RED}===================================================================${NC}"
    echo ""
    echo "Check build.log for detailed error messages."
    echo ""

    # Try to provide helpful error messages
    if grep -q "net10.0" build.log 2>/dev/null; then
        echo -e "${YELLOW}The build failed likely due to .NET 10.0 target framework.${NC}"
        echo "To fix, update all .csproj files:"
        echo "  find src/ -name '*.csproj' -exec sed -i 's/net10.0/net8.0/g' {} +"
        echo "  find tests/ -name '*.csproj' -exec sed -i 's/net10.0/net8.0/g' {} +"
    fi

    exit 1
fi
