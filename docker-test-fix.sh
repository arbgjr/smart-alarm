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

# Função para obter IP de um contêiner
get_container_ip() {
    local container_name=$1
    local network_name=${2:-"smartalarm-test-net"}
    
    # Verificar se o contêiner existe
    if ! docker ps --format "{{.Names}}" | grep -q "^${container_name}$"; then
        echo ""
        return 1
    fi
    
    # Tentar obter IP da rede específica primeiro
    local ip=$(docker inspect "$container_name" 2>/dev/null | \
        grep -A 10 "\"$network_name\"" | \
        grep '"IPAddress"' | \
        head -1 | \
        cut -d'"' -f4)
    
    # Se não conseguiu da rede específica, tentar da rede padrão
    if [[ -z "$ip" || "$ip" == "" ]]; then
        ip=$(docker inspect "$container_name" \
            --format='{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' \
            2>/dev/null | head -1)
    fi
    
    # Validar se é um IP válido
    if [[ "$ip" =~ ^[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
        echo "$ip"
        return 0
    else
        echo ""
        return 1
    fi
}

# Função para gerar mapeamentos de host
generate_host_mappings() {
    local host_mappings=""
    
    print_message "${BLUE}" "Gerando mapeamentos de host para contêineres..." >&2
    
    # Detectar o prefixo dos contêineres
    local prefix="smart-alarm"
    if docker ps | grep -q "smart-alarm"; then
        prefix="smart-alarm"
    elif docker ps | grep -q "smartalarm"; then
        prefix="smartalarm"
    fi
    
    # Lista de serviços para mapear (ordem de prioridade)
    local services=("postgres" "vault" "minio" "rabbitmq" "prometheus" "loki" "jaeger" "grafana")
    
    for service in "${services[@]}"; do
        local container_name="${prefix}-${service}-1"
        
        # Verificar se o contêiner existe
        if ! docker ps --format "{{.Names}}" | grep -q "^${container_name}$"; then
            print_message "${YELLOW}" "  ⚠️  Contêiner $container_name não encontrado" >&2
            continue
        fi
        
        # Tentar obter IP do contêiner na rede de teste
        local ip=$(get_container_ip "$container_name" "smartalarm-test-net")
        
        # Se não conseguiu da rede de teste, tentar da rede padrão
        if [[ -z "$ip" || "$ip" == "" ]]; then
            ip=$(docker inspect "$container_name" --format='{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' 2>/dev/null | head -1)
        fi
        
        if [[ -n "$ip" && "$ip" =~ ^[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
            host_mappings="$host_mappings --add-host $service:$ip"
            print_message "${GREEN}" "  ✅ $service -> $container_name ($ip)" >&2
        else
            print_message "${RED}" "  ❌ $service -> $container_name (IP inválido: '$ip')" >&2
        fi
    done
    
    # Verificar se temos pelo menos os serviços essenciais
    if [[ "$host_mappings" == *"postgres"* ]]; then
        print_message "${GREEN}" "Mapeamentos essenciais detectados" >&2
    else
        print_message "${RED}" "ERRO: Postgres não mapeado! Host mappings: $host_mappings" >&2
    fi
    
    echo "$host_mappings"
}
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
    
    # Lista de serviços para conectar
    local services=("postgres" "rabbitmq" "minio" "vault" "prometheus" "loki" "jaeger" "grafana")
    
    # Conectar contêineres de serviço à rede compartilhada
    for service in "${services[@]}"; do
        local container_name="${CONTAINER_PREFIX}-${service}-1"
        
        # Verificar se o contêiner existe e está em execução
        if docker ps --format '{{.Names}}' | grep -q "^${container_name}$"; then
            print_message "${YELLOW}" "Conectando ${container_name} à rede compartilhada..."
            
            # Verificar se já está conectado antes de tentar conectar
            if ! docker network inspect smartalarm-test-net --format '{{range .Containers}}{{.Name}} {{end}}' | grep -q "${container_name}"; then
                docker network connect smartalarm-test-net "$container_name" 2>/dev/null || {
                    print_message "${YELLOW}" "  Aviso: ${container_name} pode já estar conectado à rede"
                }
            else
                print_message "${GREEN}" "  ${container_name} já está conectado à rede"
            fi
        else
            print_message "${YELLOW}" "  ⚠️  Contêiner ${container_name} não encontrado ou não está em execução"
        fi
    done
    
    # Confirmar conexões
    print_message "${GREEN}" "Configuração de rede concluída. Contêineres conectados:"
    docker network inspect -f '{{range .Containers}}{{.Name}} {{end}}' smartalarm-test-net | tr " " "\n" | grep -v '^$' | sort
    
    # Imprimir IPs dos contêineres para diagnóstico
    print_message "${YELLOW}" "Mapeamento de IPs na rede smartalarm-test-net:"
    for service in "${services[@]}"; do
        local container_name="${CONTAINER_PREFIX}-${service}-1"
        local ip=$(get_container_ip "$container_name" "smartalarm-test-net")
        if [[ -n "$ip" && "$ip" =~ ^[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
            echo "  $service -> $container_name -> $ip"
        fi
    done
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

# Instalar ferramentas necessárias (incluindo netcat para testar conectividade)
RUN apt-get update && apt-get install -y curl iputils-ping dnsutils netcat-openbsd

# Script para execução dos testes
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

ENTRYPOINT ["/entrypoint.sh"]
EOF
    
    # Criar script entrypoint para execução dos testes
    cat > ${temp_dir}/entrypoint.sh <<EOF
#!/bin/bash

# Aguardar um pouco para a rede se estabilizar
sleep 2

# Mostrar informações de rede
echo "=== Informações de rede ==="
echo "Endereço IP do contêiner:"
hostname -I
echo "Arquivo hosts original:"
cat /etc/hosts
echo "Arquivo resolv.conf:"
cat /etc/resolv.conf

# Aguardar que os serviços estejam disponíveis
echo "=== Aguardando serviços ficarem disponíveis ==="
wait_for_service() {
    local service=\$1
    local port=\$2
    local timeout=30
    local count=0
    
    echo "Aguardando \$service:\$port..."
    while ! nc -z \$service \$port 2>/dev/null; do
        if [ \$count -ge \$timeout ]; then
            echo "⚠️ Timeout aguardando \$service:\$port"
            return 1
        fi
        sleep 1
        count=\$((count + 1))
    done
    echo "✅ \$service:\$port está disponível"
    return 0
}

# Aguardar serviços críticos
wait_for_service postgres 5432
wait_for_service vault 8200
wait_for_service minio 9000
wait_for_service rabbitmq 5672

# Testar conectividade de rede com cada serviço
echo "=== Testando conectividade de rede ==="
services=("postgres" "rabbitmq" "minio" "vault" "prometheus" "loki" "jaeger" "grafana")

for service in "\${services[@]}"; do
    echo "Testando conectividade com \$service..."
    
    # Verificar resolução DNS primeiro
    if getent hosts \$service >/dev/null 2>&1; then
        echo "  ✓ DNS resolve \$service"
        
        # Tentar ping
        if ping -c 2 -W 1 \$service &>/dev/null; then
            echo "  ✓ Ping para \$service bem-sucedido"
        else
            echo "  ⚠️ Ping para \$service falhou"
        fi
        
        # Testar conectividade da porta específica
        case "\$service" in
            postgres) port=5432 ;;
            rabbitmq) port=5672 ;;
            minio) port=9000 ;;
            vault) port=8200 ;;
            prometheus) port=9090 ;;
            loki) port=3100 ;;
            jaeger) port=16686 ;;
            grafana) port=3000 ;;
        esac
        
        if nc -z \$service \$port 2>/dev/null; then
            echo "  ✅ \$service:\$port está acessível"
        else
            echo "  ❌ \$service:\$port não está acessível"
        fi
    else
        echo "  ❌ DNS não consegue resolver \$service"
        
        # Tentar usar variáveis de ambiente
        service_upper=\$(echo \$service | tr '[:lower:]' '[:upper:]')
        case "\$service_upper" in
            POSTGRES) host_value="\$POSTGRES_HOST" ;;
            RABBITMQ) host_value="\$RABBITMQ_HOST" ;;
            MINIO) host_value="\$MINIO_HOST" ;;
            VAULT) host_value="\$VAULT_HOST" ;;
            PROMETHEUS) host_value="\$PROMETHEUS_HOST" ;;
            LOKI) host_value="\$LOKI_HOST" ;;
            JAEGER) host_value="\$JAEGER_HOST" ;;
            GRAFANA) host_value="\$GRAFANA_HOST" ;;
        esac
        
        if [ -n "\$host_value" ]; then
            echo "  Tentando via variável de ambiente: \$host_value"
            if getent hosts "\$host_value" >/dev/null 2>&1; then
                echo "  ✓ Variável de ambiente resolve"
            else
                echo "  ❌ Variável de ambiente não resolve"
            fi
        fi
    fi
    echo ""
