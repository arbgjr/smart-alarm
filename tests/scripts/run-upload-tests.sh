#!/bin/bash

# Script especializado para execução de testes Upload/Storage
# Este script assume que o SmartAlarm-test.sh já preparou o ambiente

# Importar cores e funções básicas do script principal
source "$(dirname "$0")/../test-common.sh"

# Detectar diretório raiz do projeto (usar função comum)
detect_project_root

print_message "${BLUE}" "=== Upload/Storage dotnet test Integration ==="
print_message "${YELLOW}" "📍 Diretório do projeto: $PROJECT_ROOT"

# Função para configurar rede compartilhada (usar verificação comum)
setup_shared_network() {
    print_message "${BLUE}" "Verificando rede compartilhada para testes Upload/Storage..."
    
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
    
    # Lista de serviços para conectar (MinIO é essencial para testes de Upload)
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
            print_message "${YELLOW}" "  ⚠️  Contêiner ${container_name} não encontrado"
        fi
    done
    
    # Aguardar serviços estarem prontos (especialmente MinIO)
    wait_for_minio_ready "${CONTAINER_PREFIX}"
    
    return 0
}

# Função para aguardar MinIO estar pronto
wait_for_minio_ready() {
    local container_prefix="$1"
    local minio_container="${container_prefix}-minio-1"
    
    print_message "${BLUE}" "Aguardando MinIO estar pronto..."
    
    local max_attempts=30
    local attempt=1
    
    while [[ $attempt -le $max_attempts ]]; do
        if docker exec "$minio_container" mc ready local 2>/dev/null; then
            print_message "${GREEN}" "✅ MinIO está pronto"
            return 0
        fi
        
        print_message "${YELLOW}" "⏳ Tentativa $attempt/$max_attempts - Aguardando MinIO..."
        sleep 2
        ((attempt++))
    done
    
    print_message "${RED}" "❌ MinIO não ficou pronto após $max_attempts tentativas"
    return 1
}

# Função para executar dotnet test dentro de container conectado à rede
run_dotnet_test_in_container() {
    local project_path="$1"
    local filter="$2"
    local additional_args="$3"
    
    # Converter caminho do projeto para path dentro do container
    local container_project_path="${project_path/$PROJECT_ROOT/\/app}"
    
    print_message "${CYAN}" "Executando dotnet test para: $container_project_path"
    print_message "${CYAN}" "Filtro: $filter"
    
    # Configurar variáveis de ambiente para testes
    local env_vars="-e MINIO_ENDPOINT=minio \
                    -e MINIO_PORT=9000 \
                    -e MINIO_ACCESS_KEY=minioadmin \
                    -e MINIO_SECRET_KEY=minioadmin \
                    -e VAULT_HOST=vault \
                    -e VAULT_PORT=8200 \
                    -e POSTGRES_HOST=postgres \
                    -e POSTGRES_PORT=5432 \
                    -e POSTGRES_USER=smartalarm \
                    -e POSTGRES_PASSWORD=smartalarm123 \
                    -e POSTGRES_DB=smartalarm \
                    -e ASPNETCORE_ENVIRONMENT=Testing"
    
    # Executar dotnet test no container conectado à rede compartilhada
    docker run --rm \
        --network smartalarm-test-net \
        $env_vars \
        -v "$PROJECT_ROOT:/app" \
        -w /app \
        mcr.microsoft.com/dotnet/sdk:8.0 \
        dotnet test "$container_project_path" \
        --filter "$filter" \
        --logger "console;verbosity=detailed" \
        --no-build \
        $additional_args
    
    return $?
}

