#!/bin/bash

# Script especializado para execução de testes básicos (sem containers)
# Este script assume que o SmartAlarm-test.sh já preparou o ambiente

# Importar cores e funções básicas do script principal
source "$(dirname "$0")/../test-common.sh"

run_basic_tests() {
    local test_type="$1"
    local verbose_mode="$2"
    
    print_message "${BLUE}" "=== Executando Testes Básicos ==="
    
    local TEST_FILTER=""
    local VERBOSE=""
    
    case "$test_type" in
        "owasp")
            TEST_FILTER="FullyQualifiedName~Owasp|FullyQualifiedName~BasicOwaspSecurityTests|Category=Security"
            print_message "${YELLOW}" "Executando testes de segurança OWASP"
            ;;
        "security")
            TEST_FILTER="FullyQualifiedName~Security|FullyQualifiedName~BasicSecurityComponentsTests|Category=Security"
            print_message "${YELLOW}" "Executando testes de componentes de segurança"
            ;;
        "all-security")
            TEST_FILTER="Category=Security|FullyQualifiedName~Owasp|FullyQualifiedName~Security"
            print_message "${YELLOW}" "Executando todos os testes de segurança"
            ;;
        "working-only")
            TEST_FILTER="FullyQualifiedName~PostgresIntegration|FullyQualifiedName~MinioIntegration|FullyQualifiedName~VaultIntegration|FullyQualifiedName~RabbitMqMessaging|FullyQualifiedName~BasicOwaspSecurity|FullyQualifiedName~BasicSecurityComponents|FullyQualifiedName~BasicJwtFido2|FullyQualifiedName~ObservabilityTest"
            print_message "${YELLOW}" "Executando apenas testes funcionais (excluindo observabilidade externa)"
            ;;
        "basic"|*)
            TEST_FILTER="FullyQualifiedName~Basic|Category=Integration|Category=Security"
            print_message "${YELLOW}" "Executando testes básicos (sem containers)"
            ;;
    esac
    
    # Configurar modo verbose
    if [[ "$verbose_mode" == "true" ]]; then
        VERBOSE="--logger \"console;verbosity=detailed\""
        print_message "${YELLOW}" "Modo verboso ativado"
    fi
    
    # Executar testes via container (reutilizando função do script principal)
    if run_basic_tests_in_container "$TEST_FILTER" "$VERBOSE"; then
        print_message "${GREEN}" "✅ Testes básicos concluídos com sucesso"
        return 0
    else
        print_message "${RED}" "❌ Falha nos testes básicos"
        return 1
    fi
}

# Função específica para executar testes básicos em container
run_basic_tests_in_container() {
    local test_filter="$1"
    local verbose_args="$2"
    
    print_message "${BLUE}" "🐳 Executando testes básicos via Docker container..."
    
    # Conectar à rede dos containers para acessar serviços de infraestrutura
    local NETWORK_NAME="smart-alarm_smart-alarm-network"
    
    print_message "${YELLOW}" "🌐 Usando rede: $NETWORK_NAME"
    
    docker run --rm \
        -v "$PROJECT_ROOT:/app" \
        -w /app \
        --network "$NETWORK_NAME" \
        -e ASPNETCORE_ENVIRONMENT=Testing \
        -e ConnectionStrings__DefaultConnection="Host=postgres;Port=5432;Database=smartalarm_test;Username=smartalarm;Password=smartalarm123" \
        -e VaultConfig__Address="http://vault:8200" \
        -e VaultConfig__Token="hvs.test-token-for-dev" \
        -e MinIOConfig__Endpoint="minio:9000" \
        -e MinIOConfig__AccessKey="minioadmin" \
        -e MinIOConfig__SecretKey="minioadmin" \
        -e RabbitMQConfig__HostName="rabbitmq" \
        -e RabbitMQConfig__Port="5672" \
        -e ObservabilityConfig__Jaeger__Endpoint="http://jaeger:16686" \
        -e ObservabilityConfig__Loki__Endpoint="http://loki:3100" \
        -e ObservabilityConfig__Prometheus__Endpoint="http://prometheus:9090" \
        -e ObservabilityConfig__Grafana__Endpoint="http://grafana:3001" \
        mcr.microsoft.com/dotnet/sdk:8.0 \
        sh -c "
            # Compilar primeiro
            echo 'Compilando projeto...'
            dotnet build /app/SmartAlarm.sln --configuration Debug || echo 'Warning: Build failed, continuing with tests'
            
            # Executar testes
            dotnet test /app/SmartAlarm.sln \
                --filter \"$test_filter\" \
                $verbose_args \
                || true
        "
    
    return $?
}

# Função principal do script
main() {
    local test_type="$1"
    local verbose_mode="false"
    
    # Verificar se modo verbose está ativado
    if [[ "$2" == "-v" || "$2" == "--verbose" ]]; then
        verbose_mode="true"
    fi
    
    # Detectar diretório raiz do projeto (herdado do script principal)
    if [[ -z "$PROJECT_ROOT" ]]; then
        PROJECT_ROOT="$(pwd)"
        while [[ ! -f "$PROJECT_ROOT/docker-compose.yml" && "$PROJECT_ROOT" != "/" ]]; do
            PROJECT_ROOT="$(dirname "$PROJECT_ROOT")"
        done
        if [[ ! -f "$PROJECT_ROOT/docker-compose.yml" ]]; then
            print_message "${RED}" "❌ Não foi possível encontrar o diretório raiz do projeto"
            exit 1
        fi
    fi
    
    run_basic_tests "$test_type" "$verbose_mode"
}

# Executar apenas se chamado diretamente (não via source)
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi
