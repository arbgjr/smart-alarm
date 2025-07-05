# Storage Layer

Esta pasta contém a abstração e mock para armazenamento de arquivos do Smart Alarm.

## Serviços Disponíveis

- `IStorageService` / `MockStorageService`: Interface e mock para upload, download e delete de arquivos.

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

Para produção, implemente integrações reais (ex: OCI Object Storage, Azure Blob, S3) e registre na DI.
