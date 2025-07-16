
Write-Host "Cenário: Integração com Serviços Externos via IntegrationService"

# Envio de notificação push
Write-Host "Teste: envio de notificação push"
$push = Invoke-RestMethod -Uri "http://localhost:5000/api/integration/notify" -Method Post -Body (@{ userId = "user1"; type = "push"; message = "Teste de notificação" } | ConvertTo-Json) -ContentType "application/json"
$push | ConvertTo-Json | Out-File -FilePath "../../data/integration-push.json"

# Envio de notificação email
Write-Host "Teste: envio de notificação email"
$email = Invoke-RestMethod -Uri "http://localhost:5000/api/integration/notify" -Method Post -Body (@{ userId = "user2"; type = "email"; message = "Teste de email" } | ConvertTo-Json) -ContentType "application/json"
$email | ConvertTo-Json | Out-File -FilePath "../../data/integration-email.json"

# Integração com API de terceiros (mock)
Write-Host "Teste: integração com API de terceiros (mock)"
$external = Invoke-RestMethod -Uri "http://localhost:5000/api/integration/external" -Method Post -Body (@{ service = "calendar"; action = "add"; data = @{ event = "Reunião" } } | ConvertTo-Json) -ContentType "application/json"
$external | ConvertTo-Json | Out-File -FilePath "../../data/integration-external.json"

# Erro de integração (serviço parado)
Write-Host "Teste: erro de integração (serviço parado)"
docker stop integrationservice-mock
try {
    Invoke-RestMethod -Uri "http://localhost:5000/api/integration/notify" -Method Post -Body (@{ userId = "user1"; type = "push"; message = "Teste de erro" } | ConvertTo-Json) -ContentType "application/json"
} catch {
    Write-Host "Erro de conexão com IntegrationService"
}
docker start integrationservice-mock

# Fallback (simulação)
Write-Host "Teste: fallback de integração (mock)"
$fallback = Invoke-RestMethod -Uri "http://localhost:5000/api/integration/notify" -Method Post -Body (@{ userId = "user1"; type = "sms"; message = "Fallback de SMS" } | ConvertTo-Json) -ContentType "application/json"
$fallback | ConvertTo-Json | Out-File -FilePath "../../data/integration-fallback.json"

# Logging: Verificar logs da integração
Write-Host "Verificando logs da integração..."
docker logs integrationservice-mock --tail 20 | Out-File -FilePath "../../data/integration-logs.txt"

Write-Host "Cenário de integração com serviços externos finalizado."
