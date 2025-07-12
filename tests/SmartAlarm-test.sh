#!/bin/bash

clear

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

# Função para mostrar ajuda
show_help() {
    print_message "${BLUE}" "=== Smart Alarm - Testes de Integração ==="
    echo ""
    print_message "${YELLOW}" "Uso: $0 [opção] [-v|--verbose]"
    echo ""
    print_message "${GREEN}" "📋 Opções disponíveis:"
    echo ""
    print_message "${CYAN}" "🧪 Testes Básicos (sem containers):"
    echo "  basic       - Todos os testes básicos (OWASP + Security)"
    echo "  owasp       - Testes de segurança OWASP Top 10"
    echo "  security    - Testes de componentes de segurança"
    echo "  all-security- Todos os testes de segurança"
    echo ""
    print_message "${CYAN}" "🐳 Testes de Integração (com containers):"
    echo "  postgres    - Testes do PostgreSQL"
    echo "  vault       - Testes do HashiCorp Vault"
    echo "  minio       - Testes do MinIO"
    echo "  rabbitmq    - Testes do RabbitMQ"
    echo "  jwt-fido2   - Testes de autenticação JWT/FIDO2"
    echo "  essentials  - Testes essenciais marcados"
    echo ""
    print_message "${CYAN}" "📊 Análise e Depuração:"
    echo "  coverage    - Testes com análise de cobertura"
    echo "  debug       - Modo interativo para depuração"
    echo ""
    print_message "${CYAN}" "🆘 Ajuda:"
    echo "  help, -h, --help - Mostra esta ajuda"
    echo ""
    print_message "${YELLOW}" "Exemplos:"
    echo "  $0 basic              # Testes rápidos sem containers"
    echo "  $0 postgres -v        # Testes PostgreSQL com saída detalhada"
    echo "  $0 coverage           # Análise de cobertura completa"
    echo "  $0 debug              # Modo interativo para diagnóstico"
    echo ""
    print_message "${GREEN}" "💡 Dica: Use 'basic' para validação rápida durante desenvolvimento!"
}

print_message "${BLUE}" "=== Smart Alarm - Testes de Integração com Resolução de Rede ==="

# Detectar diretório raiz do projeto
if [[ -f "docker-compose.yml" ]]; then
    # Já estamos no diretório raiz
    PROJECT_ROOT="$(pwd)"
elif [[ -f "../docker-compose.yml" ]]; then
    # Estamos no diretório tests
    PROJECT_ROOT="$(dirname "$(pwd)")"
else
    # Tentar encontrar o diretório raiz
    PROJECT_ROOT="$(pwd)"
    while [[ ! -f "$PROJECT_ROOT/docker-compose.yml" && "$PROJECT_ROOT" != "/" ]]; do
        PROJECT_ROOT="$(dirname "$PROJECT_ROOT")"
    done
    if [[ ! -f "$PROJECT_ROOT/docker-compose.yml" ]]; then
        print_message "${RED}" "❌ Não foi possível encontrar o diretório raiz do projeto (docker-compose.yml)"
        exit 1
    fi
fi

print_message "${YELLOW}" "📍 Diretório do projeto: $PROJECT_ROOT"

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
    TEST_FILTER="FullyQualifiedName~MinioIntegrationTests|FullyQualifiedName~Minio|Category=Integration"
    print_message "${YELLOW}" "Executando testes do MinIO"
elif [[ "$1" == "postgres" ]]; then
    TEST_FILTER="FullyQualifiedName~PostgresIntegrationTests|FullyQualifiedName~Postgres|Category=Integration"
    print_message "${YELLOW}" "Executando testes do Postgres"
elif [[ "$1" == "vault" ]]; then
    TEST_FILTER="FullyQualifiedName~VaultIntegrationTests|FullyQualifiedName~Vault|Category=Integration"
    print_message "${YELLOW}" "Executando testes do Vault"
