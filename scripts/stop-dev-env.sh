#!/bin/bash

# Define cores para melhor legibilidade
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${BLUE}=== Smart Alarm - Encerrando Ambiente de Desenvolvimento ===${NC}"

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

# Verificar argumento para modo de limpeza
CLEAN_MODE="stop"
if [ ! -z "$1" ]; then
    case $1 in
        clean)
            CLEAN_MODE="down"
            echo -e "${YELLOW}Modo de limpeza: Removendo containers e redes (mantendo volumes)${NC}"
            ;;
        purge)
            CLEAN_MODE="purge"
            echo -e "${RED}Modo de limpeza completa: Removendo containers, redes E volumes${NC}"
            ;;
    esac
fi

USE_COMPOSE=true

# Verificar se docker-compose está disponível
if ! command -v docker-compose &> /dev/null && ! command -v docker compose &> /dev/null; then
    echo -e "${YELLOW}Docker Compose não está disponível. Usando abordagem de containers individuais.${NC}"
    USE_COMPOSE=false
fi

if [ "$USE_COMPOSE" = true ]; then
    # Verificar qual comando docker-compose está disponível
    if command -v docker-compose &> /dev/null; then
        DOCKER_COMPOSE_CMD="docker-compose"
    else
        DOCKER_COMPOSE_CMD="docker compose"
    fi
    
    # Confirmar se o arquivo docker-compose.yml existe
    if [ ! -f "./docker-compose.yml" ]; then
        echo -e "${RED}Arquivo docker-compose.yml não encontrado. Verifique se você está no diretório correto.${NC}"
        USE_COMPOSE=false
    else
        case $CLEAN_MODE in
            stop)
                # Parar todos os serviços iniciados pelo docker-compose
                echo -e "${YELLOW}Parando todos os serviços via Docker Compose...${NC}"
                $DOCKER_COMPOSE_CMD stop
                echo -e "${GREEN}Serviços encerrados via Docker Compose.${NC}"
                ;;
            down)
                # Parar e remover containers e redes, mas manter volumes
                echo -e "${YELLOW}Removendo containers e redes via Docker Compose...${NC}"
                $DOCKER_COMPOSE_CMD down
                echo -e "${GREEN}Containers e redes removidos via Docker Compose.${NC}"
                ;;
            purge)
                # Confirmar antes de remover volumes
                echo -e "${RED}ATENÇÃO: Isso removerá todos os containers, redes E volumes do projeto.${NC}"
                echo -e "${RED}Todos os dados armazenados serão perdidos (PostgreSQL, MinIO, etc).${NC}"
                read -p "Você tem certeza que deseja continuar? (s/N) " -n 1 -r
                echo
                if [[ $REPLY =~ ^[Ss]$ ]]; then
                    echo -e "${RED}Removendo containers, redes e volumes via Docker Compose...${NC}"
                    $DOCKER_COMPOSE_CMD down -v
                    echo -e "${GREEN}Ambiente completamente limpo via Docker Compose.${NC}"
                else
                    echo -e "${YELLOW}Operação cancelada pelo usuário.${NC}"
                fi
                ;;
        esac
    fi
fi

