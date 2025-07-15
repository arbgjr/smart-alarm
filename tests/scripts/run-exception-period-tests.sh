#!/bin/bash

# Script especializado para execução de testes ExceptionPeriod API
# Este script assume que o SmartAlarm-test.sh já preparou o ambiente

# Importar cores e funções básicas do script principal
source "$(dirname "$0")/../test-common.sh"

# Detectar diretório raiz do projeto (usar função comum)
detect_project_root

print_message "${BLUE}" "=== ExceptionPeriod API dotnet test Integration ==="
print_message "${YELLOW}" "📍 Diretório do projeto: $PROJECT_ROOT"

# Função para configurar rede compartilhada (usar verificação comum)
setup_shared_network() {
    print_message "${BLUE}" "Verificando rede compartilhada para testes ExceptionPeriod..."
    
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
        fi
    done
    
    return 0
}

# Função para executar dotnet test via container Docker (seguindo padrão SmartAlarm-test.sh)
run_dotnet_test_in_container() {
    local project_path="$1"
    local filter="$2"
    local additional_args="$3"
    
    # Converter caminho absoluto para relativo dentro do container
    local container_project_path=$(echo "$project_path" | sed "s|$PROJECT_ROOT|/app|")
    
    print_message "${BLUE}" "🐳 Executando via Docker container com rede compartilhada:"
    print_message "${BLUE}" "   Projeto: $container_project_path"
    print_message "${BLUE}" "   Filtro: $filter"
    print_message "${BLUE}" "   Rede: smartalarm-test-net"
    
    # Preparar variáveis de ambiente (seguindo padrão do SmartAlarm-test.sh)
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

# Função para executar build via container Docker (seguindo padrão SmartAlarm-test.sh)
run_dotnet_build_in_container() {
    local solution_path="$1"
    
    # Converter caminho absoleto para relativo dentro do container
    local container_solution_path=$(echo "$solution_path" | sed "s|$PROJECT_ROOT|/app|")
    
    print_message "${BLUE}" "🐳 Executando build via Docker container com rede compartilhada:"
    print_message "${BLUE}" "   Solution: $container_solution_path"
    print_message "${BLUE}" "   Rede: smartalarm-test-net"
    
    # Preparar variáveis de ambiente
    local env_vars="-e POSTGRES_HOST=postgres \
                    -e ASPNETCORE_ENVIRONMENT=Testing"
    
    # Executar dotnet restore primeiro para limpar cache NuGet
    print_message "${YELLOW}" "Executando dotnet restore..."
    docker run --rm \
        --network smartalarm-test-net \
        $env_vars \
        -v "$PROJECT_ROOT:/app" \
        -w /app \
        mcr.microsoft.com/dotnet/sdk:8.0 \
        dotnet restore "$container_solution_path" \
        --force \
        --no-cache
    
    local restore_exit_code=$?
    if [[ $restore_exit_code -ne 0 ]]; then
        print_message "${YELLOW}" "⚠️  Restore falhou, tentando build sem restore..."
    fi
    
    # Executar dotnet build no container
    docker run --rm \
        --network smartalarm-test-net \
        $env_vars \
        -v "$PROJECT_ROOT:/app" \
        -w /app \
        mcr.microsoft.com/dotnet/sdk:8.0 \
        dotnet build "$container_solution_path" \
        --configuration Release \
        --no-restore \
        --verbosity quiet
    
    return $?
}

# Função para executar testes ExceptionPeriod específicos (seguindo padrão SmartAlarm-test.sh)
run_exception_period_dotnet_tests() {
    print_message "${BLUE}" "🧪 Executando testes ExceptionPeriod com dotnet test via Docker..."
    
    # Configurar rede compartilhada primeiro
    if ! setup_shared_network; then
        print_message "${RED}" "❌ Falha ao configurar rede compartilhada"
        return 1
    fi
    
    # Encontrar projetos de teste que podem conter testes ExceptionPeriod
    local test_projects=(
        "$PROJECT_ROOT/tests/SmartAlarm.Api.Tests/SmartAlarm.Api.Tests.csproj"
        "$PROJECT_ROOT/tests/SmartAlarm.Tests/SmartAlarm.Tests.csproj"
        "$PROJECT_ROOT/tests/SmartAlarm.Application.Tests/SmartAlarm.Application.Tests.csproj"
        "$PROJECT_ROOT/tests/SmartAlarm.Domain.Tests/SmartAlarm.Domain.Tests.csproj"
    )
    
    local tests_executed=false
    local total_exit_code=0
    
    for project in "${test_projects[@]}"; do
        if [[ -f "$project" ]]; then
            local project_name=$(basename "$(dirname "$project")")
            print_message "${CYAN}" "📋 Verificando projeto: $project_name"
            
            # Executar testes ExceptionPeriod específicos
            print_message "${YELLOW}" "Executando testes ExceptionPeriod em $project_name..."
            
            # Usar filtro específico para ExceptionPeriod
            local exception_period_filter="FullyQualifiedName~ExceptionPeriod"
            
            # Executar teste com filtro ExceptionPeriod via container
            if run_dotnet_test_in_container "$project" "$exception_period_filter" ""; then
                print_message "${GREEN}" "✅ Testes ExceptionPeriod em $project_name: SUCESSO"
                tests_executed=true
            else
                local exit_code=$?
                print_message "${RED}" "❌ Testes ExceptionPeriod em $project_name: FALHA (código: $exit_code)"
                total_exit_code=$((total_exit_code + exit_code))
                tests_executed=true
            fi
            
            echo ""
        fi
    done
    
    if [[ "$tests_executed" == "false" ]]; then
        print_message "${YELLOW}" "⚠️  Nenhum projeto de teste encontrado ou nenhum teste ExceptionPeriod executado"
        return 1
    fi
    
    return $total_exit_code
}

# Função para executar build primeiro (seguindo padrão SmartAlarm-test.sh)
run_exception_period_tests_with_build() {
    print_message "${BLUE}" "🔨 Executando build antes dos testes via Docker..."
    
    # Configurar rede compartilhada primeiro
    if ! setup_shared_network; then
        print_message "${RED}" "❌ Falha ao configurar rede compartilhada"
        return 1
    fi
    
    # Build da solution via container
    if run_dotnet_build_in_container "$PROJECT_ROOT/SmartAlarm.sln"; then
        print_message "${GREEN}" "✅ Build concluído com sucesso"
    else
        print_message "${YELLOW}" "⚠️  Build falhou, mas continuando com os testes (os testes podem funcionar sem build completo)"
        print_message "${BLUE}" "📝 Nota: Isso é comum em containers devido a configurações do NuGet/Visual Studio"
    fi
    
    # Executar testes independentemente do resultado do build
    print_message "${BLUE}" "Continuando com os testes ExceptionPeriod..."
    run_exception_period_dotnet_tests
}

# Função para executar testes com cobertura (seguindo padrão SmartAlarm-test.sh)
run_exception_period_tests_with_coverage() {
    print_message "${BLUE}" "📊 Executando testes ExceptionPeriod com análise de cobertura via Docker..."
    
    # Configurar rede compartilhada primeiro
    if ! setup_shared_network; then
        print_message "${RED}" "❌ Falha ao configurar rede compartilhada"
        return 1
    fi
    
    # Encontrar projeto principal de testes
    local main_test_project="$PROJECT_ROOT/tests/SmartAlarm.Api.Tests/SmartAlarm.Api.Tests.csproj"
    if [[ ! -f "$main_test_project" ]]; then
        main_test_project="$PROJECT_ROOT/tests/SmartAlarm.Tests/SmartAlarm.Tests.csproj"
    fi
    
    if [[ ! -f "$main_test_project" ]]; then
        print_message "${RED}" "❌ Nenhum projeto de teste principal encontrado"
        return 1
    fi
    
    local project_name=$(basename "$(dirname "$main_test_project")")
    print_message "${CYAN}" "📋 Usando projeto: $project_name"
    
    # Executar com cobertura via container
    local coverage_output="$PROJECT_ROOT/tests/coverage-report/exception-period-coverage"
    mkdir -p "$(dirname "$coverage_output")"
    
    # Converter caminhos para container
    local container_project_path=$(echo "$main_test_project" | sed "s|$PROJECT_ROOT|/app|")
    local container_coverage_path="/app/tests/coverage-report/exception-period-coverage"
    
    # Preparar variáveis de ambiente
    local env_vars="-e POSTGRES_HOST=postgres \
                    -e RABBITMQ_HOST=rabbitmq \
                    -e MINIO_HOST=minio \
                    -e VAULT_HOST=vault \
                    -e POSTGRES_PORT=5432 \
                    -e POSTGRES_USER=smartalarm \
                    -e POSTGRES_PASSWORD=smartalarm123 \
                    -e POSTGRES_DB=smartalarm \
                    -e ASPNETCORE_ENVIRONMENT=Testing"
    
    print_message "${BLUE}" "🐳 Executando testes com cobertura via Docker com rede compartilhada..."
    
    if docker run --rm \
        --network smartalarm-test-net \
        $env_vars \
        -v "$PROJECT_ROOT:/app" \
        -w /app \
        mcr.microsoft.com/dotnet/sdk:8.0 \
        dotnet test "$container_project_path" \
        --filter "FullyQualifiedName~ExceptionPeriod" \
        --collect:"XPlat Code Coverage" \
        --results-directory "$container_coverage_path" \
        --logger "console;verbosity=detailed"; then
        
        print_message "${GREEN}" "✅ Testes ExceptionPeriod com cobertura: SUCESSO"
        print_message "${BLUE}" "📊 Relatório de cobertura salvo em: $coverage_output"
        
        # Procurar arquivo de cobertura
        local coverage_file=$(find "$coverage_output" -name "coverage.cobertura.xml" 2>/dev/null | head -1)
        if [[ -f "$coverage_file" ]]; then
            print_message "${BLUE}" "📄 Arquivo de cobertura: $coverage_file"
        fi
        
        return 0
    else
        print_message "${RED}" "❌ Testes ExceptionPeriod com cobertura: FALHA"
        return 1
    fi
}

# Função para executar testes específicos por categoria (seguindo padrão SmartAlarm-test.sh)
run_exception_period_tests_by_category() {
    local category="$1"
    
    print_message "${BLUE}" "🎯 Executando testes ExceptionPeriod - Categoria: $category"
    
    # Configurar rede compartilhada primeiro
    if ! setup_shared_network; then
        print_message "${RED}" "❌ Falha ao configurar rede compartilhada"
        return 1
    fi
    
    local filter=""
    case "$category" in
        "unit")
            filter="FullyQualifiedName~ExceptionPeriod&Category=Unit"
            ;;
        "integration")
            filter="FullyQualifiedName~ExceptionPeriod&Category=Integration"
            ;;
        "api")
            filter="FullyQualifiedName~ExceptionPeriod&(Category=Integration|FullyQualifiedName~Controller)"
            ;;
        "handlers")
            filter="FullyQualifiedName~ExceptionPeriod&FullyQualifiedName~Handler"
            ;;
        "validators")
            filter="FullyQualifiedName~ExceptionPeriod&FullyQualifiedName~Validator"
            ;;
        "repository")
            filter="FullyQualifiedName~ExceptionPeriod&FullyQualifiedName~Repository"
            ;;
        "entity")
            filter="FullyQualifiedName~ExceptionPeriod&FullyQualifiedName~Entity"
            ;;
        "dto")
            filter="FullyQualifiedName~ExceptionPeriod&FullyQualifiedName~Dto"
            ;;
        *)
            filter="FullyQualifiedName~ExceptionPeriod"
            ;;
    esac
    
    print_message "${YELLOW}" "Filtro aplicado: $filter"
    
    # Encontrar e executar em todos os projetos relevantes
    local test_projects=(
        "$PROJECT_ROOT/tests/SmartAlarm.Api.Tests/SmartAlarm.Api.Tests.csproj"
        "$PROJECT_ROOT/tests/SmartAlarm.Application.Tests/SmartAlarm.Application.Tests.csproj"
        "$PROJECT_ROOT/tests/SmartAlarm.Domain.Tests/SmartAlarm.Domain.Tests.csproj"
        "$PROJECT_ROOT/tests/SmartAlarm.Infrastructure.Tests/SmartAlarm.Infrastructure.Tests.csproj"
    )
    
    local total_exit_code=0
    local tests_found=false
    
    for project in "${test_projects[@]}"; do
        if [[ -f "$project" ]]; then
            local project_name=$(basename "$(dirname "$project")")
            print_message "${CYAN}" "📋 Executando em: $project_name"
            
            if run_dotnet_test_in_container "$project" "$filter" ""; then
                print_message "${GREEN}" "✅ $project_name: SUCESSO"
                tests_found=true
            else
                local exit_code=$?
                if [[ $exit_code -ne 0 ]]; then
                    print_message "${RED}" "❌ $project_name: FALHA (código: $exit_code)"
                    total_exit_code=$((total_exit_code + exit_code))
                fi
                tests_found=true
            fi
        fi
    done
    
    if [[ "$tests_found" == "false" ]]; then
        print_message "${YELLOW}" "⚠️  Nenhum teste encontrado para categoria: $category"
        return 1
    fi
    
    return $total_exit_code
}

# Função para executar testes de API específicos com foco nos problemas identificados
run_exception_period_api_debug_tests() {
    print_message "${BLUE}" "🔍 Executando testes ExceptionPeriod API com modo debug..."
    
    # Configurar rede compartilhada primeiro
    if ! setup_shared_network; then
        print_message "${RED}" "❌ Falha ao configurar rede compartilhada"
        return 1
    fi
    
    local api_test_project="$PROJECT_ROOT/tests/SmartAlarm.Api.Tests/SmartAlarm.Api.Tests.csproj"
    
    if [[ ! -f "$api_test_project" ]]; then
        print_message "${RED}" "❌ Projeto de teste de API não encontrado: $api_test_project"
        return 1
    fi
    
    print_message "${CYAN}" "📋 Executando testes de API ExceptionPeriod com diagnóstico detalhado..."
    
    # Filtro específico para testes de integração da API
    local api_filter="FullyQualifiedName~ExceptionPeriodsControllerIntegrationTests"
    
    # Executar com verbosidade máxima e configurações especiais para debug
    local debug_args="--verbosity diagnostic --logger trx --results-directory /app/tests/TestResults"
    
    if run_dotnet_test_in_container "$api_test_project" "$api_filter" "$debug_args"; then
        print_message "${GREEN}" "✅ Testes ExceptionPeriod API Debug: SUCESSO"
        
        # Mostrar resultados detalhados se disponíveis
        local results_dir="$PROJECT_ROOT/tests/TestResults"
        if [[ -d "$results_dir" ]]; then
            local latest_trx=$(find "$results_dir" -name "*.trx" -type f -printf '%T@ %p\n' 2>/dev/null | sort -n | tail -1 | cut -d' ' -f2-)
            if [[ -f "$latest_trx" ]]; then
                print_message "${BLUE}" "📄 Resultado detalhado salvo em: $latest_trx"
            fi
        fi
        
        return 0
    else
        local exit_code=$?
        print_message "${RED}" "❌ Testes ExceptionPeriod API Debug: FALHA (código: $exit_code)"
        
        print_message "${YELLOW}" "🔍 Problemas conhecidos identificados anteriormente:"
        print_message "${CYAN}" "  1. Configuração de autenticação nos testes"
        print_message "${CYAN}" "  2. Ordem de validação (validator vs. business rules)"
        print_message "${CYAN}" "  3. Isolamento de dados entre testes"
        print_message "${CYAN}" "  4. Seed de dados não persistindo"
        
        return $exit_code
    fi
}

