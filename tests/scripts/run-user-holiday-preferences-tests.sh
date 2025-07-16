#!/bin/bash

# Script especializado para execução de testes UserHolidayPreferences API
# Este script assume que o SmartAlarm-test.sh já preparou o ambiente

# Importar cores e funções básicas do script principal
source "$(dirname "$0")/../test-common.sh"

# Detectar diretório raiz do projeto (usar função comum)
detect_project_root

print_message "${BLUE}" "=== UserHolidayPreferences API dotnet test Integration ==="
print_message "${YELLOW}" "📍 Diretório do projeto: $PROJECT_ROOT"

# Função para configurar rede compartilhada (usar verificação comum)
setup_shared_network() {
    print_message "${BLUE}" "Verificando rede compartilhada para testes UserHolidayPreferences..."
    
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

# Função para executar testes UserHolidayPreferences específicos (seguindo padrão SmartAlarm-test.sh)
run_user_holiday_preferences_dotnet_tests() {
    print_message "${BLUE}" "🧪 Executando testes UserHolidayPreferences com dotnet test via Docker..."
    
    # Configurar rede compartilhada primeiro
    if ! setup_shared_network; then
        print_message "${RED}" "❌ Falha ao configurar rede compartilhada"
        return 1
    fi
    
    # Encontrar projetos de teste que podem conter testes UserHolidayPreferences
    local test_projects=(
        "$PROJECT_ROOT/tests/SmartAlarm.Api.Tests/SmartAlarm.Api.Tests.csproj"
        "$PROJECT_ROOT/tests/SmartAlarm.Tests/SmartAlarm.Tests.csproj"
        "$PROJECT_ROOT/tests/SmartAlarm.Application.Tests/SmartAlarm.Application.Tests.csproj"
        "$PROJECT_ROOT/tests/SmartAlarm.Domain.Tests/SmartAlarm.Domain.Tests.csproj"
        "$PROJECT_ROOT/tests/SmartAlarm.Infrastructure.Tests/SmartAlarm.Infrastructure.Tests.csproj"
    )
    
    local tests_executed=false
    local total_exit_code=0
    
    for project in "${test_projects[@]}"; do
        if [[ -f "$project" ]]; then
            local project_name=$(basename "$(dirname "$project")")
            print_message "${CYAN}" "📋 Verificando projeto: $project_name"
            
            # Executar testes UserHolidayPreferences específicos
            print_message "${YELLOW}" "Executando testes UserHolidayPreferences em $project_name..."
            
            # Usar filtro específico para UserHolidayPreferences
            local user_holiday_preferences_filter="FullyQualifiedName~UserHolidayPreference"
            
            # Executar teste com filtro UserHolidayPreferences via container
            if run_dotnet_test_in_container "$project" "$user_holiday_preferences_filter" ""; then
                print_message "${GREEN}" "✅ Testes UserHolidayPreferences em $project_name: SUCESSO"
                tests_executed=true
            else
                local exit_code=$?
                print_message "${RED}" "❌ Testes UserHolidayPreferences em $project_name: FALHA (código: $exit_code)"
                total_exit_code=$((total_exit_code + exit_code))
                tests_executed=true
            fi
            
            echo ""
        fi
    done
    
    if [[ "$tests_executed" == "false" ]]; then
        print_message "${YELLOW}" "⚠️  Nenhum projeto de teste encontrado ou nenhum teste UserHolidayPreferences executado"
        return 1
    fi
    
    return $total_exit_code
}

# Função para executar build primeiro (seguindo padrão SmartAlarm-test.sh)
run_user_holiday_preferences_tests_with_build() {
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
    print_message "${BLUE}" "Continuando com os testes UserHolidayPreferences..."
    run_user_holiday_preferences_dotnet_tests
}

# Função para executar testes com cobertura (seguindo padrão SmartAlarm-test.sh)
run_user_holiday_preferences_tests_with_coverage() {
    print_message "${BLUE}" "📊 Executando testes UserHolidayPreferences com análise de cobertura via Docker..."
    
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
    local coverage_output="$PROJECT_ROOT/tests/coverage-report/user-holiday-preferences-coverage"
    mkdir -p "$(dirname "$coverage_output")"
    
    # Converter caminhos para container
    local container_project_path=$(echo "$main_test_project" | sed "s|$PROJECT_ROOT|/app|")
    local container_coverage_path="/app/tests/coverage-report/user-holiday-preferences-coverage"
    
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
        --filter "FullyQualifiedName~UserHolidayPreference" \
        --collect:"XPlat Code Coverage" \
        --results-directory "$container_coverage_path" \
        --logger "console;verbosity=detailed"; then
        
        print_message "${GREEN}" "✅ Testes UserHolidayPreferences com cobertura: SUCESSO"
        print_message "${BLUE}" "📊 Relatório de cobertura salvo em: $coverage_output"
        
        # Procurar arquivo de cobertura
        local coverage_file=$(find "$coverage_output" -name "coverage.cobertura.xml" 2>/dev/null | head -1)
        if [[ -f "$coverage_file" ]]; then
            print_message "${BLUE}" "📄 Arquivo de cobertura: $coverage_file"
        fi
        
        return 0
    else
        print_message "${RED}" "❌ Testes UserHolidayPreferences com cobertura: FALHA"
        return 1
    fi
}

# Função para executar testes específicos por categoria (seguindo padrão SmartAlarm-test.sh)
run_user_holiday_preferences_tests_by_category() {
    local category="$1"
    local test_group="$2"
    
    print_message "${BLUE}" "🎯 Executando testes UserHolidayPreferences por categoria: $category"
    
    # Configurar rede compartilhada primeiro
    if ! setup_shared_network; then
        print_message "${RED}" "❌ Falha ao configurar rede compartilhada"
        return 1
    fi
    
    local total_exit_code=0
    local tests_executed=false
    
    case "$category" in
        "controller"|"api")
            print_message "${CYAN}" "🎮 Executando testes de Controller/API..."
            local api_project="$PROJECT_ROOT/tests/SmartAlarm.Api.Tests/SmartAlarm.Api.Tests.csproj"
            if [[ -f "$api_project" ]]; then
                local filter="FullyQualifiedName~UserHolidayPreferencesController"
                if run_dotnet_test_in_container "$api_project" "$filter" ""; then
                    print_message "${GREEN}" "✅ Testes de Controller UserHolidayPreferences: SUCESSO"
                else
                    local exit_code=$?
                    print_message "${RED}" "❌ Testes de Controller UserHolidayPreferences: FALHA (código: $exit_code)"
                    total_exit_code=$((total_exit_code + exit_code))
                fi
                tests_executed=true
            fi
            ;;
        
        "repository"|"infrastructure")
            print_message "${CYAN}" "🗄️ Executando testes de Repository/Infrastructure..."
            local infra_project="$PROJECT_ROOT/tests/SmartAlarm.Infrastructure.Tests/SmartAlarm.Infrastructure.Tests.csproj"
            if [[ -f "$infra_project" ]]; then
                local filter="FullyQualifiedName~UserHolidayPreferenceRepository"
                if run_dotnet_test_in_container "$infra_project" "$filter" ""; then
                    print_message "${GREEN}" "✅ Testes de Repository UserHolidayPreferences: SUCESSO"
                else
                    local exit_code=$?
                    print_message "${RED}" "❌ Testes de Repository UserHolidayPreferences: FALHA (código: $exit_code)"
                    total_exit_code=$((total_exit_code + exit_code))
                fi
                tests_executed=true
            fi
            ;;
        
        "application"|"queries"|"commands")
            print_message "${CYAN}" "📱 Executando testes de Application/Queries/Commands..."
            local app_project="$PROJECT_ROOT/tests/SmartAlarm.Application.Tests/SmartAlarm.Application.Tests.csproj"
            if [[ -f "$app_project" ]]; then
                local filter="FullyQualifiedName~UserHolidayPreference"
                if run_dotnet_test_in_container "$app_project" "$filter" ""; then
                    print_message "${GREEN}" "✅ Testes de Application UserHolidayPreferences: SUCESSO"
                else
                    local exit_code=$?
                    print_message "${RED}" "❌ Testes de Application UserHolidayPreferences: FALHA (código: $exit_code)"
                    total_exit_code=$((total_exit_code + exit_code))
                fi
                tests_executed=true
            fi
            ;;
        
        "domain"|"entities")
            print_message "${CYAN}" "🏗️ Executando testes de Domain/Entities..."
            local domain_project="$PROJECT_ROOT/tests/SmartAlarm.Domain.Tests/SmartAlarm.Domain.Tests.csproj"
            if [[ -f "$domain_project" ]]; then
                local filter="FullyQualifiedName~UserHolidayPreference"
                if run_dotnet_test_in_container "$domain_project" "$filter" ""; then
                    print_message "${GREEN}" "✅ Testes de Domain UserHolidayPreferences: SUCESSO"
                else
                    local exit_code=$?
                    print_message "${RED}" "❌ Testes de Domain UserHolidayPreferences: FALHA (código: $exit_code)"
                    total_exit_code=$((total_exit_code + exit_code))
                fi
                tests_executed=true
            fi
            ;;
        
        "integration"|"crud")
            print_message "${CYAN}" "🔄 Executando testes de Integração/CRUD..."
            # Executar todos os testes relacionados a UserHolidayPreferences
            run_user_holiday_preferences_dotnet_tests
            return $?
            ;;
        
        *)
            print_message "${RED}" "❌ Categoria de teste inválida: $category"
            print_message "${YELLOW}" "Categorias disponíveis: controller, repository, application, domain, integration"
            return 1
            ;;
    esac
    
    if [[ "$tests_executed" == "false" ]]; then
        print_message "${YELLOW}" "⚠️  Nenhum teste executado para a categoria: $category"
        return 1
    fi
    
    return $total_exit_code
}

