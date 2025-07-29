#!/bin/bash

# Script para limpar arquivos problemáticos copiados do Windows para WSL
# Smart Alarm Project - Windows File Cleanup

echo "🧹 Smart Alarm - Limpeza de Arquivos Windows → WSL"
echo "=================================================="

# Função para listar arquivos
list_files() {
    echo "📋 Listando arquivos problemáticos encontrados:"
    echo ""

    echo "🔸 Arquivos .tmp:"
    find . -name "*.tmp" -type f | head -10
    echo "   ℹ️  Nota: commit.tmp será preservado"

    echo ""
    echo "🔸 Arquivos .bak:"
    find . -name "*.bak" -type f | head -10

    echo ""
    echo "🔸 Arquivos :sec.endpointdlp (Windows Security):"
    find . -name "*:sec.endpointdlp" -type f | head -10

    echo ""
    echo "🔸 Arquivos .orig:"
    find . -name "*.orig" -type f | head -10

    echo ""
    echo "🔸 Total de arquivos problemáticos:"
    problematic_count=$(find . \( -name "*.tmp" -o -name "*.bak" -o -name "*.orig" -o -name "*:sec.endpointdlp" \) -type f | wc -l)
    echo "   $problematic_count arquivos encontrados"
    tmp_count=$(find . -name "*.tmp" -type f ! -name "commit.tmp" | wc -l)
    echo "   $tmp_count arquivos .tmp serão removidos (commit.tmp preservado)"
}

# Função para analisar por tipo
analyze_by_type() {
    echo ""
    echo "📊 Análise por tipo:"
    echo "==================="

    echo "🔹 Arquivos .tmp (temporários):"
    echo "   - Geralmente seguros para remover"
    echo "   - São criados pelo build process e podem ser regenerados"
    echo "   - ⚠️  EXCEÇÃO: commit.tmp será preservado conforme solicitado"

    echo ""
    echo "🔹 Arquivos .bak (backup):"
    echo "   - Podem conter código importante"
    echo "   - Revisar antes de remover"

    echo ""
    echo "🔹 Arquivos :sec.endpointdlp (Windows Defender):"
    echo "   - Criados pelo Windows Defender"
    echo "   - Seguros para remover no WSL"
    echo "   - São duplicatas dos arquivos originais"

    echo ""
    echo "🔹 Arquivos .orig (merge conflicts):"
    echo "   - Podem conter código de merge conflicts"
    echo "   - Revisar antes de remover"
}

# Função para remover arquivos seguros
clean_safe_files() {
    echo ""
    echo "🧹 Removendo arquivos seguros para limpar..."
    echo "============================================"

    # Remove .tmp files (sempre seguros) - EXCETO commit.tmp
    echo "🗑️  Removendo arquivos .tmp (exceto commit.tmp)..."
    find . -name "*.tmp" -type f ! -name "commit.tmp" -delete

    # Remove :sec.endpointdlp files (Windows Defender duplicates)
    echo "🗑️  Removendo arquivos :sec.endpointdlp..."
    find . -name "*:sec.endpointdlp" -type f -delete

    # Remove arquivos obj temporários (exceto commit.tmp)
    echo "🗑️  Removendo arquivos temporários obj..."
    find . -path "*/obj/*" -name "*.tmp" -type f ! -name "commit.tmp" -delete

    echo "✅ Arquivos seguros removidos (commit.tmp preservado)!"
}

# Função para listar arquivos que precisam de revisão
list_review_files() {
    echo ""
    echo "⚠️  Arquivos que precisam de revisão manual:"
    echo "==========================================="

    echo "🔍 Arquivos .bak (podem conter código importante):"
    find . -name "*.bak" -type f

    echo ""
    echo "🔍 Arquivos .orig (podem conter código de merge):"
    find . -name "*.orig" -type f

    echo ""
    echo "💡 Para remover arquivos .bak após revisar:"
    echo "   find . -name '*.bak' -type f -delete"
    echo ""
    echo "💡 Para remover arquivos .orig após revisar:"
    echo "   find . -name '*.orig' -type f -delete"
}

# Função para verificar git status
check_git_status() {
    echo ""
    echo "📄 Status do Git após limpeza:"
    echo "=============================="
    git status --porcelain | head -20

    changed_files=$(git status --porcelain | wc -l)
    echo ""
    echo "📊 Total de arquivos modificados no git: $changed_files"
}

# Função para criar .gitignore adicional se necessário
update_gitignore() {
    echo ""
    echo "📝 Verificando .gitignore..."

    # Verificar se já tem as regras necessárias
    if ! grep -q "*.tmp" .gitignore; then
        echo "🔧 Adicionando regras ao .gitignore..."
        echo "" >> .gitignore
        echo "# Windows cleanup - additional rules" >> .gitignore
        echo "*:sec.endpointdlp" >> .gitignore
        echo "*.zwc" >> .gitignore
        echo "*.zone.identifier" >> .gitignore
        echo "*.lnk" >> .gitignore
        echo "desktop.ini" >> .gitignore
        echo "Thumbs.db" >> .gitignore
        echo ""
        echo "✅ Regras adicionadas ao .gitignore"
    else
        echo "✅ .gitignore já contém as regras necessárias"
    fi
}

# Menu principal
main_menu() {
    echo ""
    echo "🔧 O que você gostaria de fazer?"
    echo "==============================="
    echo "1) Listar arquivos problemáticos"
    echo "2) Analisar tipos de arquivos"
    echo "3) Limpar arquivos seguros automaticamente"
    echo "4) Listar arquivos para revisão manual"
    echo "5) Verificar status do git"
    echo "6) Atualizar .gitignore"
    echo "7) Fazer limpeza completa (recomendado)"
    echo "8) Sair"
    echo ""
    read -p "Escolha uma opção (1-8): " choice

    case $choice in
        1) list_files ;;
        2) analyze_by_type ;;
        3) clean_safe_files ;;
        4) list_review_files ;;
        5) check_git_status ;;
        6) update_gitignore ;;
        7)
            echo "🚀 Executando limpeza completa..."
            list_files
            analyze_by_type
            clean_safe_files
            update_gitignore
            list_review_files
            check_git_status
            echo ""
            echo "✅ Limpeza completa realizada!"
            ;;
        8)
            echo "👋 Saindo..."
            exit 0
            ;;
        *)
            echo "❌ Opção inválida. Tente novamente."
            main_menu
            ;;
    esac
}

# Execução principal
if [ "$1" == "--auto" ]; then
    echo "🤖 Modo automático - executando limpeza completa..."
    list_files
    clean_safe_files
    update_gitignore
    check_git_status
    echo ""
    echo "✅ Limpeza automática concluída!"
    echo "⚠️  Verifique manualmente os arquivos .bak e .orig se existirem"
else
    list_files
    main_menu
fi
