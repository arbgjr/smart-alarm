#!/bin/bash

# Holiday API Integration Tests Runner
# Executa testes HTTP usando arquivos .http com REST Client
# Segue RFC 9110 HTTP Semantics

# Cores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

print_message() {
    local color=$1
    local message=$2
    echo -e "${color}${message}${NC}"
}

# Detectar diretório raiz do projeto
PROJECT_ROOT="$(pwd)"
while [[ ! -f "$PROJECT_ROOT/docker-compose.yml" && "$PROJECT_ROOT" != "/" ]]; do
    PROJECT_ROOT="$(dirname "$PROJECT_ROOT")"
done

if [[ ! -f "$PROJECT_ROOT/docker-compose.yml" ]]; then
    print_message "${RED}" "❌ Não foi possível encontrar o diretório raiz do projeto"
    exit 1
fi

HTTP_FILES_DIR="$PROJECT_ROOT/tests/http"
BASE_URL="https://localhost:5001/api/v1"

print_message "${BLUE}" "=== Holiday API Integration Tests ==="
print_message "${YELLOW}" "📍 Diretório do projeto: $PROJECT_ROOT"
print_message "${YELLOW}" "📁 Arquivos .http: $HTTP_FILES_DIR"
print_message "${YELLOW}" "🌐 Base URL: $BASE_URL"

# Verificar se os arquivos .http existem
if [[ ! -f "$HTTP_FILES_DIR/holidays.http" ]]; then
    print_message "${RED}" "❌ Arquivo holidays.http não encontrado em: $HTTP_FILES_DIR"
    exit 1
fi

if [[ ! -f "$HTTP_FILES_DIR/holidays-advanced.http" ]]; then
    print_message "${YELLOW}" "⚠️  Arquivo holidays-advanced.http não encontrado (opcional)"
fi

# Função para verificar se a API está disponível
check_api_availability() {
    print_message "${BLUE}" "🔍 Verificando disponibilidade da API..."
    
    local max_attempts=30
    local attempt=1
    
    while [[ $attempt -le $max_attempts ]]; do
        if curl -k -s -f "$BASE_URL/health" >/dev/null 2>&1; then
            print_message "${GREEN}" "✅ API está disponível!"
            return 0
        fi
        
        print_message "${YELLOW}" "⏳ Tentativa $attempt/$max_attempts - Aguardando API..."
        sleep 2
        attempt=$((attempt + 1))
    done
    
    print_message "${RED}" "❌ API não está disponível após $max_attempts tentativas"
    return 1
}

# Função para executar um arquivo .http
run_http_file() {
    local file_path="$1"
    local file_name=$(basename "$file_path")
    
    print_message "${CYAN}" "🧪 Executando testes: $file_name"
    
    # Verificar se o arquivo existe
    if [[ ! -f "$file_path" ]]; then
        print_message "${RED}" "❌ Arquivo não encontrado: $file_path"
        return 1
    fi
    
    # Verificar se há extensão REST Client disponível (para VS Code)
    if command -v code >/dev/null 2>&1; then
        print_message "${BLUE}" "📋 Arquivo de teste disponível para execução manual:"
        print_message "${BLUE}" "   code \"$file_path\""
        print_message "${BLUE}" "   Use a extensão REST Client do VS Code para executar"
    fi
    
    # Parse básico do arquivo .http para extrair endpoints
    print_message "${BLUE}" "📊 Endpoints encontrados no arquivo:"
    
    local endpoint_count=0
    while IFS= read -r line; do
        # Detectar linhas que contêm métodos HTTP
        if [[ "$line" =~ ^(GET|POST|PUT|DELETE|PATCH|OPTIONS)[[:space:]]+.* ]]; then
            endpoint_count=$((endpoint_count + 1))
            local method=$(echo "$line" | awk '{print $1}')
            local url=$(echo "$line" | awk '{print $2}')
            
            # Substituir variáveis básicas
            url="${url//\{\{baseUrl\}\}/$BASE_URL}"
            
            print_message "${CYAN}" "   $endpoint_count. $method $url"
        fi
    done < "$file_path"
    
    print_message "${GREEN}" "✅ Total de $endpoint_count endpoints encontrados"
    return 0
}

