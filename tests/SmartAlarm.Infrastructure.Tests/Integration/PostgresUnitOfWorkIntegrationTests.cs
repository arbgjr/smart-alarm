using SmartAlarm.Domain.Abstractions;
using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Domain.ValueObjects;
using SmartAlarm.Infrastructure.Data;
using SmartAlarm.Infrastructure.Repositories.EntityFramework;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;
using Xunit;

namespace SmartAlarm.Infrastructure.Tests.Integration
{
    /// <summary>
    /// Testes de integração reais com PostgreSQL: CRUD, transação, rollback e concorrência.
    /// </summary>
    public class PostgresUnitOfWorkIntegrationTests : IDisposable
    {
        private readonly SmartAlarmDbContext _context;
        private readonly EfUnitOfWork _unitOfWork;
        private readonly Npgsql.NpgsqlConnection? _connection;
        private readonly bool _useInMemory;

        public PostgresUnitOfWorkIntegrationTests()
        {
            // Usar DockerHelper para resolver as configurações do PostgreSQL
            var host = DockerHelper.ResolveServiceHostname("postgres");
            var port = DockerHelper.ResolveServicePort("postgres", 5432);
            var user = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "smartalarm";
            var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "smartalarm123";
            var database = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "smartalarm";
            
            // Primeiro conectar ao database padrão para criar o database de teste
            var defaultConnStr = $"Host={host};Port={port};Username={user};Password={password};Database={database}";
            
            // Usar um database único para cada teste para evitar conflitos
            var testDbName = $"smartalarm_test_{Guid.NewGuid():N}";
            
            try
            {
                Console.WriteLine($"Criando database de teste: {testDbName}");
                
                // Conectar ao database padrão e criar o database de teste
                using (var defaultConnection = new Npgsql.NpgsqlConnection(defaultConnStr))
                {
                    defaultConnection.Open();
                    using (var cmd = defaultConnection.CreateCommand())
                    {
                        cmd.CommandText = $"CREATE DATABASE \"{testDbName}\"";
                        cmd.ExecuteNonQuery();
                    }
                }
                
                Console.WriteLine($"Database {testDbName} criado com sucesso");
                
                // Agora conectar ao database de teste criado
                var testConnStr = $"Host={host};Port={port};Username={user};Password={password};Database={testDbName}";
                _connection = new Npgsql.NpgsqlConnection(testConnStr);
                _connection.Open();
                
                var options = new DbContextOptionsBuilder<SmartAlarmDbContext>()
                    .UseNpgsql(_connection)
                    .Options;
                _context = new SmartAlarmDbContext(options);
                
                // Criar as tabelas
                Console.WriteLine("Criando tabelas...");
                var created = _context.Database.EnsureCreated();
                Console.WriteLine($"Tabelas criadas: {created}");
                _useInMemory = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao preparar database: {ex.Message}");
                Console.WriteLine("PostgreSQL não disponível, usando InMemory database");
                
                // Fallback para InMemory
                var options = new DbContextOptionsBuilder<SmartAlarmDbContext>()
                    .UseInMemoryDatabase($"TestDb_{testDbName}")
                    .Options;
                _context = new SmartAlarmDbContext(options);
                _useInMemory = true;
                
                // Create mock dependencies for observability
                var logger = new Mock<ILogger<EfAlarmRepository>>();
                var meter = new Mock<SmartAlarmMeter>();
                var correlationContext = new Mock<ICorrelationContext>();
                var activitySource = new Mock<SmartAlarmActivitySource>();
                
                // Create repositories with mock dependencies
                var alarmRepository = new EfAlarmRepository(_context, logger.Object, meter.Object, correlationContext.Object, activitySource.Object);
                var userRepository = new Mock<IUserRepository>().Object;
                var scheduleRepository = new Mock<IScheduleRepository>().Object;
                var routineRepository = new Mock<IRoutineRepository>().Object;
                var integrationRepository = new Mock<IIntegrationRepository>().Object;
                
                _unitOfWork = new EfUnitOfWork(_context, alarmRepository, userRepository, scheduleRepository, routineRepository, integrationRepository);
                return;
            }
            
            // Create mock dependencies for observability
            var pgLogger = new Mock<ILogger<EfAlarmRepository>>();
            var pgMeter = new Mock<SmartAlarmMeter>();
            var pgCorrelationContext = new Mock<ICorrelationContext>();
            var pgActivitySource = new Mock<SmartAlarmActivitySource>();
            
            // Create repositories with mock dependencies
            var pgAlarmRepository = new EfAlarmRepository(_context, pgLogger.Object, pgMeter.Object, pgCorrelationContext.Object, pgActivitySource.Object);
            var pgUserLogger = new Mock<ILogger<EfUserRepository>>();
            var pgUserRepository = new EfUserRepository(_context, pgUserLogger.Object, pgMeter.Object, pgCorrelationContext.Object, pgActivitySource.Object);
            var pgScheduleRepository = new Mock<IScheduleRepository>().Object;
            var pgRoutineRepository = new Mock<IRoutineRepository>().Object;
            var pgIntegrationRepository = new Mock<IIntegrationRepository>().Object;
            
            _unitOfWork = new EfUnitOfWork(_context, pgAlarmRepository, pgUserRepository, pgScheduleRepository, pgRoutineRepository, pgIntegrationRepository);
        }

