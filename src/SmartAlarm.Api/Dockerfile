# Dockerfile for SmartAlarm.Api (.NET 8)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["SmartAlarm.Api/SmartAlarm.Api.csproj", "SmartAlarm.Api/"]
COPY ["SmartAlarm.Observability/SmartAlarm.Observability.csproj", "SmartAlarm.Observability/"]
RUN dotnet restore "SmartAlarm.Api/SmartAlarm.Api.csproj"
COPY . .
WORKDIR "/src/SmartAlarm.Api"
RUN dotnet build "SmartAlarm.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SmartAlarm.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SmartAlarm.Api.dll"]