# Função principal para executar testes Upload
execute_upload_tests() {
    print_message "${BLUE}" "🧪 Executando testes Upload/Storage com dotnet test via Docker..."
    
    # Configurar rede compartilhada primeiro
    if ! setup_shared_network; then
        print_message "${RED}" "❌ Falha ao configurar rede compartilhada"
        return 1
    fi
    
    # Encontrar projetos de teste que podem conter testes Upload/Storage
    local test_projects=(
        "$PROJECT_ROOT/tests/SmartAlarm.Infrastructure.Tests/SmartAlarm.Infrastructure.Tests.csproj"
        "$PROJECT_ROOT/tests/SmartAlarm.Api.Tests/SmartAlarm.Api.Tests.csproj"
        "$PROJECT_ROOT/tests/SmartAlarm.Tests/SmartAlarm.Tests.csproj"
        "$PROJECT_ROOT/tests/SmartAlarm.Application.Tests/SmartAlarm.Application.Tests.csproj"
    )
    
    local tests_executed=false
    local total_exit_code=0
    
    for project in "${test_projects[@]}"; do
        if [[ -f "$project" ]]; then
            local project_name=$(basename "$(dirname "$project")")
            print_message "${CYAN}" "📋 Verificando projeto: $project_name"
            
            # Executar testes Upload/Storage específicos
            print_message "${YELLOW}" "Executando testes Upload/Storage em $project_name..."
            
            # Usar filtros específicos para Upload/Storage
            local upload_filters=(
                "FullyQualifiedName~Upload"
                "FullyQualifiedName~Storage"
                "FullyQualifiedName~MinioStorage"
                "FullyQualifiedName~MockStorage"
                "FullyQualifiedName~OciObjectStorage"
            )
            
            local project_tests_executed=false
            
            for filter in "${upload_filters[@]}"; do
                print_message "${CYAN}" "🔍 Testando com filtro: $filter"
                
                # Executar teste com filtro específico via container
                if run_dotnet_test_in_container "$project" "$filter" ""; then
                    print_message "${GREEN}" "✅ Testes ($filter) em $project_name: SUCESSO"
                    tests_executed=true
                    project_tests_executed=true
                else
                    local exit_code=$?
                    if [[ $exit_code -eq 0 ]]; then
                        print_message "${YELLOW}" "⚠️  Nenhum teste encontrado para filtro: $filter"
                    else
                        print_message "${RED}" "❌ Testes ($filter) em $project_name: FALHA (código: $exit_code)"
                        total_exit_code=$((total_exit_code + exit_code))
                        tests_executed=true
                        project_tests_executed=true
                    fi
                fi
            done
            
            if [[ "$project_tests_executed" == "true" ]]; then
                echo ""
            fi
        fi
    done
    
    if [[ "$tests_executed" == "false" ]]; then
        print_message "${YELLOW}" "⚠️  Nenhum projeto de teste encontrado ou nenhum teste Upload/Storage executado"
        return 1
    fi
    
    return $total_exit_code
}

# Função para executar testes específicos de MinIO
execute_minio_integration_tests() {
    print_message "${BLUE}" "🐳 Executando testes específicos de integração MinIO..."
    
    # Configurar rede compartilhada primeiro
    if ! setup_shared_network; then
        print_message "${RED}" "❌ Falha ao configurar rede compartilhada"
        return 1
    fi
    
    # Projeto específico para testes de integração
    local integration_project="$PROJECT_ROOT/tests/SmartAlarm.Infrastructure.Tests/SmartAlarm.Infrastructure.Tests.csproj"
    
    if [[ ! -f "$integration_project" ]]; then
        print_message "${RED}" "❌ Projeto de testes de infraestrutura não encontrado: $integration_project"
        return 1
    fi
    
    print_message "${YELLOW}" "Executando testes de integração MinIO..."
    
    # Filtro específico para testes de integração MinIO
    local minio_filter="FullyQualifiedName~MinioStorageServiceIntegrationTests"
    
    # Executar teste específico de integração MinIO
    if run_dotnet_test_in_container "$integration_project" "$minio_filter" "--environment Testing"; then
        print_message "${GREEN}" "✅ Testes de integração MinIO: SUCESSO"
        return 0
    else
        local exit_code=$?
        print_message "${RED}" "❌ Testes de integração MinIO: FALHA (código: $exit_code)"
        return $exit_code
    fi
}

