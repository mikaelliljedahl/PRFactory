# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/PRFactory.Worker/PRFactory.Worker.csproj", "PRFactory.Worker/"]
COPY ["src/PRFactory.Domain/PRFactory.Domain.csproj", "PRFactory.Domain/"]
COPY ["src/PRFactory.Infrastructure/PRFactory.Infrastructure.csproj", "PRFactory.Infrastructure/"]
COPY ["src/PRFactory.Core/PRFactory.Core.csproj", "PRFactory.Core/"]
RUN dotnet restore "PRFactory.Worker/PRFactory.Worker.csproj"

# Copy source code and build
COPY src/ .
WORKDIR "/src/PRFactory.Worker"
RUN dotnet build "PRFactory.Worker.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "PRFactory.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app

# Create directories for workspace and install git
RUN mkdir -p /var/prfactory/workspace/checkpoints && \
    mkdir -p /var/prfactory/workspace/repositories && \
    mkdir -p /var/prfactory/data && \
    apt-get update && \
    apt-get install -y git curl && \
    rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

# Change ownership of application and workspace directories to the built-in 'app' user (UID 1654)
RUN chown -R app:app /app /var/prfactory

# Health check - Worker service doesn't expose HTTP endpoint, check process
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD pgrep -f "PRFactory.Worker" || exit 1

# Switch to non-root user (Microsoft's built-in 'app' user)
USER app

ENTRYPOINT ["dotnet", "PRFactory.Worker.dll"]
