using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SmartAlarm.Api.Functions;
using SmartAlarm.Application.Commands;
using SmartAlarm.Application.DTOs;
using SmartAlarm.Infrastructure.KeyVault;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace SmartAlarm.Api.Tests.Functions
{
    public class AlarmFunctionTests
    {
        /*
        [Fact]
        public async Task Should_Create_Alarm_Successfully()
        {
            // Arrange
            var mediator = new Mock<IMediator>();
            var logger = new Mock<ILogger<AlarmFunction>>();
            var configuration = new Mock<IConfiguration>();
            var keyVault = new Mock<IKeyVaultProvider>();
            var command = new CreateAlarmCommand("Alarme Teste", null);
            var expectedResponse = new AlarmResponseDto { Id = System.Guid.NewGuid(), Name = "Alarme Teste", IsActive = true };
            mediator.Setup(m => m.Send(command, default)).ReturnsAsync(expectedResponse);
            keyVault.Setup(k => k.GetSecretAsync("DbPassword")).ReturnsAsync("fake-password");
            var function = new AlarmFunction(mediator.Object, logger.Object, configuration.Object, keyVault.Object);

            // Act
            var result = await function.HandleAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("Alarme Teste");
            result.IsActive.Should().BeTrue();
        }
        */
    }
}
