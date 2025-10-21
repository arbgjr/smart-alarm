# Smart Alarm - Critical Tests Runner (Build-First Strategy) - PowerShell Version
# This script runs only critical/essential tests after successful build
# Author: Smart Alarm Team
# Usage: .\scripts\run-critical-tests.ps1 [-Verbose] [-NoBuild]

param(
    [switch]$Verbose = $false,
    [switch]$NoBuild = $false
)

# Configuration
$TestLogFile = "critical-tests-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"
$TestStartTime = Get-Date
$FailedTests = @()

# Function to write colored output
function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )

    Write-Host $Message -ForegroundColor $Color

    if ($Verbose) {
        "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') - $Message" | Add-Content -Path $TestLogFile
    }
}

# Function to run test command with logging
function Invoke-TestCommand {
    param(
        [string]$Description,
        [string]$Command,
        [string[]]$Arguments = @(),
        [bool]$IsCritical = $true
    )

    Write-ColorOutput "🧪 $Description" "Blue"

    try {
        if ($Verbose) {
            "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') - Executing: $Command $($Arguments -join ' ')" | Add-Content -Path $TestLogFile

            if ($Arguments.Count -gt 0) {
                & $Command @Arguments 2>&1 | Tee-Object -FilePath $TestLogFile -Append
            } else {
                & $Command 2>&1 | Tee-Object -FilePath $TestLogFile -Append
            }
        } else {
            if ($Arguments.Count -gt 0) {
                & $Command @Arguments 2>&1 | Out-Null
            } else {
                & $Command 2>&1 | Out-Null
            }
        }

        if ($LASTEXITCODE -eq 0) {
            Write-ColorOutput "  ✅ $Description passed" "Green"
            return $true
        } else {
            Write-ColorOutput "  ❌ $Description failed (exit code: $LASTEXITCODE)" "Red"

            if ($IsCritical) {
                $script:FailedTests += $Description
            }

            if (-not $Verbose) {
                Write-ColorOutput "  💡 Run with -Verbose flag for detailed logs" "Yellow"
            }
            return $false
        }
    }
    catch {
        Write-ColorOutput "  ❌ $Description failed with exception: $($_.Exception.Message)" "Red"

        if ($IsCritical) {
            $script:FailedTests += $Description
        }

        return $false
    }
}

# Function to ensure build is completed first
function Confirm-BuildCompleted {
    if (-not $NoBuild) {
        Write-ColorOutput "🔨 Ensuring full build is completed first..." "Blue"

        if (Test-Path "scripts/full-build.ps1") {
            $buildArgs = @()
            if ($Verbose) {
                $buildArgs += "-Verbose"
            }

            try {
                if ($buildArgs.Count -gt 0) {
                    & ".\scripts\full-build.ps1" @buildArgs
                } else {
                    & ".\scripts\full-build.ps1"
                }

                if ($LASTEXITCODE -ne 0) {
                    Write-ColorOutput "❌ Full build failed - cannot proceed with tests" "Red"
                    exit 1
                }

                Write-ColorOutput "✅ Full build completed - proceeding with critical tests" "Green"
            }
            catch {
                Write-ColorOutput "❌ Full build failed with exception: $($_.Exception.Message)" "Red"
                exit 1
            }
        } else {
            Write-ColorOutput "⚠️  Full build script not found - assuming build is already completed" "Yellow"
        }
    } else {
        Write-ColorOutput "⚠️  Skipping build validation (-NoBuild flag used)" "Yellow"
    }
}

