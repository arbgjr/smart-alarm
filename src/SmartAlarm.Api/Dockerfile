# Dockerfile for SmartAlarm.Api (.NET 8)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Configurar variáveis de ambiente do NuGet
ENV NUGET_PACKAGES=/src/.nuget/packages
ENV NUGET_HTTP_CACHE_PATH=/src/.nuget/v3-cache
ENV NUGET_PLUGINS_CACHE_PATH=/src/.nuget/plugins-cache
ENV NUGET_XMLDOC_MODE=skip
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV DOTNET_NOLOGO=1
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Criar diretório para NuGet packages
RUN mkdir -p /src/.nuget/packages

# Criar um NuGet.Config completamente limpo diretamente no container
RUN echo '<?xml version="1.0" encoding="utf-8"?>' > /src/NuGet.Config && \
    echo '<configuration>' >> /src/NuGet.Config && \
    echo '  <packageSources>' >> /src/NuGet.Config && \
    echo '    <clear />' >> /src/NuGet.Config && \
    echo '    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />' >> /src/NuGet.Config && \
    echo '  </packageSources>' >> /src/NuGet.Config && \
    echo '  <config>' >> /src/NuGet.Config && \
    echo '    <add key="globalPackagesFolder" value="/src/.nuget/packages" />' >> /src/NuGet.Config && \
    echo '    <add key="repositoryPath" value="/src/.nuget/packages" />' >> /src/NuGet.Config && \
    echo '  </config>' >> /src/NuGet.Config && \
    echo '  <fallbackPackageFolders>' >> /src/NuGet.Config && \
    echo '    <clear />' >> /src/NuGet.Config && \
    echo '  </fallbackPackageFolders>' >> /src/NuGet.Config && \
    echo '</configuration>' >> /src/NuGet.Config

# Copiar arquivos de projeto
COPY src/SmartAlarm.Api/SmartAlarm.Api.csproj SmartAlarm.Api/
COPY src/SmartAlarm.Observability/SmartAlarm.Observability.csproj SmartAlarm.Observability/
COPY src/SmartAlarm.Application/SmartAlarm.Application.csproj SmartAlarm.Application/
COPY src/SmartAlarm.Domain/SmartAlarm.Domain.csproj SmartAlarm.Domain/
COPY src/SmartAlarm.Infrastructure/SmartAlarm.Infrastructure.csproj SmartAlarm.Infrastructure/
COPY src/SmartAlarm.KeyVault/SmartAlarm.KeyVault.csproj SmartAlarm.KeyVault/

# Limpar cache e restaurar dependências
RUN dotnet nuget locals all --clear && \
    dotnet restore "SmartAlarm.Api/SmartAlarm.Api.csproj" --configfile /src/NuGet.Config --packages /src/.nuget/packages --force

# Copiar apenas os projetos necessários
COPY src/SmartAlarm.Api/ ./SmartAlarm.Api/
COPY src/SmartAlarm.Observability/ ./SmartAlarm.Observability/
COPY src/SmartAlarm.Application/ ./SmartAlarm.Application/
COPY src/SmartAlarm.Domain/ ./SmartAlarm.Domain/
COPY src/SmartAlarm.Infrastructure/ ./SmartAlarm.Infrastructure/
COPY src/SmartAlarm.KeyVault/ ./SmartAlarm.KeyVault/

WORKDIR /src/SmartAlarm.Api
RUN dotnet build "SmartAlarm.Api.csproj" -c Release -o /app/build --configfile /src/NuGet.Config --no-restore

FROM build AS publish
RUN dotnet publish "SmartAlarm.Api.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SmartAlarm.Api.dll"]