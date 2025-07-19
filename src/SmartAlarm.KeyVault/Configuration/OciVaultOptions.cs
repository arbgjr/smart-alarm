namespace SmartAlarm.KeyVault.Configuration
{
    /// <summary>
    /// Configuration for Oracle Cloud Infrastructure (OCI) Vault provider.
    /// </summary>
    public class OciVaultOptions
    {
        public const string ConfigSection = "OciVault";

        /// <summary>
        /// OCI Config file path (optional, will use default location if not specified).
        /// </summary>
        public string? ConfigFilePath { get; set; }

        /// <summary>
        /// OCI Profile name (optional, will use DEFAULT if not specified).
        /// </summary>
        public string? ProfileName { get; set; }

        /// <summary>
        /// OCI Tenancy OCID.
        /// </summary>
        public string TenancyId { get; set; } = string.Empty;

        /// <summary>
        /// OCI User OCID.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// OCI Region (e.g., "us-ashburn-1").
        /// </summary>
        public string Region { get; set; } = string.Empty;

        /// <summary>
        /// OCI Private key file path.
        /// </summary>
        public string? PrivateKeyPath { get; set; }

        /// <summary>
        /// OCI Private key content (alternative to file path).
        /// </summary>
        public string? PrivateKeyContent { get; set; }

        /// <summary>
        /// OCI Key fingerprint.
        /// </summary>
        public string Fingerprint { get; set; } = string.Empty;

        /// <summary>
        /// OCI Vault OCID.
        /// </summary>
        public string VaultId { get; set; } = string.Empty;

        /// <summary>
        /// OCI Compartment OCID.
        /// </summary>
        public string CompartmentId { get; set; } = string.Empty;

        /// <summary>
        /// Timeout in seconds for vault operations.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Priority order for this provider (lower = higher priority).
        /// </summary>
        public int Priority { get; set; } = 5;
    }
}