using System;
using System.Threading.Tasks;

namespace SmartAlarm.Domain.Repositories
{
    /// <summary>
    /// Interface para Unit of Work pattern
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Salva todas as mudanças pendentes
        /// </summary>
        /// <returns></returns>
        Task<int> SaveChangesAsync();

        /// <summary>
        /// Inicia uma transação
        /// </summary>
        /// <returns></returns>
        Task BeginTransactionAsync();

        /// <summary>
        /// Confirma a transação
        /// </summary>
        /// <returns></returns>
        Task CommitTransactionAsync();

        /// <summary>
        /// Desfaz a transação
        /// </summary>
        /// <returns></returns>
        Task RollbackTransactionAsync();
    }
}
