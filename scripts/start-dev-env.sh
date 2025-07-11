#!/bin/bash

# Define cores para melhor legibilidade
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${BLUE}=== Smart Alarm - Ambiente de Desenvolvimento ===${NC}"
echo -e "${YELLOW}Iniciando serviços necessários para testes de integração...${NC}"

# Verificar se o Docker está rodando
if ! docker info > /dev/null 2>&1; then
    echo -e "${RED}Docker não está rodando. Por favor, inicie o Docker e tente novamente.${NC}"
    exit 1
fi

# Função para verificar se uma porta está em uso
check_port() {
    local port=$1
    local service=$2
    
    # Em sistemas Windows com WSL, precisamos verificar de forma diferente
    if command -v netstat > /dev/null 2>&1; then
        if netstat -ano | grep -q ":$port "; then
            echo -e "${RED}⚠ A porta $port usada pelo $service já está em uso. Verifique outros serviços em execução.${NC}"
            return 1
        fi
    elif command -v ss > /dev/null 2>&1; then
        if ss -tulpn | grep -q ":$port "; then
            echo -e "${RED}⚠ A porta $port usada pelo $service já está em uso. Verifique outros serviços em execução.${NC}"
            return 1
        fi
    fi
    
    return 0
}

# Verificar portas essenciais
echo -e "${YELLOW}Verificando disponibilidade de portas...${NC}"
check_port 5432 "PostgreSQL" || echo -e "${YELLOW}Tentando continuar mesmo assim...${NC}"
check_port 5672 "RabbitMQ" || echo -e "${YELLOW}Tentando continuar mesmo assim...${NC}"
check_port 15672 "RabbitMQ Management" || echo -e "${YELLOW}Tentando continuar mesmo assim...${NC}"
check_port 9000 "MinIO API" || echo -e "${YELLOW}Tentando continuar mesmo assim...${NC}"
check_port 9001 "MinIO Console" || echo -e "${YELLOW}Tentando continuar mesmo assim...${NC}"
check_port 8200 "HashiCorp Vault" || echo -e "${YELLOW}Tentando continuar mesmo assim...${NC}"

# Verificar argumento opcional para serviços específicos
SERVICES="rabbitmq postgres minio vault"
if [ ! -z "$1" ]; then
    case $1 in
        observability)
            SERVICES="$SERVICES prometheus loki jaeger grafana"
            echo -e "${YELLOW}Incluindo stack de observabilidade${NC}"
            ;;
        api)
            SERVICES="$SERVICES prometheus loki jaeger grafana api"
            echo -e "${YELLOW}Incluindo stack de observabilidade e API${NC}"
            ;;
        all)
            SERVICES="rabbitmq postgres minio vault prometheus loki jaeger grafana api"
            echo -e "${YELLOW}Iniciando todos os serviços (integração, observabilidade e API)${NC}"
            ;;
    esac
fi

USE_COMPOSE=true

# Verificar se docker-compose está disponível
if ! command -v docker-compose &> /dev/null && ! command -v docker compose &> /dev/null; then
    echo -e "${YELLOW}Docker Compose não está disponível. Usando abordagem de containers individuais.${NC}"
    USE_COMPOSE=false
fi