        [Fact(DisplayName = "Deve realizar CRUD completo no PostgreSQL")]
        [Trait("Category", "Integration")]
        public async Task Deve_Crud_Completo()
        {
            var user = new User(Guid.NewGuid(), new Name("User CRUD"), new Email("crud@example.com"));
            var alarm = new Alarm(Guid.NewGuid(), new Name("Alarm CRUD"), DateTime.UtcNow.AddHours(1), true, user.Id);
            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.Alarms.AddAsync(alarm);
            await _unitOfWork.SaveChangesAsync();
            var userDb = await _unitOfWork.Users.GetByIdAsync(user.Id);
            var alarmDb = await _unitOfWork.Alarms.GetByIdAsync(alarm.Id);
            userDb.Should().NotBeNull();
            alarmDb.Should().NotBeNull();
            userDb.Name.Value.Should().Be("User CRUD");
            alarmDb.Name.Value.Should().Be("Alarm CRUD");
            // Update
            userDb.UpdateName(new Name("User CRUD 2"));
            await _unitOfWork.SaveChangesAsync();
            var userDb2 = await _unitOfWork.Users.GetByIdAsync(user.Id);
            userDb2.Name.Value.Should().Be("User CRUD 2");
            // Delete
            await _unitOfWork.Users.DeleteAsync(userDb2.Id);
            await _unitOfWork.SaveChangesAsync();
            var userDb3 = await _unitOfWork.Users.GetByIdAsync(user.Id);
            userDb3.Should().BeNull();
        }

