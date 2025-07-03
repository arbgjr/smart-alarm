namespace SmartAlarm.KeyVault.Configuration
{
    /// <summary>
    /// Configuration for Google Cloud Secret Manager provider.
    /// </summary>
    public class GcpSecretManagerOptions
    {
        public const string ConfigSection = "GcpSecretManager";

        /// <summary>
        /// Google Cloud Project ID.
        /// </summary>
        public string ProjectId { get; set; } = string.Empty;

        /// <summary>
        /// Path to the service account key file (optional, will use application default credentials if not specified).
        /// </summary>
        public string? ServiceAccountKeyPath { get; set; }

        /// <summary>
        /// Service account key JSON content (optional, alternative to file path).
        /// </summary>
        public string? ServiceAccountKeyJson { get; set; }

        /// <summary>
        /// Whether to use application default credentials.
        /// </summary>
        public bool UseApplicationDefaultCredentials { get; set; } = true;

        /// <summary>
        /// Timeout in seconds for secret manager operations.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Priority order for this provider (lower = higher priority).
        /// </summary>
        public int Priority { get; set; } = 4;
    }
}