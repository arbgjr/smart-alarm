# PowerShell script to run performance tests

param(
    [string]$BaseUrl = "https://example.com",
    [string]$TestType = "all",
    [int]$Duration = 300,
    [int]$Users = 10
)

Write-Host "Smart Alarm Performance Test Runner" -ForegroundColor Green
Write-Host "===================================" -ForegroundColor Green
Write-Host ""

# Check if k6 is installed
$k6Available = $false
try {
    $k6Version = k6 version 2>$null
    if ($LASTEXITCODE -eq 0) {
        $k6Available = $true
        Write-Host "✓ k6 is available: $k6Version" -ForegroundColor Green
    }
} catch {
    Write-Host "⚠ k6 is not installed" -ForegroundColor Yellow
}

# Check if Node.js is available
$nodeAvailable = $false
try {
    $nodeVersion = node --version 2>$null
    if ($LASTEXITCODE -eq 0) {
        $nodeAvailable = $true
        Write-Host "✓ Node.js is available: $nodeVersion" -ForegroundColor Green
    }
} catch {
    Write-Host "⚠ Node.js is not installed" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Target URL: $BaseUrl" -ForegroundColor Cyan
Write-Host "Test Type: $TestType" -ForegroundColor Cyan
Write-Host ""

# Function to run Node.js performance tests
function Run-NodePerformanceTests {
    Write-Host "Running Node.js API Performance Tests..." -ForegroundColor Yellow

    try {
        node api-performance.js $BaseUrl
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ Node.js performance tests completed successfully" -ForegroundColor Green
        } else {
            Write-Host "✗ Node.js performance tests failed" -ForegroundColor Red
        }
    } catch {
        Write-Host "✗ Failed to run Node.js performance tests: $_" -ForegroundColor Red
    }
}

# Function to run k6 load tests
function Run-K6LoadTests {
    Write-Host "Running k6 Load Tests..." -ForegroundColor Yellow

    $env:BASE_URL = $BaseUrl

    try {
        k6 run --duration ${Duration}s --vus $Users load-test.js
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ k6 load tests completed successfully" -ForegroundColor Green
        } else {
            Write-Host "✗ k6 load tests failed" -ForegroundColor Red
        }
    } catch {
        Write-Host "✗ Failed to run k6 load tests: $_" -ForegroundColor Red
    }
}

# Function to run k6 stress tests
function Run-K6StressTests {
    Write-Host "Running k6 Stress Tests..." -ForegroundColor Yellow

    $env:BASE_URL = $BaseUrl

    try {
        k6 run stress-test.js
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ k6 stress tests completed successfully" -ForegroundColor Green
        } else {
            Write-Host "✗ k6 stress tests failed" -ForegroundColor Red
        }
    } catch {
        Write-Host "✗ Failed to run k6 stress tests: $_" -ForegroundColor Red
    }
}

# Function to run k6 spike tests
function Run-K6SpikeTests {
    Write-Host "Running k6 Spike Tests..." -ForegroundColor Yellow

    $env:BASE_URL = $BaseUrl

    try {
        k6 run spike-test.js
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ k6 spike tests completed successfully" -ForegroundColor Green
        } else {
            Write-Host "✗ k6 spike tests failed" -ForegroundColor Red
        }
    } catch {
        Write-Host "✗ Failed to run k6 spike tests: $_" -ForegroundColor Red
    }
}

# Main execution logic
switch ($TestType.ToLower()) {
    "node" {
        if ($nodeAvailable) {
            Run-NodePerformanceTests
        } else {
            Write-Host "✗ Node.js is required for this test type" -ForegroundColor Red
            exit 1
        }
    }
    "load" {
        if ($k6Available) {
            Run-K6LoadTests
        } else {
            Write-Host "✗ k6 is required for load tests" -ForegroundColor Red
            exit 1
        }
    }
    "stress" {
        if ($k6Available) {
            Run-K6StressTests
        } else {
            Write-Host "✗ k6 is required for stress tests" -ForegroundColor Red
            exit 1
        }
    }
    "spike" {
        if ($k6Available) {
            Run-K6SpikeTests
        } else {
            Write-Host "✗ k6 is required for spike tests" -ForegroundColor Red
            exit 1
        }
    }
    "all" {
        $testsPassed = 0
        $totalTests = 0

        # Run Node.js tests if available
        if ($nodeAvailable) {
            $totalTests++
            Write-Host "--- Running Node.js Performance Tests ---" -ForegroundColor Magenta
            Run-NodePerformanceTests
            if ($LASTEXITCODE -eq 0) { $testsPassed++ }
            Write-Host ""
        }

        # Run k6 tests if available
        if ($k6Available) {
            $totalTests += 3

            Write-Host "--- Running k6 Load Tests ---" -ForegroundColor Magenta
            Run-K6LoadTests
            if ($LASTEXITCODE -eq 0) { $testsPassed++ }
            Write-Host ""

            Write-Host "--- Running k6 Stress Tests ---" -ForegroundColor Magenta
            Run-K6StressTests
            if ($LASTEXITCODE -eq 0) { $testsPassed++ }
            Write-Host ""

            Write-Host "--- Running k6 Spike Tests ---" -ForegroundColor Magenta
            Run-K6SpikeTests
            if ($LASTEXITCODE -eq 0) { $testsPassed++ }
            Write-Host ""
        }

        # Summary
        Write-Host "=== PERFORMANCE TEST SUMMARY ===" -ForegroundColor Green
        Write-Host "Tests passed: $testsPassed/$totalTests" -ForegroundColor Cyan

        if ($testsPassed -eq $totalTests -and $totalTests -gt 0) {
            Write-Host "✓ All performance tests passed!" -ForegroundColor Green
            exit 0
        } elseif ($totalTests -eq 0) {
            Write-Host "⚠ No performance testing tools available" -ForegroundColor Yellow
            Write-Host "Install k6 (https://k6.io/docs/getting-started/installation/) or Node.js to run performance tests" -ForegroundColor Yellow
            exit 1
        } else {
            Write-Host "✗ Some performance tests failed" -ForegroundColor Red
            exit 1
        }
    }
    default {
        Write-Host "Invalid test type: $TestType" -ForegroundColor Red
        Write-Host "Valid options: node, load, stress, spike, all" -ForegroundColor Yellow
        exit 1
    }
}

Write-Host ""
Write-Host "Performance tests completed!" -ForegroundColor Green
