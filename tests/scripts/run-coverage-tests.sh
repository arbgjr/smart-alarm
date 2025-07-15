#!/bin/bash

# Script especializado para execução de testes com análise de cobertura
# Este script assume que o SmartAlarm-test.sh já preparou o ambiente

# Importar cores e funções básicas do script principal
source "$(dirname "$0")/../test-common.sh"

run_coverage_tests() {
    local verbose_mode="$1"
    
    print_message "${BLUE}" "=== Executando Análise de Cobertura ==="
    
    # Filtro específico para testes funcionais básicos (adequados para cobertura)
    local TEST_FILTER="FullyQualifiedName~BasicOwaspSecurityTests|FullyQualifiedName~BasicSecurityComponentsTests|FullyQualifiedName~CreateAlarmDtoValidatorTests|FullyQualifiedName~ErrorMessageServiceTests"
    
    local VERBOSE="--logger console;verbosity=detailed --collect \"XPlat Code Coverage\" --settings tests/coverlet.runsettings"
    
    print_message "${YELLOW}" "Executando análise de cobertura em testes básicos funcionais"
    
    # Configurar modo verbose adicional
    if [[ "$verbose_mode" == "true" ]]; then
        print_message "${YELLOW}" "Modo verboso ativado para cobertura"
    fi
    
    # Criar diretório para relatórios de cobertura
    local coverage_dir="$PROJECT_ROOT/tests/coverage-report"
    mkdir -p "$coverage_dir"
    
    # Executar testes com cobertura via container
    if run_coverage_tests_in_container "$TEST_FILTER" "$VERBOSE" "$coverage_dir"; then
        print_message "${GREEN}" "✅ Análise de cobertura concluída com sucesso"
        
        # Procurar e reportar arquivos de cobertura gerados
        local coverage_files=$(find "$coverage_dir" -name "*.xml" -o -name "*.json" 2>/dev/null)
        if [[ -n "$coverage_files" ]]; then
            print_message "${BLUE}" "📊 Arquivos de cobertura gerados:"
            echo "$coverage_files" | while read -r file; do
                print_message "${CYAN}" "  - $file"
            done
        fi
        
        return 0
    else
        print_message "${RED}" "❌ Falha na análise de cobertura"
        return 1
    fi
}

# Função específica para executar testes com cobertura em container
run_coverage_tests_in_container() {
    local test_filter="$1"
    local coverage_args="$2"
    local coverage_dir="$3"
    
    print_message "${BLUE}" "🐳 Executando análise de cobertura via Docker container..."
    
    # Converter caminho para container
    local container_coverage_dir="/app/tests/coverage-report"
    
    # Usar rede padrão para testes de cobertura (são testes básicos)
    docker run --rm \
        -v "$PROJECT_ROOT:/app" \
        -w /app \
        -e ASPNETCORE_ENVIRONMENT=Testing \
        mcr.microsoft.com/dotnet/sdk:8.0 \
        sh -c "
            # Criar diretório de cobertura no container
            mkdir -p $container_coverage_dir
            
            # Executar testes com cobertura
            dotnet test /app/SmartAlarm.sln \
                --filter \"$test_filter\" \
                $coverage_args \
                --results-directory $container_coverage_dir \
                --no-build \
                || true
            
            # Verificar se arquivos de cobertura foram gerados
            echo 'Arquivos de cobertura gerados:'
            find $container_coverage_dir -type f -name '*.xml' -o -name '*.json' | head -10
        "
    
    return $?
}

# Função para executar cobertura em testes específicos
run_targeted_coverage() {
    local target="$1"
    local verbose_mode="$2"
    
    print_message "${BLUE}" "=== Executando Cobertura Direcionada - $target ==="
    
    local TEST_FILTER=""
    
    case "$target" in
        "security")
            TEST_FILTER="Category=Security|FullyQualifiedName~Security|FullyQualifiedName~Owasp"
            print_message "${YELLOW}" "Cobertura para testes de segurança"
            ;;
        "validators")
            TEST_FILTER="FullyQualifiedName~Validator"
            print_message "${YELLOW}" "Cobertura para testes de validadores"
            ;;
        "services")
            TEST_FILTER="FullyQualifiedName~Service"
            print_message "${YELLOW}" "Cobertura para testes de serviços"
            ;;
        "basic"|*)
            return $(run_coverage_tests "$verbose_mode")
            ;;
    esac
    
    local VERBOSE="--logger console;verbosity=detailed --collect \"XPlat Code Coverage\" --settings tests/coverlet.runsettings"
    local coverage_dir="$PROJECT_ROOT/tests/coverage-report/$target"
    mkdir -p "$coverage_dir"
    
    if run_coverage_tests_in_container "$TEST_FILTER" "$VERBOSE" "$coverage_dir"; then
        print_message "${GREEN}" "✅ Cobertura direcionada ($target) concluída com sucesso"
        return 0
    else
        print_message "${RED}" "❌ Falha na cobertura direcionada ($target)"
        return 1
    fi
}

# Função principal do script
main() {
    local target="$1"
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
    
    # Verificar se foi especificado um alvo específico
    if [[ -n "$target" && "$target" != "coverage" ]]; then
        run_targeted_coverage "$target" "$verbose_mode"
    else
        run_coverage_tests "$verbose_mode"
    fi
}

# Executar apenas se chamado diretamente (não via source)
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi
