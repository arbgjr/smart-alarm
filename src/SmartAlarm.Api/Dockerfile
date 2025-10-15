# Dockerfile for SmartAlarm.Api (.NET 8)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Configurar variáveis de ambiente do NuGet (simplificadas)
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV DOTNET_NOLOGO=1
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Copiar o arquivo de gerenciamento de pacotes centralizados PRIMEIRO
COPY Directory.Packages.props ./

# Copiar arquivos de projeto (caminhos corretos - COM 'src/' prefixo)
COPY src/SmartAlarm.Api/SmartAlarm.Api.csproj SmartAlarm.Api/
COPY src/SmartAlarm.Observability/SmartAlarm.Observability.csproj SmartAlarm.Observability/
COPY src/SmartAlarm.Application/SmartAlarm.Application.csproj SmartAlarm.Application/
COPY src/SmartAlarm.Domain/SmartAlarm.Domain.csproj SmartAlarm.Domain/
COPY src/SmartAlarm.Infrastructure/SmartAlarm.Infrastructure.csproj SmartAlarm.Infrastructure/
COPY src/SmartAlarm.KeyVault/SmartAlarm.KeyVault.csproj SmartAlarm.KeyVault/

# Restaurar dependências (usando configuração padrão do .NET e limpando cache)
RUN dotnet nuget locals all --clear
RUN dotnet restore "SmartAlarm.Api/SmartAlarm.Api.csproj" --verbosity normal --force

# Copiar código fonte
COPY src/SmartAlarm.Api/ ./SmartAlarm.Api/
COPY src/SmartAlarm.Observability/ ./SmartAlarm.Observability/
COPY src/SmartAlarm.Application/ ./SmartAlarm.Application/
COPY src/SmartAlarm.Domain/ ./SmartAlarm.Domain/
COPY src/SmartAlarm.Infrastructure/ ./SmartAlarm.Infrastructure/
COPY src/SmartAlarm.KeyVault/ ./SmartAlarm.KeyVault/

# Build
WORKDIR /src/SmartAlarm.Api
RUN dotnet build "SmartAlarm.Api.csproj" -c Release -o /app/build --no-restore

FROM build AS publish
RUN dotnet publish "SmartAlarm.Api.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SmartAlarm.Api.dll"]