done

# Verificar se temos argumentos válidos para dotnet test
if [ \$# -eq 0 ]; then
    echo "⚠️ Nenhum argumento fornecido para dotnet test"
    echo "Modo interativo - iniciando bash..."
    exec /bin/bash
else
    # Executar testes apenas se temos projetos válidos
    echo "=== Executando testes ==="
    echo "Argumentos recebidos: \$*"
    
    # Verificar se o primeiro argumento é um arquivo de projeto válido
    first_arg="\$1"
    if [[ "\$first_arg" == *.csproj ]]; then
        echo "Executando testes com dotnet test..."
        dotnet test \$*
        exit \$?
    else
        echo "⚠️ Primeiro argumento não é um arquivo .csproj válido: \$first_arg"
        echo "Modo interativo - iniciando bash..."
        exec /bin/bash
    fi
fi
EOF
    
    # Construir a imagem de teste
    print_message "${BLUE}" "Construindo imagem de teste..."
    docker build -t smartalarm-test-image:latest ${temp_dir}
    
    # Limpar arquivos temporários
    rm -rf ${temp_dir}
    
    # Preparar variáveis de ambiente para os testes
    # Usar nomes em maiúsculas para compatibilidade com DockerHelper
    local env_vars=""
    
    # Configurar variáveis de ambiente com nomes de serviço em maiúsculas
    # Isso permite que os testes usem POSTGRES_HOST ao invés de nomes hardcoded
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
    
    # Adicionar credenciais específicas para PostgreSQL
    env_vars="${env_vars} -e POSTGRES_USER=smartalarm \
              -e POSTGRES_PASSWORD=smartalarm123 \
              -e POSTGRES_DB=smartalarm"
    
    # Se não for modo debug, executar testes
    if [[ "$1" != "debug" ]]; then
        print_message "${BLUE}" "Executando testes de integração..."
        
        # Gerar mapeamentos de host dinamicamente
        local host_mappings=$(generate_host_mappings)
        
        # Encontrar os projetos de teste de integração
        local test_projects=""
        if [[ -d "$(pwd)/tests" ]]; then
            test_projects=$(find "$(pwd)/tests" -name "*Integration*.csproj" 2>/dev/null | head -1)
        fi
        
        if [[ -z "$test_projects" ]]; then
            print_message "${RED}" "Nenhum projeto de teste de integração encontrado!"
            print_message "${YELLOW}" "Buscando qualquer projeto de teste..."
            test_projects=$(find "$(pwd)/tests" -name "*.csproj" 2>/dev/null | head -1)
        fi
        
        if [[ -z "$test_projects" ]]; then
            print_message "${RED}" "Nenhum projeto de teste encontrado! Verifique a estrutura do projeto."
            return 1
        fi
        
        print_message "${YELLOW}" "Projeto de teste selecionado: $test_projects"
        
        # Executar contêiner com hosts mapeados
        docker run --rm \
            --name smartalarm-test-runner \
            --network=smartalarm-test-net \
            --hostname test-runner \
            ${host_mappings} \
            ${env_vars} \
            -v "$(pwd):/app" \
            smartalarm-test-image:latest \
            "$test_projects" \
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
        # Gerar mapeamentos de host dinamicamente
        local host_mappings=$(generate_host_mappings)
        
        print_message "${YELLOW}" "Host mappings que serão aplicados:"
        echo "  $host_mappings"
        
        # Executar contêiner com hosts mapeados e modo interativo
        docker run --rm -it \
            --name smartalarm-test-runner \
            --network=smartalarm-test-net \
            --hostname test-runner \
            ${host_mappings} \
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
