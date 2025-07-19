using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SmartAlarm.KeyVault.Abstractions
{
    /// <summary>
    /// Service that manages multiple secret providers and provides unified access to secrets.
    /// </summary>
    public interface IKeyVaultService
    {
        /// <summary>
        /// Gets a secret by trying providers in priority order.
        /// </summary>
        /// <param name="secretKey">The key/name of the secret to retrieve.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The secret value if found, null otherwise.</returns>
        Task<string?> GetSecretAsync(string secretKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets multiple secrets by trying providers in priority order.
        /// </summary>
        /// <param name="secretKeys">The keys/names of the secrets to retrieve.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A dictionary with the secret keys and their values.</returns>
        Task<Dictionary<string, string?>> GetSecretsAsync(IEnumerable<string> secretKeys, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets a secret using the first available provider.
        /// </summary>
        /// <param name="secretKey">The key/name of the secret to set.</param>
        /// <param name="secretValue">The value to set.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the secret was set successfully.</returns>
        Task<bool> SetSecretAsync(string secretKey, string secretValue, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a list of available providers in priority order.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of available provider names.</returns>
        Task<IEnumerable<string>> GetAvailableProvidersAsync(CancellationToken cancellationToken = default);
    }
}