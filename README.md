# PRFactory

An AI-powered Pull Request Factory that automates GitHub pull request creation and management using Microsoft Agent Framework.

## Prerequisites

Before you begin, ensure you have the following installed:

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later
- [Docker](https://www.docker.com/get-started) and Docker Compose (for local development)
- [Git](https://git-scm.com/downloads)
- A GitHub account with a Personal Access Token (PAT)

## Project Structure

```
PRFactory/
├── src/
│   ├── PRFactory.Domain/          # Domain entities and interfaces
│   ├── PRFactory.Infrastructure/  # Data access, repositories, external services
│   ├── PRFactory.Api/              # REST API web service
│   └── PRFactory.Worker/           # Background worker service
├── tests/
│   └── PRFactory.Tests/           # Unit and integration tests
├── PRFactory.sln                   # Solution file
├── docker-compose.yml              # Docker compose configuration
└── global.json                     # .NET SDK version specification
```

## Getting Started

### 1. Clone the Repository

```bash
git clone <repository-url>
cd PRFactory
```

### 2. Configure Application Settings

The application requires configuration for GitHub integration and other services. Configuration can be provided via:

- `appsettings.json` / `appsettings.Development.json` files
- Environment variables
- User secrets (recommended for local development)

#### Required Configuration

**GitHub Settings:**
```json
{
  "GitHub": {
    "Token": "your-github-personal-access-token",
    "BaseUrl": "https://api.github.com"
  }
}
```

**Database Connection:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=prfactory.db"
  }
}
```

**OpenTelemetry (Optional):**
```json
{
  "OpenTelemetry": {
    "ServiceName": "PRFactory",
    "Jaeger": {
      "AgentHost": "localhost",
      "AgentPort": 6831
    }
  }
}
```

#### Using User Secrets (Recommended for Local Development)

```bash
# For the API project
cd src/PRFactory.Api
dotnet user-secrets set "GitHub:Token" "your-github-token"

# For the Worker project
cd ../PRFactory.Worker
dotnet user-secrets set "GitHub:Token" "your-github-token"
```

### 3. Build the Solution

```bash
# Restore dependencies and build all projects
dotnet restore
dotnet build

# Or build in Release mode
dotnet build -c Release
```

### 4. Run Database Migrations

```bash
# From the solution root
cd src/PRFactory.Api
dotnet ef database update
```

### 5. Run the Application

#### Option A: Run with Docker Compose (Recommended)

```bash
# Build and start all services
docker-compose up --build

# Run in detached mode
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down
```

Services will be available at:
- **API**: http://localhost:5000
- **Swagger UI**: http://localhost:5000/swagger
- **Jaeger UI**: http://localhost:16686

#### Option B: Run Locally Without Docker

Start the API:
```bash
cd src/PRFactory.Api
dotnet run
```

In a separate terminal, start the Worker:
```bash
cd src/PRFactory.Worker
dotnet run
```

(Optional) Start Jaeger for tracing:
```bash
docker run -d --name jaeger \
  -p 5775:5775/udp \
  -p 6831:6831/udp \
  -p 6832:6832/udp \
  -p 5778:5778 \
  -p 16686:16686 \
  -p 14268:14268 \
  -p 14250:14250 \
  -p 9411:9411 \
  jaegertracing/all-in-one:1.51
```

## Running Tests

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test /p:CollectCoverage=true

# Run tests in a specific project
dotnet test tests/PRFactory.Tests/PRFactory.Tests.csproj

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"
```

## Development

### Code Style and Standards

- Target Framework: .NET 9
- Language Version: C# 12
- Nullable Reference Types: Enabled
- Implicit Usings: Enabled

### Adding New Packages

Shared package versions are defined in `src/Directory.Build.props`. To add a new package:

1. Add the version property in `Directory.Build.props`
2. Reference the package in your project using the version property

Example:
```xml
<!-- In Directory.Build.props -->
<PropertyGroup>
  <MyPackageVersion>1.0.0</MyPackageVersion>
</PropertyGroup>

<!-- In your .csproj -->
<PackageReference Include="MyPackage" Version="$(MyPackageVersion)" />
```

### Database Migrations

Create a new migration:
```bash
cd src/PRFactory.Api
dotnet ef migrations add MigrationName
```

Apply migrations:
```bash
dotnet ef database update
```

Remove last migration:
```bash
dotnet ef migrations remove
```

## Architecture Overview

PRFactory follows Clean Architecture principles with clear separation of concerns:

- **Domain Layer**: Core business entities, value objects, and domain interfaces
- **Infrastructure Layer**: Data access, external service integrations, and implementations
- **API Layer**: REST endpoints, controllers, and API-specific logic
- **Worker Layer**: Background job processing and scheduled tasks

For detailed architecture documentation, see [docs/architecture.md](docs/architecture.md) (if available).

## Observability

The application includes built-in observability features:

- **Structured Logging**: Using Serilog with console and file sinks
- **Distributed Tracing**: OpenTelemetry with Jaeger exporter
- **Metrics**: Built-in ASP.NET Core metrics

Access Jaeger UI at http://localhost:16686 to view traces.

## Troubleshooting

### Common Issues

**Issue**: `dotnet: command not found`
- **Solution**: Install .NET 9 SDK from https://dotnet.microsoft.com/download

**Issue**: Database connection errors
- **Solution**: Ensure the database file path is writable and migrations are applied

**Issue**: GitHub API rate limiting
- **Solution**: Verify your GitHub token has appropriate permissions and hasn't exceeded rate limits

**Issue**: Docker containers fail to start
- **Solution**: Check Docker logs with `docker-compose logs` and ensure ports aren't already in use

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For issues, questions, or contributions, please open an issue on the GitHub repository.
