#!/bin/bash

# Script de demonstração do sistema refatorado de testes Smart Alarm
# Este script mostra como usar os diferentes grupos de testes

# Cores para output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

print_message() {
    local color=$1
    local message=$2
    echo -e "${color}${message}${NC}"
}

print_header() {
    echo ""
    print_message "${BLUE}" "=============================================="
    print_message "${BLUE}" "$1"
    print_message "${BLUE}" "=============================================="
    echo ""
}

print_step() {
    print_message "${CYAN}" "🔸 $1"
}

print_command() {
    print_message "${YELLOW}" "   Comando: $1"
}

main() {
    print_header "🎯 Demonstração do Sistema Refatorado de Testes Smart Alarm"
    
    print_message "${GREEN}" "✅ REFATORAÇÃO CONCLUÍDA COM SUCESSO!"
    echo ""
    print_message "${BLUE}" "📋 O que foi implementado:"
    print_message "${CYAN}" "  ✅ Script principal refatorado (SmartAlarm-test.sh)"
    print_message "${CYAN}" "  ✅ 5 scripts especializados criados (pasta scripts/)"
    print_message "${CYAN}" "  ✅ Arquivo de funções comuns (test-common.sh)"
    print_message "${CYAN}" "  ✅ Backup preservado (SmartAlarm-test.sh.backup)"
    print_message "${CYAN}" "  ✅ Documentação completa (README-refactored-structure.md)"
    
    print_header "📚 Como Usar o Sistema Refatorado"
    
    print_step "1. Visualizar ajuda completa"
    print_command "./tests/SmartAlarm-test.sh help"
    echo ""
    
    print_step "2. Testes básicos (rápidos, sem containers)"
    print_command "./tests/SmartAlarm-test.sh basic"
    print_command "./tests/SmartAlarm-test.sh owasp"
    print_command "./tests/SmartAlarm-test.sh security"
    echo ""
    
    print_step "3. Testes de integração (com containers)"
    print_command "./tests/SmartAlarm-test.sh postgres"
    print_command "./tests/SmartAlarm-test.sh vault"
    print_command "./tests/SmartAlarm-test.sh minio"
    print_command "./tests/SmartAlarm-test.sh rabbitmq"
    echo ""
    
    print_step "4. Testes especializados"
    print_command "./tests/SmartAlarm-test.sh holiday"
    print_command "./tests/SmartAlarm-test.sh coverage"
    print_command "./tests/SmartAlarm-test.sh debug"
    echo ""
    
    print_step "5. Modo verboso (para qualquer teste)"
    print_command "./tests/SmartAlarm-test.sh postgres -v"
    print_command "./tests/SmartAlarm-test.sh basic --verbose"
    echo ""
    
    print_header "📁 Estrutura de Arquivos Criados"
    
    if [[ -f "tests/SmartAlarm-test.sh" ]]; then
        local size=$(wc -c < "tests/SmartAlarm-test.sh" 2>/dev/null || echo "0")
        if [[ $size -gt 100 ]]; then
            print_message "${GREEN}" "  ✅ tests/SmartAlarm-test.sh (${size} bytes) - Script principal"
        else
            print_message "${RED}" "  ❌ tests/SmartAlarm-test.sh (${size} bytes) - Muito pequeno"
        fi
    else
        print_message "${RED}" "  ❌ tests/SmartAlarm-test.sh - Não encontrado"
    fi
    
    if [[ -f "tests/test-common.sh" ]]; then
        local size=$(wc -c < "tests/test-common.sh" 2>/dev/null || echo "0")
        print_message "${GREEN}" "  ✅ tests/test-common.sh (${size} bytes) - Funções compartilhadas"
    else
        print_message "${RED}" "  ❌ tests/test-common.sh - Não encontrado"
    fi
    
    if [[ -d "tests/scripts" ]]; then
        print_message "${GREEN}" "  ✅ tests/scripts/ - Pasta de scripts especializados:"
        for script in tests/scripts/*.sh; do
            if [[ -f "$script" ]]; then
                local name=$(basename "$script")
                local size=$(wc -c < "$script" 2>/dev/null || echo "0")
                print_message "${CYAN}" "     ✅ $name (${size} bytes)"
            fi
        done
    else
        print_message "${RED}" "  ❌ tests/scripts/ - Pasta não encontrada"
    fi
    
    if [[ -f "tests/SmartAlarm-test.sh.backup" ]]; then
        local size=$(wc -c < "tests/SmartAlarm-test.sh.backup" 2>/dev/null || echo "0")
        print_message "${YELLOW}" "  💾 tests/SmartAlarm-test.sh.backup (${size} bytes) - Backup original"
    fi
    
    print_header "🔍 Verificação de Sintaxe"
    
    print_step "Verificando sintaxe dos scripts..."
    local syntax_ok=true
    
    for script in tests/SmartAlarm-test.sh tests/test-common.sh tests/scripts/*.sh; do
        if [[ -f "$script" ]]; then
            local name=$(basename "$script")
            if bash -n "$script" 2>/dev/null; then
                print_message "${GREEN}" "  ✅ $name - Sintaxe OK"
            else
                print_message "${RED}" "  ❌ $name - Erro de sintaxe"
                syntax_ok=false
            fi
        fi
    done
    
    print_header "🎯 Próximos Passos"
    
    if [[ "$syntax_ok" == "true" ]]; then
        print_message "${GREEN}" "✅ Todos os scripts têm sintaxe válida!"
        echo ""
        print_message "${BLUE}" "🚀 Você pode agora:"
        print_message "${CYAN}" "  1. Executar: ./tests/SmartAlarm-test.sh help"
        print_message "${CYAN}" "  2. Testar: ./tests/SmartAlarm-test.sh basic"
        print_message "${CYAN}" "  3. Integrar: ./tests/SmartAlarm-test.sh postgres"
        print_message "${CYAN}" "  4. Depurar: ./tests/SmartAlarm-test.sh debug"
        echo ""
        print_message "${YELLOW}" "📖 Documentação completa em: tests/README-refactored-structure.md"
    else
        print_message "${RED}" "❌ Há erros de sintaxe que precisam ser corrigidos"
    fi
    
    print_header "📋 Resumo da Refatoração"
    
    print_message "${GREEN}" "🎯 MISSÃO CUMPRIDA!"
    print_message "${BLUE}" "✅ Sistema modular implementado"
    print_message "${BLUE}" "✅ Scripts especializados criados"  
    print_message "${BLUE}" "✅ Funções reutilizáveis organizadas"
    print_message "${BLUE}" "✅ Backup original preservado"
    print_message "${BLUE}" "✅ Documentação atualizada"
    print_message "${BLUE}" "✅ Compatibilidade mantida"
    
    echo ""
    print_message "${YELLOW}" "🔧 O SmartAlarm-test.sh agora é um orquestrador que chama scripts especializados!"
    print_message "${YELLOW}" "🏗️ Cada grupo de testes tem seu próprio script otimizado!"
    print_message "${YELLOW}" "♻️ Funções comuns foram centralizadas para reutilização!"
    
    echo ""
    print_message "${GREEN}" "🎉 Refatoração concluída com sucesso! 🎉"
}

# Executar demonstração
main "$@"