# Função para executar testes essenciais UserHolidayPreferences (seguindo padrão SmartAlarm-test.sh)
run_user_holiday_preferences_essentials() {
    print_message "${BLUE}" "⭐ Executando testes essenciais UserHolidayPreferences..."
    
    # Configurar rede compartilhada primeiro
    if ! setup_shared_network; then
        print_message "${RED}" "❌ Falha ao configurar rede compartilhada"
        return 1
    fi
    
    local total_exit_code=0
    
    # 1. Testes de Controller (API endpoints)
    print_message "${CYAN}" "1️⃣ Testes de Controller/API..."
    if run_user_holiday_preferences_tests_by_category "controller" ""; then
        print_message "${GREEN}" "✅ Controller tests: SUCESSO"
    else
        local exit_code=$?
        print_message "${RED}" "❌ Controller tests: FALHA (código: $exit_code)"
        total_exit_code=$((total_exit_code + exit_code))
    fi
    
    echo ""
    
    # 2. Testes de Repository (Persistência)
    print_message "${CYAN}" "2️⃣ Testes de Repository/Infrastructure..."
    if run_user_holiday_preferences_tests_by_category "repository" ""; then
        print_message "${GREEN}" "✅ Repository tests: SUCESSO"
    else
        local exit_code=$?
        print_message "${RED}" "❌ Repository tests: FALHA (código: $exit_code)"
        total_exit_code=$((total_exit_code + exit_code))
    fi
    
    echo ""
    
    # 3. Testes de Application (Lógica de negócio)
    print_message "${CYAN}" "3️⃣ Testes de Application..."
    if run_user_holiday_preferences_tests_by_category "application" ""; then
        print_message "${GREEN}" "✅ Application tests: SUCESSO"
    else
        local exit_code=$?
        print_message "${RED}" "❌ Application tests: FALHA (código: $exit_code)"
        total_exit_code=$((total_exit_code + exit_code))
    fi
    
    echo ""
    
    # Resumo dos testes essenciais
    if [[ $total_exit_code -eq 0 ]]; then
        print_message "${GREEN}" "🎉 Todos os testes essenciais UserHolidayPreferences passaram!"
    else
        print_message "${RED}" "❌ Alguns testes essenciais UserHolidayPreferences falharam (código total: $total_exit_code)"
    fi
    
    return $total_exit_code
}

