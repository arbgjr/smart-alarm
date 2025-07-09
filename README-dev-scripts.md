# Smart Alarm - Scripts de Ambiente de Desenvolvimento

Este diretório contém scripts para facilitar a configuração, execução e teste do ambiente de desenvolvimento do Smart Alarm.

## Scripts Disponíveis

### `start-dev-env.sh`

Inicia o ambiente de desenvolvimento com os serviços necessários.

```bash
# Uso básico (apenas serviços de integração)
./start-dev-env.sh

# Iniciar com stack de observabilidade
./start-dev-env.sh observability

# Iniciar com API e observabilidade
./start-dev-env.sh api

# Iniciar todos os serviços
./start-dev-env.sh all
```

### `test-integration.sh`

Executa testes de integração para serviços específicos ou todos os serviços.

```bash
# Executar todos os testes de integração
./test-integration.sh all

# Executar testes específicos
./test-integration.sh rabbitmq
./test-integration.sh postgres
./test-integration.sh minio
./test-integration.sh vault
./test-integration.sh keyvault
./test-integration.sh observability
```

### `stop-dev-env.sh`

Encerra o ambiente de desenvolvimento.

```bash
# Apenas parar os serviços
./stop-dev-env.sh

# Remover containers e redes (mantendo volumes/dados)
./stop-dev-env.sh clean

# Remover tudo (containers, redes E volumes)
./stop-dev-env.sh purge
```

## Documentação Detalhada

Para informações mais detalhadas sobre o ambiente de desenvolvimento, consulte:

- [Documentação do Ambiente de Desenvolvimento](./dev-environment-docs.md)
- [Padrões de Teste de Integração](./docs/development/integration-testing.md)
- [Arquitetura de Observabilidade](./docs/architecture/observability-patterns.md)

## Começando

Para novos desenvolvedores, recomendamos a seguinte sequência:

1. Instale os pré-requisitos (Docker, .NET SDK)
2. Execute `./start-dev-env.sh all` para iniciar o ambiente completo
3. Execute `./test-integration.sh all` para verificar se tudo está funcionando corretamente
4. Explore os endpoints da API em `http://localhost:8080/swagger`
5. Verifique métricas e logs em `http://localhost:3001` (Grafana)

## Solução de Problemas

Se encontrar problemas, consulte a seção "Troubleshooting" em [dev-environment-docs.md](./dev-environment-docs.md).
