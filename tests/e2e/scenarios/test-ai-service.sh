#!/bin/bash

echo "Cenário: Integração com AI-Service"

# Sucesso: Envio de arquivo de áudio para análise
echo "Teste: sucesso na análise de áudio"
curl -s -X POST "http://localhost:5000/api/ai/analyze" \
  -H "Content-Type: audio/wav" \
  --data-binary "../../data/audio-test.wav" | tee /tmp/ai-success.json

# Erro: Envio de arquivo inválido
echo "Teste: erro ao enviar arquivo inválido"
curl -s -X POST "http://localhost:5000/api/ai/analyze" \
  -H "Content-Type: audio/wav" \
  --data-binary "../../data/image-test.png" | tee /tmp/ai-error.json

# Timeout: Simulação de serviço indisponível
echo "Teste: timeout do AI-Service (serviço parado)"
docker stop ai-service-mock 2>/dev/null
timeout 5 curl -s -X POST "http://localhost:5000/api/ai/analyze" \
  -H "Content-Type: audio/wav" \
  --data-binary "../../data/audio-test.wav" || echo "Timeout ou erro de conexão"
docker start ai-service-mock 2>/dev/null

# Resposta inválida: Simulação
echo "Teste: resposta inválida do AI-Service (mock)"
curl -s -X POST "http://localhost:5000/api/ai/analyze" \
  -H "Content-Type: application/json" \
  -d '{"invalid":true}' | tee /tmp/ai-invalid.json

# Logging: Verificar logs da integração
echo "Verificando logs da integração..."
docker logs ai-service-mock --tail 20

echo "Cenário de integração com AI-Service finalizado."