verify_service_health() {
    local service=$1
    local max_attempts=$2
    local attempt=1
    local wait_time=3  # Aumentando tempo de espera entre tentativas
    
    echo -e "${YELLOW}Verificando saúde do serviço $service...${NC}"
    
    # Ajustar o nome do serviço para corresponder aos nomes dos contêineres do Docker Compose
    local container_name=$service
    if [ "$USE_COMPOSE" = true ]; then
        container_name="smart-alarm-${service}-1"
    fi
    
    # Verificar primeiro se o contêiner existe
    if ! docker ps -a | grep -q $container_name; then
        echo -e "${RED}⚠ Contêiner $container_name não existe${NC}"
        return 1
    fi
    
    # Verificar se o contêiner está em execução
    if ! docker ps | grep -q $container_name; then
        echo -e "${YELLOW}Contêiner $container_name existe mas não está em execução. Tentando iniciar...${NC}"
        docker start $container_name
        sleep $wait_time
    fi
    
    while [ $attempt -le $max_attempts ]; do
        case $service in
            rabbitmq)
                # Para RabbitMQ, damos mais tempo para inicializar
                if [ $attempt -eq 1 ]; then
                    echo -e "${YELLOW}Aguardando inicialização do RabbitMQ (pode levar até 30s)...${NC}"
                    sleep 10
                fi
                if docker exec $container_name rabbitmqctl status >/dev/null 2>&1; then
                    echo -e "${GREEN}✓ RabbitMQ está saudável${NC}"
                    return 0
                fi
                ;;
            postgres)
                # Para PostgreSQL, damos mais tempo para inicializar
                if [ $attempt -eq 1 ]; then
                    echo -e "${YELLOW}Aguardando inicialização do PostgreSQL (pode levar até 15s)...${NC}"
                    sleep 5
                fi
                if docker exec $container_name pg_isready -U smartalarm >/dev/null 2>&1; then
                    echo -e "${GREEN}✓ PostgreSQL está saudável${NC}"
                    return 0
                fi
                ;;
            minio)
                if curl -s http://localhost:9000/minio/health/live >/dev/null 2>&1; then
                    echo -e "${GREEN}✓ MinIO está saudável${NC}"
                    return 0
                fi
                ;;
            vault)
                if curl -s http://localhost:8200/v1/sys/health >/dev/null 2>&1; then
                    echo -e "${GREEN}✓ HashiCorp Vault está saudável${NC}"
                    return 0
                fi
                ;;
            *)
                # Para outros serviços, apenas verificar se o container está rodando
                if docker ps | grep -q $container_name; then
                    echo -e "${GREEN}✓ $service está rodando${NC}"
                    return 0
                fi
                ;;
        esac
        
        echo -e "${YELLOW}Tentativa $attempt/$max_attempts: $service ainda não está pronto...${NC}"
        attempt=$((attempt+1))
        sleep $wait_time
    done
    
    echo -e "${YELLOW}⚠ $service não confirmou saúde após $max_attempts tentativas, mas pode estar apenas iniciando lentamente...${NC}"
    if docker ps | grep -q $container_name; then
        echo -e "${GREEN}✓ Contêiner $service está em execução, continuando...${NC}"
        return 0
    else
        echo -e "${RED}⚠ Contêiner $service não está em execução${NC}"
        return 1
    fi
}

if [ "$USE_COMPOSE" = true ]; then
    echo -e "${GREEN}Usando Docker Compose para iniciar os serviços...${NC}"
    
    # Verificar qual comando docker-compose está disponível
    if command -v docker-compose &> /dev/null; then
        DOCKER_COMPOSE_CMD="docker-compose"
    else
        DOCKER_COMPOSE_CMD="docker compose"
    fi
    
    # Iniciar os serviços necessários um a um para melhor controle de erros
    for service in $SERVICES; do
        echo -e "${YELLOW}Iniciando serviço: $service${NC}"
        if ! $DOCKER_COMPOSE_CMD up -d --no-deps $service; then
            echo -e "${RED}⚠ Falha ao iniciar $service. Tentando continuar com outros serviços...${NC}"
            # Remover o serviço com falha da lista de serviços
            SERVICES=$(echo "$SERVICES" | sed "s/$service//g")
        fi
        # Breve pausa entre a inicialização dos serviços
        sleep 1
    done
    
    # Verificar status dos serviços
    echo -e "${GREEN}Status dos serviços:${NC}"
    $DOCKER_COMPOSE_CMD ps
    
    # Verificar saúde dos principais serviços
    echo -e "${YELLOW}Verificando saúde dos serviços principais (pode levar alguns segundos)...${NC}"
    for service in "rabbitmq" "postgres" "minio" "vault"; do
        if echo "$SERVICES" | grep -q $service; then
            verify_service_health $service 5
        fi
    done
