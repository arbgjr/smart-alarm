# ===================================
# OAuth2 Flow E2E Test - Smart Alarm
# ===================================

param(
    [string]$ApiBaseUrl = "http://localhost:8080/api/v1",
    [string]$FrontendBaseUrl = "http://localhost:3000"
)

$ErrorActionPreference = "Stop"

# OAuth Providers to test
$OAuthProviders = @("Google", "GitHub", "Facebook", "Microsoft")

# Test counters
$Script:TestsPassed = 0
$Script:TestsFailed = 0
$Script:TestsTotal = 0

# Logging functions
function Write-Info {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Blue
}

function Write-Success {
    param([string]$Message)
    Write-Host "[SUCCESS] $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

# Test assertion function
function Assert-Test {
    param(
        [string]$TestName,
        [bool]$Condition,
        [string]$Message = ""
    )
    
    $Script:TestsTotal++
    
    if ($Condition) {
        Write-Success "✓ $TestName"
        $Script:TestsPassed++
    } else {
        Write-Error "✗ $TestName$(if($Message) { ": $Message" })"
        $Script:TestsFailed++
    }
}

# Wait for service to be ready
function Wait-ForService {
    param(
        [string]$Url,
        [string]$ServiceName,
        [int]$MaxAttempts = 30
    )
    
    Write-Info "Waiting for $ServiceName to be ready..."
    
    for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
        try {
            $response = Invoke-WebRequest -Uri $Url -Method Get -TimeoutSec 5 -UseBasicParsing
            if ($response.StatusCode -eq 200) {
                Write-Success "$ServiceName is ready!"
                return $true
            }
        }
        catch {
            # Service not ready yet
        }
        
        Write-Host "." -NoNewline
        Start-Sleep -Seconds 2
    }
    
    Write-Host ""
    Write-Error "$ServiceName failed to start after $MaxAttempts attempts"
    return $false
}

# Test OAuth Authorization URL Generation
function Test-OAuthAuthorizationUrl {
    param([string]$Provider)
    
    $testName = "OAuth Authorization URL - $Provider"
    Write-Info "Testing $testName..."
    
    $redirectUri = "http://localhost:3000/auth/callback"
    $state = "test-state-$(Get-Date -Format 'yyyyMMddHHmmss')"
    
    try {
        $encodedRedirectUri = [System.Web.HttpUtility]::UrlEncode($redirectUri)
        $encodedState = [System.Web.HttpUtility]::UrlEncode($state)
        $url = "$ApiBaseUrl/auth/oauth/$Provider/authorize?redirectUri=$encodedRedirectUri&state=$encodedState"
        
        $response = Invoke-WebRequest -Uri $url -Method Get -UseBasicParsing
        
        if ($response.StatusCode -eq 200) {
            $body = $response.Content | ConvertFrom-Json
            
            $isValid = $body.authorizationUrl -and 
                      $body.provider -eq $Provider -and 
                      $body.state -eq $state
            
            if ($isValid) {
                Assert-Test $testName $true
                Write-Info "Authorization URL: $($body.authorizationUrl)"
                return $true
            } else {
                Assert-Test $testName $false "Invalid response structure"
                return $false
            }
        } else {
            Assert-Test $testName $false "HTTP $($response.StatusCode)"
            return $false
        }
    }
    catch {
        Assert-Test $testName $false $_.Exception.Message
        return $false
    }
}

