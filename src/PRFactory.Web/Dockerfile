# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["PRFactory.sln", "./"]
COPY ["src/PRFactory.Domain/PRFactory.Domain.csproj", "src/PRFactory.Domain/"]
COPY ["src/PRFactory.Core/PRFactory.Core.csproj", "src/PRFactory.Core/"]
COPY ["src/PRFactory.Infrastructure/PRFactory.Infrastructure.csproj", "src/PRFactory.Infrastructure/"]
COPY ["src/PRFactory.Web/PRFactory.Web.csproj", "src/PRFactory.Web/"]

# Restore dependencies
RUN dotnet restore "src/PRFactory.Web/PRFactory.Web.csproj"

# Copy source code
COPY . .

# Build and publish
WORKDIR "/src/src/PRFactory.Web"
RUN dotnet publish "PRFactory.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install git (for LibGit2Sharp operations)
RUN apt-get update && apt-get install -y git && rm -rf /var/lib/apt/lists/*

# Create app user
RUN groupadd -r app && useradd -r -g app -u 1654 app

# Create directories for workspace and data
RUN mkdir -p /var/prfactory/workspace/checkpoints \
             /var/prfactory/workspace/repositories \
             /var/prfactory/data \
    && chown -R app:app /var/prfactory

# Copy published application
COPY --from=build /app/publish .

# Switch to non-root user
USER app

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl --fail http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "PRFactory.Web.dll"]
