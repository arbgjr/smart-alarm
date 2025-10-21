#!/bin/bash

# Smart Alarm - Critical Tests Runner (Build-First Strategy)
# This script runs only critical/essential tests after successful build
# Author: Smart Alarm Team
# Usage: ./scripts/run-critical-tests.sh [--verbose] [--build-first]

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
BUILD_FIRST=true
TEST_LOG_FILE="critical-tests-$(date +%Y%m%d-%H%M%S).log"
TEST_START_TIME=$(date +%s)
FAILED_TESTS=()

# Parse arguments
for arg in "$@"; do
    case $arg in
        -v|--verbose)
            VERBOSE_MODE=true
            shift
            ;;
        --no-build)
            BUILD_FIRST=false
            shift
            ;;
        --build-first)
            BUILD_FIRST=true
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
        echo "$(date '+%Y-%m-%d %H:%M:%S') - $message" >> "$TEST_LOG_FILE"
    fi
}

# Function to run test command with logging
run_test_command() {
    local description=$1
    local command=$2
    local is_critical=${3:-true}

    print_message "${BLUE}" "🧪 $description"

    if [[ "$VERBOSE_MODE" == "true" ]]; then
        echo "$(date '+%Y-%m-%d %H:%M:%S') - Executing: $command" >> "$TEST_LOG_FILE"
        eval "$command" 2>&1 | tee -a "$TEST_LOG_FILE"
        local exit_code=${PIPESTATUS[0]}
    else
        eval "$command" > /dev/null 2>&1
        local exit_code=$?
    fi

    if [[ $exit_code -eq 0 ]]; then
        print_message "${GREEN}" "  ✅ $description passed"
        return 0
    else
        print_message "${RED}" "  ❌ $description failed (exit code: $exit_code)"

        if [[ "$is_critical" == "true" ]]; then
            FAILED_TESTS+=("$description")
        fi

        if [[ "$VERBOSE_MODE" == "false" ]]; then
            print_message "${YELLOW}" "  💡 Run with --verbose flag for detailed logs"
        fi
        return $exit_code
    fi
}

# Function to ensure build is completed first
ensure_build_completed() {
    if [[ "$BUILD_FIRST" == "true" ]]; then
        print_message "${BLUE}" "🔨 Ensuring full build is completed first..."

        if [[ -f "scripts/full-build.sh" ]]; then
            if [[ "$VERBOSE_MODE" == "true" ]]; then
                bash scripts/full-build.sh --verbose
            else
                bash scripts/full-build.sh
            fi

            if [[ $? -ne 0 ]]; then
                print_message "${RED}" "❌ Full build failed - cannot proceed with tests"
                exit 1
            fi

            print_message "${GREEN}" "✅ Full build completed - proceeding with critical tests"
        else
            print_message "${YELLOW}" "⚠️  Full build script not found - assuming build is already completed"
        fi
    else
        print_message "${YELLOW}" "⚠️  Skipping build validation (--no-build flag used)"
    fi
}

# Function to run critical backend tests
run_critical_backend_tests() {
    print_message "${CYAN}" "🏗️  Running Critical Backend Tests"

    # Test 1: Authentication and JWT Tests
    run_test_command "Authentication JWT Tests" \
        "dotnet test tests/SmartAlarm.Api.Tests/SmartAlarm.Api.Tests.csproj --filter Category=Authentication --configuration Release --no-build --logger 'console;verbosity=minimal'" \
        true

    # Test 2: Alarm CRUD Operations
    run_test_command "Alarm CRUD Tests" \
        "dotnet test tests/SmartAlarm.Application.Tests/SmartAlarm.Application.Tests.csproj --filter Category=AlarmCRUD --configuration Release --no-build --logger 'console;verbosity=minimal'" \
        true

    # Test 3: Domain Model Validation
    run_test_command "Domain Model Tests" \
        "dotnet test tests/SmartAlarm.Domain.Tests/SmartAlarm.Domain.Tests.csproj --filter Category=Critical --configuration Release --no-build --logger 'console;verbosity=minimal'" \
        true

    # Test 4: Infrastructure Core Tests
    run_test_command "Infrastructure Core Tests" \
        "dotnet test tests/SmartAlarm.Infrastructure.Tests/SmartAlarm.Infrastructure.Tests.csproj --filter Category=Core --configuration Release --no-build --logger 'console;verbosity=minimal'" \
        true

    # Test 5: Basic API Endpoints
    run_test_command "Basic API Endpoint Tests" \
        "dotnet test tests/SmartAlarm.Api.Tests/SmartAlarm.Api.Tests.csproj --filter Category=BasicEndpoints --configuration Release --no-build --logger 'console;verbosity=minimal'" \
        true
}

# Function to run critical integration tests
run_critical_integration_tests() {
    print_message "${CYAN}" "🔗 Running Critical Integration Tests"

    # Test 1: Database Integration (Essential)
    run_test_command "Database Integration Tests" \
        "dotnet test tests/SmartAlarm.IntegrationTests/SmartAlarm.IntegrationTests.csproj --filter Category=Database --configuration Release --no-build --logger 'console;verbosity=minimal'" \
        true

    # Test 2: Authentication Flow Integration
    run_test_command "Authentication Flow Integration" \
        "dotnet test tests/SmartAlarm.IntegrationTests/SmartAlarm.IntegrationTests.csproj --filter Category=AuthFlow --configuration Release --no-build --logger 'console;verbosity=minimal'" \
        true

    # Test 3: Alarm Processing Integration
    run_test_command "Alarm Processing Integration" \
        "dotnet test tests/SmartAlarm.IntegrationTests/SmartAlarm.IntegrationTests.csproj --filter Category=AlarmProcessing --configuration Release --no-build --logger 'console;verbosity=minimal'" \
        true
}

