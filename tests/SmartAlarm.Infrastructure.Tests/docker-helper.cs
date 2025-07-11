using System;

namespace SmartAlarm.Infrastructure.Tests
{
    /// <summary>
    /// Helper class for Docker integration tests to resolve host names from environment variables.
    /// </summary>
    public static class DockerHelper
    {
        /// <summary>
        /// Get the host name for a service, checking environment variables first,
        /// then falling back to localhost.
        /// </summary>
        /// <param name="serviceName">Base service name (e.g., "POSTGRES", "MINIO")</param>
        /// <returns>The host name to use in connection strings</returns>
        public static string GetHost(string serviceName)
        {
            var envVar = $"{serviceName}_HOST";
            return Environment.GetEnvironmentVariable(envVar) ?? "localhost";
        }

        /// <summary>
        /// Get the port for a service, checking environment variables first,
        /// then falling back to the default port.
        /// </summary>
        /// <param name="serviceName">Base service name (e.g., "POSTGRES", "MINIO")</param>
        /// <param name="defaultPort">The default port to use if environment variable is not set</param>
        /// <returns>The port to use in connection strings</returns>
        public static int GetPort(string serviceName, int defaultPort)
        {
            var envVar = $"{serviceName}_PORT";
            var portStr = Environment.GetEnvironmentVariable(envVar);
            
            if (int.TryParse(portStr, out int port))
            {
                return port;
            }
            
            return defaultPort;
        }

        /// <summary>
        /// Get the connection string for a PostgreSQL database.
        /// </summary>
        /// <param name="database">Database name</param>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <returns>PostgreSQL connection string</returns>
        public static string GetPostgresConnectionString(string database, string username, string password)
        {
            var host = GetHost("POSTGRES");
            var port = GetPort("POSTGRES", 5432);
            
            return $"Host={host};Port={port};Database={database};Username={username};Password={password}";
        }

        /// <summary>
        /// Get the URL for a service.
        /// </summary>
        /// <param name="serviceName">Base service name (e.g., "MINIO", "VAULT")</param>
        /// <param name="defaultPort">Default port if environment variable is not set</param>
        /// <param name="protocol">Protocol (http or https)</param>
        /// <returns>URL to the service</returns>
        public static string GetServiceUrl(string serviceName, int defaultPort, string protocol = "http")
        {
            var host = GetHost(serviceName);
            var port = GetPort(serviceName, defaultPort);
            
            return $"{protocol}://{host}:{port}";
        }
    }
}
