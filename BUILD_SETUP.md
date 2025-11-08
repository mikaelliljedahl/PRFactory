# Build Setup Instructions

## Current Status

The `verify.sh` build script has been created and configured to handle:

- ✅ NuGet proxy configuration (automatically detects and configures)
- ✅ .NET SDK version detection
- ✅ .NET 10.0 target framework workaround (creates global.json with rollForward policy)
- ❌ .NET SDK installation (requires manual setup due to environment restrictions)

## Prerequisites

### .NET SDK Installation Required

The project requires .NET SDK to build. Due to environment restrictions (proxy blocks Microsoft download URLs, sudo permissions issues), automatic installation is not possible.

### Installation Options

#### Option 1: Ubuntu Package Manager (Recommended)

```bash
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0
```

> **Note**: .NET SDK 8.0 is available in Ubuntu 24.04 repositories and will work with this project (with the global.json rollForward policy).

#### Option 2: Manual Binary Installation

1. Download .NET SDK 8.0 or 9.0 from: https://dotnet.microsoft.com/download/dotnet
2. Extract to `~/.dotnet/`
3. Add to PATH: `export PATH="$HOME/.dotnet:$PATH"`

#### Option 3: Use Docker

If Docker is available:

```bash
docker-compose up --build
```

## Building the Project

Once .NET SDK is installed, run:

```bash
./verify.sh
```

### What verify.sh Does

1. **Checks for .NET SDK** - Looks in standard locations
2. **Detects proxy settings** - Automatically configures NuGet.config with proxy
3. **Handles version mismatch** - Creates global.json to work around .NET 10.0 target
4. **Restores NuGet packages** - With proxy support
5. **Builds the solution** - In Release configuration
6. **Shows results** - Build artifacts and helpful error messages

### Expected Issues

#### Issue: Projects target .NET 10.0

**Symptom**: Build fails with ".NET 10.0 SDK not found"

**Solution**: The verify.sh script creates a `global.json` with rollForward policy. If the build still fails, update project files:

```bash
# Update all projects to target .NET 8.0
find src/ -name '*.csproj' -exec sed -i 's/net10.0/net8.0/g' {} +
find tests/ -name '*.csproj' -exec sed -i 's/net10.0/net8.0/g' {} +
```

#### Issue: NuGet restore fails

**Symptom**: "Unable to load the service index for source https://api.nuget.org/v3/index.json"

**Solution**: The verify.sh script should handle this automatically by creating NuGet.config with proxy settings. If it still fails, manually set:

```bash
export HTTP_PROXY=http://your-proxy:port
export HTTPS_PROXY=http://your-proxy:port
```

## Files Created by verify.sh

- `NuGet.config` - NuGet proxy configuration
- `global.json` - SDK version rollForward policy (if needed)
- `restore.log` - NuGet restore output
- `build.log` - Build output
- `build_output.txt` - Complete verification output

## Proxy Configuration

The current environment has proxy configured at:
- `http://container_container_*:noauth@21.0.0.33:15002`

The verify.sh script automatically detects this and configures NuGet.config accordingly.

### Allowed Domains

The proxy allows:
- ✅ api.nuget.org (NuGet packages)
- ❌ download.visualstudio.microsoft.com (.NET SDK downloads)
- ❌ builds.dotnet.microsoft.com (.NET install scripts)

This is why .NET SDK must be installed via Ubuntu repositories or manual download outside the environment.

## Next Steps

1. Install .NET SDK using Option 1 above (Ubuntu package manager)
2. Run `./verify.sh` to build and verify
3. If build fails due to .NET 10.0, update project files as shown above
4. Commit NuGet.config if proxy is permanent requirement

## Troubleshooting

### "dotnet: command not found"

.NET SDK is not installed or not in PATH. Follow installation options above.

### "sudo: error initializing audit plugin"

The environment has sudo permission issues. Use apt-get outside this environment or use Docker.

### "403 Forbidden" during downloads

The proxy is blocking the URL. Use Ubuntu repositories instead of direct downloads.

