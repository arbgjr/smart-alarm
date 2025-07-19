# Ambiente de Desenvolvimento com Docker

Este documento descreve como configurar, executar e gerenciar o ambiente de desenvolvimento para testes de integração do Smart Alarm. O ambiente inclui todos os serviços necessários para executar testes de integração completos, desenvolvimento local e validação de funcionalidades.

## Pré-requisitos

- **Docker**: Instalado e rodando
- **WSL (Windows Subsystem for Linux)**: Configurado para execução de scripts shell
- **.NET SDK**: Versão 8.0 ou superior instalado
- **Docker Compose** (opcional): Recomendado para facilitar a gestão de todos os serviços

## Scripts Disponíveis

O projeto inclui três scripts principais para gerenciar o ambiente de desenvolvimento:

### 1. Iniciar o Ambiente de Desenvolvimento

```bash
# No terminal WSL ou compatível com Bash
chmod +x ./start-dev-env.sh
./start-dev-env.sh [opção]
```

**Opções disponíveis:**

- Sem parâmetro: Inicia apenas os serviços básicos (RabbitMQ, PostgreSQL, MinIO, Vault)
- `observability`: Inicia serviços básicos + stack de observabilidade (Prometheus, Loki, Jaeger, Grafana)
- `api`: Inicia serviços básicos + observabilidade + API do Smart Alarm
- `all`: Inicia todos os serviços

**O que este script faz:**

- Verifica se o Docker está rodando
- Detecta se o Docker Compose está disponível (preferencial) ou cria containers individuais
- Inicia os serviços solicitados e verifica se estão saudáveis
- Exibe informações de acesso às interfaces de gerenciamento
- Fornece instruções para os próximos passos (testes, encerramento)

### 2. Executar Testes de Integração

```bash
# No terminal WSL ou compatível com Bash
chmod +x ./test-integration.sh
./test-integration.sh [serviço]
```

**Serviços disponíveis para teste:**

- `rabbitmq`: Testa integração com RabbitMQ
- `postgres`: Testa integração com PostgreSQL
- `minio`: Testa integração com MinIO
- `vault`: Testa integração com HashiCorp Vault
- `keyvault`: Testa serviço KeyVault (depende do HashiCorp Vault)
- `observability`: Testa serviços de observabilidade (logs, métricas e tracing)
- `all`: Executa todos os testes de integração

**O que este script faz:**

- Verifica se os containers necessários estão rodando
- Executa os testes específicos para o serviço solicitado
- Exibe o resultado dos testes e instruções para próximos passos

### 3. Encerrar o Ambiente de Desenvolvimento

```bash
# No terminal WSL ou compatível com Bash
chmod +x ./stop-dev-env.sh
./stop-dev-env.sh [opção]
```

**Opções disponíveis:**

- Sem parâmetro: Apenas para os containers (mantém dados e configurações)
- `clean`: Remove containers e redes, mas mantém volumes (dados)
- `purge`: Remove completamente containers, redes e volumes (limpa todos os dados)

**O que este script faz:**

- Detecta se o Docker Compose está disponível ou gerencia containers individualmente
- Encerra ou remove os containers conforme a opção escolhida
- Exibe o estado atual dos containers e instruções para próximos passos

## Acesso às Interfaces de Gerenciamento

Após iniciar o ambiente, você pode acessar as seguintes interfaces:

| Serviço | URL | Credenciais |
|---------|-----|-------------|
| RabbitMQ | `http://localhost:15672/` | guest/guest |
| MinIO | `http://localhost:9001/` | minio/minio123 |
| HashiCorp Vault | `http://localhost:8200/` | Token: dev-token |
| PostgreSQL | localhost:5432 | smartalarm/smartalarm123 |
| Grafana | `http://localhost:3001/` | admin/admin |
| Jaeger | `http://localhost:16686/` | N/A |
| Prometheus | `http://localhost:9090/` | N/A |
| API Smart Alarm | `http://localhost:8080/` | N/A |
| Swagger UI | `http://localhost:8080/swagger` | N/A |

## Configurações dos Serviços

### RabbitMQ

- **Porta AMQP**: 5672
- **Porta Interface Web**: 15672
- **Usuário**: guest
- **Senha**: guest
- **Uso**: Mensageria e eventos do sistema

### PostgreSQL

- **Porta**: 5432
- **Banco de Dados**: smartalarm
- **Usuário**: smartalarm
- **Senha**: smartalarm123
- **Uso**: Persistência de dados principal

### MinIO

- **Porta API**: 9000
- **Porta Interface Web**: 9001
- **Access Key**: minio
- **Secret Key**: minio123
- **Uso**: Armazenamento de objetos e arquivos

### HashiCorp Vault

- **Porta**: 8200
- **Token de Root**: dev-token
- **Modo**: desenvolvimento (não seguro para produção)
- **Uso**: Gerenciamento de segredos e credenciais

### Serviços de Observabilidade

O Docker Compose também inclui os seguintes serviços para observabilidade:

- **Grafana**: Visualização de métricas e logs (porta 3001)
- **Prometheus**: Coleta e armazenamento de métricas (porta 9090)
- **Loki**: Coleta e armazenamento de logs (porta 3100)
- **Jaeger**: Rastreamento distribuído (porta 16686)

## Fluxos de Trabalho Comuns

### Executar todos os testes de integração

```bash
./start-dev-env.sh
./test-integration.sh all
```

### Trabalhar com observabilidade

```bash
./start-dev-env.sh observability
# Acesse Grafana em http://localhost:3001
```

### Executar e testar a API completa

```bash
./start-dev-env.sh api
# Acesse Swagger em http://localhost:8080/swagger
```

### Limpar completamente o ambiente

```bash
./stop-dev-env.sh purge
```

## Troubleshooting

Se encontrar problemas com os testes de integração, verifique:

1. **Docker está rodando?**

   ```bash
   docker info
   ```

2. **Containers necessários estão ativos?**

   ```bash
   docker ps | grep -E "rabbitmq|postgres|minio|vault"
   ```

3. **Portas necessárias estão disponíveis?**

   ```bash
   # No Windows
   netstat -ano | findstr "5672 15672 5432 9000 9001 8200"
   # No Linux
   netstat -tulpn | grep -E "5672|15672|5432|9000|9001|8200"
   ```

4. **Containers conseguem se comunicar?**

   ```bash
   docker network inspect smart-alarm-network
   ```

5. **Verificar logs de um container específico:**

   ```bash
   docker logs [nome-do-container]
   ```

6. **Reiniciar um container específico:**

   ```bash
   docker restart [nome-do-container]
   ```

7. **Verificar saúde de um serviço específico:**

   ```bash
   # RabbitMQ
   docker exec -it rabbitmq rabbitmqctl status
   
   # PostgreSQL
   docker exec -it postgres pg_isready -U smartalarm
   
   # MinIO
   curl -s http://localhost:9000/minio/health/live
   
   # Vault
   curl -s http://localhost:8200/v1/sys/health
   ```

## Docker Compose

O projeto inclui um arquivo `docker-compose.yml` na raiz que configura todos os serviços necessários. Os scripts apresentados acima utilizarão preferencialmente o Docker Compose se disponível, ou criarão containers individuais caso contrário.

Para usar o Docker Compose diretamente:

```bash
# Iniciar todos os serviços
docker-compose up -d

# Iniciar apenas serviços específicos
docker-compose up -d rabbitmq postgres minio vault

# Verificar status
docker-compose ps

# Visualizar logs
docker-compose logs -f [serviço]

# Parar serviços
docker-compose stop

# Remover containers e networks (mantém volumes)
docker-compose down

# Remover tudo, incluindo volumes
docker-compose down -v
```

O Docker Compose configura a seguinte infraestrutura:

1. **Serviços básicos para integração**:
   - RabbitMQ (mensageria)
   - PostgreSQL (banco de dados)
   - MinIO (armazenamento de objetos)
   - HashiCorp Vault (gerenciamento de segredos)

2. **Stack de observabilidade**:
   - Loki (logs)
   - Jaeger (tracing)
   - Prometheus (métricas)
   - Grafana (dashboards)

3. **API do projeto** (construída a partir do Dockerfile)

## Estrutura dos Testes de Integração

Os testes de integração estão organizados nas seguintes pastas:

- `tests/SmartAlarm.Infrastructure.Tests/Integration/`: Testes de integração com serviços externos
- `tests/SmartAlarm.KeyVault.Tests/Integration/`: Testes de integração com KeyVault
- `tests/SmartAlarm.Api.Tests/Integration/`: Testes de integração de API
- `tests/integration/`: Testes de integração adicionais

Todos os testes seguem os padrões definidos no Memory Bank:

- Pattern AAA (Arrange, Act, Assert)
- Categorias de teste claramente definidas
- Cobertura mínima de 80% para código crítico
- Tratamento apropriado de recursos externos

## Integração com CI/CD

Os scripts de ambiente de desenvolvimento são compatíveis com pipelines de CI/CD:

```yaml
# Exemplo de uso em pipeline
steps:
  - name: Start test environment
    run: ./start-dev-env.sh
    
  - name: Run integration tests
    run: ./test-integration.sh all
    
  - name: Stop test environment
    run: ./stop-dev-env.sh clean
```

## Próximos Passos

1. Executar testes de integração específicos conforme necessário
2. Consultar logs e métricas no Grafana após execução de testes
3. Verificar rastreamento distribuído no Jaeger para depuração
4. Atualizar Memory Bank com resultados e observações importantes
