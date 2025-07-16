#!/bin/bash
# Inicializa todos os serviços necessários para os testes E2E
echo "Iniciando serviços: AlarmService, AI-Service, IntegrationService, Storage, Usuários..."
# Inicializa todos os serviços necessários para os testes E2E

echo "Subindo containers com Docker Compose..."
docker-compose -f ../../docker-compose.yml up -d

echo "Aguardando inicialização dos serviços..."
sleep 10

# Health check AlarmService
echo "Verificando AlarmService..."
if curl -sSf http://localhost:5000/api/health > /dev/null; then
  echo "AlarmService OK"
else
  echo "AlarmService indisponível"
fi

# Health check AI-Service
echo "Verificando AI-Service..."
if curl -sSf http://localhost:5001/api/health > /dev/null; then
  echo "AI-Service OK"
else
  echo "AI-Service indisponível"
fi

# Health check IntegrationService
echo "Verificando IntegrationService..."
if curl -sSf http://localhost:5002/api/health > /dev/null; then
  echo "IntegrationService OK"
else
  echo "IntegrationService indisponível"
fi

# Health check Storage (MinIO)
echo "Verificando Storage (MinIO)..."
if curl -sSf http://localhost:9000/minio/health/live > /dev/null; then
  echo "Storage OK"
else
  echo "Storage indisponível"
fi

# Inicializar mocks/stubs (exemplo: mock-server)
echo "Inicializando mocks/stubs..."
docker-compose -f ../../docker-compose.yml up -d mock-server

echo "Setup do ambiente de testes E2E finalizado."