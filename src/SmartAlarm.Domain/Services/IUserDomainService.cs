using System;
using System.Threading.Tasks;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Domain.Services
{
    /// <summary>
    /// Serviço de domínio para operações de negócio relacionadas a usuários.
    /// </summary>
    public interface IUserDomainService
    {
        /// <summary>
        /// Verifica se um e-mail já está em uso por outro usuário.
        /// </summary>
        Task<bool> IsEmailAlreadyInUseAsync(string email, Guid? excludeUserId = null);

        /// <summary>
        /// Valida se um usuário pode ser ativado.
        /// </summary>
        Task<bool> CanActivateUserAsync(Guid userId);

        /// <summary>
        /// Valida se um usuário pode ser desativado.
        /// </summary>
        Task<bool> CanDeactivateUserAsync(Guid userId);

        /// <summary>
        /// Verifica se um usuário possui alarmes ativos.
        /// </summary>
        Task<bool> HasActiveAlarmsAsync(Guid userId);
    }
}