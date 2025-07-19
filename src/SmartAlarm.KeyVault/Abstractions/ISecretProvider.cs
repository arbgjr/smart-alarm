using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SmartAlarm.KeyVault.Abstractions
{
    /// <summary>
    /// Interface for secret providers that can retrieve secrets from various vault systems.
    /// </summary>
    public interface ISecretProvider
    {
        /// <summary>
        /// The name of the provider (e.g., "HashiCorp", "Azure", "AWS", "GCP", "OCI").
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// The priority order for this provider (lower numbers = higher priority).
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Checks if the provider is available and properly configured.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the provider is available and ready to use.</returns>
        Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a secret value by its key.
        /// </summary>
        /// <param name="secretKey">The key/name of the secret to retrieve.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The secret value if found, null otherwise.</returns>
        Task<string?> GetSecretAsync(string secretKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves multiple secrets by their keys.
        /// </summary>
        /// <param name="secretKeys">The keys/names of the secrets to retrieve.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A dictionary with the secret keys and their values.</returns>
        Task<Dictionary<string, string?>> GetSecretsAsync(IEnumerable<string> secretKeys, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets a secret value (if supported by the provider).
        /// </summary>
        /// <param name="secretKey">The key/name of the secret to set.</param>
        /// <param name="secretValue">The value to set.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the secret was set successfully.</returns>
        Task<bool> SetSecretAsync(string secretKey, string secretValue, CancellationToken cancellationToken = default);
    }
}