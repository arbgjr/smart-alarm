namespace SmartAlarm.KeyVault.Configuration
{
    /// <summary>
    /// Configuration for AWS Secrets Manager provider.
    /// </summary>
    public class AwsSecretsManagerOptions
    {
        public const string ConfigSection = "AwsSecretsManager";

        /// <summary>
        /// AWS Region (e.g., "us-east-1").
        /// </summary>
        public string Region { get; set; } = string.Empty;

        /// <summary>
        /// AWS Access Key ID (optional, will use IAM role if not specified).
        /// </summary>
        public string? AccessKeyId { get; set; }

        /// <summary>
        /// AWS Secret Access Key (optional, will use IAM role if not specified).
        /// </summary>
        public string? SecretAccessKey { get; set; }

        /// <summary>
        /// AWS Session Token (optional, for temporary credentials).
        /// </summary>
        public string? SessionToken { get; set; }

        /// <summary>
        /// Whether to use IAM role for authentication.
        /// </summary>
        public bool UseIamRole { get; set; } = true;

        /// <summary>
        /// Timeout in seconds for secrets manager operations.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Priority order for this provider (lower = higher priority).
        /// </summary>
        public int Priority { get; set; } = 3;
    }
}