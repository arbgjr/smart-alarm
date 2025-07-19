# Executa todos os cenários E2E Smart Alarm
Write-Host "Preparando ambiente de testes E2E..."

# 1. Setup dos serviços
& ./helpers/setup-services.ps1

# 2. Geração de dados de teste
& ./helpers/generate-test-data.ps1

# 3. Execução dos cenários
Get-ChildItem ./scenarios/*.ps1 | ForEach-Object {
    Write-Host "Executando cenário: $($_.Name)"
    & $_.FullName
}

# 4. Teardown (opcional)
& ./helpers/teardown-services.ps1

Write-Host "Testes E2E finalizados."
