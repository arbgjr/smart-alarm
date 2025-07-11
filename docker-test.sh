#!/bin/bash

# Cores para melhorar a legibilidade
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Definições de modos de teste
TEST_MODE=${1:-all}
TEST_FILTER="Category=Integration"
EXTRA_PARAMS=""

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

# Função para verificar saúde do serviço com URL específica
check_http_health() {
    local service=$1
    local host=$2
    local port=$3
    local path=$4
    local expected_status="${5:-200}"  # Status HTTP esperado, padrão 200
    
    echo -n "$service: "
    
    # Primeiro verifica se o contêiner está rodando (verificação melhorada)
    # Usa uma abordagem mais robusta para detectar o contêiner independentemente do prefixo
    if ! docker ps | grep -i -E "\b${service}(-1|\b)" > /dev/null; then
        echo -e "${RED}CONTÊINER NÃO ESTÁ RODANDO${NC}"
        return 1
    fi
    
    # Verifica se o serviço HTTP está respondendo
    local status_code
    if command -v curl &> /dev/null; then
        status_code=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:${port}${path})
    else
        echo -e "${YELLOW}curl não encontrado, pulando verificação HTTP${NC}"
        return 0
    fi
    
    if [[ "$status_code" == "$expected_status" || "$status_code" == "200" || "$status_code" == "204" ]]; then
        echo -e "${GREEN}OK${NC}"
        return 0
    else
        echo -e "${RED}FALHA (Status HTTP: $status_code)${NC}"
        return 1
    fi
}

# Função para verificar saúde do serviço
check_service_health() {
    local service_name=$1
    local max_attempts=${2:-5}
    local interval=${3:-3}
    
    echo -n "Verificando saúde do $service_name "
    
    for ((i=1; i<=max_attempts; i++)); do
        # Lógica para verificar saúde do serviço (diferente para cada tipo)
        case "$service_name" in
            "PostgreSQL")
                if docker ps -q -f name=postgres &>/dev/null && \
                   docker exec $(docker ps -q -f name=postgres) pg_isready -U smartalarm &> /dev/null; then
                    echo -e "\r${service_name}: ${GREEN}OK${NC}"
                    return 0
                fi
                ;;
            "RabbitMQ")
                if docker ps -q -f name=rabbitmq &>/dev/null && \
                   docker exec $(docker ps -q -f name=rabbitmq) rabbitmqctl status &> /dev/null; then
                    echo -e "\r${service_name}: ${GREEN}OK${NC}"
                    return 0
                fi
                ;;
            "MinIO")
                if command -v curl &> /dev/null && \
                   curl -s -f -o /dev/null http://localhost:9000/minio/health/live; then
                    echo -e "\r${service_name}: ${GREEN}OK${NC}"
                    return 0
                fi
                ;;
            "Vault")
                if command -v curl &> /dev/null && \
                   curl -s -f -o /dev/null http://localhost:8200/v1/sys/seal-status; then
                    echo -e "\r${service_name}: ${GREEN}OK${NC}"
                    return 0
                fi
                ;;
            *)
                echo -e "\r${service_name}: ${YELLOW}Verificação não implementada${NC}"
                return 0
                ;;
        esac
        
        echo -n "."
        sleep $interval
    done
    
    echo -e "\r${service_name}: ${RED}FALHA após $max_attempts tentativas${NC}"
    return 1
}

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

echo -e "${BLUE}=== Smart Alarm - Testes de Integração em Docker ===${NC}"

# Detecção do ambiente
USING_WSL=false
if [[ -f /proc/sys/fs/binfmt_misc/WSLInterop ]]; then
    USING_WSL=true
    echo -e "${BLUE}Ambiente WSL detectado${NC}"
fi

# Ajustando nomes de contêineres com base no ambiente Docker
if docker info 2>/dev/null | grep -q "Docker Desktop"; then
    echo -e "${BLUE}Docker Desktop detectado${NC}"
    CONTAINER_PREFIX="smart-alarm-"
    IS_DOCKER_DESKTOP=true
else
    CONTAINER_PREFIX=""
    IS_DOCKER_DESKTOP=false
fi

