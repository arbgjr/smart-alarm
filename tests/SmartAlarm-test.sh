#!/bin/bash

clear

# Script principal para execução de testes de integração Smart Alarm
# Este script coordena a preparação do ambiente e chama scripts especializados para cada grupo de testes

# Definições de cores para saída
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[0;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Função para imprimir mensagens com cores
print_message() {
    local color=$1
    local message=$2
    echo -e "${color}${message}${NC}"
}

# Função para mostrar ajuda
show_help() {
    print_message "${BLUE}" "=== Smart Alarm - Sistema de Testes de Integração ==="
    echo ""
    print_message "${YELLOW}" "Uso: $0 [opção] [-v|--verbose]"
    echo ""
    print_message "${GREEN}" "📋 Grupos de Testes Disponíveis:"
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
    print_message "${CYAN}" "🔬 Testes Especializados:"
    echo "  api         - Testes da API (Controllers, Endpoints, Auth)"
    echo "  holiday     - Testes da API de Holidays (HTTP/REST)"
    echo "  exception-period - Testes da API de ExceptionPeriod"
    echo ""
    print_message "${CYAN}" "📊 Análise e Depuração:"
    echo "  coverage    - Testes com análise de cobertura"
    echo "  working-only - Executa apenas testes funcionais (exclui observabilidade)"
    echo "  debug       - Modo interativo para depuração"
    echo ""
    print_message "${CYAN}" "🆘 Ajuda:"
    echo "  help, -h, --help - Mostra esta ajuda"
    echo ""
    print_message "${YELLOW}" "Exemplos:"
    echo "  $0 basic              # Testes rápidos sem containers"
    echo "  $0 postgres -v        # Testes PostgreSQL com saída detalhada"
    echo "  $0 api                # Testes da API"
    echo "  $0 holiday            # Testes da API Holiday"
    echo "  $0 exception-period   # Testes da API ExceptionPeriod"
    echo "  $0 working-only       # Apenas testes funcionais (sem observabilidade)"
    echo "  $0 coverage           # Análise de cobertura completa"
    echo "  $0 debug              # Modo interativo para diagnóstico"
    echo ""
    print_message "${GREEN}" "💡 Dica: Use 'basic' para validação rápida durante desenvolvimento!"
    echo ""
    print_message "${BLUE}" "📁 Estrutura de Scripts:"
    print_message "${CYAN}" "  - SmartAlarm-test.sh              # Script principal (este)"
    print_message "${CYAN}" "  - scripts/run-basic-tests.sh      # Testes básicos"
    print_message "${CYAN}" "  - scripts/run-integration-tests.sh # Testes de integração"
    print_message "${CYAN}" "  - scripts/run-coverage-tests.sh   # Análise de cobertura"
    print_message "${CYAN}" "  - scripts/run-api-tests.sh        # Testes da API"
    print_message "${CYAN}" "  - scripts/run-holiday-tests.sh    # Testes Holiday API"
    print_message "${CYAN}" "  - scripts/run-exception-period-tests.sh # Testes ExceptionPeriod API"
    print_message "${CYAN}" "  - scripts/run-debug.sh            # Ferramentas de debug"
    print_message "${CYAN}" "  - test-common.sh                  # Funções compartilhadas"
}

print_message "${BLUE}" "=== Smart Alarm - Sistema de Testes de Integração ==="

# Detectar diretório raiz do projeto
if [[ -f "docker-compose.yml" ]]; then
    PROJECT_ROOT="$(pwd)"
elif [[ -f "../docker-compose.yml" ]]; then
    PROJECT_ROOT="$(dirname "$(pwd)")"
else
    PROJECT_ROOT="$(pwd)"
    while [[ ! -f "$PROJECT_ROOT/docker-compose.yml" && "$PROJECT_ROOT" != "/" ]]; do
        PROJECT_ROOT="$(dirname "$PROJECT_ROOT")"
    done
    if [[ ! -f "$PROJECT_ROOT/docker-compose.yml" ]]; then
        print_message "${RED}" "❌ Não foi possível encontrar o diretório raiz do projeto (docker-compose.yml)"
        exit 1
    fi
fi

export PROJECT_ROOT
print_message "${YELLOW}" "📍 Diretório do projeto: $PROJECT_ROOT"

# Verificar argumentos e configurar variáveis
TEST_GROUP="$1"
VERBOSE_MODE="false"

# Verificar se modo verbose está ativado
if [[ "$2" == "-v" || "$2" == "--verbose" ]]; then
    VERBOSE_MODE="true"
    print_message "${YELLOW}" "Modo verboso ativado"
fi

# Função para mostrar ajuda caso nenhum argumento seja fornecido
if [[ -z "$TEST_GROUP" ]]; then
    print_message "${YELLOW}" "⚠️  Nenhum grupo de teste especificado"
    echo ""
    show_help
    exit 1
fi

# Se ajuda foi solicitada, mostrar e sair
if [[ "$TEST_GROUP" == "help" || "$TEST_GROUP" == "-h" || "$TEST_GROUP" == "--help" ]]; then
    show_help
    exit 0
fi

# Verificar comando Docker
if ! command -v docker &> /dev/null; then
    print_message "${RED}" "Docker não encontrado. Por favor, instale o Docker e tente novamente."
    exit 1
fi

# Função para chamar scripts especializados
call_specialized_script() {
    local script_name="$1"
    local test_group="$2"
    local verbose_flag="$3"
    
    local script_path="$PROJECT_ROOT/tests/scripts/${script_name}"
    
    # Verificar se o script existe
    if [[ ! -f "$script_path" ]]; then
        print_message "${RED}" "❌ Script especializado não encontrado: $script_path"
        return 1
    fi
    
    # Tornar o script executável
    chmod +x "$script_path"
    
    print_message "${BLUE}" "🚀 Chamando script especializado: $script_name"
    print_message "${CYAN}" "   Grupo: $test_group"
    if [[ "$verbose_flag" == "true" ]]; then
        print_message "${CYAN}" "   Modo: Verboso"
    fi
    
    # Exportar variáveis necessárias para o script especializado
    export PROJECT_ROOT
    
    # Chamar o script especializado
    if [[ "$verbose_flag" == "true" ]]; then
        bash "$script_path" "$test_group" "--verbose"
    else
        bash "$script_path" "$test_group"
    fi
    
    local exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        print_message "${GREEN}" "✅ Script especializado ($script_name) concluído com sucesso"
    else
        print_message "${RED}" "❌ Script especializado ($script_name) falhou (código: $exit_code)"
    fi
    
    return $exit_code
}

# Função para executar grupo de testes
execute_test_group() {
    local test_group="$1"
    local verbose_mode="$2"
    
    print_message "${BLUE}" "=== Executando Grupo de Testes: $test_group ==="
    
    case "$test_group" in
        # Testes básicos (sem containers)
        "basic"|"owasp"|"security"|"all-security")
            call_specialized_script "run-basic-tests.sh" "$test_group" "$verbose_mode"
            ;;
        
        # Testes de integração (com containers)
        "postgres"|"vault"|"minio"|"rabbitmq"|"jwt-fido2"|"essentials")
            call_specialized_script "run-integration-tests.sh" "$test_group" "$verbose_mode"
            ;;
        
        # Testes especializados
        "api")
            call_specialized_script "run-api-tests.sh" "$test_group" "$verbose_mode"
            ;;
        "holiday")
            call_specialized_script "run-holiday-tests.sh" "$test_group" "$verbose_mode"
            ;;
        "exception-period")
            call_specialized_script "run-exception-period-tests.sh" "$test_group" "$verbose_mode"
            ;;
        
        # Análise de cobertura
        "coverage")
            call_specialized_script "run-coverage-tests.sh" "$test_group" "$verbose_mode"
            ;;
        
        # Testes funcionais (sem observabilidade)
        "working-only")
            call_specialized_script "run-working-only-tests.sh" "$test_group" "$verbose_mode"
            ;;
        
        # Modo de depuração
        "debug")
            call_specialized_script "run-debug.sh" "$test_group" "$verbose_mode"
            ;;
        
        # Grupo inválido
        *)
            print_message "${RED}" "❌ Grupo de teste inválido: $test_group"
            echo ""
            show_help
            return 1
            ;;
    esac
}

