#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Runs SmartAlarm E2E tests with Playwright
.DESCRIPTION
    This script sets up the environment and runs E2E tests with proper reporting
#>

param(
    [string]$Project = "chromium",
    [string]$BaseUrl = "http://localhost:5000",
    [switch]$Headed,
    [switch]$Debug,
    [switch]$UI,
    [switch]$UpdateSnapshots,
    [string]$Grep = "",
    [switch]$Parallel = $true,
    [switch]$InstallDeps,
    [switch]$StartApp
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

function Test-ApplicationRunning {
    param([string]$Url)

    try {
        $response = Invoke-WebRequest -Uri "$Url/health" -Method GET -TimeoutSec 5 -UseBasicParsing
        return $response.StatusCode -eq 200
    }
    catch {
        return $false
    }
}

function Start-Application {
    Write-Info "Starting SmartAlarm application..."

    $appPath = "../../src/SmartAlarm.Api"
    if (-not (Test-Path "$appPath/SmartAlarm.Api.csproj")) {
        Write-Error "Application project not found at $appPath"
        return $false
    }

    # Start application in background
    $job = Start-Job -ScriptBlock {
        param($Path, $Url)
        Set-Location $Path
        $env:ASPNETCORE_URLS = $Url
        dotnet run --configuration Release
    } -ArgumentList (Resolve-Path $appPath), $BaseUrl

    # Wait for application to start
    Write-Info "Waiting for application to start at $BaseUrl..."
    $timeout = 60
    $elapsed = 0

    while ($elapsed -lt $timeout) {
        if (Test-ApplicationRunning -Url $BaseUrl) {
            Write-Success "Application is running at $BaseUrl"
            return $job
        }

        Start-Sleep -Seconds 2
        $elapsed += 2
        Write-Host "." -NoNewline
    }

    Write-Error "Application failed to start within $timeout seconds"
    Stop-Job $job -Force
    Remove-Job $job -Force
    return $null
}

function Install-Dependencies {
    Write-Info "Installing Playwright dependencies..."

    try {
        npm install
        npx playwright install

        if ($InstallDeps) {
            npx playwright install-deps
        }

        Write-Success "Dependencies installed successfully"
        return $true
    }
    catch {
        Write-Error "Failed to install dependencies: $_"
        return $false
    }
}

function Run-PlaywrightTests {
    Write-Info "Running Playwright E2E tests..."

    $args = @("npx", "playwright", "test")

    # Add project filter
    if ($Project -ne "all") {
        $args += "--project", $Project
    }

    # Add UI mode
    if ($UI) {
        $args += "--ui"
    }
    elseif ($Debug) {
        $args += "--debug"
    }
    elseif ($Headed) {
        $args += "--headed"
    }

    # Add grep filter
    if ($Grep) {
        $args += "--grep", $Grep
    }

    # Add parallel execution
    if (-not $Parallel) {
        $args += "--workers", "1"
    }

    # Update snapshots
    if ($UpdateSnapshots) {
        $args += "--update-snapshots"
    }

    # Set base URL
    $env:BASE_URL = $BaseUrl

    try {
        & $args[0] $args[1..($args.Length-1)]

        if ($LASTEXITCODE -eq 0) {
            Write-Success "E2E tests completed successfully"
            return $true
        }
        else {
            Write-Error "E2E tests failed"
            return $false
        }
    }
    catch {
        Write-Error "Failed to run E2E tests: $_"
        return $false
    }
}

function Show-TestReport {
    Write-Info "Opening test report..."

    try {
        npx playwright show-report
    }
    catch {
        Write-Warning "Could not open test report automatically"
        Write-Info "You can view the report by running: npx playwright show-report"
    }
}

# Main execution
Write-Info "Starting SmartAlarm E2E test run..."
Write-Info "Project: $Project"
Write-Info "Base URL: $BaseUrl"

# Change to E2E test directory
$originalLocation = Get-Location
Set-Location $PSScriptRoot

try {
    # Install dependencies if needed
    if (-not (Test-Path "node_modules") -or $InstallDeps) {
        if (-not (Install-Dependencies)) {
            exit 1
        }
    }

    # Check if application is running
    $appJob = $null
    if (-not (Test-ApplicationRunning -Url $BaseUrl)) {
        if ($StartApp) {
            $appJob = Start-Application
            if (-not $appJob) {
                exit 1
            }
        }
        else {
            Write-Warning "Application is not running at $BaseUrl"
            Write-Info "Start the application manually or use -StartApp flag"
            Write-Info "To start manually: dotnet run --project ../../src/SmartAlarm.Api"
        }
    }
    else {
        Write-Success "Application is already running at $BaseUrl"
    }

    # Run tests
    $testResult = Run-PlaywrightTests

    # Show report if tests completed (even if some failed)
    if (-not $UI -and -not $Debug) {
        Show-TestReport
    }

    # Cleanup
    if ($appJob) {
        Write-Info "Stopping application..."
        Stop-Job $appJob -Force
        Remove-Job $appJob -Force
    }

    if ($testResult) {
        Write-Success "E2E test run completed successfully! 🎉"
        exit 0
    }
    else {
        Write-Error "E2E test run failed. Check the report for details."
        exit 1
    }
}
finally {
    Set-Location $originalLocation
}
