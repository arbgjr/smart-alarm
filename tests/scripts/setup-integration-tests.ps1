#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Sets up integration test environment with TestContainers
.DESCRIPTION
    This script configures and starts the required containers for integration testing:
    - PostgreSQL for database tests
    - Redis for caching tests
    - MinIO for storage tests
    - RabbitMQ for messaging tests
    - Vault for secrets management tests
.EXAMPLE
    .\setup-integration-tests.ps1
#>

param(
    [switch]$SkipBuild,
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

Write-Host "🚀 Setting up Smart Alarm Integration Test Environment" -ForegroundColor Green

# Check if Docker is running
try {
    docker version | Out-Null
    Write-Host "✅ Docker is running" -ForegroundColor Green
} catch {
    Write-Error "❌ Docker is not running. Please start Docker Desktop and try again."
    exit 1
}

# Build the test project if not skipped
if (-not $SkipBuild) {
    Write-Host "🔨 Building test projects..." -ForegroundColor Yellow
    dotnet build tests/SmartAlarm.Infrastructure.Tests/SmartAlarm.Infrastructure.Tests.csproj
    if ($LASTEXITCODE -ne 0) {
        Write-Error "❌ Build failed"
        exit 1
    }
    Write-Host "✅ Build completed" -ForegroundColor Green
}

# Function to check if container is running
function Test-ContainerRunning {
    param([string]$ContainerName)
    $result = docker ps --filter "name=$ContainerName" --format "{{.Names}}" 2>$null
    return $result -eq $ContainerName
}

# Function to wait for container to be ready
function Wait-ForContainer {
    param(
        [string]$ContainerName,
        [string]$HealthCheck,
        [int]$MaxAttempts = 30
    )

    Write-Host "⏳ Waiting for $ContainerName to be ready..." -ForegroundColor Yellow

    for ($i = 1; $i -le $MaxAttempts; $i++) {
        try {
            Invoke-Expression $HealthCheck | Out-Null
            Write-Host "✅ $ContainerName is ready" -ForegroundColor Green
            return $true
        } catch {
            if ($i -eq $MaxAttempts) {
                Write-Error "❌ $ContainerName failed to start after $MaxAttempts attempts"
                return $false
            }
            Start-Sleep -Seconds 2
        }
    }
}

# Start PostgreSQL container
$postgresContainer = "smartalarm-test-postgres"
if (-not (Test-ContainerRunning $postgresContainer)) {
    Write-Host "🐘 Starting PostgreSQL container..." -ForegroundColor Yellow
    docker run -d --name $postgresContainer `
        -e POSTGRES_DB=smartalarm_test `
        -e POSTGRES_USER=postgres `
        -e POSTGRES_PASSWORD=postgres `
        -p 5432:5432 `
        postgres:15-alpine

    if (-not (Wait-ForContainer $postgresContainer "docker exec $postgresContainer pg_isready -U postgres")) {
        exit 1
    }
} else {
    Write-Host "✅ PostgreSQL container already running" -ForegroundColor Green
}

# Start Redis container
$redisContainer = "smartalarm-test-redis"
if (-not (Test-ContainerRunning $redisContainer)) {
    Write-Host "🔴 Starting Redis container..." -ForegroundColor Yellow
    docker run -d --name $redisContainer `
        -p 6379:6379 `
        redis:7-alpine

    if (-not (Wait-ForContainer $redisContainer "docker exec $redisContainer redis-cli ping")) {
        exit 1
    }
} else {
    Write-Host "✅ Redis container already running" -ForegroundColor Green
}

# Start MinIO container
$minioContainer = "smartalarm-test-minio"
if (-not (Test-ContainerRunning $minioContainer)) {
    Write-Host "🗄️ Starting MinIO container..." -ForegroundColor Yellow
    docker run -d --name $minioContainer `
        -e MINIO_ROOT_USER=minioadmin `
        -e MINIO_ROOT_PASSWORD=minioadmin `
        -p 9000:9000 `
        -p 9001:9001 `
        minio/minio server /data --console-address ":9001"

    if (-not (Wait-ForContainer $minioContainer "docker exec $minioContainer mc --version")) {
        exit 1
    }
} else {
    Write-Host "✅ MinIO container already running" -ForegroundColor Green
}

# Start RabbitMQ container
$rabbitContainer = "smartalarm-test-rabbitmq"
if (-not (Test-ContainerRunning $rabbitContainer)) {
    Write-Host "🐰 Starting RabbitMQ container..." -ForegroundColor Yellow
    docker run -d --name $rabbitContainer `
        -e RABBITMQ_DEFAULT_USER=guest `
        -e RABBITMQ_DEFAULT_PASS=guest `
        -p 5672:5672 `
        -p 15672:15672 `
        rabbitmq:3-management-alpine

    if (-not (Wait-ForContainer $rabbitContainer "docker exec $rabbitContainer rabbitmqctl status")) {
        exit 1
    }
} else {
    Write-Host "✅ RabbitMQ container already running" -ForegroundColor Green
}

# Start Vault container (for KeyVault tests)
$vaultContainer = "smartalarm-test-vault"
if (-not (Test-ContainerRunning $vaultContainer)) {
    Write-Host "🔐 Starting Vault container..." -ForegroundColor Yellow
    docker run -d --name $vaultContainer `
        -e VAULT_DEV_ROOT_TOKEN_ID=myroot `
        -e VAULT_DEV_LISTEN_ADDRESS=0.0.0.0:8200 `
        -p 8200:8200 `
        vault:latest

    if (-not (Wait-ForContainer $vaultContainer "docker exec $vaultContainer vault status")) {
        exit 1
    }
} else {
    Write-Host "✅ Vault container already running" -ForegroundColor Green
}

Write-Host ""
Write-Host "🎉 Integration test environment is ready!" -ForegroundColor Green
Write-Host ""
Write-Host "📋 Container Status:" -ForegroundColor Cyan
Write-Host "  PostgreSQL: localhost:5432 (postgres/postgres)" -ForegroundColor White
Write-Host "  Redis:      localhost:6379" -ForegroundColor White
Write-Host "  MinIO:      localhost:9000 (minioadmin/minioadmin)" -ForegroundColor White
Write-Host "  RabbitMQ:   localhost:5672 (guest/guest)" -ForegroundColor White
Write-Host "  Vault:      localhost:8200 (token: myroot)" -ForegroundColor White
Write-Host ""
Write-Host "🧪 Run integration tests with:" -ForegroundColor Cyan
Write-Host "  dotnet test tests/SmartAlarm.Infrastructure.Tests/ --filter Category=Integration" -ForegroundColor White
Write-Host ""
Write-Host "🛑 To stop containers:" -ForegroundColor Cyan
Write-Host "  .\cleanup-integration-tests.ps1" -ForegroundColor White
