#!/bin/bash

# ===================================
# OAuth2 Flow E2E Test - Smart Alarm
# ===================================

set -e

API_BASE_URL="${API_BASE_URL:-http://localhost:8080/api/v1}"
FRONTEND_BASE_URL="${FRONTEND_BASE_URL:-http://localhost:3000}"
OAUTH_PROVIDERS=("Google" "GitHub" "Facebook" "Microsoft")

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Test counter
TESTS_PASSED=0
TESTS_FAILED=0
TESTS_TOTAL=0

# Test result function
assert_test() {
    local test_name="$1"
    local condition="$2"
    local message="$3"
    
    TESTS_TOTAL=$((TESTS_TOTAL + 1))
    
    if [ "$condition" = "0" ]; then
        log_success "✓ $test_name"
        TESTS_PASSED=$((TESTS_PASSED + 1))
    else
        log_error "✗ $test_name: $message"
        TESTS_FAILED=$((TESTS_FAILED + 1))
    fi
}

# Wait for services to be ready
wait_for_service() {
    local url="$1"
    local service_name="$2"
    local max_attempts=30
    local attempt=1
    
    log_info "Waiting for $service_name to be ready..."
    
    while [ $attempt -le $max_attempts ]; do
        if curl -sf "$url" >/dev/null 2>&1; then
            log_success "$service_name is ready!"
            return 0
        fi
        
        echo -n "."
        sleep 2
        attempt=$((attempt + 1))
    done
    
    log_error "$service_name failed to start after $max_attempts attempts"
    return 1
}

# Test OAuth Authorization URL Generation
test_oauth_authorization_url() {
    local provider="$1"
    local test_name="OAuth Authorization URL - $provider"
    
    log_info "Testing $test_name..."
    
    local redirect_uri="http://localhost:3000/auth/callback"
    local state="test-state-$(date +%s)"
    
    local response=$(curl -s -w "%{http_code}" \
        "${API_BASE_URL}/auth/oauth/${provider}/authorize?redirectUri=$(echo "$redirect_uri" | jq -sRr @uri)&state=$(echo "$state" | jq -sRr @uri)")
    
    local http_code="${response: -3}"
    local body="${response%???}"
    
    if [ "$http_code" = "200" ]; then
        # Parse JSON response
        local auth_url=$(echo "$body" | jq -r '.authorizationUrl // empty')
        local response_provider=$(echo "$body" | jq -r '.provider // empty')
        local response_state=$(echo "$body" | jq -r '.state // empty')
        
        # Validate response structure
        if [ -n "$auth_url" ] && [ "$response_provider" = "$provider" ] && [ "$response_state" = "$state" ]; then
            assert_test "$test_name" "0" ""
            log_info "Authorization URL: $auth_url"
            return 0
        else
            assert_test "$test_name" "1" "Invalid response structure"
            return 1
        fi
    else
        assert_test "$test_name" "1" "HTTP $http_code - $body"
        return 1
    fi
}

# Test OAuth Callback with Error
test_oauth_callback_error() {
    local provider="$1"
    local test_name="OAuth Callback Error Handling - $provider"
    
    log_info "Testing $test_name..."
    
    local payload=$(cat <<EOF
{
    "code": "",
    "state": "test-state-123",
    "error": "access_denied",
    "errorDescription": "User denied access"
}
EOF
)
    
    local response=$(curl -s -w "%{http_code}" \
        -X POST \
        -H "Content-Type: application/json" \
        -d "$payload" \
        "${API_BASE_URL}/auth/oauth/${provider}/callback")
    
    local http_code="${response: -3}"
    local body="${response%???}"
    
    if [ "$http_code" = "400" ]; then
        local success=$(echo "$body" | jq -r '.success // empty')
        local message=$(echo "$body" | jq -r '.message // empty')
        
        if [ "$success" = "false" ] && [[ "$message" == *"access_denied"* ]]; then
            assert_test "$test_name" "0" ""
            return 0
        else
            assert_test "$test_name" "1" "Expected error response structure not found"
            return 1
        fi
    else
        assert_test "$test_name" "1" "Expected HTTP 400, got $http_code"
        return 1
    fi
}

# Test OAuth Callback with Invalid Code
test_oauth_callback_invalid_code() {
    local provider="$1"
    local test_name="OAuth Callback Invalid Code - $provider"
    
    log_info "Testing $test_name..."
    
    local payload=$(cat <<EOF
{
    "code": "",
    "state": "test-state-123"
}
EOF
)
    
    local response=$(curl -s -w "%{http_code}" \
        -X POST \
        -H "Content-Type: application/json" \
        -d "$payload" \
        "${API_BASE_URL}/auth/oauth/${provider}/callback")
    
    local http_code="${response: -3}"
    
    if [ "$http_code" = "400" ]; then
        assert_test "$test_name" "0" ""
        return 0
    else
        assert_test "$test_name" "1" "Expected HTTP 400, got $http_code"
        return 1
    fi
}