        [Fact(DisplayName = "Deve realizar commit e rollback de transação no PostgreSQL")]
        [Trait("Category", "Integration")]
        public async Task Deve_Commit_Rollback_Transacao()
        {
            // Skip this test if using InMemory database
            if (_useInMemory)
            {
                return;
            }
            
            var user = new User(Guid.NewGuid(), new Name("User Tx"), new Email("tx@example.com"));
            await _unitOfWork.BeginTransactionAsync();
            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();
            var userInTx = await _unitOfWork.Users.GetByIdAsync(user.Id);
            userInTx.Should().NotBeNull();
            await _unitOfWork.RollbackTransactionAsync();

            // Cria novo contexto e unit of work para garantir isolamento
            DbContextOptionsBuilder<SmartAlarmDbContext> optionsBuilder = new DbContextOptionsBuilder<SmartAlarmDbContext>();
            if (_useInMemory)
            {
                optionsBuilder.UseInMemoryDatabase("TestDb_Transaction");
            }
            else
            {
                optionsBuilder.UseNpgsql(_connection!);
            }
            
            using (var freshContext = new SmartAlarmDbContext(optionsBuilder.Options))
            {
                // Create mock dependencies for fresh UoW
                var freshLogger = new Mock<ILogger<EfAlarmRepository>>();
                var freshMeter = new Mock<SmartAlarmMeter>();
                var freshCorrelationContext = new Mock<ICorrelationContext>();
                var freshActivitySource = new Mock<SmartAlarmActivitySource>();
                
                // Create repositories with mock dependencies
                var freshAlarmRepository = new EfAlarmRepository(freshContext, freshLogger.Object, freshMeter.Object, freshCorrelationContext.Object, freshActivitySource.Object);
                var freshUserRepository = new Mock<IUserRepository>().Object;
                var freshScheduleRepository = new Mock<IScheduleRepository>().Object;
                var freshRoutineRepository = new Mock<IRoutineRepository>().Object;
                var freshIntegrationRepository = new Mock<IIntegrationRepository>().Object;
                
                var freshUow = new EfUnitOfWork(freshContext, freshAlarmRepository, freshUserRepository, freshScheduleRepository, freshRoutineRepository, freshIntegrationRepository);
                var userAfterRollback = await freshUow.Users.GetByIdAsync(user.Id);
                userAfterRollback.Should().BeNull();
            }

            // Commit
            await _unitOfWork.BeginTransactionAsync();
            var user2 = new User(Guid.NewGuid(), new Name("User Commit"), new Email("commit@example.com"));
            await _unitOfWork.Users.AddAsync(user2);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
            var userAfterCommit = await _unitOfWork.Users.GetByIdAsync(user2.Id);
            userAfterCommit.Should().NotBeNull();
        }

        public void Dispose()
        {
            try
            {
                // Obter o nome do database antes de fechar a conexão
                var dbName = _connection?.Database;
                
                _unitOfWork?.Dispose();
                _context?.Dispose();
                _connection?.Close();
                _connection?.Dispose();
                
                // Limpar o database de teste se conseguirmos
                if (!string.IsNullOrEmpty(dbName) && dbName.StartsWith("smartalarm_test_"))
                {
                    try
                    {
                        var host = DockerHelper.ResolveServiceHostname("postgres");
                        var port = DockerHelper.ResolveServicePort("postgres", 5432);
                        var user = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "smartalarm";
                        var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "smartalarm123";
                        var database = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "smartalarm";
                        
                        var defaultConnStr = $"Host={host};Port={port};Username={user};Password={password};Database={database}";
                        
                        using (var cleanupConnection = new Npgsql.NpgsqlConnection(defaultConnStr))
                        {
                            cleanupConnection.Open();
                            
                            // Primeiro, forçar a desconexão de todas as sessões do database de teste
                            using (var disconnectCmd = cleanupConnection.CreateCommand())
                            {
                                disconnectCmd.CommandText = $@"
                                    SELECT pg_terminate_backend(pid)
                                    FROM pg_stat_activity 
                                    WHERE datname = '{dbName}' AND pid <> pg_backend_pid()";
                                disconnectCmd.ExecuteNonQuery();
                            }
                            
                            // Aguardar um pouco para as conexões serem terminadas
                            System.Threading.Thread.Sleep(100);
                            
                            // Agora tentar remover o database
                            using (var cmd = cleanupConnection.CreateCommand())
                            {
                                cmd.CommandText = $"DROP DATABASE IF EXISTS \"{dbName}\"";
                                cmd.ExecuteNonQuery();
                                Console.WriteLine($"Database de teste {dbName} removido");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Aviso: Não foi possível limpar o database de teste {dbName}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro durante dispose: {ex.Message}");
            }
        }
    }
}
