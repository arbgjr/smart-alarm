# OCI Vault Provider API Documentation

## Visão Geral
O `RealOciVaultProvider` é uma implementação concreta do provedor Oracle Cloud Infrastructure (OCI) Vault que oferece funcionalidades reais de gestão de segredos com fallback gracioso para valores simulados quando a conectividade OCI não está disponível.

## Configuração

### Configuração Básica
```json
{
  "OciVault": {
    "Environment": "real",
    "CompartmentOcid": "ocid1.compartment.oc1..aaaaaaaxxxxxxx",
    "Region": "us-ashburn-1",
    "VaultOcid": "ocid1.vault.oc1.us-ashburn-1.aaaaaaaxxxxxxx",
    "EncryptionKeyOcid": "ocid1.key.oc1.us-ashburn-1.aaaaaaaxxxxxxx"
  }
}
```

### Configuração Baseada em Ambiente
```csharp
// Registrar o provedor OCI real
services.AddOciVaultReal(configuration);

// Registrar o provedor OCI simulado
services.AddOciVaultSimulated(configuration);

// Registrar baseado no ambiente configurado
services.AddOciVault(configuration);
```

## Endpoints da API

### 1. Obter Segredo

**Endpoint:** `GET /api/vault/secrets/{secretName}`

**Parâmetros:**
- `secretName` (path, obrigatório): Nome do segredo a ser recuperado
- `version` (query, opcional): Versão específica do segredo

**Resposta de Sucesso (200):**
```json
{
  "name": "database-connection",
  "value": "encrypted_connection_string",
  "version": "1",
  "retrievedFrom": "oci-vault",
  "timestamp": "2024-12-07T10:30:00Z"
}
```

**Resposta com Fallback (200):**
```json
{
  "name": "database-connection",
  "value": "simulated_database_connection_for_database-connection",
  "version": "simulated",
  "retrievedFrom": "simulated-fallback",
  "timestamp": "2024-12-07T10:30:00Z"
}
```

### 2. Definir Segredo

**Endpoint:** `POST /api/vault/secrets`

**Body da Requisição:**
```json
{
  "name": "api-key",
  "value": "secret_api_key_value",
  "description": "API key for external service"
}
```

**Resposta de Sucesso (201):**
```json
{
  "name": "api-key",
  "version": "1",
  "status": "created",
  "storedIn": "oci-vault",
  "timestamp": "2024-12-07T10:30:00Z"
}
```

### 3. Obter Múltiplos Segredos

**Endpoint:** `POST /api/vault/secrets/batch`

**Body da Requisição:**
```json
{
  "secretNames": ["database-connection", "api-key", "jwt-secret"],
  "version": "latest"
}
```

**Resposta de Sucesso (200):**
```json
{
  "secrets": [
    {
      "name": "database-connection",
      "value": "encrypted_connection_string",
      "version": "1",
      "retrievedFrom": "oci-vault"
    },
    {
      "name": "api-key",
      "value": "simulated_api_key_for_api-key",
      "version": "simulated",
      "retrievedFrom": "simulated-fallback"
    }
  ],
  "totalRetrieved": 2,
  "timestamp": "2024-12-07T10:30:00Z"
}
```

### 4. Verificar Disponibilidade do Provedor

**Endpoint:** `GET /api/vault/providers/oci/status`

**Resposta de Sucesso (200):**
```json
{
  "providerName": "RealOciVaultProvider",
  "isAvailable": true,
  "region": "us-ashburn-1",
  "compartment": "ocid1.compartment.oc1..aaaaaaaxxxxxxx",
  "lastHealthCheck": "2024-12-07T10:30:00Z",
  "status": "healthy"
}
```

**Resposta quando Indisponível (200):**
```json
{
  "providerName": "RealOciVaultProvider",
  "isAvailable": false,
  "reason": "Failed to initialize real OCI VaultsClient",
  "fallbackMode": true,
  "lastHealthCheck": "2024-12-07T10:30:00Z",
  "status": "degraded"
}
```

## Códigos de Status HTTP

| Código | Descrição |
|--------|-----------|
| 200 | Requisição bem-sucedida |
| 201 | Segredo criado com sucesso |
| 400 | Requisição inválida (nome do segredo em branco, etc.) |
| 404 | Segredo não encontrado |
| 500 | Erro interno do servidor |
| 503 | Serviço temporariamente indisponível |

## Cabeçalhos de Resposta

### Cabeçalhos Padrão
- `Content-Type: application/json`
- `X-Provider-Name: RealOciVaultProvider`
- `X-Vault-Region: us-ashburn-1`

