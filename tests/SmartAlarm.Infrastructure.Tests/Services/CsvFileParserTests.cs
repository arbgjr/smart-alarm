using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Infrastructure.Services;
using Xunit;

namespace SmartAlarm.Infrastructure.Tests.Services;

/// <summary>
/// Testes unitários para CsvFileParser
/// </summary>
public class CsvFileParserTests
{
    private readonly Mock<ILogger<CsvFileParser>> _loggerMock;
    private readonly CsvFileParser _parser;

    public CsvFileParserTests()
    {
        _loggerMock = new Mock<ILogger<CsvFileParser>>();
        _parser = new CsvFileParser(_loggerMock.Object);
    }

    [Theory]
    [InlineData("alarmes.csv", true)]
    [InlineData("ALARMES.CSV", true)]
    [InlineData("teste.csv", true)]
    [InlineData("arquivo.txt", false)]
    [InlineData("arquivo.xlsx", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsFormatSupported_DeveValidarFormatosCorretamente(string fileName, bool expectedResult)
    {
        // Act
        var result = _parser.IsFormatSupported(fileName);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void GetSupportedFormats_DeveRetornarFormatosCorretos()
    {
        // Act
        var formats = _parser.GetSupportedFormats();

        // Assert
        Assert.Single(formats);
        Assert.Contains(".csv", formats);
    }

    [Fact]
    public async Task ParseAsync_ComArquivoCSVValido_DeveRetornarAlarmes()
    {
        // Arrange
        var csvContent = @"Name,Time,DaysOfWeek,Description,IsActive
Acordar,07:00,""segunda,sexta"",Alarme para acordar,true
Reunião,14:30,terça,Reunião importante,false
Exercício,18:00,""segunda,terça,quarta,quinta,sexta"",Hora do exercício,true";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await _parser.ParseAsync(stream, "test.csv");

        // Assert
        var alarms = result.ToList();
        Assert.Equal(3, alarms.Count);

        // Verificar primeiro alarme
        var firstAlarm = alarms[0];
        Assert.Equal("Acordar", firstAlarm.Name.Value);
        Assert.True(firstAlarm.Enabled);
        Assert.Single(firstAlarm.Schedules);
        Assert.Equal(new TimeOnly(7, 0), firstAlarm.Schedules.First().Time);
        Assert.Equal(DaysOfWeek.Monday | DaysOfWeek.Friday, firstAlarm.Schedules.First().DaysOfWeek);

        // Verificar segundo alarme
        var secondAlarm = alarms[1];
        Assert.Equal("Reunião", secondAlarm.Name.Value);
        Assert.False(secondAlarm.Enabled);
        Assert.Equal(new TimeOnly(14, 30), secondAlarm.Schedules.First().Time);
        Assert.Equal(DaysOfWeek.Tuesday, secondAlarm.Schedules.First().DaysOfWeek);

        // Verificar terceiro alarme
        var thirdAlarm = alarms[2];
        Assert.Equal("Exercício", thirdAlarm.Name.Value);
        Assert.True(thirdAlarm.Enabled);
        Assert.Equal(new TimeOnly(18, 0), thirdAlarm.Schedules.First().Time);
        Assert.Equal(DaysOfWeek.Monday | DaysOfWeek.Tuesday | DaysOfWeek.Wednesday | DaysOfWeek.Thursday | DaysOfWeek.Friday, 
                    thirdAlarm.Schedules.First().DaysOfWeek);
    }

    [Fact]
    public async Task ParseAsync_ComStreamNulo_DeveLancarArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _parser.ParseAsync(null!, "test.csv"));
    }

    [Fact]
    public async Task ParseAsync_ComNomeArquivoVazio_DeveLancarArgumentException()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _parser.ParseAsync(stream, ""));
    }

    [Fact]
    public async Task ParseAsync_ComFormatoNaoSuportado_DeveLancarArgumentException()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _parser.ParseAsync(stream, "test.txt"));
    }

    [Fact]
    public async Task ParseAsync_ComNomeVazio_DeveLancarInvalidOperationException()
    {
        // Arrange
        var csvContent = @"Name,Time,DaysOfWeek,Description,IsActive
,07:00,segunda,Alarme inválido,true";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _parser.ParseAsync(stream, "test.csv"));
        
        Assert.Contains("Nome é obrigatório", exception.Message);
    }

    [Fact]
    public async Task ParseAsync_ComHorarioInvalido_DeveLancarInvalidOperationException()
    {
        // Arrange
        var csvContent = @"Name,Time,DaysOfWeek,Description,IsActive
Alarme Teste,25:70,segunda,Horário inválido,true";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _parser.ParseAsync(stream, "test.csv"));
        
        Assert.Contains("Horário inválido", exception.Message);
    }

    [Fact]
    public async Task ParseAsync_ComDiasDaSemanaInvalidos_DeveLancarInvalidOperationException()
    {
        // Arrange
        var csvContent = @"Name,Time,DaysOfWeek,Description,IsActive
Alarme Teste,07:00,diaInválido,Dias inválidos,true";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _parser.ParseAsync(stream, "test.csv"));
        
        Assert.Contains("Dia da semana inválido", exception.Message);
    }

    [Fact]
    public async Task ParseAsync_ComStatusInvalido_DeveLancarInvalidOperationException()
    {
        // Arrange
        var csvContent = @"Name,Time,DaysOfWeek,Description,IsActive
Alarme Teste,07:00,segunda,Teste,maybe";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _parser.ParseAsync(stream, "test.csv"));
        
        Assert.Contains("Status inválido", exception.Message);
    }

    [Fact]
    public async Task ParseAsync_ComDiasDaSemanaSemValor_DeveLancarInvalidOperationException()
    {
        // Arrange
        var csvContent = @"Name,Time,DaysOfWeek,Description,IsActive
Alarme Teste,07:00,,Sem dias,true";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _parser.ParseAsync(stream, "test.csv"));
        
        Assert.Contains("Dias da semana são obrigatórios", exception.Message);
    }

    [Theory]
    [InlineData("domingo", DaysOfWeek.Sunday)]
    [InlineData("dom", DaysOfWeek.Sunday)]
    [InlineData("sunday", DaysOfWeek.Sunday)]
    [InlineData("sun", DaysOfWeek.Sunday)]
    [InlineData("segunda", DaysOfWeek.Monday)]
    [InlineData("seg", DaysOfWeek.Monday)]
    [InlineData("monday", DaysOfWeek.Monday)]
    [InlineData("mon", DaysOfWeek.Monday)]
    [InlineData("terça", DaysOfWeek.Tuesday)]
    [InlineData("ter", DaysOfWeek.Tuesday)]
    [InlineData("tuesday", DaysOfWeek.Tuesday)]
    [InlineData("tue", DaysOfWeek.Tuesday)]
    [InlineData("quarta", DaysOfWeek.Wednesday)]
    [InlineData("qua", DaysOfWeek.Wednesday)]
    [InlineData("wednesday", DaysOfWeek.Wednesday)]
    [InlineData("wed", DaysOfWeek.Wednesday)]
    [InlineData("quinta", DaysOfWeek.Thursday)]
    [InlineData("qui", DaysOfWeek.Thursday)]
    [InlineData("thursday", DaysOfWeek.Thursday)]
    [InlineData("thu", DaysOfWeek.Thursday)]
    [InlineData("sexta", DaysOfWeek.Friday)]
    [InlineData("sex", DaysOfWeek.Friday)]
    [InlineData("friday", DaysOfWeek.Friday)]
    [InlineData("fri", DaysOfWeek.Friday)]
    [InlineData("sábado", DaysOfWeek.Saturday)]
    [InlineData("sab", DaysOfWeek.Saturday)]
    [InlineData("saturday", DaysOfWeek.Saturday)]
    [InlineData("sat", DaysOfWeek.Saturday)]
    public async Task ParseAsync_ComDiferente0sFormatosDiasDaSemana_DeveParseCorreto(string diaInput, DaysOfWeek expectedDay)
    {
        // Arrange
        var csvContent = $@"Name,Time,DaysOfWeek,Description,IsActive
Teste,07:00,{diaInput},Teste de dia,true";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await _parser.ParseAsync(stream, "test.csv");

        // Assert
        var alarm = result.First();
        Assert.Equal(expectedDay, alarm.Schedules.First().DaysOfWeek);
    }

    [Fact]
    public async Task ParseAsync_ComMultiplosDiasDaSemana_DeveCombimarCorretatamente()
    {
        // Arrange
        var csvContent = @"Name,Time,DaysOfWeek,Description,IsActive
Teste,07:00,""segunda, terça, sexta"",Múltiplos dias,true";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await _parser.ParseAsync(stream, "test.csv");

        // Assert
        var alarm = result.First();
        var expectedDays = DaysOfWeek.Monday | DaysOfWeek.Tuesday | DaysOfWeek.Friday;
        Assert.Equal(expectedDays, alarm.Schedules.First().DaysOfWeek);
    }

    [Fact]
    public async Task ParseAsync_ComStatusPadrao_DeveDefinirComoAtivo()
    {
        // Arrange
        var csvContent = @"Name,Time,DaysOfWeek,Description,IsActive
Teste,07:00,segunda,Sem status definido,";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await _parser.ParseAsync(stream, "test.csv");

        // Assert
        var alarm = result.First();
        Assert.True(alarm.Enabled);
    }

    [Fact]
    public async Task ParseAsync_ComDescricaoVazia_DeveIgnorarDescricao()
    {
        // Arrange
        var csvContent = @"Name,Time,DaysOfWeek,Description,IsActive
Teste,07:00,segunda,,true";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await _parser.ParseAsync(stream, "test.csv");

        // Assert
        var alarm = result.First();
        Assert.Equal("Teste", alarm.Name.Value);
        // Nota: Como não temos campo Description na entidade, apenas verificamos se não deu erro
    }

    [Fact]
    public async Task ParseAsync_ComArquivoVazio_DeveRetornarListaVazia()
    {
        // Arrange
        var csvContent = @"Name,Time,DaysOfWeek,Description,IsActive";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await _parser.ParseAsync(stream, "test.csv");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ParseAsync_ComErroDeFormatacao_DeveLogarErro()
    {
        // Arrange
        var csvContent = @"Name,Time,DaysOfWeek,Description,IsActive
Válido,07:00,segunda,Teste,true
Inválido,99:99,terça,Erro,true";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _parser.ParseAsync(stream, "test.csv"));

        // Verificar se o log foi chamado
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Erro ao processar linha")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}
