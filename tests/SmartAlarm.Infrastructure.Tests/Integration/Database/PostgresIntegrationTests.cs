using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Npgsql;
using Xunit;

namespace SmartAlarm.Infrastructure.Tests.Integration.Database
{
    public class PostgresIntegrationTests : IDisposable
    {
        private readonly string _connectionString;
        private readonly NpgsqlConnection _connection;

        public PostgresIntegrationTests()
        {
            // Usar DockerHelper para resolver as configurações do PostgreSQL
            var host = DockerHelper.ResolveServiceHostname("postgres");
            var port = DockerHelper.ResolveServicePort("postgres", 5432);
            var user = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "smartalarm";
            var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "smartalarm123";
            var database = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "smartalarm";
            
            _connectionString = $"Host={host};Port={port};Username={user};Password={password};Database={database}";
            _connection = new NpgsqlConnection(_connectionString);
        }
        
        public void Dispose()
        {
            _connection?.Dispose();
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "PostgreSQL")]
        public async Task PostgresConnection_ShouldConnect()
        {
            // Arrange & Act
            await _connection.OpenAsync();
            
            // Assert
            Assert.Equal(System.Data.ConnectionState.Open, _connection.State);
        }
        
        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "Integration")]
        [Trait("Category", "PostgreSQL")]
        public async Task PostgresDatabase_ShouldExecuteQueries()
        {
            // Arrange
            await _connection.OpenAsync();
            
            // Criar tabela de teste temporária
            using var createTableCmd = new NpgsqlCommand(
                @"CREATE TABLE IF NOT EXISTS test_integration (
                    id SERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )", _connection);
            
            await createTableCmd.ExecuteNonQueryAsync();
            
            // Act - Inserir dados
            var testName = $"Test_{Guid.NewGuid().ToString("N")[..8]}";
            using var insertCmd = new NpgsqlCommand(
                "INSERT INTO test_integration (name) VALUES (@name) RETURNING id", _connection);
            insertCmd.Parameters.AddWithValue("name", testName);
            var newIdResult = await insertCmd.ExecuteScalarAsync();
            
            Assert.NotNull(newIdResult);
            var newId = Convert.ToInt32(newIdResult);
            
            // Recuperar dados
            using var selectCmd = new NpgsqlCommand(
                "SELECT name FROM test_integration WHERE id = @id", _connection);
            selectCmd.Parameters.AddWithValue("id", newId);
            var result = await selectCmd.ExecuteScalarAsync();
            
            // Limpar tabela
            using var deleteCmd = new NpgsqlCommand(
                "DELETE FROM test_integration WHERE id = @id", _connection);
            deleteCmd.Parameters.AddWithValue("id", newId);
            await deleteCmd.ExecuteNonQueryAsync();
            
            // Assert
            Assert.Equal(testName, result);
        }
        
        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "PostgreSQL")]
        public async Task PostgresVersion_ShouldBeCorrect()
        {
            // Arrange
            await _connection.OpenAsync();
            using var versionCmd = new NpgsqlCommand("SELECT version()", _connection);
            
            // Act
            var versionString = await versionCmd.ExecuteScalarAsync() as string;
            
            // Assert
            Assert.NotNull(versionString);
            Assert.Contains("PostgreSQL", versionString);
            // Verificar se é PostgreSQL 16 (conforme definido no docker-compose)
            Assert.Contains("16.", versionString);
        }
    }
}
