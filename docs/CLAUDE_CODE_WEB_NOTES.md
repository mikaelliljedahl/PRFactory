# Claude Code Web Environment Notes

## .NET 10 SDK Installation Issue

### Problem

When running PRFactory in Claude Code on the web, the .NET 10 SDK cannot be downloaded because the container proxy blocks Microsoft's CDN servers:

- `builds.dotnet.microsoft.com` - **403 Forbidden**
- `ci.dot.net` - **403 Forbidden**
- `download.visualstudio.microsoft.com` - **403 Forbidden**

This is a security restriction in the Claude Code web container environment.

### Workaround

The SessionStart hook (`.claude/scripts/session-start.sh`) has been updated to automatically install **.NET 8 LTS** via Ubuntu apt packages when .NET 10 is unavailable:

```bash
# The hook now:
1. Attempts to download .NET 10 SDK (will fail in Claude Code web)
2. Falls back to downloading .NET 8 via apt-get download
3. Extracts .deb packages to ~/.dotnet without requiring sudo
4. Sets up PATH and DOTNET_ROOT environment variables
```

### Impact

**.NET 8 is installed instead of .NET 10** when running in Claude Code web. This means:

❌ **The project will NOT build** with .NET 8 because:
- The project targets .NET 10 (`<TargetFramework>net10.0</TargetFramework>`)
- Dependencies use .NET 9.0+ packages (Microsoft.AspNetCore.Identity.EntityFrameworkCore 9.0.0, etc.)

### Solutions

#### Option 1: Use Local Claude Code (Recommended)

**Local Claude Code does not have this proxy restriction.** You can:
1. Install Claude Code CLI locally
2. Run `claude` in the PRFactory directory
3. .NET 10 SDK will download successfully from Microsoft

#### Option 2: Temporarily Downgrade to .NET 8 (For Testing Only)

If you must use Claude Code web and want to test basic functionality:

1. Update `global.json`:
   ```json
   {
     "sdk": {
       "version": "8.0.100",
       "rollForward": "latestFeature"
     }
   }
   ```

2. Update all `.csproj` files to target `net8.0`:
   ```xml
   <TargetFramework>net8.0</TargetFramework>
   ```

3. Downgrade .NET 9+ packages to .NET 8 compatible versions in all `.csproj` files:
   ```xml
   <!-- Change from: -->
   <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="9.0.0" />

   <!-- To: -->
   <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="8.0.0" />
   ```

**⚠️ WARNING:** This breaks the .NET 10 upgrade (PR #54) and is only for temporary testing.

#### Option 3: Pre-download .NET 10 SDK

If you have access to a machine with .NET 10 SDK installed, you can:
1. Copy `~/.dotnet` directory from that machine
2. Upload it to Claude Code web session
3. Set PATH and DOTNET_ROOT manually

### Verification

After SessionStart hook runs, verify .NET installation:

```bash
export PATH="$HOME/.dotnet:$PATH"
export DOTNET_ROOT="$HOME/.dotnet"
dotnet --version
# Should show: 8.0.121 (in Claude Code web)
```

### Related Files

- `.claude/scripts/session-start.sh` - Fixed SessionStart hook
- `global.json` - SDK version specification (currently: 10.0.100)
- `**/*.csproj` - All project files target net10.0

### Status

✅ **SessionStart hook is fixed** - No longer fails with proxy errors
❌ **Project still requires .NET 10** - Cannot build in Claude Code web without downgrades
✅ **Workarounds documented** - Users know their options