# Função principal
main() {
    local command="$1"
    
    case "$command" in
        "build")
            run_exception_period_tests_with_build
            ;;
        "coverage")
            run_exception_period_tests_with_coverage
            ;;
        "unit")
            run_exception_period_tests_by_category "unit"
            ;;
        "integration")
            run_exception_period_tests_by_category "integration"
            ;;
        "api")
            run_exception_period_tests_by_category "api"
            ;;
        "handlers")
            run_exception_period_tests_by_category "handlers"
            ;;
        "validators")
            run_exception_period_tests_by_category "validators"
            ;;
        "repository")
            run_exception_period_tests_by_category "repository"
            ;;
        "entity")
            run_exception_period_tests_by_category "entity"
            ;;
        "dto")
            run_exception_period_tests_by_category "dto"
            ;;
        "debug")
            run_exception_period_api_debug_tests
            ;;
        "help"|"-h"|"--help")
            print_message "${BLUE}" "=== ExceptionPeriod API dotnet test Runner ==="
            echo ""
            print_message "${YELLOW}" "Uso: $0 [comando]"
            echo ""
            print_message "${GREEN}" "Comandos disponíveis:"
            echo "  exception-period - Todos os testes ExceptionPeriod (padrão)"
            echo "  build           - Build + testes ExceptionPeriod"
            echo "  coverage        - Testes ExceptionPeriod com cobertura"
            echo "  unit            - Apenas testes unitários ExceptionPeriod"
            echo "  integration     - Apenas testes integração ExceptionPeriod"
            echo "  api             - Testes de API/Controller ExceptionPeriod"
            echo "  handlers        - Testes de Handlers ExceptionPeriod"
            echo "  validators      - Testes de Validators ExceptionPeriod"
            echo "  repository      - Testes de Repository ExceptionPeriod"
            echo "  entity          - Testes de Entity ExceptionPeriod"
            echo "  dto             - Testes de DTOs ExceptionPeriod"
            echo "  debug           - Modo debug para testes API com diagnóstico"
            echo "  help            - Mostra esta ajuda"
            echo ""
            print_message "${BLUE}" "Exemplos:"
            echo "  $0 exception-period # Todos os testes ExceptionPeriod (padrão)"
            echo "  $0 build           # Build + todos os testes ExceptionPeriod"
            echo "  $0 coverage        # Testes com análise de cobertura"
            echo "  $0 api             # Apenas testes de Controller"
            echo "  $0 debug           # Diagnóstico detalhado dos testes API"
            echo ""
            print_message "${YELLOW}" "📝 Nota: Este script segue o padrão do SmartAlarm-test.sh:"
            echo "  - Usa rede Docker compartilhada (smartalarm-test-net)"
            echo "  - Conecta aos serviços reais (PostgreSQL, Vault, etc.)"
            echo "  - Suporte a configuração via variáveis de ambiente"
            echo "  - Garante que docker-compose up -d foi executado antes"
            echo ""
            print_message "${CYAN}" "🔍 Modo Debug:"
            echo "  - Executa testes API com verbosidade máxima"
            echo "  - Salva resultados detalhados em TRX"
            echo "  - Identifica problemas conhecidos de configuração"
            echo ""
            exit 0
            ;;
        "exception-period"|"")
            # Comando "exception-period" ou sem argumentos - executar todos os testes ExceptionPeriod
            run_exception_period_dotnet_tests
            ;;
        *)
            print_message "${RED}" "❌ Comando inválido: $command"
            print_message "${BLUE}" "Use '$0 help' para ver os comandos disponíveis"
            exit 1
            ;;
    esac
}

# Verificar se Docker está disponível (usar função comum)
check_docker_availability

# Executar função principal
main "$@"
