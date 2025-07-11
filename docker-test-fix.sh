#!/bin/bash

# Script para execução de testes de integração com solução de problemas de rede
# Este script resolve problemas de conectividade entre contêineres Docker

# Definições de cores para saída
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[0;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Função para imprimir mensagens com cores
print_message() {
    local color=$1
    local message=$2
    echo -e "${color}${message}${NC}"
}

print_message "${BLUE}" "=== Smart Alarm - Testes de Integração com Resolução de Rede ==="

# Detectar ambiente WSL
if [[ -f /proc/sys/fs/binfmt_misc/WSLInterop ]]; then
    print_message "${YELLOW}" "Ambiente WSL detectado"
fi

# Verificar argumentos
TEST_FILTER="Category=Integration"
VERBOSE=""

# Processar argumentos
if [[ "$1" == "essentials" ]]; then
    TEST_FILTER="Trait=Essential&Category=Integration"
    print_message "${YELLOW}" "Executando testes essenciais"
elif [[ "$1" == "minio" ]]; then
    TEST_FILTER="FullyQualifiedName~MinioIntegrationTests"
    print_message "${YELLOW}" "Executando testes do MinIO"
elif [[ "$1" == "postgres" ]]; then
    TEST_FILTER="FullyQualifiedName~PostgresIntegrationTests"
    print_message "${YELLOW}" "Executando testes do Postgres"
elif [[ "$1" == "vault" ]]; then
    TEST_FILTER="FullyQualifiedName~VaultIntegrationTests"
    print_message "${YELLOW}" "Executando testes do Vault"
elif [[ "$1" == "rabbitmq" ]]; then
    TEST_FILTER="FullyQualifiedName~RabbitMqIntegrationTests"
    print_message "${YELLOW}" "Executando testes do RabbitMQ"
elif [[ "$1" == "debug" ]]; then
    print_message "${YELLOW}" "Modo de depuração - apenas preparação do ambiente"
fi

# Verificar se modo verbose está ativado
if [[ "$2" == "-v" || "$2" == "--verbose" ]]; then
    VERBOSE="--logger \"console;verbosity=detailed\""
    print_message "${YELLOW}" "Modo verboso ativado"
fi

# Verificar comando Docker
if ! command -v docker &> /dev/null; then
    print_message "${RED}" "Docker não encontrado. Por favor, instale o Docker e tente novamente."
    exit 1
fi

# Verificar comando Docker Compose
DOCKER_COMPOSE_CMD="docker-compose"
if ! command -v docker-compose &> /dev/null; then
    if docker compose version &> /dev/null; then
        DOCKER_COMPOSE_CMD="docker compose"
        print_message "${YELLOW}" "Usando 'docker compose' (plugin)"
    else
        print_message "${RED}" "Docker Compose não encontrado. Por favor, instale o Docker Compose e tente novamente."
        exit 1
    fi
fi

# Função para limpar ambientes anteriores
cleanup_previous_resources() {
    print_message "${YELLOW}" "Limpando recursos anteriores..."
    
    # Parar e remover o contêiner de teste anterior (se existir)
    if docker ps -a | grep -q "smartalarm-test-runner"; then
        print_message "${YELLOW}" "Removendo contêiner de teste anterior..."
        docker stop smartalarm-test-runner &>/dev/null || true
        docker rm smartalarm-test-runner &>/dev/null || true
    fi
    
    # Remover rede anterior (se existir)
    if docker network ls | grep -q "smartalarm-test-net"; then
        print_message "${YELLOW}" "Removendo rede de teste anterior..."
        
        # Primeiro desconectar quaisquer contêineres conectados
        for container in $(docker network inspect -f '{{range .Containers}}{{.Name}} {{end}}' smartalarm-test-net 2>/dev/null || echo ""); do
            print_message "${YELLOW}" "Desconectando ${container} da rede..."
            docker network disconnect -f smartalarm-test-net $container &>/dev/null || true
        done
        
        docker network rm smartalarm-test-net &>/dev/null || true
    fi
}

# Função para verificar e criar rede compartilhada
setup_shared_network() {
    print_message "${BLUE}" "Configurando rede compartilhada para testes..."
    
    # Criar rede compartilhada
    if ! docker network ls | grep -q "smartalarm-test-net"; then
        docker network create smartalarm-test-net
        print_message "${GREEN}" "Rede smartalarm-test-net criada"
    else
        print_message "${YELLOW}" "Rede smartalarm-test-net já existe"
    fi
    
    # Identificar contêineres de serviço (diferentes prefixos possíveis)
    if docker ps | grep -q "smart-alarm"; then
        CONTAINER_PREFIX="smart-alarm"
    elif docker ps | grep -q "smartalarm"; then
        CONTAINER_PREFIX="smartalarm"
    else
        print_message "${RED}" "Nenhum contêiner de serviço encontrado. Execute start-dev-env.sh primeiro."
        exit 1
    fi
    
    print_message "${GREEN}" "Serviços detectados com prefixo: ${CONTAINER_PREFIX}"
    
    # Conectar contêineres de serviço à rede compartilhada
    for service in postgres rabbitmq minio vault prometheus loki jaeger grafana; do
        # Tentar diferentes padrões de nomes para encontrar o contêiner
        for pattern in "${CONTAINER_PREFIX}_${service}" "${CONTAINER_PREFIX}-${service}" "${service}"; do
            container=$(docker ps --format '{{.Names}}' | grep "$pattern" | head -n 1)
            if [[ -n "$container" ]]; then
                print_message "${YELLOW}" "Conectando ${container} à rede compartilhada..."
                docker network connect smartalarm-test-net $container 2>/dev/null || true
                break
            fi
        done
    done
    
    # Confirmar conexões
    print_message "${GREEN}" "Configuração de rede concluída. Contêineres conectados:"
    docker network inspect -f '{{range .Containers}}{{.Name}} {{end}}' smartalarm-test-net | tr " " "\n" | grep -v '^$'
}

# Função para preparar e executar contêiner de teste
run_integration_tests() {
    # Criar diretório temporário para Dockerfile
    local temp_dir=$(mktemp -d)
    print_message "${YELLOW}" "Criando imagem de teste em ${temp_dir}..."
    
    # Criar Dockerfile para imagem de teste
    cat > ${temp_dir}/Dockerfile <<EOF
FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /app
ENV DOTNET_NOLOGO=true
ENV ContinueOnError=true

# Instalar ferramentas necessárias
RUN apt-get update && apt-get install -y curl iputils-ping dnsutils

# Configurar DNS para depuração
RUN echo "nameserver 8.8.8.8" >> /etc/resolv.conf

# Script para execução dos testes
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

ENTRYPOINT ["/entrypoint.sh"]
EOF
    
    # Criar script entrypoint para execução dos testes
    cat > ${temp_dir}/entrypoint.sh <<EOF
#!/bin/bash

# Mostrar informações de rede
echo "=== Informações de rede ==="
echo "Endereço IP do contêiner:"
hostname -I
echo "Arquivo hosts:"
cat /etc/hosts
echo "Arquivo resolv.conf:"
cat /etc/resolv.conf
echo "Nomes de hosts disponíveis:"
getent hosts

# Testar conexões com serviços
echo "=== Testando conexão com serviços ==="
ping -c 2 postgres || true
ping -c 2 rabbitmq || true
ping -c 2 minio || true
ping -c 2 vault || true

# Executar testes
echo "=== Executando testes ==="
dotnet test \$*
exit \$?
EOF
    
    # Construir a imagem de teste
    print_message "${BLUE}" "Construindo imagem de teste..."
    docker build -t smartalarm-test-image:latest ${temp_dir}
    
    # Limpar arquivos temporários
    rm -rf ${temp_dir}
    
    # Preparar variáveis de ambiente para os testes
    # Usar nomes de contêineres como hosts para os serviços
    local env_vars=""
    
    # Mapeamento de contêineres para variáveis de ambiente
    declare -A service_vars=(
        ["postgres"]="POSTGRES_HOST=postgres POSTGRES_PORT=5432"
        ["rabbitmq"]="RABBITMQ_HOST=rabbitmq RABBITMQ_PORT=5672"
        ["minio"]="MINIO_HOST=minio MINIO_PORT=9000"
        ["vault"]="VAULT_HOST=vault VAULT_PORT=8200"
        ["prometheus"]="PROMETHEUS_HOST=prometheus PROMETHEUS_PORT=9090"
        ["loki"]="LOKI_HOST=loki LOKI_PORT=3100"
        ["jaeger"]="JAEGER_HOST=jaeger JAEGER_PORT=16686"
        ["grafana"]="GRAFANA_HOST=grafana GRAFANA_PORT=3000"
    )
    
    # Construir a string de variáveis de ambiente
    for service in "${!service_vars[@]}"; do
        env_vars="${env_vars} -e ${service_vars[$service]}"
    done
    
    # Se não for modo debug, executar testes
    if [[ "$1" != "debug" ]]; then
        print_message "${BLUE}" "Executando testes de integração..."
        
        # Executar os testes no contêiner conectado à rede compartilhada
        docker run --rm \
            --name smartalarm-test-runner \
            --network=smartalarm-test-net \
            --hostname test-runner \
            ${env_vars} \
            -v "$(pwd):/app" \
            smartalarm-test-image:latest \
            "/app/tests/integration/**/*.csproj" \
            "--filter" "${TEST_FILTER}" \
            ${VERBOSE}
        
        local test_exit_code=$?
        
        if [[ $test_exit_code -eq 0 ]]; then
            print_message "${GREEN}" "✅ Testes concluídos com sucesso!"
        else
            print_message "${RED}" "❌ Alguns testes falharam (código: ${test_exit_code})"
        fi
        
        return $test_exit_code
    else
        print_message "${YELLOW}" "Modo de depuração - executando contêiner para diagnóstico..."
        
        # Executar contêiner em modo interativo para diagnóstico
        docker run --rm -it \
            --name smartalarm-test-runner \
            --network=smartalarm-test-net \
            --hostname test-runner \
            ${env_vars} \
            -v "$(pwd):/app" \
            smartalarm-test-image:latest \
            "/bin/bash"
        
        return 0
    fi
}

# Função principal
main() {
    # Limpar recursos anteriores
    cleanup_previous_resources
    
    # Configurar rede compartilhada
    setup_shared_network
    
    # Mostrar contêineres em execução
    print_message "${BLUE}" "Contêineres em execução:"
    docker ps
    
    # Executar testes de integração
    run_integration_tests "$1"
    
    # Mostrar instruções finais
    print_message "${BLUE}" "=== Instruções ==="
    print_message "${YELLOW}" "- Para encerrar o ambiente: ./stop-dev-env.sh"
    print_message "${YELLOW}" "- Para depurar a rede: ./docker-test-fix.sh debug"
    print_message "${YELLOW}" "- Para executar testes específicos: ./docker-test-fix.sh [essentials|minio|postgres|vault|rabbitmq]"
}

# Executar o script
main "$1"
exit $?
