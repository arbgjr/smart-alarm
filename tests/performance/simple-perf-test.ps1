# Simple PowerShell performance test runner

param(
    [string]$BaseUrl = "https://example.com"
)

Write-Host "Running Simple Performance Tests" -ForegroundColor Green
Write-Host "Target URL: $BaseUrl" -ForegroundColor Cyan
Write-Host ""

# Check if Node.js is available
try {
    $nodeVersion = node --version 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Node.js is available: $nodeVersion" -ForegroundColor Green

        Write-Host "Running Node.js performance tests..." -ForegroundColor Yellow
        node api-performance.js $BaseUrl

        if ($LASTEXITCODE -eq 0) {
            Write-Host "Performance tests completed successfully!" -ForegroundColor Green
        } else {
            Write-Host "Performance tests completed with issues" -ForegroundColor Yellow
        }
    } else {
        Write-Host "Node.js is not available" -ForegroundColor Red
    }
} catch {
    Write-Host "Error running performance tests: $_" -ForegroundColor Red
}
