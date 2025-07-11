#!/bin/bash

# Define cores para melhor legibilidade
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${BLUE}=== Smart Alarm - Testes de Integração do MinIO ===${NC}"

# Verificar se o Docker está rodando
if ! docker info > /dev/null 2>&1; then
    echo -e "${RED}Docker não está rodando. Por favor, inicie o Docker e tente novamente.${NC}"
    exit 1
fi

# Verificar qual comando docker-compose está disponível
if command -v docker-compose &> /dev/null; then
    DOCKER_COMPOSE_CMD="docker-compose"
else
    DOCKER_COMPOSE_CMD="docker compose"
fi

# Verificar se o MinIO está rodando
echo -e "${YELLOW}Verificando se o MinIO está rodando...${NC}"
MINIO_RUNNING=true

if ! docker ps | grep -q -E "minio(-1)?$"; then
    echo -e "${YELLOW}O MinIO não está rodando. Inicializando...${NC}"
    MINIO_RUNNING=false
fi

if [ "$MINIO_RUNNING" = false ]; then
    # Iniciar MinIO
    echo -e "${YELLOW}Iniciando MinIO...${NC}"
    $DOCKER_COMPOSE_CMD up -d minio
    
    # Aguardar para o MinIO inicializar
    echo -e "${YELLOW}Aguardando o MinIO inicializar (10s)...${NC}"
    sleep 10
fi

# Verificar saúde do MinIO
echo -e "${YELLOW}Verificando saúde do MinIO...${NC}"
echo -n "MinIO: "
if curl -s http://localhost:9000/minio/health/live > /dev/null 2>&1; then
    echo -e "${GREEN}OK${NC}"
else
    echo -e "${RED}FALHA${NC}"
    echo -e "${RED}O MinIO não está respondendo. Verifique os logs para mais detalhes.${NC}"
    exit 1
fi

echo -e "${YELLOW}Construindo imagem de teste...${NC}"
$DOCKER_COMPOSE_CMD build test

echo -e "${BLUE}=== Executando testes de integração do MinIO ===${NC}"

# Executar os testes com filtro para MinIO
echo -e "${GREEN}Executando testes do MinIO...${NC}"
$DOCKER_COMPOSE_CMD run --rm \
    -e ASPNETCORE_ENVIRONMENT=Testing \
    -e DOTNET_ENVIRONMENT=Testing \
    -e DOTNET_RUNNING_IN_CONTAINER=true \
    -e MINIO_HOST=minio \
    -e MINIO_PORT=9000 \
    -e MINIO_ACCESS_KEY=minio \
    -e MINIO_SECRET_KEY=minio123 \
    test --filter "Category=MinIO"

# Verificar o status de saída
TEST_EXIT_CODE=$?

if [ $TEST_EXIT_CODE -eq 0 ]; then
    echo -e "${GREEN}✅ Todos os testes do MinIO passaram com sucesso!${NC}"
else
    echo -e "${YELLOW}⚠ Alguns testes do MinIO falharam. Verifique os logs acima.${NC}"
fi

echo -e "${BLUE}=== Testes de integração do MinIO concluídos ===${NC}"
exit $TEST_EXIT_CODE
