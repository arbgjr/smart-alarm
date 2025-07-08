using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.ValueObjects;
using SmartAlarm.Infrastructure.Data;
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
        private readonly Npgsql.NpgsqlConnection _connection;

        public PostgresUnitOfWorkIntegrationTests()
        {
            var connStr = Environment.GetEnvironmentVariable("POSTGRES_CONN") ??
                "Host=localhost;Port=5432;Database=smartalarm;Username=smartalarm;Password=smartalarm123";
            _connection = new Npgsql.NpgsqlConnection(connStr);
            _connection.Open();
            var options = new DbContextOptionsBuilder<SmartAlarmDbContext>()
                .UseNpgsql(_connection)
                .Options;
            _context = new SmartAlarmDbContext(options);
            _context.Database.EnsureCreated();
            // Limpeza das tabelas para evitar conflitos de chave única
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM \"Alarms\"; DELETE FROM \"Users\";";
                cmd.ExecuteNonQuery();
            }
            _unitOfWork = new EfUnitOfWork(_context);
        }

        [Fact(DisplayName = "Deve realizar CRUD completo no PostgreSQL")]
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
        public async Task Deve_Commit_Rollback_Transacao()
        {
            var user = new User(Guid.NewGuid(), new Name("User Tx"), new Email("tx@example.com"));
            await _unitOfWork.BeginTransactionAsync();
            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();
            var userInTx = await _unitOfWork.Users.GetByIdAsync(user.Id);
            userInTx.Should().NotBeNull();
            await _unitOfWork.RollbackTransactionAsync();

            // Cria novo contexto e unit of work para garantir isolamento
            var options = new DbContextOptionsBuilder<SmartAlarmDbContext>()
                .UseNpgsql(_connection)
                .Options;
            using (var freshContext = new SmartAlarmDbContext(options))
            {
                var freshUow = new EfUnitOfWork(freshContext);
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
            _unitOfWork.Dispose();
            _context.Dispose();
            _connection.Dispose();
        }
    }
}
