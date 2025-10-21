#!/bin/bash

# Smart Alarm - Quality Validation Script (Build + Critical Tests)
# This script combines full build + critical tests as a single quality gate
# Author: Smart Alarm Team
# Usage: ./scripts/validate-quality.sh [--verbose] [--skip-build] [--skip-tests]

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
SKIP_BUILD=false
SKIP_TESTS=false
QUALITY_LOG_FILE="quality-validation-$(date +%Y%m%d-%H%M%S).log"
VALIDATION_START_TIME=$(date +%s)

# Parse arguments
for arg in "$@"; do
    case $arg in
        -v|--verbose)
            VERBOSE_MODE=true
            shift
            ;;
        --skip-build)
            SKIP_BUILD=true
            shift
            ;;
        --skip-tests)
            SKIP_TESTS=true
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
        echo "$(date '+%Y-%m-%d %H:%M:%S') - $message" >> "$QUALITY_LOG_FILE"
    fi
}

# Function to run quality gate step
run_quality_step() {
    local step_name=$1
    local script_path=$2
    local is_critical=${3:-true}

    print_message "${BLUE}" "🔍 Quality Gate: $step_name"

    if [[ ! -f "$script_path" ]]; then
        print_message "${RED}" "❌ Script not found: $script_path"
        if [[ "$is_critical" == "true" ]]; then
            exit 1
        else
            return 1
        fi
    fi

    # Make script executable
    chmod +x "$script_path"

    # Run the script
    local script_args=""
    if [[ "$VERBOSE_MODE" == "true" ]]; then
        script_args="--verbose"
    fi

    if [[ "$VERBOSE_MODE" == "true" ]]; then
        echo "$(date '+%Y-%m-%d %H:%M:%S') - Executing: bash $script_path $script_args" >> "$QUALITY_LOG_FILE"
        bash "$script_path" $script_args 2>&1 | tee -a "$QUALITY_LOG_FILE"
        local exit_code=${PIPESTATUS[0]}
    else
        bash "$script_path" $script_args > /dev/null 2>&1
        local exit_code=$?
    fi

    if [[ $exit_code -eq 0 ]]; then
        print_message "${GREEN}" "  ✅ $step_name passed"
        return 0
    else
        print_message "${RED}" "  ❌ $step_name failed (exit code: $exit_code)"

        if [[ "$is_critical" == "true" ]]; then
            print_message "${RED}" "🛑 Critical quality gate failed - stopping validation"
            exit $exit_code
        fi

        return $exit_code
    fi
}

# Function to check prerequisites
check_prerequisites() {
    print_message "${BLUE}" "🔍 Checking Quality Validation Prerequisites"

    # Check if we're in the right directory
    if [[ ! -f "SmartAlarm.sln" ]]; then
        print_message "${RED}" "❌ SmartAlarm.sln not found. Please run from project root directory."
        exit 1
    fi

    # Check required scripts
    local required_scripts=(
        "scripts/full-build.sh"
        "scripts/run-critical-tests.sh"
    )

    local missing_scripts=0
    for script in "${required_scripts[@]}"; do
        if [[ -f "$script" ]]; then
            print_message "${GREEN}" "  ✅ Required script found: $script"
        else
            print_message "${RED}" "  ❌ Required script missing: $script"
            ((missing_scripts++))
        fi
    done

    if [[ $missing_scripts -gt 0 ]]; then
        print_message "${RED}" "❌ Missing required scripts - cannot proceed"
        exit 1
    fi

    print_message "${GREEN}" "✅ All prerequisites satisfied"
}

# Function to run full build quality gate
run_build_quality_gate() {
    if [[ "$SKIP_BUILD" == "true" ]]; then
        print_message "${YELLOW}" "⚠️  Skipping build quality gate (--skip-build flag)"
        return 0
    fi

    print_message "${CYAN}" "🏗️  Build Quality Gate"
    print_message "${BLUE}" "====================="

    run_quality_step "Full Build Validation" "scripts/full-build.sh" true

    print_message "${GREEN}" "✅ Build quality gate passed"
}

# Function to run tests quality gate
run_tests_quality_gate() {
    if [[ "$SKIP_TESTS" == "true" ]]; then
        print_message "${YELLOW}" "⚠️  Skipping tests quality gate (--skip-tests flag)"
        return 0
    fi

    print_message "${CYAN}" "🧪 Tests Quality Gate"
    print_message "${BLUE}" "===================="

    # Run critical tests with --no-build since we already built
    local test_args="--no-build"
    if [[ "$VERBOSE_MODE" == "true" ]]; then
        test_args="$test_args --verbose"
    fi

    if [[ "$VERBOSE_MODE" == "true" ]]; then
        echo "$(date '+%Y-%m-%d %H:%M:%S') - Executing: bash scripts/run-critical-tests.sh $test_args" >> "$QUALITY_LOG_FILE"
        bash scripts/run-critical-tests.sh $test_args 2>&1 | tee -a "$QUALITY_LOG_FILE"
        local exit_code=${PIPESTATUS[0]}
    else
        bash scripts/run-critical-tests.sh $test_args > /dev/null 2>&1
        local exit_code=$?
    fi

    if [[ $exit_code -eq 0 ]]; then
        print_message "${GREEN}" "✅ Tests quality gate passed"
        return 0
    else
        print_message "${RED}" "❌ Tests quality gate failed"
        print_message "${RED}" "🛑 Critical tests must pass before proceeding"
        exit $exit_code
    fi
}

