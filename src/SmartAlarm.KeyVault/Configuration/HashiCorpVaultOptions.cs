namespace SmartAlarm.KeyVault.Configuration
{
    /// <summary>
    /// Configuration for HashiCorp Vault provider.
    /// </summary>
    public class HashiCorpVaultOptions
    {
        public const string ConfigSection = "HashiCorpVault";

        /// <summary>
        /// Vault server address (e.g., "http://localhost:8200").
        /// </summary>
        public string ServerAddress { get; set; } = string.Empty;

        /// <summary>
        /// Authentication token for Vault.
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Secret engine mount path (default: "secret").
        /// </summary>
        public string MountPath { get; set; } = "secret";

        /// <summary>
        /// Version of the KV engine (1 or 2, default: 2).
        /// </summary>
        public int KvVersion { get; set; } = 2;

        /// <summary>
        /// Whether to skip TLS verification (use only in development).
        /// </summary>
        public bool SkipTlsVerification { get; set; } = false;

        /// <summary>
        /// Timeout in seconds for vault operations.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Priority order for this provider (lower = higher priority).
        /// </summary>
        public int Priority { get; set; } = 1; // Default highest priority for HashiCorp Vault
    }
}