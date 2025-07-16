#!/bin/bash
# Executa todos os cenários E2E Smart Alarm
set -e

echo "Preparando ambiente de testes E2E..."

# 1. Setup dos serviços
bash ./helpers/setup-services.sh

# 2. Geração de dados de teste
bash ./helpers/generate-test-data.sh

# 3. Execução dos cenários
for scenario in ./scenarios/*.sh; do
  echo "Executando cenário: $(basename "$scenario")"
  bash "$scenario"
done

# 4. Teardown (opcional)
bash ./helpers/teardown-services.sh

echo "Testes E2E finalizados."