# Function to run additional quality checks
run_additional_quality_checks() {
    print_message "${CYAN}" "🔍 Additional Quality Checks"
    print_message "${BLUE}" "============================"

    # Check 1: Verify build artifacts exist
    print_message "${BLUE}" "Checking build artifacts..."
    local critical_artifacts=(
        "src/SmartAlarm.Api/bin/Release/net8.0/SmartAlarm.Api.dll"
        "frontend/dist/index.html"
    )

    local missing_artifacts=0
    for artifact in "${critical_artifacts[@]}"; do
        if [[ -f "$artifact" ]]; then
            print_message "${GREEN}" "  ✅ Build artifact found: $(basename "$artifact")"
        else
            print_message "${YELLOW}" "  ⚠️  Build artifact missing: $artifact"
            ((missing_artifacts++))
        fi
    done

    # Check 2: Verify no compilation warnings in critical files
    print_message "${BLUE}" "Checking for critical compilation warnings..."
    if command -v dotnet &> /dev/null; then
        local warning_count=$(dotnet build SmartAlarm.sln --verbosity quiet 2>&1 | grep -c "warning" || true)
        if [[ $warning_count -eq 0 ]]; then
            print_message "${GREEN}" "  ✅ No compilation warnings found"
        else
            print_message "${YELLOW}" "  ⚠️  Found $warning_count compilation warnings"
        fi
    fi

    # Check 3: Verify test coverage (if available)
    if [[ -f "tests/TestCoverageReport/index.html" ]]; then
        print_message "${GREEN}" "  ✅ Test coverage report available"
    else
        print_message "${YELLOW}" "  ⚠️  Test coverage report not found (optional)"
    fi

    print_message "${GREEN}" "✅ Additional quality checks completed"
}

# Function to generate quality report
generate_quality_report() {
    local validation_end_time=$(date +%s)
    local validation_duration=$((validation_end_time - VALIDATION_START_TIME))

    print_message "${BLUE}" "📊 Quality Validation Report"
    print_message "${BLUE}" "============================"
    print_message "${YELLOW}" "Validation Duration: ${validation_duration}s"
    print_message "${YELLOW}" "Quality Log: $QUALITY_LOG_FILE"
    print_message "${YELLOW}" "Timestamp: $(date)"

    # Quality gates summary
    print_message "${CYAN}" "Quality Gates Summary:"
    if [[ "$SKIP_BUILD" == "false" ]]; then
        print_message "${GREEN}" "  ✅ Build Quality Gate: PASSED"
    else
        print_message "${YELLOW}" "  ⚠️  Build Quality Gate: SKIPPED"
    fi

    if [[ "$SKIP_TESTS" == "false" ]]; then
        print_message "${GREEN}" "  ✅ Tests Quality Gate: PASSED"
    else
        print_message "${YELLOW}" "  ⚠️  Tests Quality Gate: SKIPPED"
    fi

    print_message "${GREEN}" "  ✅ Additional Checks: COMPLETED"

    # Next steps
    print_message "${CYAN}" "Next Steps:"
    print_message "${BLUE}" "  - All quality gates have passed"
    print_message "${BLUE}" "  - System is ready for development activities"
    print_message "${BLUE}" "  - You can proceed with feature implementation"

    if [[ "$VERBOSE_MODE" == "true" ]]; then
        print_message "${YELLOW}" "Detailed logs available in: $QUALITY_LOG_FILE"
    fi
}

# Function to show usage help
show_help() {
    echo "Smart Alarm - Quality Validation Script"
    echo "======================================="
    echo ""
    echo "This script runs a complete quality validation combining:"
    echo "  1. Full build of all components"
    echo "  2. Critical tests execution"
    echo "  3. Additional quality checks"
    echo ""
    echo "Usage: $0 [options]"
    echo ""
    echo "Options:"
    echo "  -v, --verbose     Enable verbose output and detailed logging"
    echo "  --skip-build      Skip the build quality gate"
    echo "  --skip-tests      Skip the tests quality gate"
    echo "  -h, --help        Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0                    # Run full quality validation"
    echo "  $0 --verbose          # Run with detailed logging"
    echo "  $0 --skip-build       # Run only tests (assume build is done)"
    echo "  $0 --skip-tests       # Run only build validation"
    echo ""
    echo "Quality Gates:"
    echo "  1. Build Gate: Compiles backend, frontend, and microservices"
    echo "  2. Tests Gate: Runs critical tests for core functionality"
    echo "  3. Additional: Validates artifacts and checks for warnings"
    echo ""
}

# Main execution
main() {
    # Check for help flag
    for arg in "$@"; do
        case $arg in
            -h|--help)
                show_help
                exit 0
                ;;
        esac
    done

    print_message "${GREEN}" "🎯 Smart Alarm - Quality Validation Started"
    print_message "${BLUE}" "============================================"

    if [[ "$VERBOSE_MODE" == "true" ]]; then
        print_message "${YELLOW}" "Verbose mode enabled - detailed logs will be saved to: $QUALITY_LOG_FILE"
    fi

    # Execute quality validation phases
    check_prerequisites
    run_build_quality_gate
    run_tests_quality_gate
    run_additional_quality_checks

    generate_quality_report

    print_message "${GREEN}" "🎉 Quality validation completed successfully!"
    print_message "${GREEN}" "✅ All quality gates passed - ready for development"

    return 0
}

# Error handling
trap 'print_message "${RED}" "❌ Quality validation failed unexpectedly at line $LINENO"; exit 1' ERR

# Execute main function
main "$@"
