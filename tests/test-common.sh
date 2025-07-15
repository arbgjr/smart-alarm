#!/bin/bash

# Arquivo de funções comuns para os scripts de teste
# Este arquivo deve ser importado (source) pelos scripts especializados

# Definições de cores para saída (se não estiverem definidas)
if [[ -z "$GREEN" ]]; then
    GREEN='\033[0;32m'
    RED='\033[0;31m'
    YELLOW='\033[1;33m'
    BLUE='\033[0;34m'
    CYAN='\033[0;36m'
    NC='\033[0m' # No Color
fi

# Função para imprimir mensagens com cores (se não estiver definida)
if ! declare -f print_message > /dev/null; then
    print_message() {
        local color=$1
        local message=$2
        echo -e "${color}${message}${NC}"
    }
fi

# Função para obter IP de um contêiner (compartilhada)
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
        ip=$(docker inspect "$container_name" --format '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' 2>/dev/null | head -1)
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

# Função para gerar mapeamentos de host (compartilhada)
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
        if docker ps --format '{{.Names}}' | grep -q "^${container_name}$"; then
            local ip=$(get_container_ip "$container_name")
            if [[ -n "$ip" && "$ip" != "" ]]; then
                host_mappings="${host_mappings} --add-host ${service}:${ip}"
                print_message "${GREEN}" "  ✅ ${service} -> ${ip}" >&2
            else
                print_message "${YELLOW}" "  ⚠️  ${container_name} sem IP válido" >&2
            fi
        else
            print_message "${YELLOW}" "  ⚠️  ${container_name} não encontrado" >&2
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

# Função para aguardar um serviço ficar disponível (compartilhada)
wait_for_service() {
    local service=$1
    local port=$2
    local timeout=${3:-30}
    local count=0
    
    print_message "${YELLOW}" "Aguardando ${service}:${port}..." >&2
    
    while ! nc -z "$service" "$port" 2>/dev/null; do
        count=$((count + 1))
        if [[ $count -ge $timeout ]]; then
            print_message "${RED}" "  ❌ Timeout aguardando ${service}:${port}" >&2
            return 1
        fi
        sleep 1
    done
    
    print_message "${GREEN}" "  ✅ ${service}:${port} está disponível" >&2
    return 0
}

# Função para verificar se Docker está disponível (compartilhada)
check_docker_availability() {
    if ! command -v docker &> /dev/null; then
        print_message "${RED}" "❌ Docker não encontrado. Por favor, instale o Docker e tente novamente."
        exit 1
    fi
    
    # Verificar se Docker está rodando
    if ! docker ps &> /dev/null; then
        print_message "${RED}" "❌ Docker não está rodando. Por favor, inicie o Docker e tente novamente."
        exit 1
    fi
}

# Função para verificar se a rede compartilhada existe (compartilhada)
check_shared_network() {
    local network_name="smartalarm-test-net"
    
    if ! docker network ls | grep -q "$network_name"; then
        print_message "${YELLOW}" "⚠️  Rede compartilhada '$network_name' não encontrada."
        print_message "${BLUE}" "Execute o script principal (SmartAlarm-test.sh) primeiro para configurar o ambiente."
        return 1
    fi
    
    return 0
}

# Função para detectar o diretório raiz do projeto (compartilhada)
detect_project_root() {
    if [[ -z "$PROJECT_ROOT" ]]; then
        PROJECT_ROOT="$(pwd)"
        while [[ ! -f "$PROJECT_ROOT/docker-compose.yml" && "$PROJECT_ROOT" != "/" ]]; do
            PROJECT_ROOT="$(dirname "$PROJECT_ROOT")"
        done
        
        if [[ ! -f "$PROJECT_ROOT/docker-compose.yml" ]]; then
            print_message "${RED}" "❌ Não foi possível encontrar o diretório raiz do projeto (docker-compose.yml)"
            exit 1
        fi
        
        export PROJECT_ROOT
        print_message "${YELLOW}" "📍 Diretório do projeto: $PROJECT_ROOT" >&2
    fi
}

# Função para validar argumentos comuns (compartilhada)
parse_common_args() {
    local verbose_mode="false"
    
    # Verificar argumentos de verbose
    for arg in "$@"; do
        case "$arg" in
            "-v"|"--verbose")
                verbose_mode="true"
                ;;
        esac
    done
    
    echo "$verbose_mode"
}

# Função para mostrar informações do ambiente (compartilhada)
show_environment_info() {
    print_message "${BLUE}" "=== Informações do Ambiente ==="
    print_message "${YELLOW}" "📍 Diretório do projeto: ${PROJECT_ROOT:-'Não detectado'}"
    print_message "${YELLOW}" "🐳 Contêineres em execução:"
    docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" | grep -E "(smart-alarm|smartalarm)" || print_message "${CYAN}" "  Nenhum contêiner SmartAlarm encontrado"
    print_message "${YELLOW}" "🔗 Rede compartilhada:"
    if docker network ls | grep -q "smartalarm-test-net"; then
        print_message "${GREEN}" "  ✅ smartalarm-test-net disponível"
    else
        print_message "${RED}" "  ❌ smartalarm-test-net não encontrada"
    fi
    echo ""
}

# Função para limpeza de recursos (compartilhada, mas simplificada)
cleanup_test_resources() {
    local container_name="$1"
    
    if [[ -n "$container_name" ]]; then
        print_message "${YELLOW}" "Limpando contêiner de teste: $container_name"
        docker stop "$container_name" &>/dev/null || true
        docker rm "$container_name" &>/dev/null || true
    fi
}

# Verificar se este arquivo está sendo executado diretamente (não deveria)
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    print_message "${RED}" "❌ Este arquivo deve ser importado (source), não executado diretamente."
    print_message "${BLUE}" "Use: source test-common.sh"
    exit 1
fi
