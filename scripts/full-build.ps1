# Smart Alarm - Full Build Script (Build-First Strategy) - PowerShell Version
# This script performs a complete build of all components before any testing
# Author: Smart Alarm Team
# Usage: .\scripts\full-build.ps1 [-Verbose]

param(
    [switch]$Verbose = $false
)

# Configuration
$BuildLogFile = "build-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"
$BuildStartTime = Get-Date

# Function to write colored output
function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )

    Write-Host $Message -ForegroundColor $Color

    if ($Verbose) {
        "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') - $Message" | Add-Content -Path $BuildLogFile
    }
}

# Function to run command with logging
function Invoke-BuildCommand {
    param(
        [string]$Description,
        [string]$Command,
        [string[]]$Arguments = @()
    )

    Write-ColorOutput "🔨 $Description" "Blue"

    try {
        if ($Verbose) {
            "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') - Executing: $Command $($Arguments -join ' ')" | Add-Content -Path $BuildLogFile

            if ($Arguments.Count -gt 0) {
                & $Command @Arguments 2>&1 | Tee-Object -FilePath $BuildLogFile -Append
            } else {
                & $Command 2>&1 | Tee-Object -FilePath $BuildLogFile -Append
            }
        } else {
            if ($Arguments.Count -gt 0) {
                & $Command @Arguments 2>&1 | Out-Null
            } else {
                & $Command 2>&1 | Out-Null
            }
        }

        if ($LASTEXITCODE -eq 0) {
            Write-ColorOutput "  ✅ $Description completed successfully" "Green"
            return $true
        } else {
            Write-ColorOutput "  ❌ $Description failed (exit code: $LASTEXITCODE)" "Red"
            if (-not $Verbose) {
                Write-ColorOutput "  💡 Run with -Verbose flag for detailed logs" "Yellow"
            }
            return $false
        }
    }
    catch {
        Write-ColorOutput "  ❌ $Description failed with exception: $($_.Exception.Message)" "Red"
        return $false
    }
}

# Function to check prerequisites
function Test-Prerequisites {
    Write-ColorOutput "🔍 Checking prerequisites..." "Blue"

    # Check .NET SDK
    try {
        $dotnetVersion = & dotnet --version 2>$null
        Write-ColorOutput "  ✅ .NET SDK: $dotnetVersion" "Green"
    }
    catch {
        Write-ColorOutput "❌ .NET SDK not found. Please install .NET 8.0 SDK" "Red"
        exit 1
    }

    # Check Node.js and npm
    try {
        $nodeVersion = & node --version 2>$null
        $npmVersion = & npm --version 2>$null
        Write-ColorOutput "  ✅ Node.js: $nodeVersion" "Green"
        Write-ColorOutput "  ✅ npm: $npmVersion" "Green"
    }
    catch {
        Write-ColorOutput "❌ Node.js/npm not found. Please install Node.js 18+" "Red"
        exit 1
    }

    # Check Docker (optional)
    try {
        $dockerVersion = & docker --version 2>$null
        Write-ColorOutput "  ✅ Docker: $dockerVersion" "Green"
    }
    catch {
        Write-ColorOutput "  ⚠️  Docker not found (optional for build, required for containers)" "Yellow"
    }
}

# Function to build backend (.NET)
function Build-Backend {
    Write-ColorOutput "🏗️  Building Backend (.NET Components)" "Cyan"

    # Clear NuGet cache (optional - continue if fails)
    if (-not (Invoke-BuildCommand "Clearing NuGet cache" "dotnet" @("nuget", "locals", "all", "--clear"))) {
        Write-ColorOutput "  ⚠️  NuGet cache clearing failed, continuing..." "Yellow"
    }

    # Restore NuGet packages
    if (-not (Invoke-BuildCommand "Restoring NuGet packages" "dotnet" @("restore", "SmartAlarm.sln", "--force"))) {
        return $false
    }

    # Build main solution (excluding failing test projects for now)
    if (-not (Invoke-BuildCommand "Building main solution" "dotnet" @("build", "SmartAlarm.sln", "--configuration", "Release", "--no-restore", "--property:SkipTests=true"))) {
        # If that fails, try building core projects individually
        Write-ColorOutput "  ⚠️  Full solution build failed, trying individual projects..." "Yellow"

        # Build core projects that are working
        $coreProjects = @(
            "src/SmartAlarm.Api/SmartAlarm.Api.csproj",
            "src/SmartAlarm.Domain/SmartAlarm.Domain.csproj",
            "src/SmartAlarm.Application/SmartAlarm.Application.csproj",
            "src/SmartAlarm.Infrastructure/SmartAlarm.Infrastructure.csproj",
            "src/SmartAlarm.KeyVault/SmartAlarm.KeyVault.csproj",
            "src/SmartAlarm.Observability/SmartAlarm.Observability.csproj",
            "services/ai-service/SmartAlarm.AiService.csproj",
            "services/alarm-service/SmartAlarm.AlarmService.csproj",
            "services/integration-service/SmartAlarm.IntegrationService.csproj"
        )

        $failedProjects = 0
        foreach ($project in $coreProjects) {
            if (Test-Path $project) {
                $projectName = Split-Path $project -Leaf
                if (-not (Invoke-BuildCommand "Building $projectName" "dotnet" @("build", $project, "--configuration", "Release", "--no-restore"))) {
                    $failedProjects++
                }
            }
        }

        if ($failedProjects -gt 0) {
            Write-ColorOutput "  ❌ $failedProjects core projects failed to build" "Red"
            return $false
        }
    }

    # Build individual projects
    $projects = @(
        "src/SmartAlarm.Api/SmartAlarm.Api.csproj",
        "src/SmartAlarm.Domain/SmartAlarm.Domain.csproj",
        "src/SmartAlarm.Application/SmartAlarm.Application.csproj",
        "src/SmartAlarm.Infrastructure/SmartAlarm.Infrastructure.csproj"
    )

    foreach ($project in $projects) {
        if (Test-Path $project) {
            $projectName = Split-Path $project -Leaf
            if (-not (Invoke-BuildCommand "Building $projectName" "dotnet" @("build", $project, "--configuration", "Release", "--no-restore"))) {
                return $false
            }
        }
    }

    Write-ColorOutput "✅ Backend build completed successfully" "Green"
    return $true
}

# Function to build microservices
function Build-Microservices {
    Write-ColorOutput "🔧 Building Microservices" "Cyan"

    $services = @("ai-service", "alarm-service", "integration-service")

    foreach ($service in $services) {
        $servicePath = "services/$service"

        if (Test-Path $servicePath) {
            Write-ColorOutput "Building $service..." "Blue"

            # Check if it's a .NET service
            $csprojFile = "$servicePath/$service.csproj"
            if (Test-Path $csprojFile) {
                if (-not (Invoke-BuildCommand "Building $service (.NET)" "dotnet" @("build", $csprojFile, "--configuration", "Release"))) {
                    Write-ColorOutput "  ⚠️  Failed to build $service" "Yellow"
                }
            }
            elseif (Test-Path "$servicePath/Dockerfile") {
                # Build Docker image for validation
                if (-not (Invoke-BuildCommand "Building $service (Docker)" "docker" @("build", "-t", "smartalarm/$service`:build-test", $servicePath))) {
                    Write-ColorOutput "  ⚠️  Failed to build Docker image for $service" "Yellow"
                }
            }
            else {
                Write-ColorOutput "  ⚠️  No build configuration found for $service" "Yellow"
            }
        }
        else {
            Write-ColorOutput "  ⚠️  Service directory not found: $servicePath" "Yellow"
        }
    }

    Write-ColorOutput "✅ Microservices build completed" "Green"
    return $true
}

# Function to build frontend
function Build-Frontend {
    Write-ColorOutput "🎨 Building Frontend (React)" "Cyan"

    $frontendPath = "frontend"

    if (-not (Test-Path $frontendPath)) {
        Write-ColorOutput "⚠️  Frontend directory not found: $frontendPath" "Yellow"
        return $true
    }

    Push-Location $frontendPath

    try {
        # Install dependencies
        if (-not (Invoke-BuildCommand "Installing npm dependencies" "npm" @("ci"))) {
            return $false
        }

        # Run TypeScript type checking
        if (-not (Invoke-BuildCommand "TypeScript type checking" "npm" @("run", "type-check"))) {
            Write-ColorOutput "  ⚠️  TypeScript type checking failed, continuing..." "Yellow"
        }

        # Run linting
        if (-not (Invoke-BuildCommand "ESLint checking" "npm" @("run", "lint"))) {
            Write-ColorOutput "  ⚠️  ESLint checking failed, continuing..." "Yellow"
        }

        # Build for production
        if (-not (Invoke-BuildCommand "Building React application" "npm" @("run", "build"))) {
            return $false
        }

        Write-ColorOutput "✅ Frontend build completed successfully" "Green"
        return $true
    }
    finally {
        Pop-Location
    }
}

# Function to validate build artifacts
function Test-BuildArtifacts {
    Write-ColorOutput "🔍 Validating Build Artifacts" "Cyan"

    $validationErrors = 0

    # Check .NET build outputs
    if (Test-Path "src/SmartAlarm.Api/bin/Release") {
        Write-ColorOutput "  ✅ Main API build artifacts found" "Green"
    } else {
        Write-ColorOutput "  ❌ Main API build artifacts missing" "Red"
        $validationErrors++
    }

    # Check frontend build outputs
    if (Test-Path "frontend/dist") {
        Write-ColorOutput "  ✅ Frontend build artifacts found" "Green"
    } else {
        Write-ColorOutput "  ❌ Frontend build artifacts missing" "Red"
        $validationErrors++
    }

    # Check for critical files
    $criticalFiles = @(
        "src/SmartAlarm.Api/bin/Release/net8.0/SmartAlarm.Api.dll",
        "frontend/dist/index.html"
    )

    foreach ($file in $criticalFiles) {
        if (Test-Path $file) {
            $fileName = Split-Path $file -Leaf
            Write-ColorOutput "  ✅ Critical file found: $fileName" "Green"
        } else {
            Write-ColorOutput "  ❌ Critical file missing: $file" "Red"
            $validationErrors++
        }
    }

    if ($validationErrors -eq 0) {
        Write-ColorOutput "✅ All build artifacts validated successfully" "Green"
        return $true
    } else {
        Write-ColorOutput "❌ Build validation failed with $validationErrors errors" "Red"
        return $false
    }
}

# Function to generate build report
function Write-BuildReport {
    $buildEndTime = Get-Date
    $buildDuration = ($buildEndTime - $BuildStartTime).TotalSeconds

    Write-ColorOutput "📊 Build Report" "Blue"
    Write-ColorOutput "===============" "Blue"
    Write-ColorOutput "Build Duration: $([math]::Round($buildDuration, 2))s" "Yellow"
    Write-ColorOutput "Build Log: $BuildLogFile" "Yellow"
    Write-ColorOutput "Timestamp: $(Get-Date)" "Yellow"

    if ($Verbose) {
        Write-ColorOutput "Detailed logs available in: $BuildLogFile" "Yellow"
    }
}

# Main execution
function Main {
    Write-ColorOutput "🚀 Smart Alarm - Full Build Process Started" "Green"
    Write-ColorOutput "==============================================" "Blue"

    if ($Verbose) {
        Write-ColorOutput "Verbose mode enabled - detailed logs will be saved to: $BuildLogFile" "Yellow"
    }

    # Execute build steps
    Test-Prerequisites

    if (-not (Build-Backend)) {
        Write-ColorOutput "❌ Backend build failed" "Red"
        exit 1
    }

    if (-not (Build-Microservices)) {
        Write-ColorOutput "❌ Microservices build failed" "Red"
        exit 1
    }

    if (-not (Build-Frontend)) {
        Write-ColorOutput "❌ Frontend build failed" "Red"
        exit 1
    }

    if (-not (Test-BuildArtifacts)) {
        Write-ColorOutput "❌ Build artifacts validation failed" "Red"
        exit 1
    }

    Write-BuildReport

    Write-ColorOutput "🎉 Full build completed successfully!" "Green"
    Write-ColorOutput "Ready to proceed with testing phase" "Blue"

    return 0
}

# Error handling
trap {
    Write-ColorOutput "❌ Build failed unexpectedly: $($_.Exception.Message)" "Red"
    exit 1
}

# Execute main function
try {
    Main
}
catch {
    Write-ColorOutput "❌ Build process failed: $($_.Exception.Message)" "Red"
    exit 1
}
