#!/bin/bash

# Define cores para melhor legibilidade
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${BLUE}=== Smart Alarm - Testes de Observabilidade ===${NC}"

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

# Verificar se os serviços de observabilidade estão rodando
echo -e "${YELLOW}Verificando serviços de observabilidade...${NC}"
SERVICES_RUNNING=true

for service in "prometheus" "loki" "jaeger" "grafana"; do
    if ! docker ps | grep -q -E "$service(-1)?$"; then
        echo -e "${YELLOW}O serviço $service não está rodando. Inicializando ambiente de observabilidade...${NC}"
        SERVICES_RUNNING=false
        break
    fi
done

if [ "$SERVICES_RUNNING" = false ]; then
    echo -e "${YELLOW}Iniciando serviços de observabilidade...${NC}"
    
    # Iniciar serviços de observabilidade
    $DOCKER_COMPOSE_CMD up -d loki jaeger prometheus grafana
    
    # Aguardar para os serviços inicializarem
    echo -e "${YELLOW}Aguardando os serviços de observabilidade inicializarem (15s)...${NC}"
    sleep 15
fi

echo -e "${YELLOW}Construindo imagem de teste...${NC}"
$DOCKER_COMPOSE_CMD build test

echo -e "${BLUE}=== Executando testes de observabilidade ===${NC}"

# Executar os testes com filtro de categoria Observability
echo -e "${GREEN}Executando testes de observabilidade...${NC}"
$DOCKER_COMPOSE_CMD run --rm \
    -e ASPNETCORE_ENVIRONMENT=Testing \
    -e DOTNET_ENVIRONMENT=Testing \
    -e DOTNET_RUNNING_IN_CONTAINER=true \
    -e LOKI_HOST=loki \
    -e LOKI_PORT=3100 \
    -e JAEGER_HOST=jaeger \
    -e JAEGER_AGENT_PORT=6831 \
    -e JAEGER_COLLECTOR_PORT=14268 \
    -e JAEGER_OTLP_PORT=4317 \
    -e PROMETHEUS_HOST=prometheus \
    -e PROMETHEUS_PORT=9090 \
    -e GRAFANA_HOST=grafana \
    -e GRAFANA_PORT=3000 \
    test --filter "Category=Observability"

# Verificar o status de saída
TEST_EXIT_CODE=$?

if [ $TEST_EXIT_CODE -eq 0 ]; then
    echo -e "${GREEN}✅ Todos os testes de observabilidade passaram com sucesso!${NC}"
else
    echo -e "${YELLOW}⚠ Alguns testes de observabilidade falharam. Verifique os logs acima.${NC}"
fi

echo -e "${BLUE}=== Testes de observabilidade concluídos ===${NC}"
echo -e "${YELLOW}Para encerrar o ambiente de desenvolvimento, execute: ./stop-dev-env.sh${NC}"

exit $TEST_EXIT_CODE
