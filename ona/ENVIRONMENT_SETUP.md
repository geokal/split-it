# Environment Setup Issues

## Current Problem

The .NET SDK is installed but cannot run due to missing ICU (International Components for Unicode) libraries.

### Error Message
```
Process terminated. Couldn't find a valid ICU package installed on the system. 
Please install libicu (or icu-libs) using your package manager and try again.
```

## Root Cause

The Gitpod/Ona base environment doesn't include the dev container configuration from `.devcontainer/docker-compose.yml`, which specifies:
- Image: `mcr.microsoft.com/devcontainers/dotnet:1-8.0`
- This image includes all required .NET dependencies

## Solutions

### Solution 1: Use Dev Container (Recommended)

A `.gitpod.yml` file has been created to configure Gitpod to use the dev container.

**Next Steps:**
1. Commit the `.gitpod.yml` file
2. Push to GitHub
3. Restart the Gitpod workspace
4. The workspace will rebuild using the proper .NET container

### Solution 2: Manual Installation (Temporary)

If you need .NET immediately without restarting:

```bash
# .NET SDK is already installed at /usr/local/dotnet
export PATH="/usr/local/dotnet:$PATH"
export DOTNET_ROOT="/usr/local/dotnet"

# Add to shell profile
echo 'export PATH="/usr/local/dotnet:$PATH"' >> ~/.bashrc
echo 'export DOTNET_ROOT="/usr/local/dotnet"' >> ~/.bashrc
```

**Limitation:** This won't work until ICU libraries are available, which requires:
- Package manager access (apt-get), OR
- Dev container rebuild

## Verification

Once the environment is properly set up, verify with:

```bash
dotnet --version
# Should output: 9.0.308

dotnet --info
# Should show SDK and runtime information

dotnet restore
# Should restore NuGet packages

dotnet build
# Should build the solution
```

## Dev Container Configuration

The project has proper dev container configuration:
- **File**: `.devcontainer/docker-compose.yml`
- **Image**: `mcr.microsoft.com/devcontainers/dotnet:1-8.0`
- **Services**: 
  - `app`: .NET 8 development container
  - `db`: SQL Server 2019
- **Ports**: 5168 (HTTP), 7290 (HTTPS), 1433 (SQL Server)

## Recommended Action

**Commit and push `.gitpod.yml`, then restart the workspace.**

This will ensure the dev container is used, providing:
- ✅ .NET SDK 9.0.308 (from global.json)
- ✅ All required dependencies (ICU, etc.)
- ✅ SQL Server 2019
- ✅ Proper environment variables
- ✅ VS Code extensions
