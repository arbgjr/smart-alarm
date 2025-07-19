# Como resolver problemas de conectividade em testes de integração com Docker

Este guia contém instruções para resolver problemas comuns de conectividade entre contêineres ao executar testes de integração no projeto Smart Alarm usando Docker. Seguindo estas diretrizes, você poderá executar testes de integração com confiabilidade tanto em ambientes locais quanto em pipelines CI/CD.

## Problema comum: "Connection refused" em testes de integração

O problema mais comum ao executar testes de integração com Docker ocorre quando os testes tentam se conectar aos serviços (PostgreSQL, MinIO, RabbitMQ, etc.) usando `localhost`, mas os serviços estão em contêineres separados.

### Sintomas

- Erros do tipo "Connection refused"
- Falhas de timeout em conexões
- Testes que funcionam localmente falham no Docker
- Falha no ping entre contêineres

## Soluções

### 1. Executar testes com o script `docker-test-fix.sh`

Este script foi criado especialmente para resolver problemas de conectividade entre contêineres:

```bash
# Tornar o script executável
chmod +x docker-test-fix.sh

# Executar todos os testes de integração
./docker-test-fix.sh

# Executar apenas testes específicos
./docker-test-fix.sh minio
./docker-test-fix.sh postgres
./docker-test-fix.sh vault
./docker-test-fix.sh rabbitmq

# Modo de depuração com saída detalhada
./docker-test-fix.sh debug -v
```

### 2. Como o script resolve o problema

O script `docker-test-fix.sh` implementa as seguintes soluções:

1. **Rede compartilhada**: Cria uma rede Docker dedicada para os testes
2. **Conectividade entre contêineres**: Conecta todos os contêineres de serviço à mesma rede
3. **Resolução de nomes**: Configura DNS para resolver os nomes dos serviços corretamente
4. **Variáveis de ambiente**: Define variáveis de ambiente para que os testes usem os nomes dos serviços em vez de localhost
5. **Diagnóstico**: Inclui ferramentas de diagnóstico para identificar problemas de rede

### 3. Modificar os testes para usar variáveis de ambiente

Para tornar seus testes mais robustos, modifique-os para usar variáveis de ambiente para obter os hosts dos serviços:

```csharp
// Em vez de hardcoded "localhost"
var host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost";
var port = int.Parse(Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432");
```

### 4. Usar a classe helper

Foi criada uma classe `DockerHelper.cs` para facilitar a resolução de hosts em testes de integração:

```csharp
using DockerHelper;

// Obter host e porta para um serviço
var host = DockerHelper.GetHost("POSTGRES");
var port = DockerHelper.GetPort("POSTGRES", 5432);

// Obter string de conexão completa
var connectionString = DockerHelper.GetPostgresConnectionString("mydatabase", "user", "password");

// Obter URL para um serviço
var minioUrl = DockerHelper.GetServiceUrl("MINIO", 9000);
```

## Solução de problemas

### Verificar conectividade entre contêineres

```bash
# Executar em modo de depuração para acessar um shell dentro do contêiner de teste
./docker-test-fix.sh debug

# Dentro do contêiner, testar a conectividade
ping postgres
ping minio
ping rabbitmq
ping vault

# Verificar resolução de nomes
nslookup postgres
dig postgres
```

### Verificar redes Docker

```bash
# Listar todas as redes Docker
docker network ls

# Inspecionar a rede de teste
docker network inspect smart-alarm-test-network

# Listar contêineres conectados à rede
docker network inspect smart-alarm-test-network -f "{{json .Containers}}" | jq
```

## Boas Práticas

Para garantir que seus testes de integração sejam robustos em qualquer ambiente, siga estas boas práticas:

### 1. Categorizar corretamente os testes

```csharp
[Fact]
[Category("Integration")] // Importante para execução seletiva
public async Task Should_Connect_To_Postgres() 
{
    // Código de teste...
}
```

### 2. Usar resolução dinâmica de hostnames

Sempre prefira resolver hostnames dinamicamente em vez de usar hardcoded "localhost":

```csharp
// Exemplo de uso do DockerHelper
var postgresHost = DockerHelper.ResolveServiceHostname("postgres");
var connectionString = $"Host={postgresHost};Port=5432;...";
```

### 3. Implementar verificação de saúde antes dos testes

```csharp
[Fact]
[Category("Integration")]
public async Task Should_Connect_To_Postgres()
{
    // Verificar se o serviço está disponível antes de testar
    var postgresHost = DockerHelper.ResolveServiceHostname("postgres");
    await WaitForServiceHealthyAsync(postgresHost, 5432);
    
    // Continuar com o teste...
}

private async Task WaitForServiceHealthyAsync(string host, int port, int timeoutSeconds = 30)
{
    // Implementar lógica de verificação de saúde com retry
}
```

### 4. Tratamento de timeout e retry

Adicione retry para lidar com serviços que podem ter inicialização mais lenta:

```csharp
// Exemplo de política de retry
var retryPolicy = Policy
    .Handle<Exception>()
    .WaitAndRetryAsync(
        retryCount: 5,
        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
    );

await retryPolicy.ExecuteAsync(async () => 
{
    // Código que pode falhar temporariamente
});
```

## Próximos Passos

1. Corrigir o erro de compilação em PostgresIntegrationTests.cs
2. Atualizar todos os testes de integração para usar o DockerHelper
3. Implementar verificação de saúde consistente para todos os serviços
4. Adicionar documentação detalhada sobre cada serviço de integração

# Inspecionar rede de teste
docker network inspect smartalarm-test-net

# Ver quais contêineres estão conectados
docker network inspect -f '{{range .Containers}}{{.Name}} {{end}}' smartalarm-test-net
```

### Diagnosticar problemas de rede com script

```bash
# Executar stop-dev-env.sh com modo de depuração
./stop-dev-env.sh debug
```

## Melhores práticas

1. **Sempre use variáveis de ambiente** para configurações de conexão
2. **Não hardcode "localhost"** em testes de integração
3. **Use a classe DockerHelper** para obter hosts e portas
4. **Execute testes em uma rede Docker dedicada**
5. **Verifique a saúde dos serviços** antes de iniciar os testes

---

Com estas instruções, você deve conseguir resolver a maioria dos problemas de conectividade ao executar testes de integração com Docker no projeto Smart Alarm.
