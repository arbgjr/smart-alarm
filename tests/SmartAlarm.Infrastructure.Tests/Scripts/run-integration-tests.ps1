#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Runs SmartAlarm integration tests with TestContainers
.DESCRIPTION
    This script runs integration tests in the correct order and provides
    detailed reporting on test results.
#>

param(
    [string]$Filter = "Category=Integration",
    [string]$Configuration = "Debug",
    [switch]$Coverage,
    [switch]$Parallel,
    [switch]$Verbose,
    [string]$Logger = "console;verbosity=normal"
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

function Run-TestSuite {
    param(
        [string]$SuiteName,
        [string]$TestFilter,
        [string]$Description
    )

    Write-Info "Running $SuiteName tests: $Description"

    $testArgs = @(
        "test"
        "tests/SmartAlarm.Infrastructure.Tests"
        "--configuration", $Configuration
        "--filter", $TestFilter
        "--logger", $Logger
        "--no-build"
    )

    if ($Verbose) {
        $testArgs += "--verbosity", "detailed"
    }

    if (-not $Parallel) {
        $testArgs += "--parallel", "none"
    }

    if ($Coverage) {
        $testArgs += "--collect", "XPlat Code Coverage"
    }

    try {
        & dotnet @testArgs
        if ($LASTEXITCODE -eq 0) {
            Write-Success "$SuiteName tests passed"
            return $true
        }
        else {
            Write-Error "$SuiteName tests failed"
            return $false
        }
    }
    catch {
        Write-Error "Failed to run $SuiteName tests: $_"
        return $false
    }
}

# Main execution
Write-Info "Starting SmartAlarm integration test run..."
Write-Info "Configuration: $Configuration"
Write-Info "Filter: $Filter"

# Build the test project first
Write-Info "Building test project..."
try {
    dotnet build tests/SmartAlarm.Infrastructure.Tests --configuration $Configuration --verbosity minimal
    Write-Success "Build completed successfully"
}
catch {
    Write-Error "Build failed"
    exit 1
}

$testResults = @()

# Run different test suites based on filter
switch -Regex ($Filter) {
    "Category=Integration" {
        Write-Info "Running all integration tests..."

        # 1. Basic integration tests (fast)
        $result1 = Run-TestSuite -SuiteName "Basic Integration" -TestFilter "Category=Integration&Category!=TestContainers&Category!=WebAPI" -Description "Basic integration tests without containers"
        $testResults += @{ Name = "Basic Integration"; Passed = $result1 }

        # 2. TestContainer tests (slower)
        $result2 = Run-TestSuite -SuiteName "TestContainers" -TestFilter "Category=TestContainers" -Description "Tests using TestContainers"
        $testResults += @{ Name = "TestContainers"; Passed = $result2 }

        # 3. Web API tests (slowest)
        $result3 = Run-TestSuite -SuiteName "Web API" -TestFilter "Category=WebAPI" -Description "Full web application tests"
        $testResults += @{ Name = "Web API"; Passed = $result3 }
    }

    "Category=TestContainers" {
        Write-Info "Running TestContainer tests only..."
        $result = Run-TestSuite -SuiteName "TestContainers" -TestFilter $Filter -Description "TestContainer integration tests"
        $testResults += @{ Name = "TestContainers"; Passed = $result }
    }

    "Category=WebAPI" {
        Write-Info "Running Web API tests only..."
        $result = Run-TestSuite -SuiteName "Web API" -TestFilter $Filter -Description "Web API integration tests"
        $testResults += @{ Name = "Web API"; Passed = $result }
    }

    default {
        Write-Info "Running custom filter tests..."
        $result = Run-TestSuite -SuiteName "Custom" -TestFilter $Filter -Description "Custom filtered tests"
        $testResults += @{ Name = "Custom"; Passed = $result }
    }
}

# Report results
Write-Info ""
Write-Info "Test Results Summary:"
Write-Info "===================="

$allPassed = $true
foreach ($result in $testResults) {
    $status = if ($result.Passed) { "✅ PASSED" } else { "❌ FAILED" }
    Write-Host "$($result.Name): $status"
    if (-not $result.Passed) {
        $allPassed = $false
    }
}

Write-Info ""
if ($allPassed) {
    Write-Success "All test suites passed! 🎉"
    exit 0
}
else {
    Write-Error "Some test suites failed. Check the output above for details."
    exit 1
}
