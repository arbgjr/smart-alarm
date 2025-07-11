#!/bin/bash

# Define cores para melhor legibilidade
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${BLUE}=== Smart Alarm - Testes de Integração do Vault ===${NC}"

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

# Verificar se o Vault está rodando
echo -e "${YELLOW}Verificando se o Vault está rodando...${NC}"
VAULT_RUNNING=true

if ! docker ps | grep -q -E "vault(-1)?$"; then
    echo -e "${YELLOW}O Vault não está rodando. Inicializando...${NC}"
    VAULT_RUNNING=false
fi

if [ "$VAULT_RUNNING" = false ]; then
    # Iniciar Vault
    echo -e "${YELLOW}Iniciando Vault...${NC}"
    $DOCKER_COMPOSE_CMD up -d vault
    
    # Aguardar para o Vault inicializar
    echo -e "${YELLOW}Aguardando o Vault inicializar (10s)...${NC}"
    sleep 10
fi

# Verificar saúde do Vault
echo -e "${YELLOW}Verificando saúde do Vault...${NC}"
echo -n "Vault: "
if curl -s http://localhost:8200/v1/sys/seal-status > /dev/null 2>&1; then
    echo -e "${GREEN}OK${NC}"
else
    echo -e "${RED}FALHA${NC}"
    echo -e "${RED}O Vault não está respondendo. Verifique os logs para mais detalhes.${NC}"
    exit 1
fi

echo -e "${YELLOW}Construindo imagem de teste...${NC}"
$DOCKER_COMPOSE_CMD build test

echo -e "${BLUE}=== Executando testes de integração do Vault ===${NC}"

# Executar os testes com filtro para Vault
echo -e "${GREEN}Executando testes do Vault...${NC}"
$DOCKER_COMPOSE_CMD run --rm \
    -e ASPNETCORE_ENVIRONMENT=Testing \
    -e DOTNET_ENVIRONMENT=Testing \
    -e DOTNET_RUNNING_IN_CONTAINER=true \
    -e VAULT_HOST=vault \
    -e VAULT_PORT=8200 \
    -e VAULT_TOKEN=dev-token \
    test --filter "Category=Vault"

# Verificar o status de saída
TEST_EXIT_CODE=$?

if [ $TEST_EXIT_CODE -eq 0 ]; then
    echo -e "${GREEN}✅ Todos os testes do Vault passaram com sucesso!${NC}"
else
    echo -e "${YELLOW}⚠ Alguns testes do Vault falharam. Verifique os logs acima.${NC}"
fi

echo -e "${BLUE}=== Testes de integração do Vault concluídos ===${NC}"
exit $TEST_EXIT_CODE
