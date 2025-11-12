# .NET 10 SDK Hook Troubleshooting Summary

## Issue Fixed ✅

The `.claude/scripts/session-start.sh` SessionStart hook was failing to install .NET 10 SDK due to Claude Code web proxy blocking Microsoft's CDN servers.

### Root Cause

Claude Code web environment proxy blocks these Microsoft domains with **403 Forbidden**:
- `builds.dotnet.microsoft.com`
- `ci.dot.net`
- `download.visualstudio.microsoft.com`
- `aka.ms` redirects to blocked domains

This is a security restriction in Claude Code's containerized web environment.

### Solution Implemented

Updated `.claude/scripts/session-start.sh` to:
1. Install **.NET 8 LTS** via Ubuntu apt packages (not blocked)
2. Use `apt-get download` + `dpkg-deb -x` to extract packages without sudo
3. Install to `~/.dotnet` with proper PATH/DOTNET_ROOT setup
4. Provide clear messaging about the .NET version mismatch

### Current Status

✅ **SessionStart hook works** - No more proxy 403 errors
✅ **.NET 8.0.121 SDK installed** successfully in `~/.dotnet`
✅ **Basic `dotnet` commands work** - Can run `dotnet --version`, `dotnet --info`
❌ **Project won't build yet** - Requires .NET 10 (project targets `net10.0`)
❌ **Dependency conflicts** - .NET 9+ packages incompatible with .NET 8

### To Build the Project

You have **three options**:

#### Option 1: Use Local Claude Code (Recommended) ⭐

Local Claude Code CLI does **not** have the proxy restriction:
```bash
# On your local machine:
cd /path/to/PRFactory
claude

# .NET 10 SDK will download successfully
```

#### Option 2: Manually Downgrade to .NET 8 (Testing Only)

**WARNING:** This breaks the .NET 10 upgrade from PR #54.

1. Update `global.json`:
   ```json
   {
     "sdk": {
       "version": "8.0.100",
       "rollForward": "latestFeature"
     }
   }
   ```

2. Update all `.csproj` files:
   ```bash
   find . -name "*.csproj" -exec sed -i 's/<TargetFramework>net10\.0<\/TargetFramework>/<TargetFramework>net8.0<\/TargetFramework>/g' {} \;
   ```

3. Downgrade NuGet packages to .NET 8 versions:
   - `Microsoft.AspNetCore.Identity.EntityFrameworkCore`: 9.0.0 → 8.0.0
   - `Microsoft.AspNetCore.Authentication.Google`: 9.0.0 → 8.0.0
   - `Microsoft.AspNetCore.Authentication.MicrosoftAccount`: 9.0.0 → 8.0.0
   - `Microsoft.AspNetCore.Mvc.Testing`: 9.0.0 → 8.0.0

4. Run:
   ```bash
   export PATH="$HOME/.dotnet:$PATH"
   export DOTNET_ROOT="$HOME/.dotnet"
   dotnet restore
   dotnet build
   ```

#### Option 3: Pre-download .NET 10 SDK

If you have .NET 10 installed elsewhere:
```bash
# On machine with .NET 10:
tar -czf dotnet-10-sdk.tar.gz -C ~/.dotnet .

# Upload to Claude Code web and extract:
tar -xzf dotnet-10-sdk.tar.gz -C ~/.dotnet
export PATH="$HOME/.dotnet:$PATH"
export DOTNET_ROOT="$HOME/.dotnet"
```

### Verification

Check your .NET installation:
```bash
export PATH="$HOME/.dotnet:$PATH"
export DOTNET_ROOT="$HOME/.dotnet"
dotnet --version
# Claude Code web: 8.0.121
# Local Claude Code: 10.0.100
```

### Files Changed

- `.claude/scripts/session-start.sh` - Fixed hook with apt-based .NET 8 installation
- `docs/CLAUDE_CODE_WEB_NOTES.md` - Detailed documentation
- `TROUBLESHOOTING_SUMMARY.md` - This file

### Commit

```
commit 0a609cb
fix: Update SessionStart hook to handle proxy-blocked .NET 10 downloads
```

### Next Steps

**Recommended:** Use **local Claude Code CLI** for full .NET 10 support. The web version has environment limitations that block Microsoft CDN downloads.

---

**Questions?** See:
- `docs/CLAUDE_CODE_WEB_NOTES.md` - Detailed technical explanation
- `.claude/scripts/session-start.sh` - Updated hook implementation
- `CLAUDE.md` - General AI agent guidelines