### Cabeçalhos de Observabilidade
- `X-Trace-Id`: ID único para rastreamento distribuído
- `X-Request-Duration`: Duração da requisição em milissegundos
- `X-Fallback-Used`: "true" se fallback foi utilizado

## Exemplos de Uso

### cURL - Obter Segredo
```bash
curl -X GET "https://api.smartalarm.com/api/vault/secrets/database-connection" \
  -H "Authorization: Bearer {token}" \
  -H "Accept: application/json"
```

### cURL - Definir Segredo
```bash
curl -X POST "https://api.smartalarm.com/api/vault/secrets" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "new-api-key",
    "value": "super_secret_value",
    "description": "New API key for integration"
  }'
```

### cURL - Verificar Status
```bash
curl -X GET "https://api.smartalarm.com/api/vault/providers/oci/status" \
  -H "Authorization: Bearer {token}" \
  -H "Accept: application/json"
```

## Configuração de Autenticação OCI

### Arquivo de Configuração
Crie o arquivo `~/.oci/config`:
```ini
[DEFAULT]
user=ocid1.user.oc1..aaaaaaaxxxxxxx
fingerprint=xx:xx:xx:xx:xx:xx:xx:xx:xx:xx:xx:xx:xx:xx:xx:xx
tenancy=ocid1.tenancy.oc1..aaaaaaaxxxxxxx
region=us-ashburn-1
key_file=~/.oci/oci_api_key.pem
```

### Chave Privada
Coloque sua chave privada OCI em `~/.oci/oci_api_key.pem` com permissões 600.

## Observabilidade

### Métricas Expostas
- `oci_vault_requests_total`: Total de requisições ao OCI Vault
- `oci_vault_request_duration_seconds`: Duração das requisições
- `oci_vault_fallback_usage_total`: Uso de fallback simulado
- `oci_vault_errors_total`: Total de erros

### Logs Estruturados
```json
{
  "timestamp": "2024-12-07T10:30:00Z",
  "level": "Information",
  "message": "Successfully retrieved real secret {SecretName} from OCI Vault in {Duration}ms",
  "properties": {
    "SecretName": "database-connection",
    "Duration": 245,
    "Provider": "RealOciVaultProvider",
    "Region": "us-ashburn-1"
  }
}
```

### Traces Distribuídos
O provedor gera spans com as seguintes operações:
- `oci-vault.get-secret`
- `oci-vault.set-secret`
- `oci-vault.get-multiple-secrets`
- `oci-vault.check-availability`

## Segurança

### Boas Práticas
1. **Autenticação**: Use credenciais OCI adequadas
2. **Autorização**: Configure IAM policies restritivas
3. **Criptografia**: Segredos são criptografados em trânsito e em repouso
4. **Auditoria**: Logs detalhados para compliance
5. **Fallback Seguro**: Valores simulados não expõem dados reais

### Configurações de Segurança
```json
{
  "OciVault": {
    "EnableFallback": true,
    "FallbackPrefix": "simulated_",
    "MaxRetryAttempts": 3,
    "TimeoutSeconds": 30,
    "EnableAuditLogs": true
  }
}
```

## Troubleshooting

### Problemas Comuns

#### 1. Configuração OCI Inválida
**Erro:** `Failed to configure OCI authentication`
**Solução:** Verifique o arquivo `~/.oci/config` e chave privada

#### 2. Permissões Insuficientes
**Erro:** `Access denied to vault`
**Solução:** Configure IAM policies adequadas no OCI

#### 3. Rede/Conectividade
**Erro:** `Failed to connect to OCI Vault`
**Solução:** Verifique conectividade de rede e endpoints OCI

### Logs de Debug
Para habilitar logs detalhados:
```json
{
  "Logging": {
    "LogLevel": {
      "SmartAlarm.KeyVault.Providers.RealOciVaultProvider": "Debug"
    }
  }
}
```

## Limitações e Considerações

1. **Rate Limiting**: OCI Vault tem limites de requisições por minuto
2. **Latência**: Requisições reais podem ter latência de rede
3. **Fallback**: Valores simulados são previsíveis (não usar em produção sem OCI real)
4. **Dependências**: Requer OCI.DotNetSDK.Vault package
5. **Configuração**: Arquivo de configuração OCI obrigatório para operação real

## Versionamento da API

A API segue versionamento semântico (SemVer):
- **Major**: Mudanças incompatíveis
- **Minor**: Novas funcionalidades compatíveis
- **Patch**: Correções de bugs

Versão atual: **1.0.0**