# Verificar se Docker está disponível
if ! command -v docker &> /dev/null; then
    echo -e "${RED}Docker não está instalado ou não está no PATH. Por favor, instale o Docker e tente novamente.${NC}"
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
        echo -e "${YELLOW}Tentando continuar com comandos Docker diretos...${NC}"
        # Vamos definir uma função alternativa que usa docker run diretamente
        docker_compose_alternative() {
            echo -e "${YELLOW}Usando alternativa para docker-compose...${NC}"
            # Simplificada para demonstração, seriam necessários mais parâmetros para uma implementação completa
            local service=$3
            
            case "$service" in
                "postgres")
                    docker run --name postgres-test -e POSTGRES_USER=smartalarm -e POSTGRES_PASSWORD=smartalarm -d -p 5432:5432 postgres:15
                    ;;
                "rabbitmq")
                    docker run --name rabbitmq-test -d -p 5672:5672 -p 15672:15672 rabbitmq:3-management
                    ;;
                "minio")
                    docker run --name minio-test -d -p 9000:9000 -p 9001:9001 -e "MINIO_ROOT_USER=minioadmin" -e "MINIO_ROOT_PASSWORD=minioadmin" minio/minio server /data --console-address ":9001"
                    ;;
                "vault")
                    docker run --name vault-test -d -p 8200:8200 -e "VAULT_DEV_ROOT_TOKEN_ID=dev-only-token" vault:latest
                    ;;
                *)
                    echo -e "${RED}Serviço $service não suportado no modo alternativo${NC}"
                    ;;
            esac
        }
        DOCKER_COMPOSE_CMD="docker_compose_alternative"
    fi
fi

# Executar limpeza inicial
cleanup_docker_resources

# Verificar se o Docker está rodando
if ! docker info > /dev/null 2>&1; then
    echo -e "${RED}Erro: Docker não está rodando. Inicie o Docker e tente novamente.${NC}"
    exit 1
fi

echo -e "${BLUE}=== Iniciando serviços de dependência ===${NC}"

# Cria rede Docker se não existir
if ! docker network ls | grep -q smartalarm-test; then
    docker network create smartalarm-test
    echo "Rede smartalarm-test criada"
else
    echo "Rede smartalarm-test já existe"
fi

# Verifica serviços essenciais
ESSENTIAL_SERVICES="postgres rabbitmq minio vault"
echo -e "${BLUE}Iniciando serviços essenciais...${NC}"
$DOCKER_COMPOSE_CMD -f docker-compose.yml up -d $ESSENTIAL_SERVICES || echo -e "${RED}Erro ao iniciar serviços. Verifique se docker-compose.yml existe e é válido.${NC}"

# Aguardar e verificar serviços essenciais
all_services_ready=true
echo -e "\n${BLUE}=== Verificando saúde dos serviços essenciais ===${NC}"
check_service_health "PostgreSQL" 5 3 || all_services_ready=false
check_service_health "RabbitMQ" 5 3 || all_services_ready=false
check_service_health "MinIO" 5 3 || all_services_ready=false
check_service_health "Vault" 5 3 || all_services_ready=false

# Iniciar serviços de observabilidade apenas se necessário
if [[ "$TEST_MODE" == "observability" || "$TEST_MODE" == "all" ]]; then
    echo -e "\n${BLUE}=== Iniciando serviços de observabilidade ===${NC}"
    $DOCKER_COMPOSE_CMD -f docker-compose.yml up -d grafana loki jaeger prometheus
    
    # Aguardar os serviços de observabilidade (com timeout mais curto)
    echo -e "\n${BLUE}=== Verificando saúde dos serviços de observabilidade ===${NC}"
    echo -e "${YELLOW}Nota: A verificação de serviços de observabilidade pode demorar mais tempo${NC}"
    sleep 15  # Dar um tempo inicial para os serviços inicializarem
fi

# Define esta variável antes do bloco de verificação de serviços
echo -e "\n${BLUE}=== Verificação detalhada de saúde dos serviços ===${NC}"

# Função auxiliar para verificar se um contêiner específico está rodando
is_container_running() {
    local service=$1
    # Usa uma abordagem mais robusta para detectar o contêiner
    if [[ "$IS_DOCKER_DESKTOP" == "true" ]]; then
        # Para Docker Desktop, verificar com prefixo smart-alarm-
        if docker ps | grep -i -E "${CONTAINER_PREFIX}${service}(-1|\b)" > /dev/null; then
            return 0
        else
            return 1
        fi
    else
        # Verificação padrão para outros ambientes
        if docker ps | grep -i -E "\b${service}(-1|\b)" > /dev/null; then
            return 0
        else
            return 1
        fi
    fi
}

# Verificar RabbitMQ
echo -n "RabbitMQ: "
if is_container_running "rabbitmq"; then
    # Obtenha o ID do contêiner RabbitMQ independentemente do prefixo
    RABBITMQ_ID=$(docker ps | grep -i -E "${CONTAINER_PREFIX}rabbitmq(-1|\b)" | awk '{print $1}' | head -1)
    if [[ -n "$RABBITMQ_ID" ]] && docker exec $RABBITMQ_ID rabbitmqctl status > /dev/null 2>&1; then
        echo -e "${GREEN}OK${NC}"
    else
        echo -e "${YELLOW}INICIANDO${NC}"
        all_services_ready=false
    fi
else
    echo -e "${RED}FALHA - CONTÊINER NÃO ENCONTRADO${NC}"
    all_services_ready=false
