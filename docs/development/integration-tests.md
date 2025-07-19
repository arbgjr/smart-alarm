# Testes de Integração - Documentação

## Visão Geral

Este documento descreve a abordagem de testes de integração utilizada no projeto Smart Alarm, especificamente focando na verificação de conectividade com serviços externos como MinIO, Vault, PostgreSQL, RabbitMQ e a stack de observabilidade (Grafana, Loki, Jaeger, Prometheus).

## Categorias de Testes

Os testes de integração estão organizados nas seguintes categorias:

### Testes Essenciais

- **MinIO**: Verificação de conectividade com o serviço de armazenamento de objetos
- **Vault**: Verificação de conectividade com o gerenciador de segredos
- **PostgreSQL**: Verificação de conectividade com o banco de dados
- **RabbitMQ**: Verificação de conectividade com o message broker

### Testes de Observabilidade

- **Grafana**: Verificação de conectividade com o dashboard de visualização
- **Loki**: Verificação de conectividade com o agregador de logs
- **Jaeger**: Verificação de conectividade com o sistema de tracing
- **Prometheus**: Verificação de conectividade com o coletor de métricas

## Abordagem de Verificação de Saúde

Para garantir maior estabilidade e independência de versões específicas de SDKs, adotamos uma abordagem baseada em verificações HTTP de saúde:

### Endpoints de Saúde

- **MinIO**: `http://localhost:9000/minio/health/live`
- **Vault**: `http://localhost:8200/v1/sys/health` ou `http://localhost:8200/v1/sys/seal-status`
- **PostgreSQL**: Comando `pg_isready -U smartalarm`
- **RabbitMQ**: Comando `rabbitmqctl status`
- **Grafana**: `http://localhost:3001/api/health`
- **Loki**: `http://localhost:3100/ready`
- **Jaeger**: `http://localhost:16686/`
- **Prometheus**: `http://localhost:9090/-/healthy`

## Script de Teste Docker (`docker-test.sh`)

O script `docker-test.sh` oferece uma maneira flexível de executar testes de integração em um ambiente Docker isolado:

### Modos de Execução

- **Modo Padrão**: `./docker-test.sh` - Executa todos os testes de integração
- **Modo Essencial**: `./docker-test.sh essentials` - Executa apenas testes essenciais
- **Modo Observabilidade**: `./docker-test.sh observability` - Executa apenas testes de observabilidade
- **Testes Específicos**: `./docker-test.sh [minio|vault|postgres|rabbitmq]` - Executa testes para um serviço específico
- **Modo Debug**: `./docker-test.sh debug` - Apenas verifica o ambiente, sem executar testes
- **Modo Verboso**: Adicione `-v` ou `--verbose` como segundo parâmetro

### Funcionalidades do Script

1. **Verificação Dinâmica de Saúde**:
   - Substitui sleeps fixos por checagens ativas de disponibilidade
   - Implementa retentativas com intervalos configuráveis
   - Fornece feedback visual sobre o status de cada serviço

2. **Inicialização Condicional**:
   - Inicia apenas os serviços necessários para o conjunto de testes selecionado
   - Economiza recursos e acelera a execução

3. **Diagnóstico Aprimorado**:
   - Verifica detalhadamente cada serviço antes de executar os testes
   - Exibe informações específicas sobre falhas de serviços
   - Fornece sugestões para resolução de problemas

4. **Isolamento de Ambiente**:
   - Utiliza rede Docker dedicada para os testes
   - Garante que os testes não interfiram em outros contêineres

## Executando os Testes

Para executar os testes de integração:

```bash
# Todos os testes
./docker-test.sh

# Apenas testes essenciais
./docker-test.sh essentials

# Testes de observabilidade
./docker-test.sh observability

# Serviço específico
./docker-test.sh minio

# Modo verboso
./docker-test.sh essentials -v
```

## Solução de Problemas

### Erros Comuns e Soluções

- **Resource temporarily unavailable**: Verifique se o Docker tem recursos suficientes e se não há conflitos de porta
- **Connection refused**: Verifique se o serviço está rodando com `docker ps` e inspecione logs com `docker-compose logs [service]`
- **Falhas nos serviços de observabilidade**: Execute apenas testes essenciais com `./docker-test.sh essentials`

### Logs e Diagnóstico

Para obter informações detalhadas de diagnóstico:

```bash
# Ver logs de um serviço específico
docker-compose logs [nome-do-serviço]

# Verificar status dos contêineres
docker ps

# Executar testes em modo verboso
./docker-test.sh [modo] -v
```

## Integração com CI/CD

Para integração com pipelines CI/CD, recomenda-se:

1. Executar primeiro apenas os testes essenciais
2. Se bem-sucedidos, executar os testes de observabilidade
3. Capturar e armazenar logs detalhados em caso de falha
4. Configurar timeouts adequados para o ambiente de CI/CD
