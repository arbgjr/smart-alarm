using SmartAlarm.Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Infrastructure.Repositories;
using Xunit;

namespace SmartAlarm.Infrastructure.Tests.Repositories
{
    /// <summary>
    /// Testes unitários para AlarmRepository
    /// Nota: Estes são testes que validam a lógica dos métodos, mas para testes de integração 
    /// com Oracle DB real, usar os testes de integração separados.
    /// </summary>
    public class AlarmRepositoryTests
    {
        private readonly Mock<ILogger<AlarmRepository>> _loggerMock;
        private readonly string _connectionString;

        public AlarmRepositoryTests()
        {
            _loggerMock = new Mock<ILogger<AlarmRepository>>();
            _connectionString = "Data Source=localhost:1521/XEPDB1;User Id=smart_alarm;Password=test;";
        }

        [Fact]
        public void Constructor_Should_Throw_When_ConnectionString_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new AlarmRepository(null, _loggerMock.Object));
        }

        [Fact]
        public void Constructor_Should_Throw_When_Logger_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new AlarmRepository(_connectionString, null));
        }

        [Fact]
        public void Constructor_Should_Create_Instance_With_Valid_Parameters()
        {
            // Act
            var repository = new AlarmRepository(_connectionString, _loggerMock.Object);

            // Assert
            Assert.NotNull(repository);
        }

        // Nota: Os testes abaixo são para validar a estrutura e comportamento dos métodos.
        // Para testes com banco de dados real, usar testes de integração com TestContainers
        // ou ambiente de teste dedicado.

        [Fact]
        public async Task GetAllEnabledAsync_Should_Handle_Database_Errors_Gracefully()
        {
            // Este teste validaria que exceções de banco são tratadas adequadamente
            // Em um cenário real, usaríamos TestContainers ou mock do Dapper
            
            // Arrange
            var repository = new AlarmRepository("invalid_connection", _loggerMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => repository.GetAllEnabledAsync());
        }

        [Fact]
        public async Task GetDueForTriggeringAsync_Should_Handle_Database_Errors_Gracefully()
        {
            // Este teste validaria que exceções de banco são tratadas adequadamente
            
            // Arrange
            var repository = new AlarmRepository("invalid_connection", _loggerMock.Object);
            var now = DateTime.Now;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => repository.GetDueForTriggeringAsync(now));
        }
    }
}