fi

# Verificar PostgreSQL
echo -n "PostgreSQL: "
if is_container_running "postgres"; then
    # Obtenha o ID do contêiner PostgreSQL independentemente do prefixo
    POSTGRES_ID=$(docker ps | grep -i -E "${CONTAINER_PREFIX}postgres(-1|\b)" | awk '{print $1}' | head -1)
    if [[ -n "$POSTGRES_ID" ]] && docker exec $POSTGRES_ID pg_isready -U smartalarm > /dev/null 2>&1; then
        echo -e "${GREEN}OK${NC}"
    else
        echo -e "${YELLOW}INICIANDO${NC}"
        all_services_ready=false
    fi
else
    echo -e "${RED}FALHA - CONTÊINER NÃO ENCONTRADO${NC}"
    all_services_ready=false
fi

# Verificar MinIO
echo -n "MinIO: "
if is_container_running "minio"; then
    if command -v curl &> /dev/null && curl -s -f -o /dev/null http://localhost:9000/minio/health/live; then
        echo -e "${GREEN}OK${NC}"
    else
        echo -e "${YELLOW}INICIANDO${NC}"
        all_services_ready=false
    fi
else
    echo -e "${RED}FALHA - CONTÊINER NÃO ENCONTRADO${NC}"
    all_services_ready=false
fi

# Verificar Vault
echo -n "Vault: "
if is_container_running "vault"; then
    if command -v curl &> /dev/null && curl -s -f -o /dev/null http://localhost:8200/v1/sys/seal-status; then
        echo -e "${GREEN}OK${NC}"
    else
        echo -e "${YELLOW}INICIANDO${NC}"
        all_services_ready=false
    fi
else
    echo -e "${RED}FALHA - CONTÊINER NÃO ENCONTRADO${NC}"
    all_services_ready=false
fi

# Verificar serviços de observabilidade apenas se necessário
if [[ "$TEST_FILTER" == *"Observability"* || "$TEST_FILTER" == "Category=Integration" || "$1" == "all" ]]; then
    echo -e "\n${BLUE}Verificando serviços de observabilidade:${NC}"
    
    if command -v curl &> /dev/null; then
        # Verificar serviços de observabilidade
        for service in "prometheus:9090:/-/healthy" "loki:3100:/ready" "jaeger:16686:/" "grafana:3001:/api/health"; do
            IFS=":" read svc port path <<< "$service"
            echo -n "$svc: "
            if is_container_running "$svc"; then
                if curl -s -f -o /dev/null http://localhost:$port$path; then
                    echo -e "${GREEN}OK${NC}"
                else
                    echo -e "${YELLOW}INICIANDO${NC}"
                fi
            else
                echo -e "${RED}FALHA - CONTÊINER NÃO ENCONTRADO${NC}"
            fi
        done
    else
        echo -e "${YELLOW}curl não disponível, pulando verificação HTTP de serviços de observabilidade${NC}"
    fi
    
    echo -e "\n${YELLOW}Nota: Falhas nos serviços de observabilidade são comuns e não impedem os testes essenciais.${NC}"
    echo -e "${YELLOW}Para executar apenas testes essenciais: ./docker-test.sh essentials${NC}"
fi

