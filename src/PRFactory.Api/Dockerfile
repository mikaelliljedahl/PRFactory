# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/PRFactory.Api/PRFactory.Api.csproj", "PRFactory.Api/"]
COPY ["src/PRFactory.Domain/PRFactory.Domain.csproj", "PRFactory.Domain/"]
COPY ["src/PRFactory.Infrastructure/PRFactory.Infrastructure.csproj", "PRFactory.Infrastructure/"]
RUN dotnet restore "PRFactory.Api/PRFactory.Api.csproj"

# Copy source code and build
COPY src/ .
WORKDIR "/src/PRFactory.Api"
RUN dotnet build "PRFactory.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "PRFactory.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Create directories for workspace
RUN mkdir -p /var/prfactory/workspace/checkpoints && \
    mkdir -p /var/prfactory/workspace/repositories && \
    mkdir -p /var/prfactory/data

# Install git for repository operations
RUN apt-get update && \
    apt-get install -y git && \
    rm -rf /var/lib/apt/lists/*

# Create a non-root user to run the application
RUN useradd -m -u 1000 -s /bin/bash appuser && \
    chown -R appuser:appuser /app /var/prfactory

COPY --from=publish /app/publish .

# Configure ASP.NET Core
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost/health || exit 1

# Switch to non-root user
USER appuser

ENTRYPOINT ["dotnet", "PRFactory.Api.dll"]
