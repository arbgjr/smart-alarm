#!/bin/bash

# Define cores para melhor legibilidade
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${BLUE}=== Smart Alarm - Testes de Integração em Docker ===${NC}"

# Verificar se Docker e Docker Compose estão disponíveis
if ! command -v docker &> /dev/null; then
    echo -e "${RED}Docker não está instalado ou não está no PATH. Por favor, instale o Docker e tente novamente.${NC}"
    exit 1
fi

# Função para limpar recursos Docker anteriores
cleanup_docker_resources() {
    echo -e "${YELLOW}Limpando recursos Docker anteriores...${NC}"
    
    # Parar e remover containers de teste anteriores se existirem
    if docker ps -a | grep -q "smart-alarm.*test"; then
        echo -e "${YELLOW}Removendo contêineres de teste anteriores...${NC}"
        docker ps -a | grep "smart-alarm.*test" | awk '{print $1}' | xargs -r docker rm -f
    fi
    
    # Remover a rede para garantir que será recriada corretamente
    if docker network ls | grep -q "smart-alarm-network"; then
        echo -e "${YELLOW}Removendo rede Docker anterior...${NC}"
        docker network rm smart-alarm-network >/dev/null 2>&1 || true
    fi
}

# Executar limpeza inicial
cleanup_docker_resources

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

# Verificar se os serviços necessários estão rodando
echo -e "${YELLOW}Verificando serviços necessários...${NC}"
SERVICES_RUNNING=true

for service in "rabbitmq" "postgres" "minio" "vault"; do
    # Verifica tanto por nome exato quanto pelo padrão nome-serviço-1 usado pelo Docker Compose
    if ! docker ps | grep -q -E "$service(-1)?$"; then
        echo -e "${YELLOW}O serviço $service não está rodando. Inicializando ambiente...${NC}"
        SERVICES_RUNNING=false
        break
    fi
done

if [ "$SERVICES_RUNNING" = false ]; then
    echo -e "${YELLOW}Iniciando serviços necessários para os testes...${NC}"
    
    # Resetar completamente o ambiente Docker para evitar conflitos
    echo -e "${YELLOW}Resetando o ambiente Docker Compose...${NC}"
    $DOCKER_COMPOSE_CMD down -v >/dev/null 2>&1 || true
    
    # Iniciar serviços necessários
    echo -e "${YELLOW}Iniciando serviços essenciais...${NC}"
    $DOCKER_COMPOSE_CMD up -d rabbitmq postgres minio vault
    
    # Aguardar um pouco para os serviços inicializarem
    echo -e "${YELLOW}Aguardando os serviços inicializarem (20s)...${NC}"
    sleep 20
fi

echo -e "${YELLOW}Construindo imagem de teste...${NC}"
$DOCKER_COMPOSE_CMD build test

echo -e "${BLUE}=== Executando testes de integração ===${NC}"

# Processar argumentos para filtrar testes
TEST_FILTER="Category=Integration"
if [ ! -z "$1" ]; then
    if [ "$1" != "all" ]; then
        TEST_FILTER="Category=$1"
    fi
fi

echo -e "${YELLOW}Filtro de teste: ${TEST_FILTER}${NC}"
echo -e "${YELLOW}Iniciando contêiner de testes...${NC}"

# Verificar se os containers estão em execução usando docker-compose
echo -e "${YELLOW}Verificando status dos serviços com docker-compose...${NC}"
$DOCKER_COMPOSE_CMD ps
all_services_ready=true

# Executar os testes em modo interativo para ver os resultados em tempo real
echo -e "${GREEN}Executando testes...${NC}"
$DOCKER_COMPOSE_CMD run --rm \
    -e ASPNETCORE_ENVIRONMENT=Testing \
    -e DOTNET_ENVIRONMENT=Testing \
    -e DOTNET_RUNNING_IN_CONTAINER=true \
    -e RABBITMQ_HOST=rabbitmq \
    test --filter "${TEST_FILTER}"

# Verificar o status de saída
TEST_EXIT_CODE=$?

if [ $TEST_EXIT_CODE -eq 0 ]; then
    echo -e "${GREEN}✅ Todos os testes passaram com sucesso!${NC}"
else
    echo -e "${YELLOW}⚠ Alguns testes falharam. Verifique os logs acima.${NC}"
fi

echo -e "${BLUE}=== Testes de integração concluídos ===${NC}"
echo -e "${YELLOW}Para encerrar o ambiente de desenvolvimento, execute: ./stop-dev-env.sh${NC}"

exit $TEST_EXIT_CODE