# Execução dos testes
if [[ "$TEST_FILTER" != "none" ]]; then
    echo -e "\n${BLUE}=== Executando testes de integração ===${NC}"
    
    # Verificar se dotnet está disponível
    if ! command -v dotnet &> /dev/null; then
        echo -e "${YELLOW}dotnet não está instalado localmente. Tentando usar Docker para execução dos testes...${NC}"
        
        # Obter o caminho atual compatível com Docker
        # Para WSL, convertemos o caminho Windows para um formato que o Docker possa entender
        CURRENT_PATH=$(pwd)
        
        # Verificar se estamos no WSL e o caminho começa com /mnt/
        if [[ -f /proc/sys/fs/binfmt_misc/WSLInterop ]] && [[ "$CURRENT_PATH" == "/mnt/"* ]]; then
            echo -e "${YELLOW}Detectado ambiente WSL, ajustando caminhos para Docker...${NC}"
        fi
        
        # Preparar o comando Docker para executar os testes
        echo -e "${BLUE}Executando testes via Docker...${NC}"
        DOCKER_TEST_CMD="docker run --rm --network=smartalarm-test -v ${CURRENT_PATH}:/app mcr.microsoft.com/dotnet/sdk:8.0 dotnet test /app --filter \"$TEST_FILTER\""
        
        if [[ -n "$EXTRA_PARAMS" ]]; then
            DOCKER_TEST_CMD="$DOCKER_TEST_CMD $EXTRA_PARAMS"
        fi
        
        echo -e "Executando: $DOCKER_TEST_CMD"
        
        # Executar o comando
        eval $DOCKER_TEST_CMD
        TEST_EXIT_CODE=$?
        
        if [ $TEST_EXIT_CODE -eq 0 ]; then
            echo -e "\n${GREEN}✅ Testes concluídos com sucesso via Docker!${NC}"
        else
            echo -e "\n${RED}❌ Testes falharam com código de saída $TEST_EXIT_CODE${NC}"
            echo -e "\n${YELLOW}Dicas de solução:${NC}"
            echo "1. Verifique se todos os serviços estão rodando corretamente"
            echo "2. Verifique se a rede Docker está configurada corretamente"
            echo "3. Tente executar apenas um grupo específico de testes: ./docker-test.sh minio"
            echo "4. Verifique logs dos contêineres: $DOCKER_COMPOSE_CMD logs [nome-do-serviço]"
        fi
    else
        if ! $all_services_ready; then
            echo -e "${YELLOW}⚠️ Aviso: Nem todos os serviços estão prontos. Os testes podem falhar.${NC}"
        fi
        
        # Comando de teste com melhor formatação
        echo -e "Executando: dotnet test --filter \"$TEST_FILTER\" $EXTRA_PARAMS"
        dotnet test --filter "$TEST_FILTER" $EXTRA_PARAMS
        
        TEST_EXIT_CODE=$?
        
        if [ $TEST_EXIT_CODE -eq 0 ]; then
            echo -e "\n${GREEN}✅ Testes concluídos com sucesso!${NC}"
        else
            echo -e "\n${RED}❌ Testes falharam com código de saída $TEST_EXIT_CODE${NC}"
            echo -e "\n${YELLOW}Dicas de solução:${NC}"
            echo "1. Verifique se todos os serviços estão rodando corretamente"
            echo "2. Tente executar apenas um grupo específico de testes: ./docker-test.sh [minio|vault|postgres|rabbitmq]"
            echo "3. Verifique logs dos contêineres: $DOCKER_COMPOSE_CMD logs [nome-do-serviço]"
            echo "4. Para modo verboso adicione -v como parâmetro: ./docker-test.sh essentials -v"
            echo "5. Verifique a conectividade da rede Docker: docker network inspect smartalarm-test"
        fi
    fi
else
    echo -e "\n${BLUE}Modo debug: Verifique o ambiente e execute os testes manualmente se necessário${NC}"
    TEST_EXIT_CODE=0
fi

# Informações de ajuda
echo -e "\n${BLUE}=== Uso do script ===${NC}"
echo "- ./docker-test.sh                # Executa todos os testes de integração"
echo "- ./docker-test.sh essentials     # Executa apenas testes essenciais (MinIO, Vault, PostgreSQL, RabbitMQ)"
echo "- ./docker-test.sh observability  # Executa apenas testes de observabilidade"
echo "- ./docker-test.sh [minio|vault|postgres|rabbitmq]  # Executa testes específicos"
echo "- ./docker-test.sh debug          # Apenas verifica o ambiente, sem executar testes"
echo "- Adicione -v ou --verbose para saída detalhada: ./docker-test.sh essentials -v"

# Informações de ajuda específicas para WSL
if [[ "$USING_WSL" == "true" ]]; then
    echo -e "\n${BLUE}=== Dicas para execução no WSL ===${NC}"
    echo "1. Se estiver usando WSL, verifique se o Docker Desktop está configurado para integração WSL"
    echo "2. Se for executar testes via .NET, instale o SDK .NET no WSL: wget https://dot.net/v1/dotnet-install.sh && chmod +x dotnet-install.sh && ./dotnet-install.sh"
    echo "3. Para usar o .NET instalado no Windows a partir do WSL, adicione o caminho ao PATH: export PATH=\$PATH:/mnt/c/Program\\ Files/dotnet"
    echo "4. Para problemas de conectividade, verifique se as portas estão acessíveis no WSL: curl -v telnet://localhost:5432"
fi

echo -e "${BLUE}=== Testes de integração concluídos ===${NC}"

# Informações de diagnóstico adicionais
echo -e "\n${BLUE}=== Diagnóstico do ambiente ===${NC}"
echo -e "Docker: $(docker --version)"
echo -n "Docker Compose: "
if command -v docker-compose &> /dev/null; then
    docker-compose --version
elif docker compose version &> /dev/null; then
    docker compose version | head -n 1
else
    echo "Não disponível (usando alternativa)"
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

echo -n "Curl: "
if command -v curl &> /dev/null; then
    curl --version | head -n 1
else
    echo "Não disponível"
fi

echo -e "${YELLOW}Se você está tendo problemas, verifique as permissões do script: chmod +x ./docker-test.sh${NC}"

exit ${TEST_EXIT_CODE:-1}
