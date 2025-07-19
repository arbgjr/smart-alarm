# 📤 Testes de Upload/Storage - Smart Alarm

Este documento descreve a implementação e execução dos testes relacionados às funcionalidades de upload e storage de arquivos no sistema Smart Alarm.

## � Visão Geral

O sistema de testes de Upload/Storage cobre:

- **Testes Unitários**: MockStorageService, UploadController
- **Testes de Integração**: MinioStorageService, endpoints HTTP
- **Testes de Performance**: Upload de arquivos grandes, uploads simultâneos
- **Testes de Cenários**: Diferentes tipos de arquivo, caracteres especiais

## 🏗️ Arquitetura de Testes

### Camadas Testadas

```
┌─────────────────────────────────────┐
│           API Layer                 │
│  UploadController + Endpoints       │
├─────────────────────────────────────┤
│         Application Layer           │
│    Upload Commands/Queries          │
├─────────────────────────────────────┤
│       Infrastructure Layer          │
│  IStorageService Implementations    │
│  - MockStorageService              │
│  - MinioStorageService             │
│  - OciObjectStorageService         │
└─────────────────────────────────────┘
```

### Tipos de Teste

1. **Unit Tests**
   - MockStorageService comportamento
   - UploadController lógica
   - Validações e tratamento de erros

2. **Integration Tests**
   - MinioStorageService + MinIO real
   - Endpoints HTTP completos
   - Cenários end-to-end

3. **Performance Tests**
   - Upload de arquivos grandes (1MB+)
   - Uploads simultâneos/concorrentes
   - Stress testing
- **Localização**: `tests/SmartAlarm.Api.Tests/Controllers/AlarmControllerUploadTests.cs`
- **Propósito**: Validar o endpoint de upload de alarmes
- **Cenários Testados**:
  - Validação de arquivo obrigatório
  - Validação de autenticação
  - Processamento de CSV válido
  - Diferentes tipos de arquivo
  - Arquivos grandes
  - Parâmetro overwriteExisting
  - Tratamento de exceções
  - Cancellation token

### Testes de Integração

#### MinioStorageServiceIntegrationTests
- **Localização**: `tests/SmartAlarm.Infrastructure.Tests/Integration/MinioStorageServiceIntegrationTests.cs`
- **Propósito**: Validar integração real com MinIO
- **Cenários Testados**:
  - Upload, download e delete básico
  - Upload de arquivos grandes (1MB)
  - Diferentes tipos de arquivo
  - Arquivos vazios
  - Diretórios aninhados
  - Sobrescrita de arquivos
  - Upload múltiplo simultâneo
  - Caracteres especiais em nomes

#### AlarmUploadIntegrationTests
- **Localização**: `tests/SmartAlarm.Api.Tests/Integration/AlarmUploadIntegrationTests.cs`
- **Propósito**: Validar endpoint HTTP de upload
- **Cenários Testados**:
  - Autenticação obrigatória
  - Validação de arquivo obrigatório
  - Processamento de CSV válido
  - Diferentes tipos de arquivo
  - Arquivos grandes
  - Validação de tamanho máximo
  - Parâmetro overwriteExisting
  - Formato CSV inválido
  - Extensões não suportadas
  - Timeout apropriado

## 🚀 Executando os Testes

### Via Script Principal

```bash
# Todos os testes de upload/storage
./tests/SmartAlarm-test.sh storage

# Apenas testes de upload
./tests/SmartAlarm-test.sh upload

# Apenas testes MinIO
./tests/SmartAlarm-test.sh minio

# Apenas testes mock
./tests/SmartAlarm-test.sh mock-storage
```

### Via Script Especializado

```bash
# Executar diretamente o script de upload
./tests/scripts/run-upload-tests.sh storage

# Com modo verboso
./tests/scripts/run-upload-tests.sh storage --verbose
```

### Via dotnet test

```bash
# Testes de unidade (Mock Storage)
dotnet test tests/SmartAlarm.Infrastructure.Tests/ --filter "FullyQualifiedName~MockStorage"

# Testes de integração (MinIO)
dotnet test tests/SmartAlarm.Infrastructure.Tests/ --filter "FullyQualifiedName~MinioStorage"

# Testes de API (Upload Controller)
dotnet test tests/SmartAlarm.Api.Tests/ --filter "FullyQualifiedName~Upload"
```