else
    # Criar rede Docker para que os containers possam se comunicar
    docker network inspect smart-alarm-network >/dev/null 2>&1 || docker network create smart-alarm-network

    # Iniciar RabbitMQ
    if echo "$SERVICES" | grep -q "rabbitmq"; then
        if docker ps | grep -q rabbitmq; then
            echo -e "${GREEN}Container RabbitMQ já está rodando.${NC}"
        else
            # Verificar se o container existe mas está parado
            if docker ps -a | grep -q rabbitmq; then
                echo -e "${YELLOW}Container RabbitMQ existe mas está parado. Iniciando...${NC}"
                docker start rabbitmq
            else
                echo -e "${YELLOW}Criando e iniciando container RabbitMQ...${NC}"
                docker run -d --name rabbitmq --network smart-alarm-network \
                    -p 5672:5672 -p 15672:15672 \
                    -e RABBITMQ_DEFAULT_USER=guest \
                    -e RABBITMQ_DEFAULT_PASS=guest \
                    rabbitmq:3-management
            fi
            verify_service_health "rabbitmq" 5
        fi
    fi

    # Iniciar PostgreSQL
    if echo "$SERVICES" | grep -q "postgres"; then
        if docker ps | grep -q postgres; then
            echo -e "${GREEN}Container PostgreSQL já está rodando.${NC}"
        else
            # Verificar se o container existe mas está parado
            if docker ps -a | grep -q postgres; then
                echo -e "${YELLOW}Container PostgreSQL existe mas está parado. Iniciando...${NC}"
                docker start postgres
            else
                echo -e "${YELLOW}Criando e iniciando container PostgreSQL...${NC}"
                docker run -d --name postgres --network smart-alarm-network \
                    -e POSTGRES_USER=smartalarm \
                    -e POSTGRES_PASSWORD=smartalarm123 \
                    -e POSTGRES_DB=smartalarm \
                    -p 5432:5432 \
                    -v postgres-data:/var/lib/postgresql/data \
                    postgres:16
            fi
            verify_service_health "postgres" 5
        fi
    fi

    # Iniciar MinIO
    if echo "$SERVICES" | grep -q "minio"; then
        if docker ps | grep -q minio; then
            echo -e "${GREEN}Container MinIO já está rodando.${NC}"
        else
            # Verificar se o container existe mas está parado
            if docker ps -a | grep -q minio; then
                echo -e "${YELLOW}Container MinIO existe mas está parado. Iniciando...${NC}"
                docker start minio
            else
                echo -e "${YELLOW}Criando e iniciando container MinIO...${NC}"
                docker run -d --name minio --network smart-alarm-network \
                    -e MINIO_ROOT_USER=minio \
                    -e MINIO_ROOT_PASSWORD=minio123 \
                    -p 9000:9000 -p 9001:9001 \
                    -v minio-data:/data \
                    minio/minio server /data --console-address ":9001"
            fi
            verify_service_health "minio" 5
        fi
    fi

    # Iniciar HashiCorp Vault
    if echo "$SERVICES" | grep -q "vault"; then
        if docker ps | grep -q vault; then
            echo -e "${GREEN}Container HashiCorp Vault já está rodando.${NC}"
        else
            # Verificar se o container existe mas está parado
            if docker ps -a | grep -q vault; then
                echo -e "${YELLOW}Container HashiCorp Vault existe mas está parado. Iniciando...${NC}"
                docker start vault
            else
                # Verificar se a porta padrão do Vault está disponível
                VAULT_PORT=8200
                ALTERNATE_PORT=8201
                
                if ! check_port 8200 "HashiCorp Vault"; then
                    echo -e "${YELLOW}Porta 8200 já em uso. Tentando porta alternativa $ALTERNATE_PORT...${NC}"
                    if check_port $ALTERNATE_PORT "HashiCorp Vault (alternativa)"; then
                        VAULT_PORT=$ALTERNATE_PORT
                    else
                        echo -e "${YELLOW}Porta alternativa $ALTERNATE_PORT também ocupada. Tentando usar porta padrão mesmo assim...${NC}"
                    fi
                fi
                
                echo -e "${YELLOW}Criando e iniciando container HashiCorp Vault na porta $VAULT_PORT...${NC}"
                docker run -d --name vault --network smart-alarm-network \
                    -e VAULT_DEV_ROOT_TOKEN_ID=dev-token \
                    -e VAULT_DEV_LISTEN_ADDRESS=0.0.0.0:$VAULT_PORT \
                    -p $VAULT_PORT:$VAULT_PORT \
                    hashicorp/vault:1.15 vault server -dev
                
                # Atualizar a variável global se a porta foi alterada
                if [ "$VAULT_PORT" != "8200" ]; then
                    echo -e "${YELLOW}⚠ HashiCorp Vault usando porta alternativa: $VAULT_PORT${NC}"
                    export VAULT_PORT=$VAULT_PORT
                fi
            fi
            verify_service_health "vault" 5
        fi
    fi

    # Se o usuário solicitou a stack de observabilidade ou a API, alertar que isso só está disponível via Docker Compose
    if echo "$SERVICES" | grep -E "prometheus|loki|jaeger|grafana|api" > /dev/null; then
        echo -e "${YELLOW}⚠ Os serviços de observabilidade e API só estão disponíveis via Docker Compose.${NC}"
        echo -e "${YELLOW}Para iniciar esses serviços, instale o Docker Compose e execute:${NC}"
        echo -e "${BLUE}docker-compose up -d [serviço1] [serviço2] ...${NC}"
    fi

    # Exibir informações dos containers
    echo -e "${GREEN}Informações dos containers:${NC}"
    docker ps | grep -E "rabbitmq|postgres|minio|vault|prometheus|loki|jaeger|grafana|api" || echo -e "${RED}Nenhum container relevante encontrado.${NC}"
