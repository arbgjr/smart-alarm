namespace SmartAlarm.KeyVault.Configuration
{
    /// <summary>
    /// Configuration for Azure Key Vault provider.
    /// </summary>
    public class AzureKeyVaultOptions
    {
        public const string ConfigSection = "AzureKeyVault";

        /// <summary>
        /// Key Vault URI (e.g., "https://myvault.vault.azure.net/").
        /// </summary>
        public string VaultUri { get; set; } = string.Empty;

        /// <summary>
        /// Azure AD Tenant ID (optional, will use default tenant if not specified).
        /// </summary>
        public string? TenantId { get; set; }

        /// <summary>
        /// Client ID for service principal authentication (optional).
        /// </summary>
        public string? ClientId { get; set; }

        /// <summary>
        /// Client secret for service principal authentication (optional).
        /// </summary>
        public string? ClientSecret { get; set; }

        /// <summary>
        /// Whether to use managed identity for authentication.
        /// </summary>
        public bool UseManagedIdentity { get; set; } = true;

        /// <summary>
        /// Timeout in seconds for vault operations.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Priority order for this provider (lower = higher priority).
        /// </summary>
        public int Priority { get; set; } = 2;
    }
}