elif [[ "$1" == "rabbitmq" ]]; then
    TEST_FILTER="FullyQualifiedName~RabbitMqIntegrationTests|FullyQualifiedName~RabbitMq|Category=Integration"
    print_message "${YELLOW}" "Executando testes do RabbitMQ"
elif [[ "$1" == "jwt-fido2" ]]; then
    TEST_FILTER="FullyQualifiedName~JwtFido2|FullyQualifiedName~BasicJwtFido2Tests|Category=Integration"
    print_message "${YELLOW}" "Executando testes de autenticação JWT/FIDO2"
elif [[ "$1" == "owasp" ]]; then
    TEST_FILTER="FullyQualifiedName~Owasp|FullyQualifiedName~BasicOwaspSecurityTests|Category=Security"
    print_message "${YELLOW}" "Executando testes de segurança OWASP"
elif [[ "$1" == "security" ]]; then
    TEST_FILTER="FullyQualifiedName~Security|FullyQualifiedName~BasicSecurityComponentsTests|Category=Security"
    print_message "${YELLOW}" "Executando testes de componentes de segurança"
elif [[ "$1" == "basic" ]]; then
    TEST_FILTER="FullyQualifiedName~Basic|Category=Integration|Category=Security"
    print_message "${YELLOW}" "Executando testes básicos (sem containers)"
elif [[ "$1" == "all-security" ]]; then
    TEST_FILTER="Category=Security|FullyQualifiedName~Owasp|FullyQualifiedName~Security"
    print_message "${YELLOW}" "Executando todos os testes de segurança"
elif [[ "$1" == "coverage" ]]; then
    TEST_FILTER="FullyQualifiedName~BasicOwaspSecurityTests|FullyQualifiedName~BasicSecurityComponentsTests|FullyQualifiedName~CreateAlarmDtoValidatorTests|FullyQualifiedName~ErrorMessageServiceTests"
    VERBOSE="--logger console;verbosity=detailed --collect XPlat Code Coverage --settings tests/coverlet.runsettings"
    print_message "${YELLOW}" "Executando análise de cobertura em testes básicos funcionais"
