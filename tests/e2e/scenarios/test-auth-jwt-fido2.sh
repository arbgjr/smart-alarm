#!/bin/bash
echo "Simulando autenticação FIDO2 (mock)..."

echo "Cenário: Autenticação JWT/FIDO2 e RBAC"

# Login JWT - sucesso
echo "Teste: login JWT válido"
TOKEN=$(curl -s -X POST "http://localhost:5000/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username":"user1","password":"senha123"}' | jq -r .token)
echo "Token recebido: $TOKEN"

# Teste de acesso autorizado
echo "Teste: acesso autorizado com JWT"
curl -s -X GET "http://localhost:5000/api/alarmes" -H "Authorization: Bearer $TOKEN" | tee /tmp/auth-success.json

# Teste de acesso não autorizado
echo "Teste: acesso não autorizado (sem token)"
curl -s -X GET "http://localhost:5000/api/alarmes" | tee /tmp/auth-unauth.json

# Teste de expiração de token (simulação)
echo "Teste: expiração de token (mock)"
EXPIRED_TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE2MDAwMDAwMDB9.signature"
curl -s -X GET "http://localhost:5000/api/alarmes" -H "Authorization: Bearer $EXPIRED_TOKEN" | tee /tmp/auth-expired.json

# Teste de erro de autenticação
echo "Teste: login JWT inválido"
curl -s -X POST "http://localhost:5000/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username":"user1","password":"senhaErrada"}' | tee /tmp/auth-error.json

# MFA (mock)
echo "Teste: autenticação multifator (FIDO2/WebAuthn - mock)"
echo "Simulando MFA..."
curl -s -X POST "http://localhost:5000/api/auth/mfa" \
  -H "Content-Type: application/json" \
  -d '{"username":"user1","mfaCode":"123456"}' | tee /tmp/auth-mfa.json

# Logging: Verificar logs de autenticação
echo "Verificando logs de autenticação..."
docker logs alarmservice --tail 20

echo "Cenário de autenticação JWT/FIDO2 e RBAC finalizado."
