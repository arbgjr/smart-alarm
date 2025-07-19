# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution file and project files
COPY SmartAlarm.sln .
COPY services/alarm-service/*.csproj services/alarm-service/
COPY src/SmartAlarm.Domain/*.csproj src/SmartAlarm.Domain/
COPY src/SmartAlarm.Application/*.csproj src/SmartAlarm.Application/
COPY src/SmartAlarm.Infrastructure/*.csproj src/SmartAlarm.Infrastructure/
COPY src/SmartAlarm.Observability/*.csproj src/SmartAlarm.Observability/

# Restore dependencies
RUN dotnet restore services/alarm-service/

# Copy source code
COPY services/alarm-service/ services/alarm-service/
COPY src/ src/

# Build the application
WORKDIR /src/services/alarm-service
RUN dotnet build -c Release --no-restore

# Publish the application
RUN dotnet publish -c Release --no-build -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install required packages for observability
RUN apt-get update && apt-get install -y \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN groupadd -r smartalarm && useradd -r -g smartalarm smartalarm
RUN chown -R smartalarm:smartalarm /app
USER smartalarm

# Copy published application
COPY --from=build --chown=smartalarm:smartalarm /app/publish .

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_USE_POLLING_FILE_WATCHER=true

# Set entry point
ENTRYPOINT ["dotnet", "SmartAlarm.AlarmService.dll"]
