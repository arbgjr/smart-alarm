#!/bin/bash

# Script para testar o build do Docker para SmartAlarm.Api
# Deve ser executado a partir do diretório raiz do projeto

set -e

echo "🐳 Docker Build Test para SmartAlarm.Api"
echo "========================================"

# Verificar se estamos no diretório correto
if [ ! -f "SmartAlarm.sln" ]; then
    echo "❌ Erro: Execute este script a partir do diretório raiz do projeto (onde está o SmartAlarm.sln)"
    exit 1
fi

# Verificar se o Docker está rodando
if ! docker info >/dev/null 2>&1; then
    echo "❌ Erro: Docker não está rodando. Inicie o Docker primeiro."
    exit 1
fi

echo "📁 Contexto de build: $(pwd)"
echo "🔍 Verificando estrutura do projeto..."

# Verificar arquivos essenciais
if [ ! -f "Directory.Packages.props" ]; then
    echo "❌ Erro: Directory.Packages.props não encontrado no diretório raiz"
    exit 1
fi

if [ ! -f "src/SmartAlarm.Api/Dockerfile" ]; then
    echo "❌ Erro: Dockerfile não encontrado em src/SmartAlarm.Api/"
    exit 1
fi

echo "✅ Estrutura do projeto verificada"

# Limpar containers anteriores (se existirem)
echo "🧹 Limpando containers anteriores..."
docker rmi smart-alarm-api:test 2>/dev/null || true

# Build do container
echo "🏗️  Iniciando build do Docker..."
echo "Comando: docker build -f src/SmartAlarm.Api/Dockerfile -t smart-alarm-api:test ."

if docker build -f src/SmartAlarm.Api/Dockerfile -t smart-alarm-api:test .; then
    echo "✅ Build concluído com sucesso!"

    # Mostrar informações da imagem
    echo ""
    echo "📊 Informações da imagem criada:"
    docker images smart-alarm-api:test

    echo ""
    echo "🎉 Teste de build bem-sucedido!"
    echo "Para executar o container: docker run -p 8080:80 smart-alarm-api:test"
else
    echo "❌ Build falhou!"
    echo ""
    echo "🔍 Dicas para debugging:"
    echo "1. Verifique se todos os projetos em src/ estão compilando: dotnet build SmartAlarm.sln"
    echo "2. Verifique se o Directory.Packages.props está correto"
    echo "3. Execute este script do diretório raiz do projeto"
    echo "4. Verifique os logs acima para erros específicos"
    exit 1
fi
