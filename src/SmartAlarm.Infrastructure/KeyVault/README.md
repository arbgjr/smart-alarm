# KeyVault Layer

Esta pasta contém a abstração, mock e integrações reais para providers de segredos do Smart Alarm.

## Serviços Disponíveis

- `IKeyVaultProvider`: Interface para providers de segredos.
- `MockKeyVaultProvider`: Mock para desenvolvimento/testes.
- `HashiCorpVaultProvider`: Integração real com HashiCorp Vault (dev/homologação).
- `OciVaultProvider`: Stub para integração futura com OCI Vault (produção).
- `AzureKeyVaultProvider`: Stub para integração futura com Azure Key Vault (opcional).
- `AwsSecretsManagerProvider`: Stub para integração futura com AWS Secrets Manager (opcional).

## Como usar

Os serviços são registrados automaticamente na DI via `DependencyInjection.cs`.

Exemplo de uso:

```csharp
public class MinhaClasse
{
    private readonly IKeyVaultProvider _vault;
    public MinhaClasse(IKeyVaultProvider vault) { _vault = vault; }
    public async Task DoWork() {
        var secret = await _vault.GetSecretAsync("DbPassword");
    }
}
```

## Extensão

Para produção, utilize `OciVaultProvider`.
Para dev/homologação, utilize `HashiCorpVaultProvider`.
Para testes, utilize `MockKeyVaultProvider`.
