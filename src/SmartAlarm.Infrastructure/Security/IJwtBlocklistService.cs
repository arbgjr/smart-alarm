using System;
using System.Threading.Tasks;

namespace SmartAlarm.Infrastructure.Security
{
    /// <summary>
    /// Interface para gerenciamento de JWT blocklist (tokens bloqueados/revogados)
    /// </summary>
    public interface IJwtBlocklistService
    {
        /// <summary>
        /// Verifica se um token está na blocklist (revogado)
        /// </summary>
        /// <param name="tokenId">ID único do token (geralmente jti claim)</param>
        /// <returns>True se o token estiver bloqueado</returns>
        Task<bool> IsTokenBlockedAsync(string tokenId);

        /// <summary>
        /// Adiciona um token à blocklist
        /// </summary>
        /// <param name="tokenId">ID único do token</param>
        /// <param name="expiration">Tempo de expiração do token para otimizar limpeza</param>
        /// <param name="reason">Motivo do bloqueio (opcional para auditoria)</param>
        /// <returns>True se adicionado com sucesso</returns>
        Task<bool> BlockTokenAsync(string tokenId, TimeSpan expiration, string? reason = null);

        /// <summary>
        /// Adiciona um token à blocklist com data de expiração
        /// </summary>
        /// <param name="tokenId">ID único do token</param>
        /// <param name="expiry">Data de expiração do token</param>
        /// <param name="reason">Motivo do bloqueio (opcional para auditoria)</param>
        /// <returns>True se adicionado com sucesso</returns>
        Task<bool> BlockTokenAsync(string tokenId, DateTime expiry, string? reason = null);

        /// <summary>
        /// Remove um token da blocklist (raramente usado)
        /// </summary>
        /// <param name="tokenId">ID único do token</param>
        /// <returns>True se removido com sucesso</returns>
        Task<bool> UnblockTokenAsync(string tokenId);

        /// <summary>
        /// Bloqueia todos os tokens de um usuário específico
        /// </summary>
        /// <param name="userId">ID do usuário</param>
        /// <param name="reason">Motivo do bloqueio</param>
        /// <returns>Número de tokens bloqueados</returns>
        Task<int> BlockAllUserTokensAsync(Guid userId, string reason);

        /// <summary>
        /// Bloqueia todos os tokens de um usuário específico com data de expiração
        /// </summary>
        /// <param name="userId">ID do usuário como string</param>
        /// <param name="expiry">Data de expiração do bloqueio</param>
        /// <param name="reason">Motivo do bloqueio</param>
        /// <returns>Número de tokens bloqueados</returns>
        Task<int> BlockAllUserTokensAsync(string userId, DateTime expiry, string reason);

        /// <summary>
        /// Bloqueia todos os tokens de um usuário específico com data de expiração
        /// </summary>
        /// <param name="userId">ID do usuário como Guid</param>
        /// <param name="expiry">Data de expiração do bloqueio</param>
        /// <param name="reason">Motivo do bloqueio</param>
        /// <returns>Número de tokens bloqueados</returns>
        Task<int> BlockAllUserTokensAsync(Guid userId, DateTime expiry, string reason);

        /// <summary>
        /// Limpa tokens expirados da blocklist para otimizar storage
        /// </summary>
        /// <returns>Número de tokens removidos</returns>
        Task<int> CleanupExpiredTokensAsync();

        /// <summary>
        /// Obtém estatísticas da blocklist para monitoramento
        /// </summary>
        /// <returns>Estatísticas da blocklist</returns>
        Task<BlocklistStatistics> GetStatisticsAsync();
    }

    /// <summary>
    /// Estatísticas da blocklist para monitoramento
    /// </summary>
    public class BlocklistStatistics
    {
        public int TotalBlockedTokens { get; set; }
        public int ExpiredTokens { get; set; }
        public int ActiveBlockedTokens { get; set; }
        public DateTime LastCleanup { get; set; }
        public long MemoryUsageBytes { get; set; }
        public Dictionary<string, int> BlockReasons { get; set; } = new();
    }
}
