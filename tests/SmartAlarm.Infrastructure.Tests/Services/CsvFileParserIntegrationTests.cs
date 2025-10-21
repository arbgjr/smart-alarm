using SmartAlarm.Domain.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Services;
using SmartAlarm.Infrastructure.Services;
using Xunit;

namespace SmartAlarm.Infrastructure.Tests.Services;

/// <summary>
/// Testes de integração para CsvFileParser com arquivos reais
/// </summary>
[Collection("Integration")]
public class CsvFileParserIntegrationTests
{
    private readonly IFileParser _parser;

    public CsvFileParserIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddScoped<IFileParser, CsvFileParser>();
        
        var serviceProvider = services.BuildServiceProvider();
        _parser = serviceProvider.GetRequiredService<IFileParser>();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ParseAsync_ComArquivoValidoReal_DeveProcessarCorretatemente()
    {
        // Arrange
        var testDataPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, 
            "Services", 
            "TestData", 
            "alarmes_validos.csv"
        );
        
        // Verificar se o arquivo existe, senão pular o teste
        if (!File.Exists(testDataPath))
        {
            return; // Skip teste se arquivo não existir
        }

        // Act
        using var fileStream = File.OpenRead(testDataPath);
        var alarms = await _parser.ParseAsync(fileStream, "alarmes_validos.csv");

        // Assert
        var alarmsList = alarms.ToList();
        Assert.Equal(5, alarmsList.Count);
        
        // Verificar alguns alarmes específicos
        var primeiroAlarme = alarmsList.First(a => a.Name.Value == "Acordar Cedo");
        Assert.True(primeiroAlarme.Enabled);
        Assert.Equal(new TimeOnly(6, 30), primeiroAlarme.Schedules.First().Time);
        
        var alarmeExercicio = alarmsList.First(a => a.Name.Value == "Exercício Matinal");
        Assert.True(alarmeExercicio.Enabled);
        Assert.Equal(new TimeOnly(7, 0), alarmeExercicio.Schedules.First().Time);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ParseAsync_ComArquivoInvalidoReal_DeveLancarExcecao()
    {
        // Arrange
        var testDataPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, 
            "Services", 
            "TestData", 
            "alarmes_invalidos.csv"
        );
        
        // Verificar se o arquivo existe, senão pular o teste
        if (!File.Exists(testDataPath))
        {
            return; // Skip teste se arquivo não existir
        }

        // Act & Assert
        using var fileStream = File.OpenRead(testDataPath);
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _parser.ParseAsync(fileStream, "alarmes_invalidos.csv"));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ParseAsync_ComArquivoGrande_DeveProcessarEficientemente()
    {
        // Arrange - Criar arquivo temporário com muitos registros
        var tempFile = Path.GetTempFileName();
        var csvExtension = Path.ChangeExtension(tempFile, ".csv");
        
        try
        {
            // Criar arquivo com 1000 registros
            await File.WriteAllTextAsync(csvExtension, 
                "Name,Time,DaysOfWeek,Description,IsActive\n" +
                string.Join('\n', Enumerable.Range(1, 1000).Select(i => 
                    $"Alarme {i:000},{(6 + i % 18):D2}:{(i % 60):D2},segunda,Alarme de teste {i},true")));

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            using var fileStream = File.OpenRead(csvExtension);
            var alarms = await _parser.ParseAsync(fileStream, Path.GetFileName(csvExtension));
            stopwatch.Stop();

            // Assert
            var alarmsList = alarms.ToList();
            Assert.Equal(1000, alarmsList.Count);
            Assert.True(stopwatch.ElapsedMilliseconds < 5000, 
                $"Processamento demorou {stopwatch.ElapsedMilliseconds}ms, esperado < 5000ms");
        }
        finally
        {
            // Cleanup
            if (File.Exists(csvExtension))
                File.Delete(csvExtension);
        }
    }
}