fi

# Informações de acesso aos serviços
echo -e "\n${BLUE}=== Interfaces de gerenciamento ===${NC}"
echo -e "${GREEN}RabbitMQ:${NC} http://localhost:15672/ (guest/guest)"
echo -e "${GREEN}MinIO:${NC} http://localhost:9001/ (minio/minio123)"

# Usar porta alternativa para Vault se foi definida
VAULT_PORT=${VAULT_PORT:-8200}
echo -e "${GREEN}HashiCorp Vault:${NC} http://localhost:$VAULT_PORT/ (token: dev-token)"
echo -e "${GREEN}PostgreSQL:${NC} localhost:5432 (smartalarm/smartalarm123)"

if echo "$SERVICES" | grep -E "prometheus|loki|jaeger|grafana" > /dev/null; then
    echo -e "\n${BLUE}=== Interfaces de Observabilidade ===${NC}"
    echo -e "${GREEN}Grafana:${NC} http://localhost:3001/ (admin/admin)"
    echo -e "${GREEN}Jaeger:${NC} http://localhost:16686/"
    echo -e "${GREEN}Prometheus:${NC} http://localhost:9090/"
fi

if echo "$SERVICES" | grep "api" > /dev/null; then
    echo -e "\n${BLUE}=== API Smart Alarm ===${NC}"
    echo -e "${GREEN}API:${NC} http://localhost:8080/"
    echo -e "${GREEN}Swagger:${NC} http://localhost:8080/swagger"
fi

# Opções para executar testes
echo -e "\n${BLUE}=== Próximos passos ===${NC}"
echo -e "${YELLOW}Para executar testes de integração:${NC}"
echo -e "${GREEN}./test-integration.sh all${NC} ${YELLOW}(todos os testes)${NC}"
echo -e "${GREEN}./test-integration.sh [serviço]${NC} ${YELLOW}(testes específicos: rabbitmq, postgres, minio, vault, keyvault, observability)${NC}"
echo -e "${YELLOW}Para encerrar o ambiente:${NC}"
echo -e "${GREEN}./stop-dev-env.sh${NC}"

echo -e "\n${GREEN}✅ Ambiente de desenvolvimento inicializado!${NC}"
