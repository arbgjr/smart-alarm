#!/bin/bash

# Script especializado para execução de testes da API
# Este script assume que o SmartAlarm-test.sh já preparou o ambiente

# Importar cores e funções básicas do script principal
source "$(dirname "$0")/../test-common.sh"

# Detectar diretório raiz do projeto (usar função comum)
detect_project_root

print_message "${BLUE}" "=== Smart Alarm API Tests ==="
print_message "${YELLOW}" "📍 Diretório do projeto: $PROJECT_ROOT"

# Função para configurar rede compartilhada (usar verificação comum)
setup_shared_network() {
    print_message "${BLUE}" "Verificando rede compartilhada para testes da API..."
    
    # Verificar se a rede já existe (deve ter sido criada pelo script principal)
    if ! check_shared_network; then
        print_message "${RED}" "❌ Execute o script principal (SmartAlarm-test.sh) primeiro para configurar o ambiente"
        return 1
    fi
    
    # Identificar contêineres de serviço
    local CONTAINER_PREFIX=""
    if docker ps | grep -q "smart-alarm"; then
        CONTAINER_PREFIX="smart-alarm"
    elif docker ps | grep -q "smartalarm"; then
        CONTAINER_PREFIX="smartalarm"
    else
        print_message "${YELLOW}" "Nenhum contêiner de serviço encontrado. Use 'docker-compose up -d' primeiro."
        return 1
    fi
    
    print_message "${GREEN}" "Serviços detectados com prefixo: ${CONTAINER_PREFIX}"
    
    # Lista de serviços para conectar
    local services=("postgres" "vault" "minio" "rabbitmq")
    
    # Conectar contêineres de serviço à rede compartilhada
    for service in "${services[@]}"; do
        local container_name="${CONTAINER_PREFIX}-${service}-1"
        
        if docker ps --format '{{.Names}}' | grep -q "^${container_name}$"; then
            if ! docker network inspect smartalarm-test-net | grep -q "${container_name}"; then
                docker network connect smartalarm-test-net "${container_name}" 2>/dev/null || true
                print_message "${GREEN}" "  ✅ ${container_name} conectado à rede"
            else
                print_message "${YELLOW}" "  ⚠️  ${container_name} já está conectado"
            fi
        else
            print_message "${YELLOW}" "  ⚠️  Container ${container_name} não encontrado"
        fi
    done
    
    # Aguardar serviços ficarem prontos
    print_message "${BLUE}" "Aguardando serviços ficarem prontos..."
    sleep 5
    
    return 0
}

# Função para executar testes da API
run_api_tests() {
    local test_type="$1"
    local verbose_mode="$2"
    
    print_message "${BLUE}" "=== Executando Testes da API ==="
    
    local TEST_FILTER=""
    local VERBOSE=""
    
    case "$test_type" in
        "controllers")
            TEST_FILTER="--filter FullyQualifiedName~Controller"
            print_message "${YELLOW}" "Executando testes dos Controllers da API"
            ;;
        "endpoints")
            TEST_FILTER="--filter FullyQualifiedName~Controller|FullyQualifiedName~Integration"
            print_message "${YELLOW}" "Executando testes de Endpoints e Integração da API"
            ;;
        "auth")
            TEST_FILTER="--filter FullyQualifiedName~Auth"
            print_message "${YELLOW}" "Executando testes de Autenticação da API"
            ;;
        "alarm")
            TEST_FILTER="--filter FullyQualifiedName~Alarm"
            print_message "${YELLOW}" "Executando testes de Alarm Controller"
            ;;
        "functions")
            TEST_FILTER="--filter FullyQualifiedName~Function"
            print_message "${YELLOW}" "Executando testes de Azure Functions"
            ;;
        "integration")
            TEST_FILTER="--filter FullyQualifiedName~Integration"
            print_message "${YELLOW}" "Executando testes de Integração da API"
            ;;
        "api"|*)
            TEST_FILTER=""
            print_message "${YELLOW}" "Executando todos os testes da API"
            ;;
    esac
    
    # Configurar modo verbose
    if [[ "$verbose_mode" == "true" ]]; then
        VERBOSE="--logger console;verbosity=detailed"
        print_message "${YELLOW}" "Modo verboso ativado"
    else
        VERBOSE="--logger console;verbosity=normal"
    fi
    
    # Executar testes via container
    if run_api_tests_in_container "$TEST_FILTER" "$VERBOSE"; then
        print_message "${GREEN}" "✅ Testes da API concluídos com sucesso"
        return 0
    else
        print_message "${RED}" "❌ Falha nos testes da API"
        return 1
    fi
}

# Função específica para executar testes da API em container
run_api_tests_in_container() {
    local test_filter="$1"
    local verbose_args="$2"
    
    print_message "${BLUE}" "🐳 Executando testes da API via Docker container..."
    
    # Gerar mapeamentos de host
    local host_mappings=$(generate_host_mappings)
    
    # Executar testes específicos da API
    docker run --rm \
        -v "$PROJECT_ROOT:/app" \
        -w /app \
        --network smartalarm-test-net \
        $host_mappings \
        -e ASPNETCORE_ENVIRONMENT=Testing \
        -e ConnectionStrings__DefaultConnection="Host=postgres;Port=5432;Database=smartalarm_test;Username=smartalarm;Password=smartalarm123" \
        -e VaultConfig__Address="http://vault:8200" \
        -e VaultConfig__Token="hvs.test-token-for-dev" \
        -e MinIOConfig__Endpoint="minio:9000" \
        -e MinIOConfig__AccessKey="minioadmin" \
        -e MinIOConfig__SecretKey="minioadmin" \
        -e RabbitMQConfig__HostName="rabbitmq" \
        -e RabbitMQConfig__Port="5672" \
        -e RabbitMQConfig__UserName="guest" \
        -e RabbitMQConfig__Password="guest" \
        -e POSTGRES_HOST=postgres \
        -e POSTGRES_PORT=5432 \
        -e POSTGRES_USER=smartalarm \
        -e POSTGRES_PASSWORD=smartalarm123 \
        -e POSTGRES_DB=smartalarm_test \
        -e RABBITMQ_HOST=rabbitmq \
        -e MINIO_HOST=minio \
        -e VAULT_HOST=vault \
        mcr.microsoft.com/dotnet/sdk:8.0 \
        sh -c "
            echo '=== Configuração do Ambiente de Testes da API ==='
            echo 'Versão do .NET:'
            dotnet --version
            echo ''
            
            echo 'Variáveis de ambiente configuradas:'
            env | grep -E '(POSTGRES|RABBITMQ|MINIO|VAULT|ASPNETCORE)' | sort
            echo ''
            
            echo '=== Restaurando dependências ==='
            dotnet restore /app/SmartAlarm.sln || exit 1
            echo ''
            
            echo '=== Executando Build ==='
            dotnet build /app/SmartAlarm.sln --no-restore || exit 1
            echo ''
            
            echo '=== Executando Testes da API ==='
            echo 'Filtro aplicado: $test_filter'
            echo 'Verbose args: $verbose_args'
            echo ''
            
            # Executar testes específicos do projeto SmartAlarm.Api.Tests
            if [ -n '$test_filter' ]; then
                dotnet test /app/tests/SmartAlarm.Api.Tests/SmartAlarm.Api.Tests.csproj $test_filter --no-build $verbose_args || true
            else
                dotnet test /app/tests/SmartAlarm.Api.Tests/SmartAlarm.Api.Tests.csproj --no-build $verbose_args || true
            fi
            
            echo ''
            echo '=== Resultados dos Testes da API ==='
            echo 'Testes da API concluídos.'
        "
    
    local exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        print_message "${GREEN}" "✅ Container de testes da API executado com sucesso"
        return 0
    else
        print_message "${RED}" "❌ Falha no container de testes da API (código: $exit_code)"
        return 1
    fi
}

# Função para mostrar ajuda específica dos testes da API
show_api_help() {
    print_message "${BLUE}" "=== Ajuda - Testes da API ==="
    echo ""
    print_message "${YELLOW}" "Uso: run-api-tests.sh [tipo] [--verbose]"
    echo ""
    print_message "${GREEN}" "Tipos de teste disponíveis:"
    echo "  api          - Todos os testes da API (padrão)"
    echo "  controllers  - Testes dos Controllers"
    echo "  endpoints    - Testes de Endpoints e Integração"
    echo "  auth         - Testes de Autenticação"
    echo "  alarm        - Testes do Alarm Controller"
    echo "  functions    - Testes de Azure Functions"
    echo "  integration  - Testes de Integração"
    echo ""
    print_message "${CYAN}" "Exemplos:"
    echo "  ./run-api-tests.sh api"
    echo "  ./run-api-tests.sh controllers --verbose"
    echo "  ./run-api-tests.sh auth"
    echo ""
}

# Função principal do script
main() {
    local test_type="$1"
    local verbose_mode="false"
    
    # Verificar argumentos
    if [[ "$2" == "--verbose" || "$2" == "-v" ]]; then
        verbose_mode="true"
    fi
    
    # Mostrar ajuda se solicitado
    if [[ "$test_type" == "help" || "$test_type" == "-h" || "$test_type" == "--help" ]]; then
        show_api_help
        exit 0
    fi
    
    # Configurar tipo de teste padrão
    if [[ -z "$test_type" ]]; then
        test_type="api"
    fi
    
    print_message "${BLUE}" "=== Iniciando Testes da API Smart Alarm ==="
    print_message "${CYAN}" "Tipo de teste: $test_type"
    
    # Configurar rede compartilhada
    if ! setup_shared_network; then
        print_message "${RED}" "❌ Falha na configuração da rede"
        exit 1
    fi
    
    # Executar testes
    if run_api_tests "$test_type" "$verbose_mode"; then
        print_message "${GREEN}" "✅ Testes da API concluídos com sucesso"
        exit 0
    else
        print_message "${RED}" "❌ Falha nos testes da API"
        exit 1
    fi
}

# Executar apenas se chamado diretamente (não via source)
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi
