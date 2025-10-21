using System.Text;
using Microsoft.Extensions.DependencyInjection;
using SmartAlarm.Application.Services;
using SmartAlarm.Infrastructure.Configuration;
using Xunit;

namespace SmartAlarm.Infrastructure.Tests.Storage;

/// <summary>
/// Testes de integração para a interface IStorageService.
/// Estes testes rodam contra o MinIO (via Docker) para validar o contrato de armazenamento.
/// Qualquer implementação de IStorageService (MinIO, OCI) deve passar nestes testes.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "Storage")]
public class StorageServiceIntegrationTests : IClassFixture<DockerWebAppFactory>
{
    private readonly IStorageService _storageService;

    public StorageServiceIntegrationTests(DockerWebAppFactory factory)
    {
        // O DockerWebAppFactory já deve configurar o MinioStorageService via DI
        // quando o ambiente de teste está ativo.
        _storageService = factory.Services.GetRequiredService<IStorageService>();
        Assert.NotNull(_storageService);
    }

    [Fact]
    public async Task UploadAsync_ShouldUploadFile_And_ReturnTrue()
    {
        // Arrange
        var objectName = $"test-upload-{Guid.NewGuid()}.txt";
        var fileContent = "Este é um teste de upload.";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));

        // Act
        var result = await _storageService.UploadAsync(objectName, stream);

        // Assert
        Assert.True(result);

        // Cleanup
        await _storageService.DeleteAsync(objectName);
    }

    [Fact]
    public async Task DownloadAsync_ShouldRetrieveUploadedFile()
    {
        // Arrange
        var objectName = $"test-download-{Guid.NewGuid()}.txt";
        var fileContent = "Conteúdo para download.";
        var uploadStream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
        await _storageService.UploadAsync(objectName, uploadStream);

        // Act
        var downloadedStream = await _storageService.DownloadAsync(objectName);

        // Assert
        Assert.NotNull(downloadedStream);
        using var reader = new StreamReader(downloadedStream);
        var content = await reader.ReadToEndAsync();
        Assert.Equal(fileContent, content);

        // Cleanup
        await _storageService.DeleteAsync(objectName);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveFile_And_DownloadShouldFail()
    {
        // Arrange
        var objectName = $"test-delete-{Guid.NewGuid()}.txt";
        var fileContent = "Arquivo para ser deletado.";
        var uploadStream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
        await _storageService.UploadAsync(objectName, uploadStream);

        // Act
        var deleteResult = await _storageService.DeleteAsync(objectName);
        var downloadResult = await _storageService.DownloadAsync(objectName);

        // Assert
        Assert.True(deleteResult);
        Assert.Null(downloadResult);
    }
}
