#!/bin/bash

# Smart Alarm - Full Build Script (Build-First Strategy)
# This script performs a complete build of all components before any testing
# Author: Smart Alarm Team
# Usage: ./scripts/full-build.sh [--verbose]

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Configuration
VERBOSE_MODE=false
BUILD_LOG_FILE="build-$(date +%Y%m%d-%H%M%S).log"
BUILD_START_TIME=$(date +%s)

# Parse arguments
for arg in "$@"; do
    case $arg in
        -v|--verbose)
            VERBOSE_MODE=true
            shift
            ;;
        *)
            # Unknown option
            ;;
    esac
done

# Function to print messages with colors
print_message() {
    local color=$1
    local message=$2
    echo -e "${color}${message}${NC}"
    if [[ "$VERBOSE_MODE" == "true" ]]; then
        echo "$(date '+%Y-%m-%d %H:%M:%S') - $message" >> "$BUILD_LOG_FILE"
    fi
}

# Function to run command with logging
run_command() {
    local description=$1
    local command=$2

    print_message "${BLUE}" "🔨 $description"

    if [[ "$VERBOSE_MODE" == "true" ]]; then
        echo "$(date '+%Y-%m-%d %H:%M:%S') - Executing: $command" >> "$BUILD_LOG_FILE"
        eval "$command" 2>&1 | tee -a "$BUILD_LOG_FILE"
        local exit_code=${PIPESTATUS[0]}
    else
        eval "$command" > /dev/null 2>&1
        local exit_code=$?
    fi

    if [[ $exit_code -eq 0 ]]; then
        print_message "${GREEN}" "  ✅ $description completed successfully"
        return 0
    else
        print_message "${RED}" "  ❌ $description failed (exit code: $exit_code)"
        if [[ "$VERBOSE_MODE" == "false" ]]; then
            print_message "${YELLOW}" "  💡 Run with --verbose flag for detailed logs"
        fi
        return $exit_code
    fi
}

# Function to check prerequisites
check_prerequisites() {
    print_message "${BLUE}" "🔍 Checking prerequisites..."

    # Check .NET SDK
    if ! command -v dotnet &> /dev/null; then
        print_message "${RED}" "❌ .NET SDK not found. Please install .NET 8.0 SDK"
        exit 1
    fi

    local dotnet_version=$(dotnet --version)
    print_message "${GREEN}" "  ✅ .NET SDK: $dotnet_version"

    # Check Node.js and npm
    if ! command -v node &> /dev/null; then
        print_message "${RED}" "❌ Node.js not found. Please install Node.js 18+"
        exit 1
    fi

    if ! command -v npm &> /dev/null; then
        print_message "${RED}" "❌ npm not found. Please install npm"
        exit 1
    fi

    local node_version=$(node --version)
    local npm_version=$(npm --version)
    print_message "${GREEN}" "  ✅ Node.js: $node_version"
    print_message "${GREEN}" "  ✅ npm: $npm_version"

    # Check Docker (optional but recommended)
    if command -v docker &> /dev/null; then
        local docker_version=$(docker --version)
        print_message "${GREEN}" "  ✅ Docker: $docker_version"
    else
        print_message "${YELLOW}" "  ⚠️  Docker not found (optional for build, required for containers)"
    fi
}

# Function to build backend (.NET)
build_backend() {
    print_message "${CYAN}" "🏗️  Building Backend (.NET Components)"

    # Clear NuGet cache to avoid cross-platform issues
    run_command "Clearing NuGet cache" "dotnet nuget locals all --clear"

    # Restore NuGet packages with force
    run_command "Restoring NuGet packages" "dotnet restore SmartAlarm.sln --force --no-cache"

    # Build main solution
    run_command "Building main solution" "dotnet build SmartAlarm.sln --configuration Release --no-restore"

    # Build main API
    run_command "Building main API" "dotnet build src/SmartAlarm.Api/SmartAlarm.Api.csproj --configuration Release --no-restore"

    # Build domain layer
    run_command "Building domain layer" "dotnet build src/SmartAlarm.Domain/SmartAlarm.Domain.csproj --configuration Release --no-restore"

    # Build application layer
    run_command "Building application layer" "dotnet build src/SmartAlarm.Application/SmartAlarm.Application.csproj --configuration Release --no-restore"

    # Build infrastructure layer
    run_command "Building infrastructure layer" "dotnet build src/SmartAlarm.Infrastructure/SmartAlarm.Infrastructure.csproj --configuration Release --no-restore"

    print_message "${GREEN}" "✅ Backend build completed successfully"
}

