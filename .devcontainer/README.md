# Dev Container Configuration

This devcontainer provides a complete development environment for JobFinder with:
- .NET 8.0 SDK
- SQL Server 2022
- Node.js 20 (for frontend tooling)
- All necessary VS Code extensions

## What's Included

### Services
- **app**: .NET 8 development container
- **db**: SQL Server 2022 with persistent storage

### Ports
- `5168`: HTTP endpoint for Blazor app
- `7290`: HTTPS endpoint for Blazor app
- `1433`: SQL Server

### VS Code Extensions
- C# Dev Kit
- SQL Server (mssql)
- Docker
- EditorConfig
- .NET Test Explorer

## Database Connection

The SQL Server container is automatically configured with:
- **Server**: `localhost,1433` (or `127.0.0.1,1433`)
- **Database**: `JobFinder` (will be created by EF migrations)
- **User**: `SA`
- **Password**: `Root1234!`
- **Connection String**: Already set in environment variables (matches appsettings.Development.json)

## First Time Setup

After the container starts:

1. **Apply EF Core Migrations**:
   ```bash
   dotnet ef database update
   ```

2. **Run the application**:
   ```bash
   dotnet run --launch-profile "QuizManager"
   ```

3. **Access the app**:
   - HTTP: http://localhost:5168
   - HTTPS: https://localhost:7290

## Customization

### Change SQL Server Password

Edit `.devcontainer/docker-compose.yml` in three places:
```yaml
# 1. In the app environment section
- ConnectionStrings__DbConnectionString=Server=localhost,1433;Database=JobFinder;User Id=SA;Password=YourNewPassword;...

# 2. In the db environment section
environment:
  SA_PASSWORD: "YourNewPassword"

# 3. In the healthcheck section
healthcheck:
  test: /opt/mssql-tools/bin/sqlcmd -S localhost -U SA -P 'YourNewPassword' -Q 'SELECT 1' || exit 1
```

Also update `appsettings.Development.json` to match.

### Add More VS Code Extensions

Edit `.devcontainer/devcontainer.json`:
```json
"customizations": {
  "vscode": {
    "extensions": [
      "existing-extensions",
      "your-new-extension-id"
    ]
  }
}
```

### Add Additional Services

Edit `.devcontainer/docker-compose.yml` to add Redis, PostgreSQL, etc.

## Troubleshooting

### SQL Server won't start
- Ensure Docker has at least 4GB RAM allocated
- Check logs: `docker-compose -f .devcontainer/docker-compose.yml logs db`

### Can't connect to database
- Wait 10-20 seconds after container starts (SQL Server needs time to initialize)
- Verify connection string in `appsettings.Development.json` or user-secrets

### Ports already in use
- Change port mappings in `docker-compose.yml`
- Update `forwardPorts` in `devcontainer.json`

## Rebuilding the Container

If you make changes to the devcontainer configuration:
1. Command Palette (Ctrl+Shift+P / Cmd+Shift+P)
2. Select: "Dev Containers: Rebuild Container"

Or use the Ona tool: `rebuild_devcontainer`

## Data Persistence

SQL Server data is persisted in a Docker volume named `sqlserver-data`. To reset:
```bash
docker volume rm devcontainer_sqlserver-data
```

Then rebuild the container.
