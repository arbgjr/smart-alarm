using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartAlarm.KeyVault.Abstractions;
using SmartAlarm.KeyVault.Configuration;
using SmartAlarm.KeyVault.Middleware;
using SmartAlarm.KeyVault.Providers;
using SmartAlarm.KeyVault.Services;

namespace SmartAlarm.KeyVault.Extensions
{
    /// <summary>
    /// Extension methods for configuring KeyVault services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds KeyVault services to the dependency injection container.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration to bind options from.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddKeyVault(this IServiceCollection services, IConfiguration configuration)
        {
            // Register configuration options
            services.Configure<KeyVaultOptions>(options => configuration.GetSection(KeyVaultOptions.ConfigSection).Bind(options));
            services.Configure<HashiCorpVaultOptions>(options => configuration.GetSection(HashiCorpVaultOptions.ConfigSection).Bind(options));
            services.Configure<AzureKeyVaultOptions>(options => configuration.GetSection(AzureKeyVaultOptions.ConfigSection).Bind(options));
            services.Configure<AwsSecretsManagerOptions>(options => configuration.GetSection(AwsSecretsManagerOptions.ConfigSection).Bind(options));
            services.Configure<GcpSecretManagerOptions>(options => configuration.GetSection(GcpSecretManagerOptions.ConfigSection).Bind(options));
            services.Configure<OciVaultOptions>(options => configuration.GetSection(OciVaultOptions.ConfigSection).Bind(options));

            // Register providers
            services.AddSingleton<ISecretProvider, HashiCorpVaultProvider>();
            services.AddSingleton<ISecretProvider, AzureKeyVaultProvider>();
            services.AddSingleton<ISecretProvider, AwsSecretsManagerProvider>();
            services.AddSingleton<ISecretProvider, GcpSecretManagerProvider>();
            services.AddSingleton<ISecretProvider, OciVaultProvider>();

            // Register main service
            services.AddSingleton<IKeyVaultService, KeyVaultService>();

            return services;
        }

        /// <summary>
        /// Adds only HashiCorp Vault provider (useful for minimal setups).
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration to bind options from.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddHashiCorpVault(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<KeyVaultOptions>(options => configuration.GetSection(KeyVaultOptions.ConfigSection).Bind(options));
            services.Configure<HashiCorpVaultOptions>(options => configuration.GetSection(HashiCorpVaultOptions.ConfigSection).Bind(options));

            services.AddSingleton<ISecretProvider, HashiCorpVaultProvider>();
            services.AddSingleton<IKeyVaultService, KeyVaultService>();

            return services;
        }

        /// <summary>
        /// Adds only Azure Key Vault provider (useful for Azure-only setups).
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration to bind options from.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddAzureKeyVault(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<KeyVaultOptions>(options => configuration.GetSection(KeyVaultOptions.ConfigSection).Bind(options));
            services.Configure<AzureKeyVaultOptions>(options => configuration.GetSection(AzureKeyVaultOptions.ConfigSection).Bind(options));

            services.AddSingleton<ISecretProvider, AzureKeyVaultProvider>();
            services.AddSingleton<IKeyVaultService, KeyVaultService>();

            return services;
        }

        /// <summary>
        /// Adds only AWS Secrets Manager provider (useful for AWS-only setups).
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration to bind options from.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddAwsSecretsManager(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<KeyVaultOptions>(options => configuration.GetSection(KeyVaultOptions.ConfigSection).Bind(options));
            services.Configure<AwsSecretsManagerOptions>(options => configuration.GetSection(AwsSecretsManagerOptions.ConfigSection).Bind(options));

            services.AddSingleton<ISecretProvider, AwsSecretsManagerProvider>();
            services.AddSingleton<IKeyVaultService, KeyVaultService>();

            return services;
        }
    }

    /// <summary>
    /// Extension methods for configuring KeyVault middleware.
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds KeyVault middleware to the application pipeline.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseKeyVault(this IApplicationBuilder app)
        {
            return app.UseMiddleware<KeyVaultMiddleware>();
        }
    }
}