# Function to run critical frontend tests
run_critical_frontend_tests() {
    print_message "${CYAN}" "🎨 Running Critical Frontend Tests"

    local frontend_path="frontend"

    if [[ ! -d "$frontend_path" ]]; then
        print_message "${YELLOW}" "⚠️  Frontend directory not found: $frontend_path - skipping frontend tests"
        return 0
    fi

    cd "$frontend_path"

    # Test 1: TypeScript Compilation
    run_test_command "TypeScript Compilation Check" \
        "npm run type-check" \
        true

    # Test 2: Critical Component Tests
    run_test_command "Critical Component Tests" \
        "npm run test -- --run --reporter=verbose --testNamePattern='(Login|Alarm|Dashboard)'" \
        true

    # Test 3: Build Validation
    run_test_command "Frontend Build Validation" \
        "npm run build" \
        true

    cd ..
}

# Function to run smoke tests
run_smoke_tests() {
    print_message "${CYAN}" "💨 Running Smoke Tests"

    # Test 1: Application Startup
    run_test_command "Application Startup Test" \
        "dotnet test tests/SmartAlarm.BasicTests/SmartAlarm.BasicTests.csproj --filter Category=Smoke --configuration Release --no-build --logger 'console;verbosity=minimal'" \
        true

    # Test 2: Health Check Endpoints
    run_test_command "Health Check Tests" \
        "dotnet test tests/SmartAlarm.Api.Tests/SmartAlarm.Api.Tests.csproj --filter Category=HealthCheck --configuration Release --no-build --logger 'console;verbosity=minimal'" \
        false
}

# Function to validate test environment
validate_test_environment() {
    print_message "${BLUE}" "🔍 Validating Test Environment"

    # Check if .NET test runner is available
    if ! command -v dotnet &> /dev/null; then
        print_message "${RED}" "❌ .NET SDK not found"
        exit 1
    fi

    # Check if npm is available for frontend tests
    if [[ -d "frontend" ]] && ! command -v npm &> /dev/null; then
        print_message "${RED}" "❌ npm not found (required for frontend tests)"
        exit 1
    fi

    # Check if test projects exist
    local test_projects=(
        "tests/SmartAlarm.Api.Tests/SmartAlarm.Api.Tests.csproj"
        "tests/SmartAlarm.Application.Tests/SmartAlarm.Application.Tests.csproj"
        "tests/SmartAlarm.Domain.Tests/SmartAlarm.Domain.Tests.csproj"
        "tests/SmartAlarm.Infrastructure.Tests/SmartAlarm.Infrastructure.Tests.csproj"
    )

    local missing_projects=0
    for project in "${test_projects[@]}"; do
        if [[ -f "$project" ]]; then
            print_message "${GREEN}" "  ✅ Test project found: $(basename "$project")"
        else
            print_message "${YELLOW}" "  ⚠️  Test project missing: $project"
            ((missing_projects++))
        fi
    done

    if [[ $missing_projects -gt 0 ]]; then
        print_message "${YELLOW}" "⚠️  Some test projects are missing - tests may be limited"
    fi
}

# Function to generate test report
generate_test_report() {
    local test_end_time=$(date +%s)
    local test_duration=$((test_end_time - TEST_START_TIME))

    print_message "${BLUE}" "📊 Critical Tests Report"
    print_message "${BLUE}" "========================"
    print_message "${YELLOW}" "Test Duration: ${test_duration}s"
    print_message "${YELLOW}" "Test Log: $TEST_LOG_FILE"
    print_message "${YELLOW}" "Timestamp: $(date)"

    if [[ ${#FAILED_TESTS[@]} -eq 0 ]]; then
        print_message "${GREEN}" "✅ All critical tests passed!"
        print_message "${GREEN}" "🚀 Ready to proceed to next phase"
    else
        print_message "${RED}" "❌ ${#FAILED_TESTS[@]} critical test(s) failed:"
        for test in "${FAILED_TESTS[@]}"; do
            print_message "${RED}" "  - $test"
        done
        print_message "${RED}" "🛑 Cannot proceed until critical tests pass"
    fi

    if [[ "$VERBOSE_MODE" == "true" ]]; then
        print_message "${YELLOW}" "Detailed logs available in: $TEST_LOG_FILE"
    fi
}

# Main execution
main() {
    print_message "${GREEN}" "🧪 Smart Alarm - Critical Tests Runner Started"
    print_message "${BLUE}" "==============================================="

    if [[ "$VERBOSE_MODE" == "true" ]]; then
        print_message "${YELLOW}" "Verbose mode enabled - detailed logs will be saved to: $TEST_LOG_FILE"
    fi

    # Execute test phases
    validate_test_environment
    ensure_build_completed

    # Run critical tests in order of importance
    run_smoke_tests
    run_critical_backend_tests
    run_critical_integration_tests
    run_critical_frontend_tests

    generate_test_report

    # Exit with error if critical tests failed
    if [[ ${#FAILED_TESTS[@]} -gt 0 ]]; then
        print_message "${RED}" "❌ Critical tests failed - blocking progression"
        exit 1
    fi

    print_message "${GREEN}" "🎉 All critical tests passed successfully!"
    print_message "${BLUE}" "✅ Ready to proceed with development activities"

    return 0
}

# Error handling
trap 'print_message "${RED}" "❌ Critical tests failed unexpectedly at line $LINENO"; exit 1' ERR

# Execute main function
main "$@"
