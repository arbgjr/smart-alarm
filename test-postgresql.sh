#!/bin/bash

# Define cores para melhor legibilidade
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${BLUE}=== Smart Alarm - Testes de Integração do PostgreSQL ===${NC}"

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

# Verificar se o PostgreSQL está rodando
echo -e "${YELLOW}Verificando se o PostgreSQL está rodando...${NC}"
POSTGRES_RUNNING=true

if ! docker ps | grep -q -E "postgres(-1)?$"; then
    echo -e "${YELLOW}O PostgreSQL não está rodando. Inicializando...${NC}"
    POSTGRES_RUNNING=false
fi

if [ "$POSTGRES_RUNNING" = false ]; then
    # Iniciar PostgreSQL
    echo -e "${YELLOW}Iniciando PostgreSQL...${NC}"
    $DOCKER_COMPOSE_CMD up -d postgres
    
    # Aguardar para o PostgreSQL inicializar
    echo -e "${YELLOW}Aguardando o PostgreSQL inicializar (15s)...${NC}"
    sleep 15
fi

# Verificar saúde do PostgreSQL
echo -e "${YELLOW}Verificando saúde do PostgreSQL...${NC}"
echo -n "PostgreSQL: "
if docker exec $(docker ps -q -f name=postgres) pg_isready -U smartalarm > /dev/null 2>&1; then
    echo -e "${GREEN}OK${NC}"
else
    echo -e "${RED}FALHA${NC}"
    echo -e "${RED}O PostgreSQL não está respondendo. Verifique os logs para mais detalhes.${NC}"
    exit 1
fi

echo -e "${YELLOW}Construindo imagem de teste...${NC}"
$DOCKER_COMPOSE_CMD build test

echo -e "${BLUE}=== Executando testes de integração do PostgreSQL ===${NC}"

# Executar os testes com filtro para PostgreSQL
echo -e "${GREEN}Executando testes do PostgreSQL...${NC}"
$DOCKER_COMPOSE_CMD run --rm \
    -e ASPNETCORE_ENVIRONMENT=Testing \
    -e DOTNET_ENVIRONMENT=Testing \
    -e DOTNET_RUNNING_IN_CONTAINER=true \
    -e POSTGRES_HOST=postgres \
    -e POSTGRES_USER=smartalarm \
    -e POSTGRES_PASSWORD=smartalarm123 \
    -e POSTGRES_DB=smartalarm \
    test --filter "Category=PostgreSQL"

# Verificar o status de saída
TEST_EXIT_CODE=$?

if [ $TEST_EXIT_CODE -eq 0 ]; then
    echo -e "${GREEN}✅ Todos os testes do PostgreSQL passaram com sucesso!${NC}"
else
    echo -e "${YELLOW}⚠ Alguns testes do PostgreSQL falharam. Verifique os logs acima.${NC}"
fi

echo -e "${BLUE}=== Testes de integração do PostgreSQL concluídos ===${NC}"
exit $TEST_EXIT_CODE
