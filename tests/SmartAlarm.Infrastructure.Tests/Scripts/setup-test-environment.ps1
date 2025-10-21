#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Sets up the test environment for SmartAlarm integration tests
.DESCRIPTION
    This script ensures Docker is running and pulls required TestContainer images
    for integration testing with PostgreSQL, Redis, MinIO, and RabbitMQ.
#>

param(
    [switch]$SkipDockerCheck,
    [switch]$PullImages = $true,
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ️  $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠️  $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor Red
}

function Test-DockerRunning {
    try {
        $null = docker version 2>$null
        return $true
    }
    catch {
        return $false
    }
}

function Pull-TestContainerImages {
    $images = @(
        "postgres:15-alpine",
        "redis:7-alpine",
        "minio/minio:latest",
        "rabbitmq:3-management-alpine"
    )

    Write-Info "Pulling TestContainer images..."

    foreach ($image in $images) {
        Write-Info "Pulling $image..."
        try {
            docker pull $image
            Write-Success "Successfully pulled $image"
        }
        catch {
            Write-Warning "Failed to pull $image - will be pulled automatically during tests"
        }
    }
}

function Test-DotNetVersion {
    try {
        $version = dotnet --version
        Write-Success ".NET version: $version"
        return $true
    }
    catch {
        Write-Error ".NET SDK not found. Please install .NET 8 SDK."
        return $false
    }
}

function Clean-TestContainers {
    Write-Info "Cleaning up any existing test containers..."

    try {
        # Stop and remove containers with smartalarm test labels
        $containers = docker ps -a --filter "label=org.testcontainers=true" --format "{{.ID}}" 2>$null
        if ($containers) {
            docker rm -f $containers 2>$null
            Write-Success "Cleaned up existing test containers"
        }
        else {
            Write-Info "No existing test containers found"
        }
    }
    catch {
        Write-Warning "Could not clean up containers (this is usually fine)"
    }
}

function Test-IntegrationTestsConfiguration {
    Write-Info "Validating integration test configuration..."

    $testProject = "tests/SmartAlarm.Infrastructure.Tests/SmartAlarm.Infrastructure.Tests.csproj"

    if (-not (Test-Path $testProject)) {
        Write-Error "Test project not found at $testProject"
        return $false
    }

    # Check if TestContainers packages are referenced
    $projectContent = Get-Content $testProject -Raw

    $requiredPackages = @(
        "Testcontainers.PostgreSql",
        "Testcontainers.Redis",
        "Testcontainers.Minio",
        "Testcontainers.RabbitMq"
    )

    $missingPackages = @()
    foreach ($package in $requiredPackages) {
        if ($projectContent -notmatch $package) {
            $missingPackages += $package
        }
    }

    if ($missingPackages.Count -gt 0) {
        Write-Warning "Missing TestContainer packages: $($missingPackages -join ', ')"
        Write-Info "Run 'dotnet add package <PackageName>' to add missing packages"
        return $false
    }

    Write-Success "All required TestContainer packages are referenced"
    return $true
}

# Main execution
Write-Info "Setting up SmartAlarm integration test environment..."

# Check .NET SDK
if (-not (Test-DotNetVersion)) {
    exit 1
}

# Check Docker (unless skipped)
if (-not $SkipDockerCheck) {
    if (-not (Test-DockerRunning)) {
        Write-Error "Docker is not running. Please start Docker Desktop or Docker daemon."
        Write-Info "You can skip this check with -SkipDockerCheck if running in CI/CD"
        exit 1
    }
    Write-Success "Docker is running"
}

# Clean up existing containers
Clean-TestContainers

# Pull images if requested
if ($PullImages -and -not $SkipDockerCheck) {
    Pull-TestContainerImages
}

# Validate test configuration
if (-not (Test-IntegrationTestsConfiguration)) {
    Write-Warning "Test configuration validation failed - tests may not work correctly"
}

# Build test project
Write-Info "Building test project..."
try {
    dotnet build tests/SmartAlarm.Infrastructure.Tests --configuration Debug --verbosity minimal
    Write-Success "Test project built successfully"
}
catch {
    Write-Error "Failed to build test project"
    exit 1
}

Write-Success "Test environment setup complete!"
Write-Info ""
Write-Info "You can now run integration tests with:"
Write-Info "  dotnet test tests/SmartAlarm.Infrastructure.Tests --filter Category=Integration"
Write-Info ""
Write-Info "Or run specific TestContainer tests with:"
Write-Info "  dotnet test tests/SmartAlarm.Infrastructure.Tests --filter Category=TestContainers"