# Test OAuth Link/Unlink without Authentication
test_oauth_auth_required() {
    local provider="$1"
    local test_name="OAuth Auth Required - $provider"
    
    log_info "Testing $test_name..."
    
    # Test link endpoint without auth
    local link_response=$(curl -s -w "%{http_code}" \
        -X POST \
        -H "Content-Type: application/json" \
        -d '{"code": "test-code", "state": "test-state"}' \
        "${API_BASE_URL}/auth/oauth/${provider}/link")
    
    local link_http_code="${link_response: -3}"
    
    # Test unlink endpoint without auth
    local unlink_response=$(curl -s -w "%{http_code}" \
        -X DELETE \
        "${API_BASE_URL}/auth/oauth/${provider}/unlink")
    
    local unlink_http_code="${unlink_response: -3}"
    
    if [ "$link_http_code" = "401" ] && [ "$unlink_http_code" = "401" ]; then
        assert_test "$test_name" "0" ""
        return 0
    else
        assert_test "$test_name" "1" "Expected HTTP 401 for both endpoints, got $link_http_code and $unlink_http_code"
        return 1
    fi
}

# Test Unsupported OAuth Provider
test_unsupported_provider() {
    local test_name="Unsupported OAuth Provider"
    local provider="Twitter"
    
    log_info "Testing $test_name..."
    
    local response=$(curl -s -w "%{http_code}" \
        "${API_BASE_URL}/auth/oauth/${provider}/authorize?redirectUri=http://localhost/callback")
    
    local http_code="${response: -3}"
    
    if [ "$http_code" = "400" ]; then
        assert_test "$test_name" "0" ""
        return 0
    else
        assert_test "$test_name" "1" "Expected HTTP 400, got $http_code"
        return 1
    fi
}

# Test OAuth Endpoints HTTP Methods
test_oauth_http_methods() {
    local provider="$1"
    local test_name="OAuth HTTP Methods - $provider"
    
    log_info "Testing $test_name..."
    
    # Test authorize endpoint with POST (should fail)
    local auth_post_response=$(curl -s -w "%{http_code}" \
        -X POST \
        "${API_BASE_URL}/auth/oauth/${provider}/authorize?redirectUri=http://localhost/callback")
    local auth_post_code="${auth_post_response: -3}"
    
    # Test callback endpoint with GET (should fail)
    local callback_get_response=$(curl -s -w "%{http_code}" \
        -X GET \
        "${API_BASE_URL}/auth/oauth/${provider}/callback")
    local callback_get_code="${callback_get_response: -3}"
    
    if [ "$auth_post_code" = "405" ] && [ "$callback_get_code" = "405" ]; then
        assert_test "$test_name" "0" ""
        return 0
    else
        assert_test "$test_name" "1" "Expected HTTP 405 for method not allowed, got $auth_post_code and $callback_get_code"
        return 1
    fi
}

# Test OAuth API Response Content-Type
test_oauth_content_type() {
    local provider="$1"
    local test_name="OAuth Content-Type - $provider"
    
    log_info "Testing $test_name..."
    
    local headers=$(curl -s -I \
        "${API_BASE_URL}/auth/oauth/${provider}/authorize?redirectUri=http://localhost/callback&state=test")
    
    if echo "$headers" | grep -q "Content-Type: application/json"; then
        assert_test "$test_name" "0" ""
        return 0
    else
        assert_test "$test_name" "1" "Expected application/json content type"
        return 1
    fi
}

# Main test execution
main() {
    log_info "Starting OAuth2 Flow E2E Tests for Smart Alarm"
    log_info "============================================="
    
    # Wait for services
    if ! wait_for_service "$API_BASE_URL/health" "Smart Alarm API"; then
        log_error "Smart Alarm API is not available. Exiting."
        exit 1
    fi
    
    log_info "Running OAuth2 tests..."
    echo ""
    
    # Test each supported provider
    for provider in "${OAUTH_PROVIDERS[@]}"; do
        log_info "Testing provider: $provider"
        echo "----------------------------------------"
        
        test_oauth_authorization_url "$provider"
        test_oauth_callback_error "$provider"
        test_oauth_callback_invalid_code "$provider"
        test_oauth_auth_required "$provider"
        test_oauth_http_methods "$provider"
        test_oauth_content_type "$provider"
        
        echo ""
    done
    
    # Test unsupported provider
    test_unsupported_provider
    
    echo ""
    log_info "OAuth2 E2E Test Results"
    log_info "========================"
    log_info "Total Tests: $TESTS_TOTAL"
    log_success "Passed: $TESTS_PASSED"
    
    if [ $TESTS_FAILED -gt 0 ]; then
        log_error "Failed: $TESTS_FAILED"
        echo ""
        log_error "Some OAuth2 tests failed. Please check the implementation."
        exit 1
    else
        echo ""
        log_success "All OAuth2 tests passed! 🎉"
        exit 0
    fi
}

# Execute tests
main "$@"