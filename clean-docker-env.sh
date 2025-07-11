#!/bin/bash

# Define cores para melhor legibilidade
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${BLUE}=== Smart Alarm - Limpeza do Ambiente Docker ===${NC}"

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

echo -e "${YELLOW}Parando todos os serviços do Docker Compose...${NC}"
$DOCKER_COMPOSE_CMD down

echo -e "${YELLOW}Removendo contêineres relacionados ao Smart Alarm...${NC}"
for container in $(docker ps -a | grep 'smart-alarm\|rabbitmq\|postgres\|minio\|vault' | awk '{print $1}'); do
    echo -e "Removendo contêiner $container..."
    docker rm -f $container
done

echo -e "${YELLOW}Removendo rede smart-alarm-network...${NC}"
docker network rm smart-alarm-network >/dev/null 2>&1 || true

echo -e "${YELLOW}Removendo imagens não utilizadas...${NC}"
docker image prune -f

echo -e "${GREEN}Ambiente limpo com sucesso!${NC}"
echo -e "${YELLOW}Para reiniciar o ambiente de desenvolvimento, execute:${NC}"
echo -e "${GREEN}./start-dev-env.sh${NC}"
