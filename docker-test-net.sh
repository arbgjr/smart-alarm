#!/bin/bash

# Define cores para melhor legibilidade
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Definições de modos de teste
TEST_MODE=${1:-all}
TEST_FILTER="Category=Integration"
EXTRA_PARAMS=""

echo -e "${BLUE}=== Smart Alarm - Testes de Integração em Docker com Rede Compartilhada ===${NC}"

# Detectar ambiente WSL
USING_WSL=false
if [[ -f /proc/sys/fs/binfmt_misc/WSLInterop ]]; then
    USING_WSL=true
    echo -e "${YELLOW}Ambiente WSL detectado${NC}"
fi

# Detectar Docker Desktop
if docker info 2>/dev/null | grep -q "Docker Desktop"; then
    echo -e "${YELLOW}Docker Desktop detectado${NC}"
    CONTAINER_PREFIX="smart-alarm-"
    IS_DOCKER_DESKTOP=true
else
    CONTAINER_PREFIX=""
    IS_DOCKER_DESKTOP=false
fi

# Verificar se o Docker está rodando
if ! docker info > /dev/null 2>&1; then
    echo -e "${RED}Docker não está rodando. Por favor, inicie o Docker e tente novamente.${NC}"
    exit 1
fi

# Verificar se Docker Compose está disponível
DOCKER_COMPOSE_CMD="docker-compose"
if ! command -v docker-compose &> /dev/null; then
    # Tenta usar o plugin compose do Docker (Docker Desktop mais recente)
    if docker compose version &> /dev/null; then
        echo -e "${YELLOW}docker-compose não encontrado, usando 'docker compose' (plugin)${NC}"
        DOCKER_COMPOSE_CMD="docker compose"
    else
        echo -e "${RED}Docker Compose não está instalado ou não está no PATH.${NC}"
        exit 1
    fi
fi

# Lógica para filtro de testes baseada no modo
case "$TEST_MODE" in
    essentials)
        echo -e "${BLUE}Executando apenas testes essenciais (MinIO, Vault, PostgreSQL, RabbitMQ)${NC}"
        TEST_FILTER="Trait=Essential&Category=Integration"
        ;;
    observability)
        echo -e "${BLUE}Executando apenas testes de observabilidade${NC}"
        TEST_FILTER="Trait=Observability&Category=Integration"
        ;;
    minio)
        echo -e "${BLUE}Executando apenas testes de MinIO${NC}"
        TEST_FILTER="FullyQualifiedName~MinioIntegrationTests"
        ;;
    vault)
        echo -e "${BLUE}Executando apenas testes de Vault${NC}"
        TEST_FILTER="FullyQualifiedName~VaultIntegrationTests"
        ;;
    postgres)
        echo -e "${BLUE}Executando apenas testes de PostgreSQL${NC}"
        TEST_FILTER="FullyQualifiedName~PostgresIntegrationTests"
        ;;
    rabbitmq)
        echo -e "${BLUE}Executando apenas testes de RabbitMQ${NC}"
        TEST_FILTER="FullyQualifiedName~RabbitMqIntegrationTests"
        ;;
    debug)
        echo -e "${BLUE}Modo depuração: apenas verificação de ambiente${NC}"
        TEST_FILTER="none"
        ;;
    *)
        echo -e "${BLUE}Executando todos os testes de integração${NC}"
        ;;
esac

# Se --verbose ou -v for passado como segundo parâmetro, ativa modo verboso
if [[ "$2" == "--verbose" || "$2" == "-v" ]]; then
    EXTRA_PARAMS="--logger \"console;verbosity=detailed\""
    echo -e "${YELLOW}Modo verboso ativado${NC}"
fi

# Função para limpar recursos Docker anteriores
cleanup_docker_resources() {
    echo -e "${YELLOW}Limpando recursos Docker anteriores...${NC}"
    
    # Parar e remover containers de teste anteriores se existirem
    if docker ps -a | grep -q "test-runner"; then
        echo -e "${YELLOW}Removendo contêiner de teste anterior...${NC}"
        docker stop test-runner > /dev/null 2>&1 || true
        docker rm test-runner > /dev/null 2>&1 || true
    fi
    
    # Remover a rede para garantir que será recriada corretamente
    if docker network ls | grep -q "smartalarm-test"; then
        echo -e "${YELLOW}Removendo rede Docker anterior...${NC}"
        docker network rm smartalarm-test >/dev/null 2>&1 || true
    fi
}

# Executar limpeza inicial
cleanup_docker_resources

echo -e "${BLUE}=== Iniciando serviços de dependência ===${NC}"

# Cria rede Docker se não existir
docker network create smartalarm-test
echo "Rede smartalarm-test criada"

# Verifica serviços essenciais
echo -e "${BLUE}Iniciando serviços essenciais...${NC}"
$DOCKER_COMPOSE_CMD -f docker-compose.yml up -d || echo -e "${RED}Erro ao iniciar serviços. Verifique se docker-compose.yml existe e é válido.${NC}"

# Aguardar alguns segundos para os serviços inicializarem
echo -e "${YELLOW}Aguardando serviços inicializarem (15s)...${NC}"
sleep 15

# Listar os contêineres em execução
echo -e "${BLUE}=== Contêineres em execução ===${NC}"
docker ps

# Resolver os nomes dos serviços baseado no ambiente
if [[ "$IS_DOCKER_DESKTOP" == "true" ]]; then
    POSTGRES_HOST="${CONTAINER_PREFIX}postgres"
    RABBITMQ_HOST="${CONTAINER_PREFIX}rabbitmq"
    MINIO_HOST="${CONTAINER_PREFIX}minio"
    VAULT_HOST="${CONTAINER_PREFIX}vault"
    PROMETHEUS_HOST="${CONTAINER_PREFIX}prometheus"
    LOKI_HOST="${CONTAINER_PREFIX}loki"
    JAEGER_HOST="${CONTAINER_PREFIX}jaeger"
    GRAFANA_HOST="${CONTAINER_PREFIX}grafana"
else
    POSTGRES_HOST="postgres"
    RABBITMQ_HOST="rabbitmq"
    MINIO_HOST="minio"
    VAULT_HOST="vault"
    PROMETHEUS_HOST="prometheus"
    LOKI_HOST="loki"
    JAEGER_HOST="jaeger"
    GRAFANA_HOST="grafana"
fi

# Verificar conectividade básica entre contêineres
echo -e "${BLUE}=== Verificando conectividade entre contêineres ===${NC}"

# Criar um contêiner temporário para testar conectividade
echo -e "${YELLOW}Criando contêiner temporário para teste de conectividade...${NC}"
docker run --rm --network=smartalarm-test --name=connectivity-test alpine ping -c 2 $POSTGRES_HOST
echo -e "${GREEN}Teste de conectividade concluído.${NC}"

# Função para configurar e executar os testes de integração
run_integration_tests() {
    # Verificar se dotnet está disponível
    if ! command -v dotnet &> /dev/null && [[ "$USING_WSL" == "true" ]]; then
        echo -e "${YELLOW}dotnet não está instalado localmente. Executando testes via Docker...${NC}"
        
        # Obter o caminho atual
        CURRENT_PATH=$(pwd)
        
        # Preparar variáveis de ambiente para o contêiner de teste
        # Isto é crucial: apontamos para os nomes dos serviços na rede Docker em vez de localhost
        echo -e "${BLUE}Configurando variáveis de ambiente para o contêiner de teste...${NC}"
        TEST_ENV_VARS="-e POSTGRES_HOST=$POSTGRES_HOST \
                      -e POSTGRES_PORT=5432 \
                      -e RABBITMQ_HOST=$RABBITMQ_HOST \
                      -e RABBITMQ_PORT=5672 \
                      -e MINIO_HOST=$MINIO_HOST \
                      -e MINIO_PORT=9000 \
                      -e VAULT_HOST=$VAULT_HOST \
                      -e VAULT_PORT=8200 \
                      -e PROMETHEUS_HOST=$PROMETHEUS_HOST \
                      -e PROMETHEUS_PORT=9090 \
                      -e LOKI_HOST=$LOKI_HOST \
                      -e LOKI_PORT=3100 \
                      -e JAEGER_HOST=$JAEGER_HOST \
                      -e JAEGER_PORT=16686 \
                      -e GRAFANA_HOST=$GRAFANA_HOST \
                      -e GRAFANA_PORT=3001"
        
        # Executar os testes em um contêiner Docker conectado à mesma rede
        echo -e "${BLUE}Executando testes via Docker...${NC}"
        DOCKER_TEST_CMD="docker run --rm --name test-runner --network=smartalarm-test $TEST_ENV_VARS -v ${CURRENT_PATH}:/app mcr.microsoft.com/dotnet/sdk:8.0 bash -c \"cd /app && dotnet test --filter \\\"$TEST_FILTER\\\" $EXTRA_PARAMS\""
        
        echo -e "Executando: $DOCKER_TEST_CMD"
        eval $DOCKER_TEST_CMD
        
        TEST_EXIT_CODE=$?
    else
        # Se dotnet estiver disponível, execute localmente mas defina variáveis de ambiente
        echo -e "${BLUE}Executando testes localmente com dotnet...${NC}"
        
        # Definir variáveis de ambiente para testes locais
        export POSTGRES_HOST=$POSTGRES_HOST
        export POSTGRES_PORT=5432
        export RABBITMQ_HOST=$RABBITMQ_HOST
        export RABBITMQ_PORT=5672
        export MINIO_HOST=$MINIO_HOST
        export MINIO_PORT=9000
        export VAULT_HOST=$VAULT_HOST
        export VAULT_PORT=8200
        export PROMETHEUS_HOST=$PROMETHEUS_HOST
        export PROMETHEUS_PORT=9090
        export LOKI_HOST=$LOKI_HOST
        export LOKI_PORT=3100
        export JAEGER_HOST=$JAEGER_HOST
        export JAEGER_PORT=16686
        export GRAFANA_HOST=$GRAFANA_HOST
        export GRAFANA_PORT=3001
        
        echo -e "Executando: dotnet test --filter \"$TEST_FILTER\" $EXTRA_PARAMS"
        dotnet test --filter "$TEST_FILTER" $EXTRA_PARAMS
        
        TEST_EXIT_CODE=$?
    fi
    
    return $TEST_EXIT_CODE
}

# Executar testes de integração
if [[ "$TEST_FILTER" != "none" ]]; then
    echo -e "\n${BLUE}=== Executando testes de integração ===${NC}"
    
    run_integration_tests
    TEST_EXIT_CODE=$?
    
    if [ $TEST_EXIT_CODE -eq 0 ]; then
        echo -e "\n${GREEN}✅ Testes concluídos com sucesso!${NC}"
    else
        echo -e "\n${RED}❌ Testes falharam com código de saída $TEST_EXIT_CODE${NC}"
        echo -e "\n${YELLOW}Dicas de solução:${NC}"
        echo "1. Verifique se todos os serviços estão rodando corretamente"
        echo "2. Verifique os logs dos contêineres: $DOCKER_COMPOSE_CMD logs [nome-do-serviço]"
        echo "3. Tente executar apenas um grupo específico de testes: ./docker-test-net.sh [minio|vault|postgres|rabbitmq]"
        echo "4. Para modo verboso adicione -v como parâmetro: ./docker-test-net.sh essentials -v"
    fi
else
    echo -e "\n${BLUE}Modo debug: Ambiente configurado, execute os testes manualmente se necessário${NC}"
    TEST_EXIT_CODE=0
fi

# Informações de ajuda
echo -e "\n${BLUE}=== Uso do script ===${NC}"
echo "- ./docker-test-net.sh                # Executa todos os testes de integração"
echo "- ./docker-test-net.sh essentials     # Executa apenas testes essenciais (MinIO, Vault, PostgreSQL, RabbitMQ)"
echo "- ./docker-test-net.sh observability  # Executa apenas testes de observabilidade"
echo "- ./docker-test-net.sh [minio|vault|postgres|rabbitmq]  # Executa testes específicos"
echo "- ./docker-test-net.sh debug          # Apenas verifica o ambiente, sem executar testes"
echo "- Adicione -v ou --verbose para saída detalhada: ./docker-test-net.sh essentials -v"

echo -e "${BLUE}=== Testes de integração concluídos ===${NC}"
echo -e "${YELLOW}Para encerrar o ambiente de desenvolvimento, execute: ./stop-dev-env.sh${NC}"

# Informações de diagnóstico adicionais
echo -e "\n${BLUE}=== Diagnóstico do ambiente ===${NC}"
echo -e "Docker: $(docker --version)"
echo -n "Docker Compose: "
if command -v docker-compose &> /dev/null; then
    docker-compose --version
elif docker compose version &> /dev/null; then
    docker compose version | head -n 1
else
    echo "Não disponível"
fi

echo -n "Dotnet: "
if command -v dotnet &> /dev/null; then
    dotnet --version
else
    echo "Não disponível"
    if [[ "$USING_WSL" == "true" ]]; then
        # Verificar se o .NET está instalado no Windows e pode ser acessado via WSL
        if [[ -f /mnt/c/Program\ Files/dotnet/dotnet.exe ]]; then
            echo -e "${YELLOW}Detectado .NET no Windows. Para usar no WSL, adicione ao PATH: export PATH=\$PATH:/mnt/c/Program\\ Files/dotnet${NC}"
        fi
    fi
fi

echo -e "${YELLOW}Se você está tendo problemas, verifique as permissões do script: chmod +x ./docker-test-net.sh${NC}"

exit $TEST_EXIT_CODE
