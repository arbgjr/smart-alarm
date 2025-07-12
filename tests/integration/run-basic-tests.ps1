# Script para executar APENAS testes básicos (sem containers)
# Estes testes não dependem de infraestrutura externa

Write-Host "🧪 Executando testes básicos (sem containers)..." -ForegroundColor Green

# Navegar para o diretório de testes
Set-Location (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent)

Write-Host "📍 Executando no diretório: $(Get-Location)" -ForegroundColor Yellow

# Executar testes que NÃO precisam de containers
Write-Host "🔒 Executando testes de segurança OWASP..." -ForegroundColor Cyan
dotnet test tests/integration/BasicOwaspSecurityTests.cs --logger "console;verbosity=detailed" --no-build

Write-Host "`n🧩 Executando testes de componentes unitários..." -ForegroundColor Cyan  
dotnet test tests/integration/BasicSecurityComponentsTests.cs --logger "console;verbosity=detailed" --no-build

# Executar todos os testes básicos juntos
Write-Host "`n📊 Executando relatório de cobertura..." -ForegroundColor Cyan
dotnet test tests/integration/ --collect:"XPlat Code Coverage" --settings:tests/integration/coverlet.runsettings --logger "console;verbosity=detailed" --no-build

Write-Host "`n✅ Testes básicos concluídos!" -ForegroundColor Green
Write-Host "📋 Estes testes rodam sem dependências externas." -ForegroundColor Yellow
Write-Host "🐳 Para testes completos de integração, execute: .\tests\integration\start-integration-tests.ps1" -ForegroundColor Cyan