# Function to build microservices
build_microservices() {
    print_message "${CYAN}" "🔧 Building Microservices"

    local services=("ai-service" "alarm-service" "integration-service")

    for service in "${services[@]}"; do
        local service_path="services/$service"

        if [[ -d "$service_path" ]]; then
            print_message "${BLUE}" "Building $service..."

            # Check if it's a .NET service
            if [[ -f "$service_path/$service.csproj" ]]; then
                run_command "Building $service (.NET)" "dotnet build $service_path/$service.csproj --configuration Release"
            elif [[ -f "$service_path/Dockerfile" ]]; then
                # Build Docker image for validation
                run_command "Building $service (Docker)" "docker build -t smartalarm/$service:build-test $service_path"
            else
                print_message "${YELLOW}" "  ⚠️  No build configuration found for $service"
            fi
        else
            print_message "${YELLOW}" "  ⚠️  Service directory not found: $service_path"
        fi
    done

    print_message "${GREEN}" "✅ Microservices build completed"
}

# Function to build frontend
build_frontend() {
    print_message "${CYAN}" "🎨 Building Frontend (React)"

    local frontend_path="frontend"

    if [[ ! -d "$frontend_path" ]]; then
        print_message "${YELLOW}" "⚠️  Frontend directory not found: $frontend_path"
        return 0
    fi

    cd "$frontend_path"

    # Install dependencies
    run_command "Installing npm dependencies" "npm ci"

    # Run TypeScript type checking
    run_command "TypeScript type checking" "npm run type-check"

    # Run linting
    run_command "ESLint checking" "npm run lint"

    # Build for production
    run_command "Building React application" "npm run build"

    cd ..

    print_message "${GREEN}" "✅ Frontend build completed successfully"
}

# Function to validate build artifacts
validate_build_artifacts() {
    print_message "${CYAN}" "🔍 Validating Build Artifacts"

    local validation_errors=0

    # Check .NET build outputs
    if [[ -d "src/SmartAlarm.Api/bin/Release" ]]; then
        print_message "${GREEN}" "  ✅ Main API build artifacts found"
    else
        print_message "${RED}" "  ❌ Main API build artifacts missing"
        ((validation_errors++))
    fi

    # Check frontend build outputs
    if [[ -d "frontend/dist" ]]; then
        print_message "${GREEN}" "  ✅ Frontend build artifacts found"
    else
        print_message "${RED}" "  ❌ Frontend build artifacts missing"
        ((validation_errors++))
    fi

    # Check for critical files
    local critical_files=(
        "src/SmartAlarm.Api/bin/Release/net8.0/SmartAlarm.Api.dll"
        "frontend/dist/index.html"
    )

    for file in "${critical_files[@]}"; do
        if [[ -f "$file" ]]; then
            print_message "${GREEN}" "  ✅ Critical file found: $(basename "$file")"
        else
            print_message "${RED}" "  ❌ Critical file missing: $file"
            ((validation_errors++))
        fi
    done

    if [[ $validation_errors -eq 0 ]]; then
        print_message "${GREEN}" "✅ All build artifacts validated successfully"
        return 0
    else
        print_message "${RED}" "❌ Build validation failed with $validation_errors errors"
        return 1
    fi
}

# Function to generate build report
generate_build_report() {
    local build_end_time=$(date +%s)
    local build_duration=$((build_end_time - BUILD_START_TIME))

    print_message "${BLUE}" "📊 Build Report"
    print_message "${BLUE}" "==============="
    print_message "${YELLOW}" "Build Duration: ${build_duration}s"
    print_message "${YELLOW}" "Build Log: $BUILD_LOG_FILE"
    print_message "${YELLOW}" "Timestamp: $(date)"

    if [[ "$VERBOSE_MODE" == "true" ]]; then
        print_message "${YELLOW}" "Detailed logs available in: $BUILD_LOG_FILE"
    fi
}

# Main execution
main() {
    print_message "${GREEN}" "🚀 Smart Alarm - Full Build Process Started"
    print_message "${BLUE}" "=============================================="

    if [[ "$VERBOSE_MODE" == "true" ]]; then
        print_message "${YELLOW}" "Verbose mode enabled - detailed logs will be saved to: $BUILD_LOG_FILE"
    fi

    # Execute build steps
    check_prerequisites
    build_backend
    build_microservices
    build_frontend
    validate_build_artifacts

    generate_build_report

    print_message "${GREEN}" "🎉 Full build completed successfully!"
    print_message "${BLUE}" "Ready to proceed with testing phase"

    return 0
}

# Error handling
trap 'print_message "${RED}" "❌ Build failed unexpectedly at line $LINENO"; exit 1' ERR

# Execute main function
main "$@"