# Executar grupo de testes
execute_test_group "$TEST_GROUP" "$VERBOSE_MODE"
test_exit_code=$?

# Mostrar instruções finais
print_message "${BLUE}" "=== Instruções Finais ==="
print_message "${YELLOW}" "🐳 Gerenciamento de Containers:"
print_message "${CYAN}" "  - Para encerrar o ambiente: docker-compose down"
print_message "${CYAN}" "  - Para limpar completamente: docker-compose down --volumes --remove-orphans"
echo ""
print_message "${YELLOW}" "🧪 Execução de Testes:"
print_message "${CYAN}" "  - Testes básicos (rápidos): $0 basic"
print_message "${CYAN}" "  - Testes específicos: $0 [postgres|vault|minio|rabbitmq]"
print_message "${CYAN}" "  - Análise de cobertura: $0 coverage"
print_message "${CYAN}" "  - Depuração de rede: $0 debug"
echo ""
print_message "${YELLOW}" "📚 Ajuda:"
print_message "${CYAN}" "  - Ver todas as opções: $0 help"
echo ""

if [[ $test_exit_code -eq 0 ]]; then
    print_message "${GREEN}" "✅ Sistema de testes concluído com sucesso!"
else
    print_message "${RED}" "❌ Sistema de testes falhou (código: $test_exit_code)"
fi

exit $test_exit_code