# Função para executar testes de mock storage
execute_mock_storage_tests() {
    print_message "${BLUE}" "🔧 Executando testes Mock Storage..."
    
    # Projeto específico para testes de infraestrutura
    local infrastructure_project="$PROJECT_ROOT/tests/SmartAlarm.Infrastructure.Tests/SmartAlarm.Infrastructure.Tests.csproj"
    
    if [[ ! -f "$infrastructure_project" ]]; then
        print_message "${RED}" "❌ Projeto de testes de infraestrutura não encontrado: $infrastructure_project"
        return 1
    fi
    
    print_message "${YELLOW}" "Executando testes Mock Storage..."
    
    # Filtro específico para testes Mock Storage (não precisam de containers)
    local mock_filter="FullyQualifiedName~MockStorageServiceTests"
    
    # Para testes de mock, podemos executar sem rede especial
    local container_project_path="${infrastructure_project/$PROJECT_ROOT/\/app}"
    
    docker run --rm \
        -e ASPNETCORE_ENVIRONMENT=Testing \
        -v "$PROJECT_ROOT:/app" \
        -w /app \
        mcr.microsoft.com/dotnet/sdk:8.0 \
        dotnet test "$container_project_path" \
        --filter "$mock_filter" \
        --logger "console;verbosity=detailed" \
        --no-build
    
    local exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        print_message "${GREEN}" "✅ Testes Mock Storage: SUCESSO"
    else
        print_message "${RED}" "❌ Testes Mock Storage: FALHA (código: $exit_code)"
    fi
    
    return $exit_code
}

# Função para executar todos os testes relacionados a Storage
execute_all_storage_tests() {
    print_message "${BLUE}" "🗄️  Executando TODOS os testes de Storage (Mock + MinIO + Upload)..."
    
    local total_exit_code=0
    
    # 1. Executar testes Mock (sem dependências)
    print_message "${CYAN}" "===== Fase 1: Testes Mock Storage ====="
    if ! execute_mock_storage_tests; then
        total_exit_code=$((total_exit_code + 1))
    fi
    
    echo ""
    
    # 2. Executar testes de integração MinIO
    print_message "${CYAN}" "===== Fase 2: Testes Integração MinIO ====="
    if ! execute_minio_integration_tests; then
        total_exit_code=$((total_exit_code + 1))
    fi
    
    echo ""
    
    # 3. Executar testes gerais de Upload
    print_message "${CYAN}" "===== Fase 3: Testes Gerais Upload/Storage ====="
    if ! execute_upload_tests; then
        total_exit_code=$((total_exit_code + 1))
    fi
    
    return $total_exit_code
}

# Função principal do script
main() {
    local test_group="$1"
    local verbose_mode="$2"
    
    # Configurar modo verboso se solicitado
    if [[ "$verbose_mode" == "--verbose" ]]; then
        set -x
        print_message "${YELLOW}" "Modo verboso ativado para testes Upload/Storage"
    fi
    
    print_message "${BLUE}" "=== Executando grupo de testes Upload/Storage: $test_group ==="
    
    case "$test_group" in
        "upload")
            execute_upload_tests
            ;;
        "storage")
            execute_all_storage_tests
            ;;
        "minio")
            execute_minio_integration_tests
            ;;
        "mock-storage")
            execute_mock_storage_tests
            ;;
        "all-upload"|"all-storage")
            execute_all_storage_tests
            ;;
        *)
            print_message "${RED}" "❌ Grupo de teste Upload/Storage inválido: $test_group"
            print_message "${YELLOW}" "Grupos válidos: upload, storage, minio, mock-storage, all-upload, all-storage"
            return 1
            ;;
    esac
    
    local exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        print_message "${GREEN}" "✅ Testes Upload/Storage ($test_group) concluídos com sucesso!"
    else
        print_message "${RED}" "❌ Testes Upload/Storage ($test_group) falharam (código: $exit_code)"
    fi
    
    return $exit_code
}

# Executar função principal com argumentos passados
main "$@"