# Test OAuth Callback with Error
function Test-OAuthCallbackError {
    param([string]$Provider)
    
    $testName = "OAuth Callback Error Handling - $Provider"
    Write-Info "Testing $testName..."
    
    $payload = @{
        code = ""
        state = "test-state-123"
        error = "access_denied"
        errorDescription = "User denied access"
    } | ConvertTo-Json
    
    try {
        $url = "$ApiBaseUrl/auth/oauth/$Provider/callback"
        $response = Invoke-WebRequest -Uri $url -Method Post -Body $payload -ContentType "application/json" -UseBasicParsing
        
        # This should fail, so if we get here, the test failed
        Assert-Test $testName $false "Expected error response, got success"
        return $false
    }
    catch {
        if ($_.Exception.Response.StatusCode -eq 400) {
            try {
                $errorBody = $_.Exception.Response | Get-Content | ConvertFrom-Json
                $isValidError = $errorBody.success -eq $false -and $errorBody.message -match "access_denied"
                
                Assert-Test $testName $isValidError $(if(-not $isValidError) { "Expected error response structure not found" })
                return $isValidError
            }
            catch {
                Assert-Test $testName $true "Got expected 400 error"
                return $true
            }
        } else {
            Assert-Test $testName $false "Expected HTTP 400, got $($_.Exception.Response.StatusCode)"
            return $false
        }
    }
}

# Test OAuth Callback with Invalid Code
function Test-OAuthCallbackInvalidCode {
    param([string]$Provider)
    
    $testName = "OAuth Callback Invalid Code - $Provider"
    Write-Info "Testing $testName..."
    
    $payload = @{
        code = ""
        state = "test-state-123"
    } | ConvertTo-Json
    
    try {
        $url = "$ApiBaseUrl/auth/oauth/$Provider/callback"
        $response = Invoke-WebRequest -Uri $url -Method Post -Body $payload -ContentType "application/json" -UseBasicParsing
        
        Assert-Test $testName $false "Expected error response, got success"
        return $false
    }
    catch {
        $isValid = $_.Exception.Response.StatusCode -eq 400
        Assert-Test $testName $isValid $(if(-not $isValid) { "Expected HTTP 400, got $($_.Exception.Response.StatusCode)" })
        return $isValid
    }
}

# Test OAuth Link/Unlink without Authentication
function Test-OAuthAuthRequired {
    param([string]$Provider)
    
    $testName = "OAuth Auth Required - $Provider"
    Write-Info "Testing $testName..."
    
    $linkPayload = @{
        code = "test-code"
        state = "test-state"
    } | ConvertTo-Json
    
    $linkUnauthorized = $false
    $unlinkUnauthorized = $false
    
    # Test link endpoint without auth
    try {
        $linkUrl = "$ApiBaseUrl/auth/oauth/$Provider/link"
        Invoke-WebRequest -Uri $linkUrl -Method Post -Body $linkPayload -ContentType "application/json" -UseBasicParsing
    }
    catch {
        $linkUnauthorized = $_.Exception.Response.StatusCode -eq 401
    }
    
    # Test unlink endpoint without auth
    try {
        $unlinkUrl = "$ApiBaseUrl/auth/oauth/$Provider/unlink"
        Invoke-WebRequest -Uri $unlinkUrl -Method Delete -UseBasicParsing
    }
    catch {
        $unlinkUnauthorized = $_.Exception.Response.StatusCode -eq 401
    }
    
    $isValid = $linkUnauthorized -and $unlinkUnauthorized
    Assert-Test $testName $isValid $(if(-not $isValid) { "Expected HTTP 401 for both endpoints" })
    return $isValid
}

# Test Unsupported OAuth Provider
function Test-UnsupportedProvider {
    $testName = "Unsupported OAuth Provider"
    $provider = "Twitter"
    
    Write-Info "Testing $testName..."
    
    try {
        $url = "$ApiBaseUrl/auth/oauth/$provider/authorize?redirectUri=http://localhost/callback"
        $response = Invoke-WebRequest -Uri $url -Method Get -UseBasicParsing
        
        Assert-Test $testName $false "Expected error response, got success"
        return $false
    }
    catch {
        $isValid = $_.Exception.Response.StatusCode -eq 400
        Assert-Test $testName $isValid $(if(-not $isValid) { "Expected HTTP 400, got $($_.Exception.Response.StatusCode)" })
        return $isValid
    }
}

# Test OAuth Endpoints HTTP Methods
function Test-OAuthHttpMethods {
    param([string]$Provider)
    
    $testName = "OAuth HTTP Methods - $Provider"
    Write-Info "Testing $testName..."
    
    $authPostFailed = $false
    $callbackGetFailed = $false
    
    # Test authorize endpoint with POST (should fail)
    try {
        $authUrl = "$ApiBaseUrl/auth/oauth/$Provider/authorize?redirectUri=http://localhost/callback"
        Invoke-WebRequest -Uri $authUrl -Method Post -UseBasicParsing
    }
    catch {
        $authPostFailed = $_.Exception.Response.StatusCode -eq 405
    }
    
    # Test callback endpoint with GET (should fail)
    try {
        $callbackUrl = "$ApiBaseUrl/auth/oauth/$Provider/callback"
        Invoke-WebRequest -Uri $callbackUrl -Method Get -UseBasicParsing
    }
    catch {
        $callbackGetFailed = $_.Exception.Response.StatusCode -eq 405
    }
    
    $isValid = $authPostFailed -and $callbackGetFailed
    Assert-Test $testName $isValid $(if(-not $isValid) { "Expected HTTP 405 for method not allowed" })
    return $isValid
}

# Test OAuth API Response Content-Type
function Test-OAuthContentType {
    param([string]$Provider)
    
    $testName = "OAuth Content-Type - $Provider"
    Write-Info "Testing $testName..."
    
    try {
        $url = "$ApiBaseUrl/auth/oauth/$Provider/authorize?redirectUri=http://localhost/callback&state=test"
        $response = Invoke-WebRequest -Uri $url -Method Get -UseBasicParsing
        
        if ($response.StatusCode -eq 200) {
            $contentType = $response.Headers["Content-Type"]
            $isValid = $contentType -match "application/json"
            Assert-Test $testName $isValid $(if(-not $isValid) { "Expected application/json content type, got $contentType" })
            return $isValid
        } else {
            Assert-Test $testName $false "Request failed with status $($response.StatusCode)"
            return $false
        }
    }
    catch {
        Assert-Test $testName $false $_.Exception.Message
        return $false
    }
}

# Main test execution
function Main {
    Write-Info "Starting OAuth2 Flow E2E Tests for Smart Alarm"
    Write-Info "============================================="
    
    # Load System.Web for URL encoding
    Add-Type -AssemblyName System.Web
    
    # Wait for services
    if (-not (Wait-ForService "$ApiBaseUrl/health" "Smart Alarm API")) {
        Write-Error "Smart Alarm API is not available. Exiting."
        exit 1
    }
    
    Write-Info "Running OAuth2 tests..."
    Write-Host ""
    
    # Test each supported provider
    foreach ($provider in $OAuthProviders) {
        Write-Info "Testing provider: $provider"
        Write-Host "----------------------------------------"
        
        Test-OAuthAuthorizationUrl $provider
        Test-OAuthCallbackError $provider
        Test-OAuthCallbackInvalidCode $provider
        Test-OAuthAuthRequired $provider
        Test-OAuthHttpMethods $provider
        Test-OAuthContentType $provider
        
        Write-Host ""
    }
    
    # Test unsupported provider
    Test-UnsupportedProvider
    
    Write-Host ""
    Write-Info "OAuth2 E2E Test Results"
    Write-Info "========================"
    Write-Info "Total Tests: $Script:TestsTotal"
    Write-Success "Passed: $Script:TestsPassed"
    
    if ($Script:TestsFailed -gt 0) {
        Write-Error "Failed: $Script:TestsFailed"
        Write-Host ""
        Write-Error "Some OAuth2 tests failed. Please check the implementation."
        exit 1
    } else {
        Write-Host ""
        Write-Success "All OAuth2 tests passed! 🎉"
        exit 0
    }
}

# Execute tests
Main