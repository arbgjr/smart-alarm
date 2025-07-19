namespace SmartAlarm.KeyVault.Configuration
{
    /// <summary>
    /// General configuration options for the KeyVault service.
    /// </summary>
    public class KeyVaultOptions
    {
        public const string ConfigSection = "KeyVault";

        /// <summary>
        /// Whether the KeyVault service is enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Global timeout in seconds for all vault operations.
        /// </summary>
        public int GlobalTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Number of retry attempts for failed operations.
        /// </summary>
        public int RetryAttempts { get; set; } = 3;

        /// <summary>
        /// Base delay in milliseconds between retry attempts.
        /// </summary>
        public int RetryDelayMs { get; set; } = 1000;

        /// <summary>
        /// Whether to use exponential backoff for retries.
        /// </summary>
        public bool UseExponentialBackoff { get; set; } = true;

        /// <summary>
        /// Whether to cache secrets in memory after retrieval.
        /// </summary>
        public bool EnableCaching { get; set; } = true;

        /// <summary>
        /// Cache expiration time in minutes.
        /// </summary>
        public int CacheExpirationMinutes { get; set; } = 15;

        /// <summary>
        /// Whether to log detailed information about provider attempts.
        /// </summary>
        public bool EnableDetailedLogging { get; set; } = false;

        /// <summary>
        /// List of provider names to disable (case-insensitive).
        /// </summary>
        public List<string> DisabledProviders { get; set; } = new();

        /// <summary>
        /// Custom provider priority overrides (provider name -> priority).
        /// </summary>
        public Dictionary<string, int> ProviderPriorities { get; set; } = new();
    }
}