# Se não usar Docker Compose ou se ocorreu algum erro, usar abordagem individual
if [ "$USE_COMPOSE" = false ]; then
    # Lista dos containers a parar
    CONTAINERS=("rabbitmq" "postgres" "minio" "vault" "prometheus" "loki" "jaeger" "grafana" "api")

    case $CLEAN_MODE in
        stop)
            # Parar cada container se estiver rodando
            for CONTAINER in "${CONTAINERS[@]}"; do
                # Verificar com e sem prefixo para maior compatibilidade
                if docker ps | grep -q -E "\b${CONTAINER_PREFIX}${CONTAINER}(-1|\b)"; then
                    echo -e "${YELLOW}Parando container ${CONTAINER_PREFIX}${CONTAINER}...${NC}"
                    docker stop $(docker ps -q --filter name="${CONTAINER_PREFIX}${CONTAINER}")
                    echo -e "${GREEN}Container ${CONTAINER_PREFIX}${CONTAINER} parado.${NC}"
                elif docker ps | grep -q -E "\b${CONTAINER}(-1|\b)"; then
                    echo -e "${YELLOW}Parando container ${CONTAINER}...${NC}"
                    docker stop $(docker ps -q --filter name="${CONTAINER}")
                    echo -e "${GREEN}Container ${CONTAINER} parado.${NC}"
                fi
            done
            ;;
        down|purge)
            # Parar e remover cada container
            for CONTAINER in "${CONTAINERS[@]}"; do
                # Verificar com e sem prefixo para maior compatibilidade
                if docker ps -a | grep -q -E "\b${CONTAINER_PREFIX}${CONTAINER}(-1|\b)"; then
                    echo -e "${YELLOW}Parando e removendo container ${CONTAINER_PREFIX}${CONTAINER}...${NC}"
                    docker stop $(docker ps -a -q --filter name="${CONTAINER_PREFIX}${CONTAINER}") 2>/dev/null || true
                    docker rm $(docker ps -a -q --filter name="${CONTAINER_PREFIX}${CONTAINER}") 2>/dev/null || true
                    echo -e "${GREEN}Container ${CONTAINER_PREFIX}${CONTAINER} removido.${NC}"
                elif docker ps -a | grep -q -E "\b${CONTAINER}(-1|\b)"; then
                    echo -e "${YELLOW}Parando e removendo container ${CONTAINER}...${NC}"
                    docker stop $(docker ps -a -q --filter name="${CONTAINER}") 2>/dev/null || true
                    docker rm $(docker ps -a -q --filter name="${CONTAINER}") 2>/dev/null || true
                    echo -e "${GREEN}Container ${CONTAINER} removido.${NC}"
                fi
            done
            
            # Remover rede
            if docker network ls | grep -q "smart-alarm-network"; then
                echo -e "${YELLOW}Removendo rede smart-alarm-network...${NC}"
                docker network rm smart-alarm-network 2>/dev/null || true
                echo -e "${GREEN}Rede removida.${NC}"
            fi
            
            # Verificar e remover redes de teste
            if docker network ls | grep -q "smartalarm-test"; then
                echo -e "${YELLOW}Removendo rede smartalarm-test...${NC}"
                # Desconectar qualquer contêiner antes de tentar remover a rede
                for container in $(docker network inspect -f '{{range .Containers}}{{.Name}} {{end}}' smartalarm-test 2>/dev/null); do
                    echo -e "${YELLOW}Desconectando ${container} da rede smartalarm-test...${NC}"
                    docker network disconnect -f smartalarm-test $container 2>/dev/null || true
                done
                docker network rm smartalarm-test 2>/dev/null || true
                echo -e "${GREEN}Rede de teste removida.${NC}"
            fi
            
            # Verificar e remover outras redes de teste que possam existir
            if docker network ls | grep -q "smart-alarm-network"; then
                echo -e "${YELLOW}Removendo rede smart-alarm-network...${NC}"
                for container in $(docker network inspect -f '{{range .Containers}}{{.Name}} {{end}}' smart-alarm-network 2>/dev/null); do
                    echo -e "${YELLOW}Desconectando ${container} da rede smart-alarm-network...${NC}"
                    docker network disconnect -f smart-alarm-network $container 2>/dev/null || true
                done
                docker network rm smart-alarm-network 2>/dev/null || true
                echo -e "${GREEN}Rede de teste removida.${NC}"
            fi
            
            # Se for modo purge, remover volumes
            if [ "$CLEAN_MODE" = "purge" ]; then
                # Confirmar antes de remover volumes
                echo -e "${RED}ATENÇÃO: Isso removerá todos os volumes do projeto.${NC}"
                echo -e "${RED}Todos os dados armazenados serão perdidos (PostgreSQL, MinIO, etc).${NC}"
                read -p "Você tem certeza que deseja continuar? (s/N) " -n 1 -r
                echo
                if [[ $REPLY =~ ^[Ss]$ ]]; then
                    # Lista de volumes a remover
                    VOLUMES=("postgres-data" "minio-data" "grafana-data")
                    for VOLUME in "${VOLUMES[@]}"; do
                        if docker volume ls | grep -q $VOLUME; then
                            echo -e "${YELLOW}Removendo volume $VOLUME...${NC}"
                            docker volume rm $VOLUME 2>/dev/null || true
                            echo -e "${GREEN}Volume $VOLUME removido.${NC}"
                        fi
                    done
                else
                    echo -e "${YELLOW}Remoção de volumes cancelada pelo usuário.${NC}"
                fi
            fi
            ;;
    esac
fi

# Função para exibir diagnóstico de redes Docker
show_docker_network_diagnostics() {
    echo -e "\n${BLUE}=== Diagnóstico de redes Docker ===${NC}"
    
    # Listar todas as redes
    echo -e "${YELLOW}Redes Docker disponíveis:${NC}"
    docker network ls
    
    # Verificar se existem redes relacionadas ao projeto
    for network in "smartalarm-test" "smart-alarm-network" "smart-alarm_default"; do
        if docker network ls | grep -q "$network"; then
            echo -e "\n${YELLOW}Detalhes da rede $network:${NC}"
            docker network inspect "$network" | grep -E '"Name"|"Driver"|"Containers"'
            
            # Listar contêineres conectados a esta rede
            echo -e "${YELLOW}Contêineres conectados à rede $network:${NC}"
            for container in $(docker network inspect -f '{{range .Containers}}{{.Name}} {{end}}' "$network" 2>/dev/null); do
                echo "- $container"
            done
        fi
    done
}

# Exibir informações sobre o estado atual
echo -e "\n${BLUE}=== Estado atual dos containers ===${NC}"
docker ps -a | grep -E "rabbitmq|postgres|minio|vault|prometheus|loki|jaeger|grafana|api" || echo -e "${YELLOW}Nenhum container relevante encontrado.${NC}"

# Mostrar diagnóstico de rede se solicitado
if [ "$1" = "debug" ]; then
    show_docker_network_diagnostics
fi

# Exibir informações sobre opções disponíveis
echo -e "\n${BLUE}=== Opções disponíveis ===${NC}"
echo -e "${YELLOW}Para reiniciar o ambiente de desenvolvimento:${NC}"
echo -e "${GREEN}./start-dev-env.sh${NC}"
echo -e "${YELLOW}Para limpar completamente o ambiente (containers e redes):${NC}"
echo -e "${GREEN}./stop-dev-env.sh clean${NC}"
echo -e "${YELLOW}Para purgar completamente o ambiente (containers, redes E volumes):${NC}"
echo -e "${GREEN}./stop-dev-env.sh purge${NC}"

echo -e "\n${GREEN}✅ Ambiente de desenvolvimento encerrado!${NC}"
