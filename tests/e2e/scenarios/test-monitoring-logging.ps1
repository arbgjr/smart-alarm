
Write-Host "Cenário: Monitoramento e Logging Estruturado"

# Verificação de logs estruturados (Serilog)
Write-Host "Teste: verificação de logs estruturados (Serilog)"
docker logs alarmservice --tail 30 | Out-File -FilePath "../../data/logs-serilog.txt"

# Verificação de rastreamento de operações (Application Insights)
Write-Host "Teste: verificação de rastreamento de operações (mock)"
$traces = Invoke-RestMethod -Uri "http://localhost:5000/api/monitoring/traces" -Method Get
$traces | ConvertTo-Json | Out-File -FilePath "../../data/monitoring-traces.json"

# Simulação de falha e validação de registro
Write-Host "Teste: simulação de falha e registro de erro"
Invoke-RestMethod -Uri "http://localhost:5000/api/alarmes" -Method Post -Body (@{ hora = ""; recorrencia = ""; usuario = "user1" } | ConvertTo-Json) -ContentType "application/json"

Write-Host "Cenário de monitoramento e logging finalizado."
