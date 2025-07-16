# Cenário: Autenticação (JWT/FIDO2) e Autorização (RBAC)
Write-Host "Cenário: Autenticação JWT/FIDO2 e RBAC"

# Login JWT - sucesso
Write-Host "Teste: login JWT válido"
$jwtResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/auth/login" -Method Post -Body (@{ username = "user1"; password = "senha123" } | ConvertTo-Json) -ContentType "application/json"
$token = $jwtResponse.token
Write-Host "Token recebido: $token"

# Teste de acesso autorizado
Write-Host "Teste: acesso autorizado com JWT"
$authSuccess = Invoke-RestMethod -Uri "http://localhost:5000/api/alarmes" -Method Get -Headers @{ Authorization = "Bearer $token" }
$authSuccess | ConvertTo-Json | Out-File -FilePath "../../data/auth-success.json"

# Teste de acesso não autorizado
Write-Host "Teste: acesso não autorizado (sem token)"
try {
    $authUnauth = Invoke-RestMethod -Uri "http://localhost:5000/api/alarmes" -Method Get
    $authUnauth | ConvertTo-Json | Out-File -FilePath "../../data/auth-unauth.json"
} catch {
    Write-Host "Acesso negado conforme esperado."
}

# Teste de expiração de token (simulação)
Write-Host "Teste: expiração de token (mock)"
$expiredToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE2MDAwMDAwMDB9.signature"
try {
    $authExpired = Invoke-RestMethod -Uri "http://localhost:5000/api/alarmes" -Method Get -Headers @{ Authorization = "Bearer $expiredToken" }
    $authExpired | ConvertTo-Json | Out-File -FilePath "../../data/auth-expired.json"
} catch {
    Write-Host "Token expirado conforme esperado."
}

# Teste de erro de autenticação
Write-Host "Teste: login JWT inválido"
try {
    $authErrorResult = Invoke-RestMethod -Uri "http://localhost:5000/api/auth/login" -Method Post -Body (@{ username = "user1"; password = "senhaErrada" } | ConvertTo-Json) -ContentType "application/json"
    $authErrorResult | ConvertTo-Json | Out-File -FilePath "../../data/auth-error.json"
} catch {
    Write-Host "Erro de autenticação conforme esperado."
}

# MFA (mock)
Write-Host "Teste: autenticação multifator (FIDO2/WebAuthn - mock)"
$mfa = Invoke-RestMethod -Uri "http://localhost:5000/api/auth/mfa" -Method Post -Body (@{ username = "user1"; mfaCode = "123456" } | ConvertTo-Json) -ContentType "application/json"
$mfa | ConvertTo-Json | Out-File -FilePath "../../data/auth-mfa.json"

# Logging: Verificar logs de autenticação
Write-Host "Verificando logs de autenticação..."
docker logs alarmservice --tail 20 | Out-File -FilePath "../../data/auth-logs.txt"

Write-Host "Cenário de autenticação JWT/FIDO2 e RBAC finalizado."