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
    
    # Criar mapeamento de nomes de serviços para nomes de contêineres
    declare -A CONTAINER_MAP
    
    # Conectar contêineres de serviço à rede compartilhada
    for service in postgres rabbitmq minio vault prometheus loki jaeger grafana; do
        # Tentar diferentes padrões de nomes para encontrar o contêiner
        for pattern in "${CONTAINER_PREFIX}_${service}" "${CONTAINER_PREFIX}-${service}" "${service}"; do
            container=$(docker ps --format '{{.Names}}' | grep "$pattern" | head -n 1)
            if [[ -n "$container" ]]; then
                print_message "${YELLOW}" "Conectando ${container} à rede compartilhada..."
                docker network connect smartalarm-test-net $container 2>/dev/null || true
                
                # Armazenar mapeamento para uso posterior
                CONTAINER_MAP[$service]=$container
                break
            fi
        done
    done
    
    # Confirmar conexões
    print_message "${GREEN}" "Configuração de rede concluída. Contêineres conectados:"
    docker network inspect -f '{{range .Containers}}{{.Name}} {{end}}' smartalarm-test-net | tr " " "\n" | grep -v '^$'
    
    # Imprimir mapeamento de nomes para depuração
    print_message "${YELLOW}" "Mapeamento de nomes de serviços para contêineres:"
    for service in "${!CONTAINER_MAP[@]}"; do
        echo "$service -> ${CONTAINER_MAP[$service]}"
    done
    
    # Exportar o mapeamento para uso em outras funções
    export CONTAINER_MAP
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

# Script para execução dos testes
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

ENTRYPOINT ["/entrypoint.sh"]
EOF
    
    # Criar script entrypoint para execução dos testes
    cat > ${temp_dir}/entrypoint.sh <<EOF
#!/bin/bash

# Adicionar mapeamentos adicionais ao /etc/hosts para resolução de nomes
{
    echo "127.0.0.1 localhost"
    echo "::1 localhost ip6-localhost ip6-loopback"
    
    # Adicionar mapeamentos para os serviços - nomes curtos
    # Usando os IPs reais dos contêineres
    # Isso é crucial para fazer a resolução de nomes funcionar
    if [ ! -z "\$SERVICE_MAPPINGS" ]; then
        echo "\$SERVICE_MAPPINGS" | tr '|' '\n' | while read mapping; do
            if [ ! -z "\$mapping" ]; then
                echo "\$mapping"
            fi
        done
    fi
    
    # Manter outras entradas do hosts original
    cat /etc/hosts | grep -v "localhost" | grep -v "postgres" | grep -v "rabbitmq" | grep -v "minio" | grep -v "vault"
} > /tmp/hosts.new

cat /tmp/hosts.new > /etc/hosts 2>/dev/null || {
    echo "Aviso: Não foi possível modificar /etc/hosts (somente leitura). Usando resolução DNS padrão."
}

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
for service in postgres rabbitmq minio vault prometheus loki jaeger grafana; do
    echo "Testando conexão com $service..."
    
    # Tentar ping para o serviço
    if ping -c 2 -W 1 $service &>/dev/null; then
        echo "✓ Conexão com $service bem-sucedida"
    else
        echo "✗ Falha ao conectar com $service"
        echo "  Verificando resolução DNS:"
        getent hosts $service || echo "  DNS não consegue resolver $service"
        echo "  Tentando ping pelo IP direto:"
        getent hosts $service | awk '{print $1}' | xargs -r ping -c 1 || true
    fi
done

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
    # Usar nomes simples dos serviços como hosts para os testes
    local env_vars=""
    
    # Configurar variáveis de ambiente com nomes de serviço simples
    # Isso permite que os testes usem postgres ao invés de smart-alarm_postgres_1
    env_vars="-e POSTGRES_HOST=postgres \
              -e RABBITMQ_HOST=rabbitmq \
              -e MINIO_HOST=minio \
              -e VAULT_HOST=vault \
              -e PROMETHEUS_HOST=prometheus \
              -e LOKI_HOST=loki \
              -e JAEGER_HOST=jaeger \
              -e GRAFANA_HOST=grafana"
    
    # Adicionar portas padrão às variáveis de ambiente
    env_vars="${env_vars} -e POSTGRES_PORT=5432 \
              -e RABBITMQ_PORT=5672 \
              -e MINIO_PORT=9000 \
              -e VAULT_PORT=8200 \
              -e PROMETHEUS_PORT=9090 \
              -e LOKI_PORT=3100 \
              -e JAEGER_PORT=16686 \
              -e GRAFANA_PORT=3000"
    
    # Se não for modo debug, executar testes
    if [[ "$1" != "debug" ]]; then
        print_message "${BLUE}" "Executando testes de integração..."
        
        # Permitir que o contêiner modifique /etc/hosts se necessário
        docker_opts="--cap-add=NET_ADMIN"
        
        # Executar os testes no contêiner conectado à rede compartilhada
        # Configurando a resolução de hosts usando aliases de rede
        local add_hosts=""
        
        # Obter IPs dos contêineres de forma segura
        for service in postgres rabbitmq minio vault prometheus loki jaeger grafana; do
            local container="${CONTAINER_PREFIX}_${service}_1"
            local container_ip=$(docker inspect -f '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' "$container" 2>/dev/null)
            
            if [[ -n "$container_ip" && "$container_ip" != "" ]]; then
                add_hosts="$add_hosts --add-host=${service}:${container_ip} "
            fi
        done
        
        # Executar contêiner com hosts adicionados
        docker run --rm \
            --name smartalarm-test-runner \
            --network=smartalarm-test-net \
            --hostname test-runner \
            ${docker_opts} \
            ${add_hosts} \
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
        
        # Permitir que o contêiner modifique /etc/hosts se necessário
        docker_opts="--cap-add=NET_ADMIN"
        
        # Executar contêiner em modo interativo para diagnóstico
        # Configurando a resolução de hosts usando aliases de rede
        local add_hosts=""
        
        # Obter IPs dos contêineres de forma segura
        for service in postgres rabbitmq minio vault prometheus loki jaeger grafana; do
            local container="${CONTAINER_PREFIX}_${service}_1"
            local container_ip=$(docker inspect -f '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' "$container" 2>/dev/null)
            
            if [[ -n "$container_ip" && "$container_ip" != "" ]]; then
                add_hosts="$add_hosts --add-host=${service}:${container_ip} "
            fi
        done
        
        # Executar contêiner com hosts adicionados e modo interativo
        docker run --rm -it \
            --name smartalarm-test-runner \
            --network=smartalarm-test-net \
            --hostname test-runner \
            ${docker_opts} \
            ${add_hosts} \
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
