# Dockerfile for SmartAlarm.Api (.NET 8)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80


FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/SmartAlarm.Api/SmartAlarm.Api.csproj SmartAlarm.Api/
COPY src/SmartAlarm.Observability/SmartAlarm.Observability.csproj SmartAlarm.Observability/
COPY src/SmartAlarm.Application/SmartAlarm.Application.csproj SmartAlarm.Application/
COPY src/SmartAlarm.Domain/SmartAlarm.Domain.csproj SmartAlarm.Domain/
COPY src/SmartAlarm.Infrastructure/SmartAlarm.Infrastructure.csproj SmartAlarm.Infrastructure/
COPY src/SmartAlarm.KeyVault/SmartAlarm.KeyVault.csproj SmartAlarm.KeyVault/
RUN dotnet restore "SmartAlarm.Api/SmartAlarm.Api.csproj"
# Copia apenas os projetos necessários
COPY src/SmartAlarm.Api/ ./SmartAlarm.Api/
COPY src/SmartAlarm.Observability/ ./SmartAlarm.Observability/
COPY src/SmartAlarm.Application/ ./SmartAlarm.Application/
COPY src/SmartAlarm.Domain/ ./SmartAlarm.Domain/
COPY src/SmartAlarm.Infrastructure/ ./SmartAlarm.Infrastructure/
COPY src/SmartAlarm.KeyVault/ ./SmartAlarm.KeyVault/
WORKDIR /src/SmartAlarm.Api
RUN dotnet build "SmartAlarm.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SmartAlarm.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SmartAlarm.Api.dll"]
