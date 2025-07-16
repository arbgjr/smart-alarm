#!/bin/bash
echo "Verificando logs estruturados..."
echo "Verificando rastreamento de operações..."

echo "Cenário: Monitoramento e Logging Estruturado"

# Verificação de logs estruturados (Serilog)
echo "Teste: verificação de logs estruturados (Serilog)"
docker logs alarmservice --tail 30 | tee /tmp/logs-serilog.txt

# Verificação de rastreamento de operações (Application Insights)
echo "Teste: verificação de rastreamento de operações (mock)"
curl -s -X GET "http://localhost:5000/api/monitoring/traces" | tee /tmp/monitoring-traces.json

# Simulação de falha e validação de registro
echo "Teste: simulação de falha e registro de erro"
curl -s -X POST "http://localhost:5000/api/alarmes" \
  -H "Content-Type: application/json" \
  -d '{"hora":"","recorrencia":"","usuario":"user1"}' | tee /tmp/logs-falha.json

echo "Cenário de monitoramento e logging finalizado."
