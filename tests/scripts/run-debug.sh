#!/bin/bash

# Script especializado para modo de depuração e análise de ambiente
# Este script assume que o SmartAlarm-test.sh já preparou o ambiente

# Importar cores e funções básicas do script principal
source "$(dirname "$0")/../test-common.sh"

run_debug_mode() {
    print_message "${BLUE}" "=== Modo de Depuração Smart Alarm ==="
    
    # Mostrar informações do ambiente
    show_environment_info
    
    print_message "${BLUE}" "=== Diagnóstico de Conectividade ==="
    
    # Verificar conectividade com serviços
    check_service_connectivity
    
    print_message "${BLUE}" "=== Opções de Depuração ==="
    print_message "${YELLOW}" "1. Entrar no container interativo"
    print_message "${YELLOW}" "2. Testar conectividade específica"
    print_message "${YELLOW}" "3. Verificar logs dos serviços"
    print_message "${YELLOW}" "4. Executar teste básico de diagnóstico"
    print_message "${YELLOW}" "5. Sair"
    
    while true; do
        echo ""
        read -p "Escolha uma opção (1-5): " choice
        
        case $choice in
            1)
                enter_interactive_container
                ;;
            2)
                test_specific_connectivity
                ;;
            3)
                check_service_logs
                ;;
            4)
                run_diagnostic_test
                ;;
            5)
                print_message "${GREEN}" "Saindo do modo de depuração..."
                break
                ;;
            *)
                print_message "${RED}" "Opção inválida. Escolha 1-5."
                ;;
        esac
    done
}

# Função para verificar conectividade com serviços
check_service_connectivity() {
    print_message "${YELLOW}" "Verificando conectividade com serviços..."
    
    # Lista de serviços para verificar
    local services=("postgres:5432" "vault:8200" "minio:9000" "rabbitmq:5672")
    
    # Verificar se a rede compartilhada existe
    if ! check_shared_network; then
        print_message "${RED}" "❌ Rede compartilhada não disponível"
        return 1
    fi
    
    # Executar teste de conectividade via container temporário
    print_message "${BLUE}" "🐳 Testando conectividade via container..."
    
    for service_port in "${services[@]}"; do
        local service=$(echo "$service_port" | cut -d':' -f1)
        local port=$(echo "$service_port" | cut -d':' -f2)
        
        print_message "${CYAN}" "Testando $service:$port..."
        
        if docker run --rm --network smartalarm-test-net \
           mcr.microsoft.com/dotnet/sdk:8.0 \
           sh -c "command -v nc >/dev/null && nc -z $service $port" 2>/dev/null; then
            print_message "${GREEN}" "  ✅ $service:$port - Conectividade OK"
        else
            print_message "${RED}" "  ❌ $service:$port - Falha na conectividade"
        fi
    done
}

# Função para entrar em container interativo
enter_interactive_container() {
    print_message "${BLUE}" "🐳 Iniciando container interativo..."
    print_message "${YELLOW}" "Você terá acesso ao ambiente de testes com todas as ferramentas."
    print_message "${YELLOW}" "Use 'exit' para sair do container."
    echo ""
    
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
    
    # Gerar mapeamentos de host
    local host_mappings=$(generate_host_mappings)
    
    docker run -it --rm \
        --network smartalarm-test-net \
        $env_vars \
        $host_mappings \
        -v "$PROJECT_ROOT:/app" \
        -w /app \
        mcr.microsoft.com/dotnet/sdk:8.0 \
        /bin/bash
}

# Função para testar conectividade específica
test_specific_connectivity() {
    echo ""
    print_message "${YELLOW}" "Digite o serviço e porta para testar (ex: postgres:5432):"
    read -p "Serviço:porta: " service_input
    
    if [[ -z "$service_input" ]]; then
        print_message "${RED}" "Entrada inválida"
        return 1
    fi
    
    local service=$(echo "$service_input" | cut -d':' -f1)
    local port=$(echo "$service_input" | cut -d':' -f2)
    
    print_message "${BLUE}" "Testando conectividade com $service:$port..."
    
    if docker run --rm --network smartalarm-test-net \
       mcr.microsoft.com/dotnet/sdk:8.0 \
       sh -c "command -v nc >/dev/null && timeout 10 nc -z $service $port" 2>/dev/null; then
        print_message "${GREEN}" "✅ $service:$port - Conectividade OK"
    else
        print_message "${RED}" "❌ $service:$port - Falha na conectividade"
        
        # Tentar diagnóstico adicional
        print_message "${YELLOW}" "Executando diagnóstico adicional..."
        docker run --rm --network smartalarm-test-net \
            mcr.microsoft.com/dotnet/sdk:8.0 \
            sh -c "
                echo 'Informações de rede:'
                hostname -I
                echo 'Resolução DNS:'
                nslookup $service || getent hosts $service || echo 'Falha na resolução DNS'
                echo 'Conectividade ICMP:'
                ping -c 2 $service || echo 'Ping falhou'
            "
    fi
}

# Função para verificar logs dos serviços
check_service_logs() {
    print_message "${BLUE}" "📄 Verificando logs dos serviços..."
    
    # Detectar prefixo dos contêineres
    local prefix="smart-alarm"
    if docker ps | grep -q "smart-alarm"; then
        prefix="smart-alarm"
    elif docker ps | grep -q "smartalarm"; then
        prefix="smartalarm"
    else
        print_message "${YELLOW}" "Nenhum contêiner de serviço encontrado"
        return 1
    fi
    
    local services=("postgres" "vault" "minio" "rabbitmq")
    
    for service in "${services[@]}"; do
        local container_name="${prefix}-${service}-1"
        
        if docker ps --format '{{.Names}}' | grep -q "^${container_name}$"; then
            print_message "${CYAN}" "📄 Logs do $service (últimas 10 linhas):"
            docker logs --tail 10 "$container_name" 2>&1 | head -20
            echo ""
        else
            print_message "${YELLOW}" "  ⚠️  Container $container_name não encontrado"
        fi
    done
}

# Função para executar teste básico de diagnóstico
run_diagnostic_test() {
    print_message "${BLUE}" "🧪 Executando teste básico de diagnóstico..."
    
    # Executar um teste muito simples para verificar se o ambiente está funcional
    docker run --rm \
        --network smartalarm-test-net \
        -v "$PROJECT_ROOT:/app" \
        -w /app \
        -e ASPNETCORE_ENVIRONMENT=Testing \
        mcr.microsoft.com/dotnet/sdk:8.0 \
        sh -c "
            echo '=== Diagnóstico do Ambiente ==='
            echo 'Versão do .NET:'
            dotnet --version
            echo ''
            echo 'Informações de rede:'
            hostname -I
            echo ''
            echo 'Verificação de projetos:'
            if [ -f '/app/SmartAlarm.sln' ]; then
                echo '✅ SmartAlarm.sln encontrado'
            else
                echo '❌ SmartAlarm.sln não encontrado'
            fi
            echo ''
            echo 'Teste de conectividade básica:'
            for service in postgres vault minio rabbitmq; do
                if command -v nc >/dev/null && timeout 5 nc -z \$service 5432 >/dev/null 2>&1 || timeout 5 nc -z \$service 8200 >/dev/null 2>&1 || timeout 5 nc -z \$service 9000 >/dev/null 2>&1 || timeout 5 nc -z \$service 5672 >/dev/null 2>&1; then
                    echo \"✅ \$service - OK\"
                else
                    echo \"❌ \$service - Falha\"
                fi
            done
            echo ''
            echo 'Executando teste .NET simples:'
            dotnet test --list-tests /app/SmartAlarm.sln 2>/dev/null | head -5 | grep -E '(Test|Tests)' | head -3 || echo 'Nenhum teste encontrado'
        "
    
    print_message "${GREEN}" "✅ Diagnóstico concluído"
}

# Função principal do script
main() {
    local mode="$1"
    
    # Detectar diretório raiz do projeto
    detect_project_root
    
    case "$mode" in
        "connectivity")
            check_service_connectivity
            ;;
        "interactive")
            enter_interactive_container
            ;;
        "logs")
            check_service_logs
            ;;
        "test")
            run_diagnostic_test
            ;;
        "")
            run_debug_mode
            ;;
        *)
            print_message "${RED}" "❌ Modo de debug inválido: $mode"
            print_message "${BLUE}" "Modos disponíveis: connectivity, interactive, logs, test"
            exit 1
            ;;
    esac
}

# Executar apenas se chamado diretamente (não via source)
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi
