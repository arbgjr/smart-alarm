#!/bin/bash

# Smart Alarm - WSL Development Script
# Este script configura e executa o frontend no WSL para acesso via Windows

echo "🚀 Smart Alarm - WSL Development Setup"
echo "========================================"

# Cores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Verificar se está no WSL
if ! grep -qEi "(Microsoft|WSL)" /proc/version &> /dev/null; then
    echo -e "${RED}❌ Este script deve ser executado no WSL${NC}"
    exit 1
fi

echo -e "${BLUE}📋 Verificando dependências...${NC}"

# Verificar Node.js
if ! command -v node &> /dev/null; then
    echo -e "${RED}❌ Node.js não encontrado. Instale o Node.js primeiro.${NC}"
    exit 1
fi

# Verificar npm
if ! command -v npm &> /dev/null; then
    echo -e "${RED}❌ npm não encontrado. Instale o npm primeiro.${NC}"
    exit 1
fi

echo -e "${GREEN}✅ Node.js $(node --version) encontrado${NC}"
echo -e "${GREEN}✅ npm $(npm --version) encontrado${NC}"

# Navegar para o diretório frontend
cd "$(dirname "$0")/frontend" || exit

echo -e "${BLUE}📦 Instalando dependências...${NC}"
npm install

# Verificar se a instalação foi bem-sucedida
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ Dependências instaladas com sucesso${NC}"
else
    echo -e "${RED}❌ Erro ao instalar dependências${NC}"
    exit 1
fi

# Obter IP do WSL
WSL_IP=$(hostname -I | awk '{print $1}')

echo -e "${YELLOW}🌐 Configuração de Rede:${NC}"
echo -e "   WSL IP: ${GREEN}$WSL_IP${NC}"
echo -e "   Porta: ${GREEN}5173${NC}"
echo -e "   Acesso Windows: ${GREEN}http://$WSL_IP:5173${NC}"
echo -e "   Acesso WSL: ${GREEN}http://localhost:5173${NC}"

echo ""
echo -e "${BLUE}🔥 Iniciando servidor de desenvolvimento...${NC}"
echo -e "${YELLOW}💡 Dica: Use Ctrl+C para parar o servidor${NC}"
echo ""

# Iniciar o servidor
npm run dev
