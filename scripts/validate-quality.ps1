# Smart Alarm - Quality Validation Script (Build + Critical Tests) - PowerShell Version
# This script combines full build + critical tests as a single quality gate
# Author: Smart Alarm Team
# Usage: .\scripts\validate-quality.ps1 [-Verbose] [-SkipBuild] [-SkipTests]

param(
    [switch]$Verbose = $false,
    [switch]$SkipBuild = $false,
    [switch]$SkipTests = $false,
    [switch]$Help = $false
)

# Configuration
$QualityLogFile = "quality-validation-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"
$ValidationStartTime = Get-Date

# Function to write colored output
function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )

    Write-Host $Message -ForegroundColor $Color

    if ($Verbose) {
        "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') - $Message" | Add-Content -Path $QualityLogFile
    }
}

# Function to show usage help
function Show-Help {
    Write-Host "Smart Alarm - Quality Validation Script" -ForegroundColor Green
    Write-Host "=======================================" -ForegroundColor Blue
    Write-Host ""
    Write-Host "This script runs a complete quality validation combining:" -ForegroundColor White
    Write-Host "  1. Full build of all components" -ForegroundColor White
    Write-Host "  2. Critical tests execution" -ForegroundColor White
    Write-Host "  3. Additional quality checks" -ForegroundColor White
    Write-Host ""
    Write-Host "Usage: .\scripts\validate-quality.ps1 [options]" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Options:" -ForegroundColor Cyan
    Write-Host "  -Verbose      Enable verbose output and detailed logging" -ForegroundColor White
    Write-Host "  -SkipBuild    Skip the build quality gate" -ForegroundColor White
    Write-Host "  -SkipTests    Skip the tests quality gate" -ForegroundColor White
    Write-Host "  -Help         Show this help message" -ForegroundColor White
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Cyan
    Write-Host "  .\scripts\validate-quality.ps1                    # Run full quality validation" -ForegroundColor White
    Write-Host "  .\scripts\validate-quality.ps1 -Verbose           # Run with detailed logging" -ForegroundColor White
    Write-Host "  .\scripts\validate-quality.ps1 -SkipBuild         # Run only tests (assume build is done)" -ForegroundColor White
    Write-Host "  .\scripts\validate-quality.ps1 -SkipTests         # Run only build validation" -ForegroundColor White
    Write-Host ""
    Write-Host "Quality Gates:" -ForegroundColor Cyan
    Write-Host "  1. Build Gate: Compiles backend, frontend, and microservices" -ForegroundColor White
    Write-Host "  2. Tests Gate: Runs critical tests for core functionality" -ForegroundColor White
    Write-Host "  3. Additional: Validates artifacts and checks for warnings" -ForegroundColor White
    Write-Host ""
}

# Function to run quality gate step
function Invoke-QualityStep {
    param(
        [string]$StepName,
        [string]$ScriptPath,
        [bool]$IsCritical = $true
    )

    Write-ColorOutput "🔍 Quality Gate: $StepName" "Blue"

    if (-not (Test-Path $ScriptPath)) {
        Write-ColorOutput "❌ Script not found: $ScriptPath" "Red"
        if ($IsCritical) {
            exit 1
        } else {
            return $false
        }
    }

    # Run the script
    $scriptArgs = @()
    if ($Verbose) {
        $scriptArgs += "-Verbose"
    }

    try {
        if ($Verbose) {
            "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') - Executing: $ScriptPath $($scriptArgs -join ' ')" | Add-Content -Path $QualityLogFile

            if ($scriptArgs.Count -gt 0) {
                & $ScriptPath @scriptArgs 2>&1 | Tee-Object -FilePath $QualityLogFile -Append
            } else {
                & $ScriptPath 2>&1 | Tee-Object -FilePath $QualityLogFile -Append
            }
        } else {
            if ($scriptArgs.Count -gt 0) {
                & $ScriptPath @scriptArgs 2>&1 | Out-Null
            } else {
                & $ScriptPath 2>&1 | Out-Null
            }
        }

        if ($LASTEXITCODE -eq 0) {
            Write-ColorOutput "  ✅ $StepName passed" "Green"
            return $true
        } else {
            Write-ColorOutput "  ❌ $StepName failed (exit code: $LASTEXITCODE)" "Red"

            if ($IsCritical) {
                Write-ColorOutput "🛑 Critical quality gate failed - stopping validation" "Red"
                exit $LASTEXITCODE
            }

            return $false
        }
    }
    catch {
        Write-ColorOutput "  ❌ $StepName failed with exception: $($_.Exception.Message)" "Red"

        if ($IsCritical) {
            Write-ColorOutput "🛑 Critical quality gate failed - stopping validation" "Red"
            exit 1
        }

        return $false
    }
}

