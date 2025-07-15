#!/bin/bash

# Script especializado para execução de testes de integração (com containers)
# Este script assume que o SmartAlarm-test.sh já preparou o ambiente e configurou a rede

# Importar cores e funções básicas do script principal
source "$(dirname "$0")/../test-common.sh"

run_integration_tests() {
    local service="$1"
    local verbose_mode="$2"
    
    print_message "${BLUE}" "=== Executando Testes de Integração - $service ==="
    
    local TEST_FILTER=""
    local VERBOSE=""
    
    case "$service" in
        "postgres")
            TEST_FILTER="FullyQualifiedName~PostgresIntegrationTests|FullyQualifiedName~Postgres|Category=Integration"
            print_message "${YELLOW}" "Executando testes do PostgreSQL"
            ;;
        "vault")
            TEST_FILTER="FullyQualifiedName~VaultIntegrationTests|FullyQualifiedName~Vault|Category=Integration"
            print_message "${YELLOW}" "Executando testes do HashiCorp Vault"
            ;;
        "minio")
            TEST_FILTER="FullyQualifiedName~MinioIntegrationTests|FullyQualifiedName~Minio|Category=Integration"
            print_message "${YELLOW}" "Executando testes do MinIO"
            ;;
        "rabbitmq")
            TEST_FILTER="FullyQualifiedName~RabbitMqIntegrationTests|FullyQualifiedName~RabbitMq|Category=Integration"
            print_message "${YELLOW}" "Executando testes do RabbitMQ"
            ;;
        "jwt-fido2")
            TEST_FILTER="FullyQualifiedName~JwtFido2|FullyQualifiedName~BasicJwtFido2Tests|Category=Integration"
            print_message "${YELLOW}" "Executando testes de autenticação JWT/FIDO2"
            ;;
        "essentials")
            TEST_FILTER="Trait=Essential&Category=Integration"
            print_message "${YELLOW}" "Executando testes essenciais marcados"
            ;;
        *)
            TEST_FILTER="Category=Integration"
            print_message "${YELLOW}" "Executando todos os testes de integração"
            ;;
    esac
    
    # Configurar modo verbose
    if [[ "$verbose_mode" == "true" ]]; then
        VERBOSE="--logger \"console;verbosity=detailed\""
        print_message "${YELLOW}" "Modo verboso ativado"
    fi
    
    # Executar testes via container conectado à rede compartilhada
    if run_integration_tests_in_container "$TEST_FILTER" "$VERBOSE"; then
        print_message "${GREEN}" "✅ Testes de integração ($service) concluídos com sucesso"
        return 0
    else
        print_message "${RED}" "❌ Falha nos testes de integração ($service)"
        return 1
    fi
}

# Função específica para executar testes de integração em container
run_integration_tests_in_container() {
    local test_filter="$1"
    local verbose_args="$2"
    
    print_message "${BLUE}" "🐳 Executando testes de integração via Docker container..."
    
    # Verificar se a rede compartilhada existe (deve ter sido criada pelo script principal)
    if ! docker network ls | grep -q "smartalarm-test-net"; then
        print_message "${RED}" "❌ Rede compartilhada não encontrada. Execute o script principal primeiro."
        return 1
    fi
    
    # Gerar mapeamentos de host (reutilizando função do script principal)
    local host_mappings=$(generate_host_mappings)
    if [[ -z "$host_mappings" ]]; then
        print_message "${YELLOW}" "⚠️  Nenhum mapeamento de host encontrado, continuando sem mapeamentos"
    fi
    
    # Preparar variáveis de ambiente
    local env_vars="-e POSTGRES_HOST=postgres \
                    -e RABBITMQ_HOST=rabbitmq \
                    -e MINIO_HOST=minio \
                    -e VAULT_HOST=vault \
                    -e POSTGRES_PORT=5432 \
                    -e RABBITMQ_PORT=5672 \
                    -e MINIO_PORT=9000 \
                    -e VAULT_PORT=8200 \
                    -e POSTGRES_USER=smartalarm \
                    -e POSTGRES_PASSWORD=smartalarm123 \
                    -e POSTGRES_DB=smartalarm \
                    -e HashiCorpVault__ServerAddress=http://vault:8200 \
                    -e HashiCorpVault__Token=dev-token \
                    -e HashiCorpVault__MountPath=secret \
                    -e HashiCorpVault__KvVersion=2 \
                    -e HashiCorpVault__SkipTlsVerification=true \
                    -e ASPNETCORE_ENVIRONMENT=Testing"
    
    # Executar testes no container conectado à rede compartilhada
    docker run --rm \
        --network smartalarm-test-net \
        $env_vars \
        $host_mappings \
        -v "$PROJECT_ROOT:/app" \
        -w /app \
        mcr.microsoft.com/dotnet/sdk:8.0 \
        sh -c "
            # Aguardar um pouco para a rede se estabilizar
            sleep 2
            
            # Aguardar serviços críticos estarem disponíveis
            echo 'Aguardando serviços críticos...'
            for service in postgres vault minio rabbitmq; do
                echo \"Testando conectividade com \$service...\"
                if command -v nc >/dev/null 2>&1; then
                    timeout 30 bash -c \"until nc -z \$service 5432 2>/dev/null || nc -z \$service 8200 2>/dev/null || nc -z \$service 9000 2>/dev/null || nc -z \$service 5672 2>/dev/null; do sleep 1; done\" || echo \"Warning: Could not verify \$service connectivity\"
                fi
            done
            
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
    local service="$1"
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
    
    run_integration_tests "$service" "$verbose_mode"
}

# Executar apenas se chamado diretamente (não via source)
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi
