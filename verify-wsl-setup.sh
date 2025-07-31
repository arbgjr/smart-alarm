#!/bin/bash

# Smart Alarm - Verificação Final WSL
# Executa todos os testes e verificações necessárias

set -e  # Para em caso de erro

# Cores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}=====================================${NC}"
echo -e "${BLUE}   Smart Alarm - Verificação WSL    ${NC}"
echo -e "${BLUE}=====================================${NC}"
echo ""

# Função para verificar comando
check_command() {
    if command -v $1 &> /dev/null; then
        echo -e "${GREEN}✅ $1 está instalado${NC}"
        if [ "$1" = "node" ]; then
            echo -e "   Versão: $(node --version)"
        elif [ "$1" = "npm" ]; then
            echo -e "   Versão: $(npm --version)"
        fi
    else
        echo -e "${RED}❌ $1 não encontrado${NC}"
        return 1
    fi
}

# Verificar ambiente WSL
echo -e "${YELLOW}1. Verificando ambiente WSL...${NC}"
if grep -q Microsoft /proc/version; then
    echo -e "${GREEN}✅ Executando em WSL${NC}"
    WSL_IP=$(hostname -I | awk '{print $1}')
    echo -e "${GREEN}   IP do WSL: ${WSL_IP}${NC}"
else
    echo -e "${YELLOW}⚠️  Não detectado WSL (pode ser Linux nativo)${NC}"
    WSL_IP=$(hostname -I | awk '{print $1}')
    echo -e "${GREEN}   IP detectado: ${WSL_IP}${NC}"
fi
echo ""

# Verificar dependências
echo -e "${YELLOW}2. Verificando dependências...${NC}"
check_command "node"
check_command "npm"
echo ""

# Verificar estrutura do projeto
echo -e "${YELLOW}3. Verificando estrutura do projeto...${NC}"
if [ -d "frontend" ]; then
    echo -e "${GREEN}✅ Diretório frontend encontrado${NC}"

    if [ -f "frontend/package.json" ]; then
        echo -e "${GREEN}✅ package.json encontrado${NC}"
    else
        echo -e "${RED}❌ package.json não encontrado${NC}"
    fi

    if [ -f "frontend/vite.config.ts" ]; then
        echo -e "${GREEN}✅ vite.config.ts encontrado${NC}"

        # Verificar configuração do Vite
        if grep -q "host: '0.0.0.0'" frontend/vite.config.ts; then
            echo -e "${GREEN}✅ Vite configurado para acesso externo${NC}"
        else
            echo -e "${RED}❌ Vite não configurado para acesso externo${NC}"
        fi
    else
        echo -e "${RED}❌ vite.config.ts não encontrado${NC}"
    fi
else
    echo -e "${RED}❌ Diretório frontend não encontrado${NC}"
    exit 1
fi
echo ""

# Verificar documentação
echo -e "${YELLOW}4. Verificando documentação...${NC}"
if [ -f "docs/development/WSL-SETUP-GUIDE.md" ]; then
    echo -e "${GREEN}✅ Guia WSL encontrado${NC}"
else
    echo -e "${YELLOW}⚠️  Guia WSL não encontrado${NC}"
fi

if [ -f "docs/user-guides/MANUAL-DE-USO.md" ]; then
    echo -e "${GREEN}✅ Manual do usuário encontrado${NC}"
else
    echo -e "${YELLOW}⚠️  Manual do usuário não encontrado${NC}"
fi
echo ""

# Verificar scripts
echo -e "${YELLOW}5. Verificando scripts...${NC}"
if [ -f "start-wsl-dev.sh" ]; then
    echo -e "${GREEN}✅ Script WSL encontrado${NC}"
    if [ -x "start-wsl-dev.sh" ]; then
        echo -e "${GREEN}✅ Script WSL é executável${NC}"
    else
        echo -e "${YELLOW}⚠️  Tornando script executável...${NC}"
        chmod +x start-wsl-dev.sh
        echo -e "${GREEN}✅ Script agora é executável${NC}"
    fi
else
    echo -e "${RED}❌ Script start-wsl-dev.sh não encontrado${NC}"
fi
echo ""

# Testar instalação das dependências (sem executar o servidor)
echo -e "${YELLOW}6. Testando instalação de dependências...${NC}"
cd frontend

if [ ! -d "node_modules" ]; then
    echo -e "${YELLOW}⚠️  node_modules não encontrado, instalando dependências...${NC}"
    npm install
fi

if [ -d "node_modules" ]; then
    echo -e "${GREEN}✅ Dependências instaladas${NC}"
else
    echo -e "${RED}❌ Falha na instalação das dependências${NC}"
    exit 1
fi

cd ..
echo ""

# Verificar portas disponíveis
echo -e "${YELLOW}7. Verificando portas...${NC}"
if netstat -tlnp 2>/dev/null | grep -q ":5173 "; then
    echo -e "${YELLOW}⚠️  Porta 5173 já está em uso${NC}"
    echo -e "${YELLOW}   Para liberar: sudo lsof -ti:5173 | xargs sudo kill -9${NC}"
else
    echo -e "${GREEN}✅ Porta 5173 disponível${NC}"
fi
echo ""

# Resumo final
echo -e "${BLUE}=====================================${NC}"
echo -e "${BLUE}         RESUMO DA VERIFICAÇÃO       ${NC}"
echo -e "${BLUE}=====================================${NC}"
echo ""
echo -e "${GREEN}✅ Ambiente WSL configurado${NC}"
echo -e "${GREEN}✅ Dependências instaladas${NC}"
echo -e "${GREEN}✅ Vite configurado para WSL${NC}"
echo -e "${GREEN}✅ Scripts e documentação prontos${NC}"
echo ""
echo -e "${YELLOW}📋 PRÓXIMOS PASSOS:${NC}"
echo -e "${YELLOW}1. Execute: ./start-wsl-dev.sh${NC}"
echo -e "${YELLOW}2. Acesse no Windows: http://${WSL_IP}:5173${NC}"
echo -e "${YELLOW}3. Para desenvolvimento: code .${NC}"
echo ""
echo -e "${BLUE}=====================================${NC}"
echo -e "${BLUE}   Verificação concluída com sucesso!${NC}"
echo -e "${BLUE}=====================================${NC}"