# Function to check prerequisites
function Test-Prerequisites {
    Write-ColorOutput "🔍 Checking Quality Validation Prerequisites" "Blue"

    # Check if we're in the right directory
    if (-not (Test-Path "SmartAlarm.sln")) {
        Write-ColorOutput "❌ SmartAlarm.sln not found. Please run from project root directory." "Red"
        exit 1
    }

    # Check required scripts
    $requiredScripts = @(
        "scripts/full-build.ps1",
        "scripts/run-critical-tests.ps1"
    )

    $missingScripts = 0
    foreach ($script in $requiredScripts) {
        if (Test-Path $script) {
            Write-ColorOutput "  ✅ Required script found: $script" "Green"
        } else {
            Write-ColorOutput "  ❌ Required script missing: $script" "Red"
            $missingScripts++
        }
    }

    if ($missingScripts -gt 0) {
        Write-ColorOutput "❌ Missing required scripts - cannot proceed" "Red"
        exit 1
    }

    Write-ColorOutput "✅ All prerequisites satisfied" "Green"
}

# Function to run full build quality gate
function Invoke-BuildQualityGate {
    if ($SkipBuild) {
        Write-ColorOutput "⚠️  Skipping build quality gate (-SkipBuild flag)" "Yellow"
        return $true
    }

    Write-ColorOutput "🏗️  Build Quality Gate" "Cyan"
    Write-ColorOutput "=====================" "Blue"

    return (Invoke-QualityStep "Full Build Validation" "scripts/full-build.ps1" $true)
}

# Function to run tests quality gate
function Invoke-TestsQualityGate {
    if ($SkipTests) {
        Write-ColorOutput "⚠️  Skipping tests quality gate (-SkipTests flag)" "Yellow"
        return $true
    }

    Write-ColorOutput "🧪 Tests Quality Gate" "Cyan"
    Write-ColorOutput "====================" "Blue"

    # Run critical tests with -NoBuild since we already built
    $testArgs = @("-NoBuild")
    if ($Verbose) {
        $testArgs += "-Verbose"
    }

    try {
        if ($Verbose) {
            "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') - Executing: scripts/run-critical-tests.ps1 $($testArgs -join ' ')" | Add-Content -Path $QualityLogFile
            & "scripts/run-critical-tests.ps1" @testArgs 2>&1 | Tee-Object -FilePath $QualityLogFile -Append
        } else {
            & "scripts/run-critical-tests.ps1" @testArgs 2>&1 | Out-Null
        }

        if ($LASTEXITCODE -eq 0) {
            Write-ColorOutput "✅ Tests quality gate passed" "Green"
            return $true
        } else {
            Write-ColorOutput "❌ Tests quality gate failed" "Red"
            Write-ColorOutput "🛑 Critical tests must pass before proceeding" "Red"
            exit $LASTEXITCODE
        }
    }
    catch {
        Write-ColorOutput "❌ Tests quality gate failed with exception: $($_.Exception.Message)" "Red"
        exit 1
    }
}

# Function to run additional quality checks
function Invoke-AdditionalQualityChecks {
    Write-ColorOutput "🔍 Additional Quality Checks" "Cyan"
    Write-ColorOutput "============================" "Blue"

    # Check 1: Verify build artifacts exist
    Write-ColorOutput "Checking build artifacts..." "Blue"
    $criticalArtifacts = @(
        "src/SmartAlarm.Api/bin/Release/net8.0/SmartAlarm.Api.dll",
        "frontend/dist/index.html"
    )

    $missingArtifacts = 0
    foreach ($artifact in $criticalArtifacts) {
        if (Test-Path $artifact) {
            $artifactName = Split-Path $artifact -Leaf
            Write-ColorOutput "  ✅ Build artifact found: $artifactName" "Green"
        } else {
            Write-ColorOutput "  ⚠️  Build artifact missing: $artifact" "Yellow"
            $missingArtifacts++
        }
    }

    # Check 2: Verify no compilation warnings in critical files
    Write-ColorOutput "Checking for critical compilation warnings..." "Blue"
    try {
        $buildOutput = & dotnet build SmartAlarm.sln --verbosity quiet 2>&1
        $warningCount = ($buildOutput | Select-String "warning").Count

        if ($warningCount -eq 0) {
            Write-ColorOutput "  ✅ No compilation warnings found" "Green"
        } else {
            Write-ColorOutput "  ⚠️  Found $warningCount compilation warnings" "Yellow"
        }
    }
    catch {
        Write-ColorOutput "  ⚠️  Could not check compilation warnings" "Yellow"
    }

    # Check 3: Verify test coverage (if available)
    if (Test-Path "tests/TestCoverageReport/index.html") {
        Write-ColorOutput "  ✅ Test coverage report available" "Green"
    } else {
        Write-ColorOutput "  ⚠️  Test coverage report not found (optional)" "Yellow"
    }

    Write-ColorOutput "✅ Additional quality checks completed" "Green"
}

# Function to generate quality report
function Write-QualityReport {
    $validationEndTime = Get-Date
    $validationDuration = ($validationEndTime - $ValidationStartTime).TotalSeconds

    Write-ColorOutput "📊 Quality Validation Report" "Blue"
    Write-ColorOutput "============================" "Blue"
    Write-ColorOutput "Validation Duration: $([math]::Round($validationDuration, 2))s" "Yellow"
    Write-ColorOutput "Quality Log: $QualityLogFile" "Yellow"
    Write-ColorOutput "Timestamp: $(Get-Date)" "Yellow"

    # Quality gates summary
    Write-ColorOutput "Quality Gates Summary:" "Cyan"
    if (-not $SkipBuild) {
        Write-ColorOutput "  ✅ Build Quality Gate: PASSED" "Green"
    } else {
        Write-ColorOutput "  ⚠️  Build Quality Gate: SKIPPED" "Yellow"
    }

    if (-not $SkipTests) {
        Write-ColorOutput "  ✅ Tests Quality Gate: PASSED" "Green"
    } else {
        Write-ColorOutput "  ⚠️  Tests Quality Gate: SKIPPED" "Yellow"
    }

    Write-ColorOutput "  ✅ Additional Checks: COMPLETED" "Green"

    # Next steps
    Write-ColorOutput "Next Steps:" "Cyan"
    Write-ColorOutput "  - All quality gates have passed" "Blue"
    Write-ColorOutput "  - System is ready for development activities" "Blue"
    Write-ColorOutput "  - You can proceed with feature implementation" "Blue"

    if ($Verbose) {
        Write-ColorOutput "Detailed logs available in: $QualityLogFile" "Yellow"
    }
}

# Main execution
function Main {
    # Check for help flag
    if ($Help) {
        Show-Help
        exit 0
    }

    Write-ColorOutput "🎯 Smart Alarm - Quality Validation Started" "Green"
    Write-ColorOutput "============================================" "Blue"

    if ($Verbose) {
        Write-ColorOutput "Verbose mode enabled - detailed logs will be saved to: $QualityLogFile" "Yellow"
    }

    # Execute quality validation phases
    Test-Prerequisites

    if (-not (Invoke-BuildQualityGate)) {
        Write-ColorOutput "❌ Build quality gate failed" "Red"
        exit 1
    }

    if (-not (Invoke-TestsQualityGate)) {
        Write-ColorOutput "❌ Tests quality gate failed" "Red"
        exit 1
    }

    Invoke-AdditionalQualityChecks

    Write-QualityReport

    Write-ColorOutput "🎉 Quality validation completed successfully!" "Green"
    Write-ColorOutput "✅ All quality gates passed - ready for development" "Green"

    return 0
}

# Error handling
trap {
    Write-ColorOutput "❌ Quality validation failed unexpectedly: $($_.Exception.Message)" "Red"
    exit 1
}

# Execute main function
try {
    Main
}
catch {
    Write-ColorOutput "❌ Quality validation process failed: $($_.Exception.Message)" "Red"
    exit 1
}
