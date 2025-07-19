# ADR-999: Integração com SmartAlarm.KeyVault para Gerenciamento de Secrets

## Status

Aceito

## Contexto

Necessidade de gerenciar secrets JWT e FIDO2 de forma segura, aproveitando a infraestrutura existente do projeto SmartAlarm.KeyVault que já suporta múltiplos provedores (Azure Key Vault, HashiCorp Vault, AWS Secrets Manager, GCP Secret Manager, OCI Vault).

## Decisão

Utilizar o projeto **SmartAlarm.KeyVault existente** para centralizar o gerenciamento de todos os secrets relacionados à autenticação JWT/FIDO2.

## Justificativa

- **Infraestrutura Existente**: Projeto KeyVault já implementado e testado
- **Multi-Provider**: Suporte a múltiplos provedores de secrets
- **Segurança**: Implementação já segue boas práticas OWASP
- **Centralização**: Único ponto de controle para todos os secrets
- **Compliance**: Alinhado com requisitos LGPD

## Secrets Gerenciados

### JWT
- `jwt-secret`: Chave para assinatura de tokens JWT
- `jwt-issuer`: Issuer para validação de tokens
- `jwt-audience`: Audience para validação de tokens

### FIDO2
- `fido2-rp-id`: Relying Party ID para WebAuthn
- `fido2-rp-name`: Nome do Relying Party
- `fido2-origin`: Origin permitida para challenges

## Consequências

### Positivas

- Reutilização de infraestrutura testada
- Suporte a múltiplos ambientes (dev, staging, prod)
- Rotação automática de chaves
- Auditoria centralizada de acesso a secrets
- Facilidade de migração entre provedores

### Negativas

- Dependência externa ao serviço de secrets
- Necessidade de configuração por ambiente

## Implementação

```csharp
// Configuração no Program.cs
builder.Services.AddKeyVault(builder.Configuration);

// Uso nos serviços de autenticação
public class JwtTokenService : IJwtTokenService
{
    private readonly IKeyVaultService _keyVault;
    
    public async Task<string> GenerateTokenAsync(User user)
    {
        var secret = await _keyVault.GetSecretAsync("jwt-secret");
        // ... implementação
    }
}
```

## Configuração de Ambiente

```json
{
  "KeyVault": {
    "Enabled": true,
    "DefaultProvider": "HashiCorpVault"
  },
  "HashiCorpVault": {
    "ServerAddress": "https://vault.example.com",
    "Token": "hvs.xxx",
    "Mount": "secret",
    "Path": "smart-alarm/auth"
  }
}
```

---
*Data: 2025-07-03*
*Revisores: Equipe de Arquitetura*