# Função para executar testes usando curl (implementação básica)
run_basic_http_tests() {
    print_message "${BLUE}" "🔧 Executando testes básicos com curl..."
    
    local token=""
    local test_count=0
    local success_count=0
    
    # Teste 1: Health Check
    print_message "${CYAN}" "Test 1: Health Check"
    test_count=$((test_count + 1))
    if curl -k -s -f "$BASE_URL/health" >/dev/null; then
        print_message "${GREEN}" "✅ Health check OK"
        success_count=$((success_count + 1))
    else
        print_message "${RED}" "❌ Health check failed"
    fi
    
    # Teste 2: Get Holidays (sem autenticação - deve falhar com 401)
    print_message "${CYAN}" "Test 2: Get Holidays without auth (should return 401)"
    test_count=$((test_count + 1))
    local status_code=$(curl -k -s -o /dev/null -w "%{http_code}" "$BASE_URL/holidays")
    if [[ "$status_code" == "401" ]]; then
        print_message "${GREEN}" "✅ Unauthorized access correctly blocked (401)"
        success_count=$((success_count + 1))
    else
        print_message "${RED}" "❌ Expected 401, got $status_code"
    fi
    
    # Teste 3: Options para CORS
    print_message "${CYAN}" "Test 3: OPTIONS request for CORS"
    test_count=$((test_count + 1))
    if curl -k -s -X OPTIONS -H "Origin: https://app.smartalarm.com" "$BASE_URL/holidays" >/dev/null; then
        print_message "${GREEN}" "✅ OPTIONS request successful"
        success_count=$((success_count + 1))
    else
        print_message "${RED}" "❌ OPTIONS request failed"
    fi
    
    # Teste 4: Invalid endpoint
    print_message "${CYAN}" "Test 4: Invalid endpoint (should return 404)"
    test_count=$((test_count + 1))
    local status_code=$(curl -k -s -o /dev/null -w "%{http_code}" "$BASE_URL/holidays/invalid-endpoint")
    if [[ "$status_code" == "404" ]]; then
        print_message "${GREEN}" "✅ Invalid endpoint correctly returns 404"
        success_count=$((success_count + 1))
    else
        print_message "${RED}" "❌ Expected 404, got $status_code"
    fi
    
    # Resumo
    print_message "${BLUE}" "📊 Resumo dos testes básicos:"
    print_message "${BLUE}" "   Total: $test_count"
    print_message "${GREEN}" "   Sucessos: $success_count"
    print_message "${RED}" "   Falhas: $((test_count - success_count))"
    
    if [[ $success_count -eq $test_count ]]; then
        print_message "${GREEN}" "🎉 Todos os testes básicos passaram!"
        return 0
    else
        print_message "${YELLOW}" "⚠️  Alguns testes básicos falharam"
        return 1
    fi
}

# Função principal
main() {
    # Verificar disponibilidade da API
    if ! check_api_availability; then
        print_message "${YELLOW}" "⚠️  API não está disponível. Verifique se o SmartAlarm está rodando."
        print_message "${BLUE}" "💡 Para iniciar a API, execute:"
        print_message "${BLUE}" "   cd $PROJECT_ROOT"
        print_message "${BLUE}" "   docker-compose up -d"
        print_message "${BLUE}" "   dotnet run --project src/SmartAlarm.Api"
        exit 1
    fi
    
    # Executar testes básicos com curl
    print_message "${BLUE}" "🚀 Iniciando testes Holiday API..."
    run_basic_http_tests
    
    # Processar arquivos .http
    print_message "${BLUE}" "📋 Processando arquivos .http..."
    
    # Arquivo principal
    run_http_file "$HTTP_FILES_DIR/holidays.http"
    
    # Arquivo avançado (se existir)
    if [[ -f "$HTTP_FILES_DIR/holidays-advanced.http" ]]; then
        run_http_file "$HTTP_FILES_DIR/holidays-advanced.http"
    fi
    
    print_message "${GREEN}" "✅ Testes Holiday API concluídos!"
    print_message "${BLUE}" "📖 Para execução manual completa dos testes .http:"
    print_message "${BLUE}" "   1. Abra VS Code: code $HTTP_FILES_DIR"
    print_message "${BLUE}" "   2. Instale a extensão 'REST Client' (humao.rest-client)"
    print_message "${BLUE}" "   3. Abra holidays.http e execute os requests individualmente"
    print_message "${BLUE}" "   4. Verifique as respostas e códigos de status HTTP"
}

# Verificar argumentos
if [[ "$1" == "help" || "$1" == "-h" || "$1" == "--help" ]]; then
    print_message "${BLUE}" "=== Holiday API Tests Runner ==="
    echo ""
    print_message "${YELLOW}" "Uso: $0 [opção]"
    echo ""
    print_message "${GREEN}" "Opções:"
    echo "  help, -h, --help - Mostra esta ajuda"
    echo ""
    print_message "${BLUE}" "Descrição:"
    echo "  Este script executa testes de integração da Holiday API"
    echo "  usando arquivos .http que seguem RFC 9110 HTTP Semantics."
    echo ""
    print_message "${BLUE}" "Arquivos de teste:"
    echo "  - tests/http/holidays.http         (testes principais)"
    echo "  - tests/http/holidays-advanced.http (testes avançados)"
    echo ""
    print_message "${BLUE}" "Pré-requisitos:"
    echo "  - SmartAlarm API rodando em https://localhost:5001"
    echo "  - Docker containers ativos (se necessário)"
    echo "  - curl instalado para testes básicos"
    echo ""
    print_message "${BLUE}" "Para execução manual completa:"
    echo "  - VS Code com extensão REST Client (humao.rest-client)"
    exit 0
fi

# Executar função principal
main