# Função principal de entrada (seguindo padrão SmartAlarm-test.sh)
main() {
    local test_group="$1"
    local verbose_mode="false"
    
    # Verificar modo verbose
    if [[ "$2" == "--verbose" || "$2" == "-v" ]]; then
        verbose_mode="true"
        print_message "${YELLOW}" "Modo verboso ativado"
    fi
    
    print_message "${BLUE}" "=== UserHolidayPreferences Tests - Grupo: $test_group ==="
    
    case "$test_group" in
        "user-holiday-preferences"|"userholidaypreferences"|"basic")
            run_user_holiday_preferences_dotnet_tests
            ;;
        
        "with-build"|"build")
            run_user_holiday_preferences_tests_with_build
            ;;
        
        "coverage"|"with-coverage")
            run_user_holiday_preferences_tests_with_coverage
            ;;
        
        "essentials"|"essential")
            run_user_holiday_preferences_essentials
            ;;
        
        "controller"|"api")
            run_user_holiday_preferences_tests_by_category "controller" "$test_group"
            ;;
        
        "repository"|"infrastructure")
            run_user_holiday_preferences_tests_by_category "repository" "$test_group"
            ;;
        
        "application"|"queries"|"commands")
            run_user_holiday_preferences_tests_by_category "application" "$test_group"
            ;;
        
        "domain"|"entities")
            run_user_holiday_preferences_tests_by_category "domain" "$test_group"
            ;;
        
        "integration"|"crud")
            run_user_holiday_preferences_tests_by_category "integration" "$test_group"
            ;;
        
        *)
            print_message "${RED}" "❌ Grupo de teste inválido: $test_group"
            print_message "${YELLOW}" "Grupos disponíveis:"
            print_message "${CYAN}" "  - user-holiday-preferences, basic: Todos os testes básicos"
            print_message "${CYAN}" "  - with-build, build: Com build completo"
            print_message "${CYAN}" "  - coverage: Com análise de cobertura"
            print_message "${CYAN}" "  - essentials: Testes essenciais"
            print_message "${CYAN}" "  - controller, api: Testes de Controller/API"
            print_message "${CYAN}" "  - repository, infrastructure: Testes de Repository"
            print_message "${CYAN}" "  - application, queries, commands: Testes de Application"
            print_message "${CYAN}" "  - domain, entities: Testes de Domain"
            print_message "${CYAN}" "  - integration, crud: Testes de Integração/CRUD"
            return 1
            ;;
    esac
}

# Verificar se o script está sendo executado diretamente
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi
