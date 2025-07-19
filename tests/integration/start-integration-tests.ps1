# Script para executar testes de integração com containers
# Inicia containers necessários para testes de integração

Write-Host "🚀 Iniciando containers para testes de integração..." -ForegroundColor Green

# Verificar se Docker está rodando
try {
    docker info | Out-Null
}
catch {
    Write-Host "❌ Docker não está rodando. Inicie o Docker Desktop primeiro." -ForegroundColor Red
    exit 1
}

# Navegar para o diretório raiz
Set-Location (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent)

Write-Host "📍 Diretório atual: $(Get-Location)" -ForegroundColor Yellow

# Parar containers existentes (se houver)
Write-Host "🛑 Parando containers existentes..." -ForegroundColor Yellow
docker-compose down 2>$null

# Iniciar apenas os containers essenciais para testes
Write-Host "🔧 Iniciando containers essenciais..." -ForegroundColor Cyan
docker-compose up -d vault postgres rabbitmq minio

# Aguardar containers iniciarem (health checks)
Write-Host "⏳ Aguardando containers iniciarem..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

# Verificar status dos containers
Write-Host "🔍 Verificando status dos containers..." -ForegroundColor Cyan
docker-compose ps

# Verificar conectividade
Write-Host "🔗 Testando conectividade..." -ForegroundColor Cyan

# Vault
try {
    $vaultStatus = Invoke-RestMethod -Uri "http://localhost:8200/v1/sys/health" -Method GET
    Write-Host "✅ Vault: OK" -ForegroundColor Green
} catch {
    Write-Host "❌ Vault: Falha na conectividade" -ForegroundColor Red
}

# PostgreSQL (usando docker exec)
try {
    docker exec postgres pg_isready -h localhost -p 5432 | Out-Null
    Write-Host "✅ PostgreSQL: OK" -ForegroundColor Green
} catch {
    Write-Host "❌ PostgreSQL: Falha na conectividade" -ForegroundColor Red
}

# RabbitMQ
try {
    $rabbitStatus = Invoke-RestMethod -Uri "http://localhost:15672/api/overview" -Headers @{Authorization="Basic $(([Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes('guest:guest'))))"} -Method GET
    Write-Host "✅ RabbitMQ: OK" -ForegroundColor Green
} catch {
    Write-Host "❌ RabbitMQ: Falha na conectividade" -ForegroundColor Red
}

# MinIO
try {
    $minioResponse = Invoke-WebRequest -Uri "http://localhost:9000/minio/health/live" -Method GET
    Write-Host "✅ MinIO: OK" -ForegroundColor Green
} catch {
    Write-Host "❌ MinIO: Falha na conectividade" -ForegroundColor Red
}

Write-Host "`n🎯 Containers prontos para testes de integração!" -ForegroundColor Green
Write-Host "📋 Para executar os testes:" -ForegroundColor Yellow
Write-Host "   dotnet test --filter Category=Integration --logger `"console;verbosity=detailed`"" -ForegroundColor Cyan

Write-Host "`n🛑 Para parar os containers após os testes:" -ForegroundColor Yellow  
Write-Host "   docker-compose down" -ForegroundColor Cyan
