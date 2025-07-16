# Inicializa todos os serviços necessários para os testes E2E
Write-Host "Iniciando serviços: AlarmService, AI-Service, IntegrationService, Storage, Usuários..."
# Inicializa todos os serviços necessários para os testes E2E

Write-Host "Subindo containers com Docker Compose..."
docker-compose -f "../../docker-compose.yml" up -d

Write-Host "Aguardando inicialização dos serviços..."
Start-Sleep -Seconds 10

# Health check AlarmService
Write-Host "Verificando AlarmService..."
try {
    $response = Invoke-RestMethod -Uri "http://localhost:5000/api/health"
    Write-Host "AlarmService OK"
} catch {
    Write-Host "AlarmService indisponível" -ForegroundColor Red
}

# Health check AI-Service
Write-Host "Verificando AI-Service..."
try {
    $response = Invoke-RestMethod -Uri "http://localhost:5001/api/health"
    Write-Host "AI-Service OK"
} catch {
    Write-Host "AI-Service indisponível" -ForegroundColor Red
}

# Health check IntegrationService
Write-Host "Verificando IntegrationService..."
try {
    $response = Invoke-RestMethod -Uri "http://localhost:5002/api/health"
    Write-Host "IntegrationService OK"
} catch {
    Write-Host "IntegrationService indisponível" -ForegroundColor Red
}

# Health check Storage (MinIO)
Write-Host "Verificando Storage (MinIO)..."
try {
    $response = Invoke-RestMethod -Uri "http://localhost:9000/minio/health/live"
    Write-Host "Storage OK"
} catch {
    Write-Host "Storage indisponível" -ForegroundColor Red
}

# Inicializar mocks/stubs (exemplo: mock-server)
Write-Host "Inicializando mocks/stubs..."
docker-compose -f \"../../docker-compose.yml\" up -d mock-server

Write-Host "Setup do ambiente de testes E2E finalizado."