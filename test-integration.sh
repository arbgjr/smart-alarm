#!/bin/bash

# Define cores para melhor legibilidade
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Verificar dependências para testes de integração
check_dependencies() {
    echo -e "${YELLOW}Verificando dependências necessárias...${NC}"
    
    # Verificar se o Docker está instalado
    if ! command -v docker &> /dev/null; then
        echo -e "${RED}⚠ Docker não está instalado ou não está disponível no PATH.${NC}"
        echo -e "${YELLOW}Instale o Docker e tente novamente.${NC}"
        return 1
    fi
    
    # Variável global para indicar se devemos usar Docker para executar os testes
    USE_DOCKER=false
    
    # Verificar se o dotnet CLI está instalado
    if ! command -v dotnet &> /dev/null; then
        echo -e "${YELLOW}⚠ O .NET SDK não está instalado ou não está disponível no PATH.${NC}"
        echo -e "${YELLOW}Para instalar o .NET SDK:${NC}"
        echo -e "  - ${BLUE}Windows:${NC} Visite https://dotnet.microsoft.com/download"
        echo -e "  - ${BLUE}Linux (Ubuntu/Debian):${NC} sudo apt-get update && sudo apt-get install -y dotnet-sdk-8.0"
        echo -e "  - ${BLUE}macOS:${NC} brew install dotnet-sdk"
        
        # Verificar se podemos usar Docker para executar os testes
        echo -e "${YELLOW}Verificando se podemos usar Docker para executar os testes...${NC}"
        if docker info > /dev/null 2>&1; then
            echo -e "${GREEN}✓ Docker está disponível, vamos usar um container .NET para executar os testes.${NC}"
            USE_DOCKER=true
        else
            echo -e "${RED}⚠ Docker não está em execução ou não está configurado corretamente.${NC}"
            echo -e "${YELLOW}Inicie o Docker e tente novamente ou instale o .NET SDK.${NC}"
            return 1
        fi
    else
        echo -e "${GREEN}✓ .NET SDK encontrado: $(dotnet --version)${NC}"
    fi
    
    return 0
}

usage() {
  echo -e "${BLUE}Uso:${NC} $0 [serviço]"
  echo -e "Serviços disponíveis:"
  echo -e "  ${GREEN}rabbitmq${NC}  - Testa integração com RabbitMQ"
  echo -e "  ${GREEN}postgres${NC}  - Testa integração com PostgreSQL"
  echo -e "  ${GREEN}minio${NC}     - Testa integração com MinIO"
  echo -e "  ${GREEN}vault${NC}     - Testa integração com HashiCorp Vault"
  echo -e "  ${GREEN}keyvault${NC}  - Testa serviço KeyVault"
  echo -e "  ${GREEN}observability${NC} - Testa serviços de observabilidade"
  echo -e "  ${GREEN}all${NC}       - Testa todos os serviços de integração"
  exit 1
}

# Verificar se o Docker está rodando
if ! docker info > /dev/null 2>&1; then
    echo -e "${RED}Docker não está rodando. Por favor, inicie o Docker e tente novamente.${NC}"
    exit 1
fi

# Verificar parâmetro
if [ -z "$1" ]; then
  usage
fi

SERVICE=$1

verify_container() {
  if ! docker ps | grep -q $1; then
    echo -e "${RED}Container $1 não está rodando. Execute primeiro o script start-dev-env.sh${NC}"
    echo -e "${YELLOW}Comando: ./start-dev-env.sh${NC}"
    exit 1
  else
    echo -e "${GREEN}✓ Container $1 está rodando${NC}"
  fi
}

check_test_dependencies() {
    local test_type=$1
    echo -e "${BLUE}Verificando dependências para testes de $test_type...${NC}"
    
    case $test_type in
        rabbitmq)
            verify_container "rabbitmq"
            ;;
        postgres)
            verify_container "postgres"
            ;;
        minio)
            verify_container "minio"
            ;;
        vault)
            verify_container "vault"
            ;;
        keyvault)
            # KeyVault depende do HashiCorp Vault para os testes de integração
            verify_container "vault"
            ;;
        observability)
            # Verifica os containers da stack de observabilidade
            echo -e "${YELLOW}Verificando containers da stack de observabilidade...${NC}"
            for container in "prometheus" "loki" "jaeger" "grafana"; do
                if ! docker ps | grep -q $container; then
                    echo -e "${YELLOW}Aviso: Container $container não encontrado. Alguns testes de observabilidade podem falhar.${NC}"
                else
                    echo -e "${GREEN}✓ Container $container está rodando${NC}"
                fi
            done
            ;;
        all)
            # Verificar os containers principais
            for container in "rabbitmq" "postgres" "minio" "vault"; do
                if ! docker ps | grep -q $container; then
                    echo -e "${YELLOW}Aviso: Container $container não encontrado. Testes relacionados podem falhar.${NC}"
                else
                    echo -e "${GREEN}✓ Container $container está rodando${NC}"
                fi
            done
            # Verificar containers da stack de observabilidade
            for container in "prometheus" "loki" "jaeger" "grafana"; do
                if ! docker ps | grep -q $container; then
                    echo -e "${YELLOW}Aviso: Container $container não encontrado. Alguns testes de observabilidade podem falhar.${NC}"
                else
                    echo -e "${GREEN}✓ Container $container está rodando${NC}"
                fi
            done
            ;;
    esac
}

run_tests() {
    local test_filter=$1
    local additional_args=$2
    local test_description=$3
    
    echo -e "\n${BLUE}=== Executando testes: ${test_description} ===${NC}"
    echo -e "${YELLOW}Filtro: ${test_filter}${NC}"
    
    # Determinar se devemos usar Docker ou dotnet diretamente
    if [ "$USE_DOCKER" = true ]; then
        echo -e "${BLUE}Usando Docker para executar os testes...${NC}"
        # Obter o caminho completo do diretório atual
        local project_dir=$(pwd)
        
        echo -e "${BLUE}Comando: docker run --rm -v \"${project_dir}:/app\" -w /app mcr.microsoft.com/dotnet/sdk:8.0 dotnet test --filter \"${test_filter}\" --logger \"console;verbosity=detailed\" ${additional_args}${NC}"
        
        # Executar os testes usando Docker
        set +e  # Desativar saída automática em erro
        docker run --rm -v "${project_dir}:/app" -w /app mcr.microsoft.com/dotnet/sdk:8.0 dotnet test --filter "${test_filter}" --logger "console;verbosity=detailed" ${additional_args}
        local result=$?
        set -e  # Reativar saída automática em erro
    else
        echo -e "${BLUE}Comando: dotnet test --filter \"${test_filter}\" --logger \"console;verbosity=detailed\" ${additional_args}${NC}"
        
        # Executar testes com o comando dotnet test diretamente
        set +e  # Desativar saída automática em erro
        dotnet test --filter "${test_filter}" --logger "console;verbosity=detailed" ${additional_args}
        local result=$?
        set -e  # Reativar saída automática em erro
    fi
    
    # Verificar resultado
    if [ $result -eq 0 ]; then
        echo -e "${GREEN}✓ Testes executados com sucesso!${NC}"
        return 0
    elif [ $result -eq 127 ]; then
        # Código de erro 127 geralmente significa "comando não encontrado"
        echo -e "${RED}⚠ Erro ao executar o comando. Verifique as mensagens de erro acima.${NC}"
        return 1
    else
        echo -e "${YELLOW}⚠ Alguns testes falharam. Verifique o log acima para mais detalhes.${NC}"
        return 0  # Continuamos mesmo com falhas nos testes
    fi
}

echo -e "${BLUE}=== Smart Alarm - Testes de Integração ===${NC}"

# Verificar dependências antes de continuar
check_dependencies || exit 1

echo -e "${YELLOW}Verificando ambiente para testes de $SERVICE...${NC}"

case $SERVICE in
  rabbitmq)
    check_test_dependencies "rabbitmq"
    run_tests "FullyQualifiedName~RabbitMqMessagingServiceIntegrationTests" "" "RabbitMQ"
    ;;

  postgres)
    check_test_dependencies "postgres"
    run_tests "FullyQualifiedName~PostgresUnitOfWorkIntegrationTests" "" "PostgreSQL"
    ;;

  minio)
    check_test_dependencies "minio"
    run_tests "FullyQualifiedName~MinioStorageServiceIntegrationTests" "" "MinIO Storage"
    ;;

  vault)
    check_test_dependencies "vault"
    run_tests "FullyQualifiedName~HashiCorpVaultProviderIntegrationTests" "" "HashiCorp Vault"
    ;;

  keyvault)
    check_test_dependencies "keyvault"
    run_tests "FullyQualifiedName~KeyVaultIntegrationTests" "" "KeyVault Service"
    ;;

  observability)
    check_test_dependencies "observability"
    run_tests "FullyQualifiedName~ObservabilityIntegrationTests" "" "Observability (Logs e Tracing)"
    run_tests "FullyQualifiedName~ObservabilityMetricsIntegrationTests" "" "Observability (Métricas)"
    ;;

  all)
    check_test_dependencies "all"
    run_tests "Category=Integration" "" "Todos os Testes de Integração"
    ;;

  *)
    echo -e "${RED}Serviço desconhecido: $SERVICE${NC}"
    usage
    ;;
esac

echo -e "\n${GREEN}✅ Execução de testes finalizada!${NC}"
echo -e "${YELLOW}Para encerrar o ambiente de desenvolvimento, execute: ./stop-dev-env.sh${NC}"