# Function to run critical backend tests
function Invoke-CriticalBackendTests {
    Write-ColorOutput "🏗️  Running Critical Backend Tests" "Cyan"

    # Test 1: Authentication and JWT Tests
    if (Test-Path "tests/SmartAlarm.Api.Tests/SmartAlarm.Api.Tests.csproj") {
        Invoke-TestCommand "Authentication JWT Tests" "dotnet" @(
            "test", "tests/SmartAlarm.Api.Tests/SmartAlarm.Api.Tests.csproj",
            "--filter", "Category=Authentication",
            "--configuration", "Release",
            "--no-build",
            "--logger", "console;verbosity=minimal"
        ) $true
    }

    # Test 2: Alarm CRUD Operations
    if (Test-Path "tests/SmartAlarm.Application.Tests/SmartAlarm.Application.Tests.csproj") {
        Invoke-TestCommand "Alarm CRUD Tests" "dotnet" @(
            "test", "tests/SmartAlarm.Application.Tests/SmartAlarm.Application.Tests.csproj",
            "--filter", "Category=AlarmCRUD",
            "--configuration", "Release",
            "--no-build",
            "--logger", "console;verbosity=minimal"
        ) $true
    }

    # Test 3: Domain Model Validation
    if (Test-Path "tests/SmartAlarm.Domain.Tests/SmartAlarm.Domain.Tests.csproj") {
        Invoke-TestCommand "Domain Model Tests" "dotnet" @(
            "test", "tests/SmartAlarm.Domain.Tests/SmartAlarm.Domain.Tests.csproj",
            "--filter", "Category=Critical",
            "--configuration", "Release",
            "--no-build",
            "--logger", "console;verbosity=minimal"
        ) $true
    }

    # Test 4: Infrastructure Core Tests
    if (Test-Path "tests/SmartAlarm.Infrastructure.Tests/SmartAlarm.Infrastructure.Tests.csproj") {
        Invoke-TestCommand "Infrastructure Core Tests" "dotnet" @(
            "test", "tests/SmartAlarm.Infrastructure.Tests/SmartAlarm.Infrastructure.Tests.csproj",
            "--filter", "Category=Core",
            "--configuration", "Release",
            "--no-build",
            "--logger", "console;verbosity=minimal"
        ) $true
    }

    # Test 5: Basic API Endpoints
    if (Test-Path "tests/SmartAlarm.Api.Tests/SmartAlarm.Api.Tests.csproj") {
        Invoke-TestCommand "Basic API Endpoint Tests" "dotnet" @(
            "test", "tests/SmartAlarm.Api.Tests/SmartAlarm.Api.Tests.csproj",
            "--filter", "Category=BasicEndpoints",
            "--configuration", "Release",
            "--no-build",
            "--logger", "console;verbosity=minimal"
        ) $true
    }
}

# Function to run critical integration tests
function Invoke-CriticalIntegrationTests {
    Write-ColorOutput "🔗 Running Critical Integration Tests" "Cyan"

    # Test 1: Database Integration (Essential)
    if (Test-Path "tests/SmartAlarm.IntegrationTests/SmartAlarm.IntegrationTests.csproj") {
        Invoke-TestCommand "Database Integration Tests" "dotnet" @(
            "test", "tests/SmartAlarm.IntegrationTests/SmartAlarm.IntegrationTests.csproj",
            "--filter", "Category=Database",
            "--configuration", "Release",
            "--no-build",
            "--logger", "console;verbosity=minimal"
        ) $true

        # Test 2: Authentication Flow Integration
        Invoke-TestCommand "Authentication Flow Integration" "dotnet" @(
            "test", "tests/SmartAlarm.IntegrationTests/SmartAlarm.IntegrationTests.csproj",
            "--filter", "Category=AuthFlow",
            "--configuration", "Release",
            "--no-build",
            "--logger", "console;verbosity=minimal"
        ) $true

        # Test 3: Alarm Processing Integration
        Invoke-TestCommand "Alarm Processing Integration" "dotnet" @(
            "test", "tests/SmartAlarm.IntegrationTests/SmartAlarm.IntegrationTests.csproj",
            "--filter", "Category=AlarmProcessing",
            "--configuration", "Release",
            "--no-build",
            "--logger", "console;verbosity=minimal"
        ) $true
    }
}

# Function to run critical frontend tests
function Invoke-CriticalFrontendTests {
    Write-ColorOutput "🎨 Running Critical Frontend Tests" "Cyan"

    $frontendPath = "frontend"

    if (-not (Test-Path $frontendPath)) {
        Write-ColorOutput "⚠️  Frontend directory not found: $frontendPath - skipping frontend tests" "Yellow"
        return
    }

    Push-Location $frontendPath

    try {
        # Test 1: TypeScript Compilation
        Invoke-TestCommand "TypeScript Compilation Check" "npm" @("run", "type-check") $true

        # Test 2: Critical Component Tests
        Invoke-TestCommand "Critical Component Tests" "npm" @(
            "run", "test", "--", "--run", "--reporter=verbose", "--testNamePattern=(Login|Alarm|Dashboard)"
        ) $true

        # Test 3: Build Validation
        Invoke-TestCommand "Frontend Build Validation" "npm" @("run", "build") $true
    }
    finally {
        Pop-Location
    }
}

# Function to run smoke tests
function Invoke-SmokeTests {
    Write-ColorOutput "💨 Running Smoke Tests" "Cyan"

    # Test 1: Application Startup
    if (Test-Path "tests/SmartAlarm.BasicTests/SmartAlarm.BasicTests.csproj") {
        Invoke-TestCommand "Application Startup Test" "dotnet" @(
            "test", "tests/SmartAlarm.BasicTests/SmartAlarm.BasicTests.csproj",
            "--filter", "Category=Smoke",
            "--configuration", "Release",
            "--no-build",
            "--logger", "console;verbosity=minimal"
        ) $true
    }

    # Test 2: Health Check Endpoints
    if (Test-Path "tests/SmartAlarm.Api.Tests/SmartAlarm.Api.Tests.csproj") {
        Invoke-TestCommand "Health Check Tests" "dotnet" @(
            "test", "tests/SmartAlarm.Api.Tests/SmartAlarm.Api.Tests.csproj",
            "--filter", "Category=HealthCheck",
            "--configuration", "Release",
            "--no-build",
            "--logger", "console;verbosity=minimal"
        ) $false
    }
}

# Function to validate test environment
function Test-TestEnvironment {
    Write-ColorOutput "🔍 Validating Test Environment" "Blue"

    # Check if .NET test runner is available
    try {
        $dotnetVersion = & dotnet --version 2>$null
        Write-ColorOutput "  ✅ .NET SDK available: $dotnetVersion" "Green"
    }
    catch {
        Write-ColorOutput "❌ .NET SDK not found" "Red"
        exit 1
    }

    # Check if npm is available for frontend tests
    if ((Test-Path "frontend") -and -not (Get-Command npm -ErrorAction SilentlyContinue)) {
        Write-ColorOutput "❌ npm not found (required for frontend tests)" "Red"
        exit 1
    }

    # Check if test projects exist
    $testProjects = @(
        "tests/SmartAlarm.Api.Tests/SmartAlarm.Api.Tests.csproj",
        "tests/SmartAlarm.Application.Tests/SmartAlarm.Application.Tests.csproj",
        "tests/SmartAlarm.Domain.Tests/SmartAlarm.Domain.Tests.csproj",
        "tests/SmartAlarm.Infrastructure.Tests/SmartAlarm.Infrastructure.Tests.csproj"
    )

    $missingProjects = 0
    foreach ($project in $testProjects) {
        if (Test-Path $project) {
            $projectName = Split-Path $project -Leaf
            Write-ColorOutput "  ✅ Test project found: $projectName" "Green"
        } else {
            Write-ColorOutput "  ⚠️  Test project missing: $project" "Yellow"
            $missingProjects++
        }
    }

    if ($missingProjects -gt 0) {
        Write-ColorOutput "⚠️  Some test projects are missing - tests may be limited" "Yellow"
    }
}

# Function to generate test report
function Write-TestReport {
    $testEndTime = Get-Date
    $testDuration = ($testEndTime - $TestStartTime).TotalSeconds

    Write-ColorOutput "📊 Critical Tests Report" "Blue"
    Write-ColorOutput "========================" "Blue"
    Write-ColorOutput "Test Duration: $([math]::Round($testDuration, 2))s" "Yellow"
    Write-ColorOutput "Test Log: $TestLogFile" "Yellow"
    Write-ColorOutput "Timestamp: $(Get-Date)" "Yellow"

    if ($FailedTests.Count -eq 0) {
        Write-ColorOutput "✅ All critical tests passed!" "Green"
        Write-ColorOutput "🚀 Ready to proceed to next phase" "Green"
    } else {
        Write-ColorOutput "❌ $($FailedTests.Count) critical test(s) failed:" "Red"
        foreach ($test in $FailedTests) {
            Write-ColorOutput "  - $test" "Red"
        }
        Write-ColorOutput "🛑 Cannot proceed until critical tests pass" "Red"
    }

    if ($Verbose) {
        Write-ColorOutput "Detailed logs available in: $TestLogFile" "Yellow"
    }
}

# Main execution
function Main {
    Write-ColorOutput "🧪 Smart Alarm - Critical Tests Runner Started" "Green"
    Write-ColorOutput "===============================================" "Blue"

    if ($Verbose) {
        Write-ColorOutput "Verbose mode enabled - detailed logs will be saved to: $TestLogFile" "Yellow"
    }

    # Execute test phases
    Test-TestEnvironment
    Confirm-BuildCompleted

    # Run critical tests in order of importance
    Invoke-SmokeTests
    Invoke-CriticalBackendTests
    Invoke-CriticalIntegrationTests
    Invoke-CriticalFrontendTests

    Write-TestReport

    # Exit with error if critical tests failed
    if ($FailedTests.Count -gt 0) {
        Write-ColorOutput "❌ Critical tests failed - blocking progression" "Red"
        exit 1
    }

    Write-ColorOutput "🎉 All critical tests passed successfully!" "Green"
    Write-ColorOutput "✅ Ready to proceed with development activities" "Blue"

    return 0
}

# Error handling
trap {
    Write-ColorOutput "❌ Critical tests failed unexpectedly: $($_.Exception.Message)" "Red"
    exit 1
}

# Execute main function
try {
    Main
}
catch {
    Write-ColorOutput "❌ Test process failed: $($_.Exception.Message)" "Red"
    exit 1
}
