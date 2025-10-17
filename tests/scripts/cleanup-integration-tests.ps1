#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Cleans up integration test environment containers
.DESCRIPTION
    This script stops and removes all containers created for integration testing
.EXAMPLE
    .\cleanup-integration-tests.ps1
#>

param(
    [switch]$Force
)

$ErrorActionPreference = "Stop"

Write-Host "🧹 Cleaning up Smart Alarm Integration Test Environment" -ForegroundColor Yellow

$containers = @(
    "smartalarm-test-postgres",
    "smartalarm-test-redis",
    "smartalarm-test-minio",
    "smartalarm-test-rabbitmq",
    "smartalarm-test-vault"
)

foreach ($container in $containers) {
    try {
        $exists = docker ps -a --filter "name=$container" --format "{{.Names}}" 2>$null
        if ($exists -eq $container) {
            Write-Host "🛑 Stopping and removing $container..." -ForegroundColor Yellow
            docker stop $container 2>$null | Out-Null
            docker rm $container 2>$null | Out-Null
            Write-Host "✅ Removed $container" -ForegroundColor Green
        } else {
            Write-Host "ℹ️  Container $container not found" -ForegroundColor Gray
        }
    } catch {
        Write-Warning "⚠️  Failed to remove $container`: $($_.Exception.Message)"
    }
}

# Clean up any dangling volumes if Force is specified
if ($Force) {
    Write-Host "🗑️  Cleaning up dangling volumes..." -ForegroundColor Yellow
    try {
        docker volume prune -f | Out-Null
        Write-Host "✅ Cleaned up dangling volumes" -ForegroundColor Green
    } catch {
        Write-Warning "⚠️  Failed to clean up volumes: $($_.Exception.Message)"
    }
}

Write-Host ""
Write-Host "✨ Integration test environment cleanup completed!" -ForegroundColor Green
