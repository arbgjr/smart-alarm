#!/bin/bash
echo "Simulando integração com API de terceiros (mock)..."

echo "Cenário: Integração com Serviços Externos via IntegrationService"

# Envio de notificação push
echo "Teste: envio de notificação push"
curl -s -X POST "http://localhost:5000/api/integration/notify" \
  -H "Content-Type: application/json" \
  -d '{"userId":"user1","type":"push","message":"Teste de notificação"}' | tee /tmp/integration-push.json

# Envio de notificação email
echo "Teste: envio de notificação email"
curl -s -X POST "http://localhost:5000/api/integration/notify" \
  -H "Content-Type: application/json" \
  -d '{"userId":"user2","type":"email","message":"Teste de email"}' | tee /tmp/integration-email.json

# Integração com API de terceiros (mock)
echo "Teste: integração com API de terceiros (mock)"
curl -s -X POST "http://localhost:5000/api/integration/external" \
  -H "Content-Type: application/json" \
  -d '{"service":"calendar","action":"add","data":{"event":"Reunião"}}' | tee /tmp/integration-external.json

# Erro de integração (serviço parado)
echo "Teste: erro de integração (serviço parado)"
docker stop integrationservice-mock 2>/dev/null
curl -s -X POST "http://localhost:5000/api/integration/notify" \
  -H "Content-Type: application/json" \
  -d '{"userId":"user1","type":"push","message":"Teste de erro"}' || echo "Erro de conexão com IntegrationService"
docker start integrationservice-mock 2>/dev/null

# Fallback (simulação)
echo "Teste: fallback de integração (mock)"
curl -s -X POST "http://localhost:5000/api/integration/notify" \
  -H "Content-Type: application/json" \
  -d '{"userId":"user1","type":"sms","message":"Fallback de SMS"}' | tee /tmp/integration-fallback.json

# Logging: Verificar logs da integração
echo "Verificando logs da integração..."
docker logs integrationservice-mock --tail 20

echo "Cenário de integração com serviços externos finalizado."