elif [[ "$1" == "help" || "$1" == "-h" || "$1" == "--help" ]]; then
    show_help
    exit 0
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
    
    # Limpar redes órfãs do Docker
    print_message "${YELLOW}" "Limpando redes órfãs..."
    docker network prune -f &>/dev/null || true
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
        print_message "${YELLOW}" "Nenhum contêiner de serviço encontrado. Iniciando serviços automaticamente..."
        
        # Verificar se o arquivo docker-compose.yml existe
        if [[ ! -f "docker-compose.yml" ]]; then
            print_message "${RED}" "Arquivo docker-compose.yml não encontrado no diretório atual."
            print_message "${YELLOW}" "Certifique-se de estar no diretório raiz do projeto."
            exit 1
        fi
        
        # Limpar ambiente Docker completamente antes de iniciar
        print_message "${BLUE}" "Limpando ambiente Docker anterior..."
        ${DOCKER_COMPOSE_CMD} down --volumes --remove-orphans 2>/dev/null || true
        
        # Aguardar um pouco para o Docker limpar recursos
        sleep 3
        
        # Iniciar os serviços
        print_message "${BLUE}" "Iniciando serviços com Docker Compose..."
        ${DOCKER_COMPOSE_CMD} up -d
        
        if [[ $? -ne 0 ]]; then
            print_message "${RED}" "Falha ao iniciar os serviços. Verifique a configuração do Docker Compose."
            exit 1
        fi
        
        # Aguardar os serviços ficarem prontos
        print_message "${YELLOW}" "Aguardando serviços ficarem prontos..."
        sleep 15
        
        # Verificar novamente os contêineres
        if docker ps | grep -q "smart-alarm"; then
            CONTAINER_PREFIX="smart-alarm"
            print_message "${GREEN}" "Serviços iniciados com sucesso! Prefixo: ${CONTAINER_PREFIX}"
        elif docker ps | grep -q "smartalarm"; then
            CONTAINER_PREFIX="smartalarm"
            print_message "${GREEN}" "Serviços iniciados com sucesso! Prefixo: ${CONTAINER_PREFIX}"
        else
            print_message "${RED}" "Falha ao iniciar os serviços. Contêineres não encontrados após inicialização."
            print_message "${YELLOW}" "Verificando logs de erros..."
            ${DOCKER_COMPOSE_CMD} logs --tail=10
            
            # Tentar uma segunda vez
            print_message "${YELLOW}" "Tentando reiniciar serviços..."
            ${DOCKER_COMPOSE_CMD} down --volumes --remove-orphans 2>/dev/null || true
            sleep 5
            ${DOCKER_COMPOSE_CMD} up -d
            sleep 15
            
            if docker ps | grep -q "smart-alarm\|smartalarm"; then
                if docker ps | grep -q "smart-alarm"; then
                    CONTAINER_PREFIX="smart-alarm"
                else
                    CONTAINER_PREFIX="smartalarm"
                fi
                print_message "${GREEN}" "Serviços iniciados na segunda tentativa! Prefixo: ${CONTAINER_PREFIX}"
            else
                print_message "${RED}" "Falha definitiva ao iniciar os serviços."
                exit 1
            fi
        fi
    fi
    
    print_message "${GREEN}" "Serviços detectados com prefixo: ${CONTAINER_PREFIX}"
    
    # Aguardar que os serviços estejam prontos para aceitar conexões
    print_message "${BLUE}" "Verificando saúde dos serviços..."
    
    # Função para aguardar um serviço ficar disponível
    wait_for_service() {
        local service_name=$1
        local port=$2
        local timeout=${3:-30}
        local count=0
        
        local container_name="${CONTAINER_PREFIX}-${service_name}-1"
        
        # Verificar se o contêiner existe
        if ! docker ps --format "{{.Names}}" | grep -q "^${container_name}$"; then
            print_message "${YELLOW}" "  ⚠️  Contêiner ${container_name} não encontrado"
            return 1
        fi
        
        # Obter IP do contêiner
        local ip=$(get_container_ip "$container_name")
        if [[ -z "$ip" ]]; then
            print_message "${YELLOW}" "  ⚠️  Não foi possível obter IP do ${container_name}"
            return 1
        fi
        
        print_message "${YELLOW}" "  Aguardando ${service_name} (${ip}:${port})..."
        
        while ! nc -z "$ip" "$port" 2>/dev/null; do
            if [ $count -ge $timeout ]; then
                print_message "${RED}" "  ❌ Timeout aguardando ${service_name}"
                return 1
            fi
            sleep 1
            count=$((count + 1))
        done
        
        print_message "${GREEN}" "  ✅ ${service_name} está pronto"
        return 0
    }
    
    # Aguardar serviços essenciais (sem falhar se algum não estiver disponível)
    print_message "${BLUE}" "Verificando disponibilidade dos serviços essenciais..."
    wait_for_service "postgres" 5432 60 || print_message "${YELLOW}" "  ⚠️  PostgreSQL pode não estar totalmente pronto"
    wait_for_service "vault" 8200 30 || print_message "${YELLOW}" "  ⚠️  Vault pode não estar totalmente pronto"
    wait_for_service "minio" 9000 30 || print_message "${YELLOW}" "  ⚠️  MinIO pode não estar totalmente pronto"
    wait_for_service "rabbitmq" 5672 30 || print_message "${YELLOW}" "  ⚠️  RabbitMQ pode não estar totalmente pronto"
    
    # Função adicional para verificação detalhada da conectividade (baseada no PowerShell)
    verify_service_connectivity() {
        print_message "${BLUE}" "🔗 Testando conectividade detalhada..."
        
        # Vault
        local container_name="${CONTAINER_PREFIX}-vault-1"
        if docker ps --format "{{.Names}}" | grep -q "^${container_name}$"; then
            local vault_ip=$(get_container_ip "$container_name")
            if [[ -n "$vault_ip" ]]; then
                if curl -s "http://${vault_ip}:8200/v1/sys/health" >/dev/null 2>&1; then
                    print_message "${GREEN}" "✅ Vault: OK (${vault_ip}:8200)"
                else
                    print_message "${RED}" "❌ Vault: Falha na conectividade HTTP"
                fi
            fi
        fi
        
        # PostgreSQL
        container_name="${CONTAINER_PREFIX}-postgres-1"
        if docker ps --format "{{.Names}}" | grep -q "^${container_name}$"; then
            if docker exec "$container_name" pg_isready -h localhost -p 5432 >/dev/null 2>&1; then
                print_message "${GREEN}" "✅ PostgreSQL: OK"
            else
                print_message "${RED}" "❌ PostgreSQL: Falha na conectividade"
            fi
        fi
        
        # RabbitMQ
        container_name="${CONTAINER_PREFIX}-rabbitmq-1"
        if docker ps --format "{{.Names}}" | grep -q "^${container_name}$"; then
            local rabbit_ip=$(get_container_ip "$container_name")
            if [[ -n "$rabbit_ip" ]]; then
                # Verificar API de management do RabbitMQ
                if curl -s -u guest:guest "http://${rabbit_ip}:15672/api/overview" >/dev/null 2>&1; then
                    print_message "${GREEN}" "✅ RabbitMQ: OK (${rabbit_ip}:15672)"
                else
                    print_message "${YELLOW}" "⚠️  RabbitMQ: API management pode não estar pronta"
                fi
            fi
        fi
        
        # MinIO
        container_name="${CONTAINER_PREFIX}-minio-1"
        if docker ps --format "{{.Names}}" | grep -q "^${container_name}$"; then
            local minio_ip=$(get_container_ip "$container_name")
            if [[ -n "$minio_ip" ]]; then
                if curl -s "http://${minio_ip}:9000/minio/health/live" >/dev/null 2>&1; then
                    print_message "${GREEN}" "✅ MinIO: OK (${minio_ip}:9000)"
                else
                    print_message "${RED}" "❌ MinIO: Falha na conectividade"
                fi
            fi
        fi
    }
    
    print_message "${GREEN}" "Verificação de serviços concluída. Prosseguindo com a configuração de rede..."
    
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
    
    # Executar verificação detalhada de conectividade
    verify_service_connectivity
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

