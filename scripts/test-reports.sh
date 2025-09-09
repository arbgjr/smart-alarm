#!/bin/bash

# Configuração de cores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Função para executar testes com relatórios
execute_tests() {
    local test_type="$1"
    local filter="$2"
    local output_dir="TestResults/${test_type}"
    
    echo -e "${BLUE}🧪 Executando testes: ${test_type}${NC}"
    echo -e "${YELLOW}📁 Relatórios serão salvos em: ${output_dir}${NC}"
    
    # Criar diretório para os resultados
    mkdir -p "${output_dir}"
    
    # Executar testes com cobertura e relatórios
    dotnet test \
        --filter "${filter}" \
        --collect:"XPlat Code Coverage" \
        --settings tests/coverlet.runsettings \
        --logger "html;LogFileName=TestResults.html" \
        --logger "trx;LogFileName=TestResults.trx" \
        --logger "console;verbosity=detailed" \
        --results-directory "${output_dir}" \
        --no-restore \
        --verbosity minimal
        
    local exit_code=$?
    
    if [ $exit_code -eq 0 ]; then
        echo -e "${GREEN}✅ ${test_type} - Todos os testes passaram!${NC}"
    else
        echo -e "${RED}❌ ${test_type} - Alguns testes falharam (código: $exit_code)${NC}"
    fi
    
    # Gerar relatório de cobertura HTML se existe
    if [ -f "${output_dir}/coverage.cobertura.xml" ]; then
        echo -e "${BLUE}📊 Gerando relatório HTML de cobertura...${NC}"
        dotnet tool install --global dotnet-reportgenerator-globaltool --ignore-failed-sources 2>/dev/null || true
        reportgenerator \
            -reports:"${output_dir}/coverage.cobertura.xml" \
            -targetdir:"${output_dir}/coverage-report" \
            -reporttypes:Html \
            2>/dev/null || echo -e "${YELLOW}⚠️  ReportGenerator não instalado. Execute: dotnet tool install --global dotnet-reportgenerator-globaltool${NC}"
    fi
    
    echo -e "${GREEN}📋 Relatórios disponíveis em:${NC}"
    echo -e "  • HTML: ${output_dir}/TestResults.html"
    echo -e "  • TRX:  ${output_dir}/TestResults.trx"
    echo -e "  • Cobertura: ${output_dir}/coverage-report/index.html"
    echo ""
    
    return $exit_code
}

# Verificar se ambiente está ativo
check_environment() {
    echo -e "${BLUE}🔍 Verificando infraestrutura...${NC}"
    
    # Verificar PostgreSQL
    if ! docker compose -f docker-compose.full.yml ps postgres-primary | grep -q "Up.*healthy"; then
        echo -e "${YELLOW}⚠️  PostgreSQL não está ativo. Iniciando...${NC}"
        docker compose -f docker-compose.full.yml up -d postgres-primary postgres-replica
        sleep 10
    fi
    
    # Verificar Redis
    if ! docker compose -f docker-compose.full.yml ps redis-master | grep -q "Up.*healthy"; then
        echo -e "${YELLOW}⚠️  Redis não está ativo. Iniciando...${NC}"
        docker compose -f docker-compose.full.yml up -d redis-master
        sleep 5
    fi
    
    echo -e "${GREEN}✅ Infraestrutura verificada${NC}"
}

# Função principal
main() {
    echo -e "${BLUE}🚀 Smart Alarm - Test Automation with Reports${NC}"
    echo -e "${BLUE}=============================================${NC}"
    
    case "$1" in
        "unit")
            echo -e "${GREEN}📋 Executando apenas testes unitários puros (Domain)${NC}"
            execute_tests "Unit" "FullyQualifiedName~Domain.Entities|FullyQualifiedName~Domain.ValueObjects"
            ;;
        "integration")
            echo -e "${GREEN}📋 Executando testes de integração${NC}"
            check_environment
            execute_tests "Integration" "Category=Integration|Trait=Category,Integration"
            ;;
        "api")
            echo -e "${GREEN}📋 Executando testes de API/Controllers${NC}"
            check_environment
            execute_tests "API" "FullyQualifiedName~Api.|FullyQualifiedName~Controller"
            ;;
        "security")
            echo -e "${GREEN}📋 Executando testes de segurança OWASP${NC}"
            check_environment
            execute_tests "Security" "FullyQualifiedName~Security|FullyQualifiedName~Owasp"
            ;;
        "e2e")
            echo -e "${GREEN}📋 Executando testes E2E (Playwright)${NC}"
            check_environment
            cd frontend
            npm run test:e2e
            cd ..
            echo -e "${GREEN}📋 Relatórios E2E em: frontend/test-results/playwright-html-report/index.html${NC}"
            ;;
        "all")
            echo -e "${GREEN}📋 Executando TODOS os testes${NC}"
            check_environment
            execute_tests "All" ""
            ;;
        "coverage")
            echo -e "${GREEN}📋 Executando testes com foco em cobertura${NC}"
            check_environment
            execute_tests "Coverage" "Category!=Integration"
            ;;
        *)
            echo -e "${YELLOW}📖 Uso: $0 {unit|integration|api|security|e2e|all|coverage}${NC}"
            echo ""
            echo -e "${BLUE}Tipos de teste disponíveis:${NC}"
            echo -e "  🔬 unit        - Testes unitários puros (Domain only)"
            echo -e "  🔗 integration - Testes de integração (requer infraestrutura)"
            echo -e "  🌐 api         - Testes de API/Controllers"
            echo -e "  🛡️  security    - Testes de segurança OWASP"
            echo -e "  🎭 e2e         - Testes End-to-End (Playwright)"
            echo -e "  📊 all         - Todos os testes"
            echo -e "  📈 coverage    - Foco em cobertura (sem integration)"
            echo ""
            echo -e "${GREEN}Todos os comandos geram relatórios automáticos em TestResults/[tipo]/${NC}"
            exit 1
            ;;
    esac
}

# Executar função principal
main "$@"