## 🐳 Dependências dos Testes

### Serviços Necessários

Os testes de integração requerem os seguintes serviços em containers:

- **MinIO**: Armazenamento de objetos (porta 9000)
- **PostgreSQL**: Banco de dados (porta 5432)
- **Vault**: Gerenciamento de segredos (porta 8200)

### Configuração do Ambiente

```bash
# Iniciar serviços
docker-compose up -d

# Verificar se estão rodando
docker ps

# Aguardar serviços estarem prontos
docker exec smart-alarm-minio-1 mc ready local
```

## 📊 Cobertura de Testes

### Cenários Cobertos

✅ **Upload de Arquivos**
- Arquivos CSV válidos
- Arquivos de diferentes tipos
- Arquivos grandes (>1MB)
- Arquivos vazios
- Validação de extensão
- Validação de tamanho

✅ **Autenticação e Autorização**
- Usuário não autenticado
- Validação de token JWT
- Permissões adequadas

✅ **Armazenamento (MinIO)**
- Upload/Download/Delete básico
- Diretórios aninhados
- Sobrescrita de arquivos
- Upload simultâneo
- Caracteres especiais

✅ **Tratamento de Erros**
- Arquivo não enviado
- Formato inválido
- Tamanho excedido
- Falhas de rede
- Timeout

### Cenários para Implementar

🔄 **Melhorias Futuras**
- Validação de vírus/malware
- Compressão automática
- Backup automático
- Versionamento de arquivos
- Cleanup automático
- Métricas de upload

## 🔧 Configurações Específicas

### Variáveis de Ambiente para Testes

```bash
# MinIO
MINIO_ENDPOINT=minio
MINIO_PORT=9000
MINIO_ACCESS_KEY=minioadmin
MINIO_SECRET_KEY=minioadmin

# PostgreSQL
POSTGRES_HOST=postgres
POSTGRES_PORT=5432
POSTGRES_USER=smartalarm
POSTGRES_PASSWORD=smartalarm123
POSTGRES_DB=smartalarm

# Vault
VAULT_HOST=vault
VAULT_PORT=8200

# Ambiente
ASPNETCORE_ENVIRONMENT=Testing
```

### Limites de Upload

- **Tamanho máximo por arquivo**: 10MB
- **Tipos suportados**: CSV, TXT, JSON
- **Timeout**: 30 segundos
- **Conexões simultâneas**: 5

## 🐛 Troubleshooting

### Problemas Comuns

1. **MinIO não está pronto**
   ```bash
   # Verificar logs do MinIO
   docker logs smart-alarm-minio-1
   
   # Aguardar estar pronto
   docker exec smart-alarm-minio-1 mc ready local
   ```

2. **Falha na rede de testes**
   ```bash
   # Recriar rede
   docker network rm smartalarm-test-net
   docker network create smartalarm-test-net
   ```

3. **Testes de integração falhando**
   ```bash
   # Verificar se todos os serviços estão rodando
   docker ps
   
   # Reiniciar ambiente
   docker-compose down
   docker-compose up -d
   ```

4. **Permissões de arquivo**
   ```bash
   # Tornar scripts executáveis
   chmod +x tests/scripts/run-upload-tests.sh
   chmod +x tests/SmartAlarm-test.sh
   ```

## 📈 Métricas e Performance

### Benchmarks Esperados

- **Upload pequeno (<1KB)**: <100ms
- **Upload médio (1MB)**: <2s
- **Upload grande (10MB)**: <10s
- **Download**: <50% do tempo de upload
- **Delete**: <50ms

### Monitoramento

Os testes incluem validação de:
- Tempo de resposta
- Uso de memória
- Conectividade de rede
- Integridade dos dados
- Cleanup adequado

## 🔗 Referências

- [MinIO Documentation](https://docs.min.io/)
- [ASP.NET Core File Upload](https://docs.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads)
- [xUnit Testing Patterns](https://xunit.net/docs/getting-started)
- [Docker Compose Testing](https://docs.docker.com/compose/)