# Verificar argumentos
echo "=== Argumentos recebidos ==="
echo "Total de argumentos: \$#"
echo "Argumentos: \$@"

# Lógica para executar testes ou entrar no modo interativo
if [ \$# -eq 0 ]; then
    echo "⚠️ Nenhum argumento fornecido"
    echo "Modo interativo - iniciando bash..."
    exec /bin/bash
elif [ "\$1" = "/bin/bash" ]; then
    echo "Modo interativo solicitado - iniciando bash..."
    exec /bin/bash
else
    # Executar testes
    echo "=== Executando testes de integração ==="
    
    # Verificar se o primeiro argumento é um arquivo de projeto válido
    first_arg="\$1"
    if [[ "\$first_arg" == *.csproj ]]; then
        echo "✅ Arquivo de projeto detectado: \$first_arg"
        if [ -f "\$first_arg" ]; then
            echo "✅ Arquivo de projeto existe"
            
            # Construir comando dotnet test cuidadosamente para argumentos com espaços
            cmd="dotnet test"
            for arg in "\$@"; do
                # Se o argumento contém espaços e não está entre aspas, adicionar aspas
                if [[ "\$arg" =~ [[:space:]] && ! "\$arg" =~ ^[\'\"].*[\'\"]$ ]]; then
                    cmd="\$cmd \"\$arg\""
                else
                    cmd="\$cmd \$arg"
                fi
            done
            
            echo "Executando: \$cmd"
            eval "\$cmd"
            exit_code=\$?
            echo "Código de saída dos testes: \$exit_code"
            exit \$exit_code
        else
            echo "❌ Arquivo de projeto não existe: \$first_arg"
            exit 1
        fi
    else
        echo "❌ Primeiro argumento não é um arquivo .csproj válido: \$first_arg"
        echo "Tentando executar como comando dotnet..."
        dotnet "\$@"
        exit_code=\$?
        echo "Código de saída: \$exit_code"
        exit \$exit_code
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
        # Verificar se é teste básico (sem containers)
        if [[ "$1" == "basic" || "$1" == "owasp" || "$1" == "security" || "$1" == "all-security" ]]; then
            print_message "${BLUE}" "🧪 Executando testes básicos (sem containers)..."
            
            # Definir caminhos dos arquivos de teste usando PROJECT_ROOT
            local owasp_test_file="$PROJECT_ROOT/tests/SmartAlarm.Tests/Security/BasicOwaspSecurityTests.cs"
            local security_test_file="$PROJECT_ROOT/tests/SmartAlarm.Tests/Unit/BasicSecurityComponentsTests.cs"
            local smartalarm_tests_project="$PROJECT_ROOT/tests/SmartAlarm.Tests/SmartAlarm.Tests.csproj"
            
            # Verificar se o projeto principal existe
            if [[ ! -f "$smartalarm_tests_project" ]]; then
                print_message "${RED}" "❌ Projeto SmartAlarm.Tests não encontrado em: $smartalarm_tests_project"
                return 1
            fi
            
            # Função para executar teste (com ou sem container)
            run_basic_test() {
                local test_filter="$1"
                local test_description="$2"
                
                print_message "${CYAN}" "$test_description"
                
                # Verificar se dotnet está disponível no host
                if command -v dotnet &> /dev/null; then
                    print_message "${YELLOW}" "Usando dotnet do host..."
                    dotnet test "$smartalarm_tests_project" --filter "$test_filter" --logger "console;verbosity=detailed"
                    local test_exit_code=$?
                else
                    print_message "${YELLOW}" "dotnet não encontrado no host, usando container Docker..."
                    
                    # Converter caminho para o container
                    local container_project_path="/app/tests/SmartAlarm.Tests/SmartAlarm.Tests.csproj"
                    
                    # Executar no container Docker
                    docker run --rm \
                        -v "$PROJECT_ROOT:/app" \
                        mcr.microsoft.com/dotnet/sdk:8.0 \
                        dotnet test "$container_project_path" --filter "$test_filter" --logger "console;verbosity=detailed"
                    local test_exit_code=$?
                fi
                
                if [[ $test_exit_code -eq 0 ]]; then
                    print_message "${GREEN}" "✅ Testes concluídos com sucesso!"
                else
                    print_message "${RED}" "❌ Alguns testes falharam (código: ${test_exit_code})"
                fi
                
                return $test_exit_code
            }
            
            if [[ "$1" == "owasp" ]]; then
                if [[ -f "$owasp_test_file" ]]; then
                    run_basic_test "FullyQualifiedName~BasicOwaspSecurityTests" "🔒 Executando testes de segurança OWASP..."
                    local test_exit_code=$?
                else
                    print_message "${RED}" "❌ Arquivo BasicOwaspSecurityTests.cs não encontrado em: $owasp_test_file"
                    local test_exit_code=1
                fi
            elif [[ "$1" == "security" ]]; then
                if [[ -f "$security_test_file" ]]; then
                    run_basic_test "FullyQualifiedName~BasicSecurityComponentsTests" "🧩 Executando testes de componentes unitários..."
                    local test_exit_code=$?
                else
                    print_message "${RED}" "❌ Arquivo BasicSecurityComponentsTests.cs não encontrado em: $security_test_file"
                    local test_exit_code=1
                fi
            elif [[ "$1" == "all-security" ]]; then
                run_basic_test "FullyQualifiedName~BasicOwaspSecurityTests|FullyQualifiedName~BasicSecurityComponentsTests" "� Executando todos os testes de segurança..."
            elif [[ "$1" == "basic" ]]; then
                run_basic_test "FullyQualifiedName~BasicOwaspSecurityTests|FullyQualifiedName~BasicSecurityComponentsTests" "📊 Executando todos os testes básicos..."
            fi
            
            print_message "${GREEN}" "✅ Testes básicos concluídos!"
            print_message "${YELLOW}" "📋 Estes testes rodam sem dependências externas."
            print_message "${CYAN}" "🐳 Para testes completos de integração, execute: ./tests/docker-test.sh [postgres|vault|minio|rabbitmq]"
            # O código de retorno já foi definido na função run_basic_test
            return $test_exit_code
        fi
        
        print_message "${BLUE}" "Executando testes de integração..."
        
        # Gerar mapeamentos de host dinamicamente
        local host_mappings=$(generate_host_mappings)
        
        # Encontrar os projetos de teste de integração
        local test_projects=""
        
        # Se estamos executando testes específicos, buscar o projeto correspondente
        if [[ "$1" == "postgres" ]]; then
            test_projects=$(find "$PROJECT_ROOT/tests" -name "*Infrastructure*.csproj" 2>/dev/null | head -1)
            print_message "${YELLOW}" "Buscando testes de infraestrutura para PostgreSQL..."
        elif [[ "$1" == "vault" ]]; then
            test_projects=$(find "$PROJECT_ROOT/tests" -name "*KeyVault*.csproj" 2>/dev/null | head -1)
            print_message "${YELLOW}" "Buscando testes de KeyVault para Vault..."
        elif [[ "$1" == "minio" || "$1" == "rabbitmq" ]]; then
            test_projects=$(find "$PROJECT_ROOT/tests" -name "*Infrastructure*.csproj" 2>/dev/null | head -1)
            print_message "${YELLOW}" "Buscando testes de infraestrutura para $1..."
        elif [[ "$1" == "jwt-fido2" ]]; then
            # Para JWT/FIDO2, usar o projeto SmartAlarm.Tests que contém os testes de autenticação
            test_projects="$PROJECT_ROOT/tests/SmartAlarm.Tests/SmartAlarm.Tests.csproj"
            if [[ -f "$test_projects" ]]; then
                print_message "${YELLOW}" "Usando projeto SmartAlarm.Tests para JWT/FIDO2..."
            else
                test_projects=$(find "$PROJECT_ROOT/tests" -name "*Tests*.csproj" 2>/dev/null | head -1)
                print_message "${YELLOW}" "Buscando projeto de testes para JWT/FIDO2..."
            fi
        elif [[ "$1" == "coverage" ]]; then
            # Para cobertura, usar o projeto SmartAlarm.Tests que sabemos que funciona
            test_projects="$PROJECT_ROOT/tests/SmartAlarm.Tests/SmartAlarm.Tests.csproj"
            if [[ -f "$test_projects" ]]; then
                print_message "${YELLOW}" "Executando análise de cobertura no projeto SmartAlarm.Tests..."
            else
                # Fallback para buscar qualquer projeto de teste
                test_projects=$(find "$PROJECT_ROOT/tests" -name "*Tests*.csproj" 2>/dev/null | head -1)
                print_message "${YELLOW}" "Executando análise de cobertura em projeto de testes..."
            fi
        fi
        
        # Se não encontrou projeto específico, buscar por projetos com "Integration" no nome
        if [[ -z "$test_projects" && -d "$PROJECT_ROOT/tests" ]]; then
            test_projects=$(find "$PROJECT_ROOT/tests" -name "*Integration*.csproj" 2>/dev/null | head -1)
        fi
        
        # Se ainda não encontrou, buscar o SmartAlarm.Infrastructure.Tests como fallback
        if [[ -z "$test_projects" ]]; then
            test_projects=$(find "$PROJECT_ROOT/tests" -name "*Infrastructure*.csproj" 2>/dev/null | head -1)
            print_message "${YELLOW}" "Usando SmartAlarm.Infrastructure.Tests como fallback..."
        fi
        
        # Se ainda não encontrou, buscar qualquer projeto de teste
        if [[ -z "$test_projects" ]]; then
            print_message "${RED}" "Nenhum projeto de teste específico encontrado!"
            print_message "${YELLOW}" "Buscando qualquer projeto de teste..."
            test_projects=$(find "$PROJECT_ROOT/tests" -name "*.csproj" 2>/dev/null | head -1)
        fi
        
        if [[ -z "$test_projects" ]]; then
            print_message "${RED}" "Nenhum projeto de teste encontrado! Verifique a estrutura do projeto."
            return 1
        fi
        
        # Converter caminho absoluto para relativo dentro do contêiner
        local container_project_path=$(echo "$test_projects" | sed "s|$PROJECT_ROOT|/app|")
        
        print_message "${YELLOW}" "Projeto de teste selecionado: $test_projects"
        print_message "${YELLOW}" "Caminho no contêiner: $container_project_path"
        
        # Preparar argumentos para dotnet test
        if [[ "$1" == "coverage" ]]; then
            # Para coverage, usar array para evitar problemas com espaços
            local dotnet_cmd=(
                "dotnet" "test" "$container_project_path"
                "--filter" "$TEST_FILTER"
                "--logger" "console;verbosity=detailed"
                "--collect" "XPlat Code Coverage"
                "--settings" "/app/tests/coverlet.runsettings"
            )
            
            print_message "${YELLOW}" "Comando que será executado: ${dotnet_cmd[*]}"
            
            # Executar diretamente no container sem usar o entrypoint complexo
            docker run --rm \
                --name smartalarm-test-runner \
                --network=smartalarm-test-net \
                --hostname test-runner \
                ${host_mappings} \
                ${env_vars} \
                -v "$PROJECT_ROOT:/app" \
                mcr.microsoft.com/dotnet/sdk:8.0 \
                "${dotnet_cmd[@]}"
        else
            local dotnet_args="$container_project_path --filter $TEST_FILTER"
            if [[ -n "$VERBOSE" ]]; then
                dotnet_args="$dotnet_args $VERBOSE"
            fi
            
            print_message "${YELLOW}" "Comando que será executado: dotnet test $dotnet_args"
            
            # Executar contêiner com hosts mapeados
            docker run --rm \
                --name smartalarm-test-runner \
                --network=smartalarm-test-net \
                --hostname test-runner \
                ${host_mappings} \
                ${env_vars} \
                -v "$PROJECT_ROOT:/app" \
                smartalarm-test-image:latest \
                $dotnet_args
        fi
        
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
            -v "$PROJECT_ROOT:/app" \
            smartalarm-test-image:latest \
            "/bin/bash"
        
        return 0
    fi
}

# Função principal
main() {
    # Se nenhum argumento for fornecido, mostrar ajuda
    if [[ $# -eq 0 ]]; then
        print_message "${YELLOW}" "⚠️  Nenhum argumento fornecido"
        echo ""
        show_help
        exit 1
    fi
    
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
    print_message "${BLUE}" "=== Instruções Finais ==="
    print_message "${YELLOW}" "🐳 Gerenciamento de Containers:"
    print_message "${CYAN}" "  - Para encerrar o ambiente: docker-compose down"
    print_message "${CYAN}" "  - Para limpar completamente: docker-compose down --volumes --remove-orphans"
    echo ""
    print_message "${YELLOW}" "🧪 Execução de Testes:"
    print_message "${CYAN}" "  - Testes básicos (rápidos): ./tests/docker-test.sh basic"
    print_message "${CYAN}" "  - Testes específicos: ./tests/docker-test.sh [postgres|vault|minio|rabbitmq]"
    print_message "${CYAN}" "  - Análise de cobertura: ./tests/docker-test.sh coverage"
    print_message "${CYAN}" "  - Depuração de rede: ./tests/docker-test.sh debug"
    echo ""
    print_message "${YELLOW}" "📚 Ajuda:"
    print_message "${CYAN}" "  - Ver todas as opções: ./tests/docker-test.sh help"
    echo ""
    print_message "${GREEN}" "✅ Script concluído com sucesso!"
}

# Executar o script
main "$@"
exit $?
