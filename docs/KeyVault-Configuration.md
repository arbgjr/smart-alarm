# KeyVault Configuration Example

This document shows how to configure the KeyVault middleware for different cloud providers.

## Environment Variables Configuration

### HashiCorp Vault (Default - Highest Priority)
```bash
# General KeyVault settings
KeyVault__Enabled=true
KeyVault__GlobalTimeoutSeconds=30
KeyVault__RetryAttempts=3
KeyVault__EnableCaching=true
KeyVault__CacheExpirationMinutes=15

# HashiCorp Vault configuration
HashiCorpVault__ServerAddress=http://localhost:8200
HashiCorpVault__Token=dev-token
HashiCorpVault__MountPath=secret
HashiCorpVault__KvVersion=2
HashiCorpVault__SkipTlsVerification=true
HashiCorpVault__Priority=1
```

### Azure Key Vault
```bash
# Azure Key Vault configuration
AzureKeyVault__VaultUri=https://myvault.vault.azure.net/
AzureKeyVault__UseManagedIdentity=true
AzureKeyVault__Priority=2

# OR with service principal authentication
AzureKeyVault__TenantId=your-tenant-id
AzureKeyVault__ClientId=your-client-id
AzureKeyVault__ClientSecret=your-client-secret
AzureKeyVault__UseManagedIdentity=false
```

### AWS Secrets Manager
```bash
# AWS Secrets Manager configuration
AwsSecretsManager__Region=us-east-1
AwsSecretsManager__UseIamRole=true
AwsSecretsManager__Priority=3

# OR with access key authentication
AwsSecretsManager__AccessKeyId=your-access-key
AwsSecretsManager__SecretAccessKey=your-secret-key
AwsSecretsManager__UseIamRole=false
```

### Google Cloud Secret Manager
```bash
# GCP Secret Manager configuration
GcpSecretManager__ProjectId=your-project-id
GcpSecretManager__UseApplicationDefaultCredentials=true
GcpSecretManager__Priority=4

# OR with service account key
GcpSecretManager__ServiceAccountKeyPath=/path/to/key.json
GcpSecretManager__UseApplicationDefaultCredentials=false
```

### Oracle Cloud Infrastructure (OCI) Vault
```bash
# OCI Vault configuration (basic structure - requires full implementation)
OciVault__TenancyId=ocid1.tenancy.oc1..xxx
OciVault__UserId=ocid1.user.oc1..xxx
OciVault__Region=us-ashburn-1
OciVault__VaultId=ocid1.vault.oc1..xxx
OciVault__CompartmentId=ocid1.compartment.oc1..xxx
OciVault__Fingerprint=your-key-fingerprint
OciVault__PrivateKeyPath=/path/to/private-key.pem
OciVault__Priority=5
```

## Docker Compose Configuration

The KeyVault middleware is already configured in `docker-compose.yml` with HashiCorp Vault:

```yaml
services:
  vault:
    image: hashicorp/vault:1.15
    ports:
      - "8200:8200"
    environment:
      - VAULT_DEV_ROOT_TOKEN_ID=dev-token
      - VAULT_DEV_LISTEN_ADDRESS=0.0.0.0:8200
    command: ["vault", "server", "-dev"]
    
  api:
    environment:
      - KeyVault__Enabled=true
      - HashiCorpVault__ServerAddress=http://vault:8200
      - HashiCorpVault__Token=dev-token
    depends_on:
      vault:
        condition: service_healthy
```

## Usage in Code

### Dependency Injection Setup
```csharp
// In Program.cs
builder.Services.AddKeyVault(builder.Configuration);
app.UseKeyVault();
```

### Using the KeyVault Service
```csharp
public class MyController : ControllerBase
{
    private readonly IKeyVaultService _keyVaultService;
    
    public MyController(IKeyVaultService keyVaultService)
    {
        _keyVaultService = keyVaultService;
    }
    
    public async Task<IActionResult> GetSecret()
    {
        var secret = await _keyVaultService.GetSecretAsync("my-secret-key");
        if (secret != null)
        {
            // Use the secret
            return Ok("Secret retrieved successfully");
        }
        return NotFound("Secret not found");
    }
    
    public async Task<IActionResult> SetSecret()
    {
        var success = await _keyVaultService.SetSecretAsync("my-secret-key", "my-secret-value");
        return success ? Ok("Secret set successfully") : BadRequest("Failed to set secret");
    }
    
    public async Task<IActionResult> GetAvailableProviders()
    {
        var providers = await _keyVaultService.GetAvailableProvidersAsync();
        return Ok(providers);
    }
}
```

## Provider Priority

The middleware tries providers in the following order by default:
1. **HashiCorp Vault** (Priority: 1) - Default choice
2. **Azure Key Vault** (Priority: 2)
3. **AWS Secrets Manager** (Priority: 3)
4. **GCP Secret Manager** (Priority: 4)
5. **OCI Vault** (Priority: 5)

You can override priorities using the `KeyVault__ProviderPriorities` configuration:
```bash
KeyVault__ProviderPriorities__Azure=1
KeyVault__ProviderPriorities__HashiCorp=2
```

## Disabling Providers

To disable specific providers:
```bash
KeyVault__DisabledProviders__0=AWS
KeyVault__DisabledProviders__1=GCP
```

## Starting the Development Environment

To start the development environment with HashiCorp Vault:

```bash
# Start all services including Vault
docker-compose up -d

# Check Vault status
curl http://localhost:8200/v1/sys/health

# Set a test secret (optional)
docker exec -it smart-alarm_vault_1 vault kv put secret/test-key value=test-value

# Start the API
dotnet run --project src/SmartAlarm.Api
```

The API will be available at `http://localhost:8080` and Vault UI at `http://localhost:8200` (token: `dev-token`).