# Storage Layer

> Observação: Qualquer referência a mocks/stubs refere-se apenas ao ambiente de desenvolvimento/teste.
> Para produção, utilize sempre as integrações reais conforme documentação.

Esta pasta contém a abstração, mock e integrações reais para armazenamento de arquivos do Smart Alarm.

## Serviços Disponíveis

- `IStorageService` / `MockStorageService`: Interface e mock para upload, download e delete de arquivos.
- `MinioStorageService`: Integração real com MinIO (dev/homologação).
- `OciObjectStorageService`: Stub para integração futura com OCI Object Storage (produção).

## Como usar

Os serviços são registrados automaticamente na DI via `DependencyInjection.cs`.

Exemplo de uso:

```csharp
public class MinhaClasse
{
    private readonly IStorageService _storage;
    public MinhaClasse(IStorageService storage) { _storage = storage; }
    public async Task DoWork() {
        await _storage.UploadAsync("/test.txt", new MemoryStream());
    }
}
```

## Extensão

Para produção, utilize `OciObjectStorageService`.
Para dev/homologação, utilize `MinioStorageService`.
Para testes, utilize `MockStorageService`.
