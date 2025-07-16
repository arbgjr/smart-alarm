# Cenário: Integração com AI-Service
Write-Host "Cenário: Integração com AI-Service"

# Sucesso: Envio de arquivo de áudio para análise
Write-Host "Teste: sucesso na análise de áudio"
$aiSuccess = Invoke-RestMethod -Uri "http://localhost:5000/api/ai/analyze" -Method Post -InFile "../../data/audio-test.wav" -ContentType "audio/wav"
$aiSuccess | ConvertTo-Json | Out-File -FilePath "../../data/ai-success.json"

# Erro: Envio de arquivo inválido
Write-Host "Teste: erro ao enviar arquivo inválido"
try {
    $aiErrorResult = Invoke-RestMethod -Uri "http://localhost:5000/api/ai/analyze" -Method Post -InFile "../../data/image-test.png" -ContentType "audio/wav"
    $aiErrorResult | ConvertTo-Json | Out-File -FilePath "../../data/ai-error.json"
} catch {
    Write-Host "Erro esperado ao enviar arquivo inválido."
}

# Timeout: Simulação de serviço indisponível
Write-Host "Teste: timeout do AI-Service (serviço parado)"
docker stop ai-service-mock
try {
    $aiTimeoutResult = Invoke-RestMethod -Uri "http://localhost:5000/api/ai/analyze" -Method Post -InFile "../../data/audio-test.wav" -ContentType "audio/wav"
} catch {
    Write-Host "Timeout ou erro de conexão"
}
docker start ai-service-mock

# Resposta inválida: Simulação
Write-Host "Teste: resposta inválida do AI-Service (mock)"
$aiInvalid = Invoke-RestMethod -Uri "http://localhost:5000/api/ai/analyze" -Method Post -Body (@{ invalid = $true } | ConvertTo-Json) -ContentType "application/json"
$aiInvalid | ConvertTo-Json | Out-File -FilePath "../../data/ai-invalid.json"

# Logging: Verificar logs da integração
Write-Host "Verificando logs da integração..."
docker logs ai-service-mock --tail 20 | Out-File -FilePath "../../data/ai-logs.txt"

Write-Host "Cenário de integração com AI-Service finalizado."