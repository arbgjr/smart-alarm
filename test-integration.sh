#!/bin/bash

# Define cores para melhor legibilidade
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m' # No Color

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
    echo -e "${BLUE}Comando: dotnet test --filter \"${test_filter}\" --logger \"console;verbosity=detailed\" ${additional_args}${NC}"
    
    # Executar testes com o comando dotnet test
    dotnet test --filter "${test_filter}" --logger "console;verbosity=detailed" ${additional_args} || true
    
    # Verificar resultado
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✓ Testes executados com sucesso!${NC}"
    else
        echo -e "${YELLOW}⚠ Alguns testes falharam. Verifique o log acima para mais detalhes.${NC}"
    fi
}

echo -e "${BLUE}=== Smart Alarm - Testes de Integração ===${NC}"
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
