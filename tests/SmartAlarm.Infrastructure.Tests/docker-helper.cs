using SmartAlarm.Domain.Abstractions;
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
            // Verificar primeiro as variáveis de conexão específicas dos containers
            switch (serviceName.ToUpper())
            {
                case "POSTGRES":
                    var pgConnString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
                    if (!string.IsNullOrEmpty(pgConnString) && pgConnString.Contains("Host="))
                    {
                        var hostStart = pgConnString.IndexOf("Host=") + 5;
                        var hostEnd = pgConnString.IndexOf(";", hostStart);
                        if (hostEnd > hostStart)
                        {
                            return pgConnString.Substring(hostStart, hostEnd - hostStart);
                        }
                    }
                    break;
                    
                case "VAULT":
                    var vaultAddress = Environment.GetEnvironmentVariable("VaultConfig__Address");
                    if (!string.IsNullOrEmpty(vaultAddress))
                    {
                        var uri = new Uri(vaultAddress);
                        return uri.Host;
                    }
                    break;
                    
                case "MINIO":
                    var minioEndpoint = Environment.GetEnvironmentVariable("MinIOConfig__Endpoint");
                    if (!string.IsNullOrEmpty(minioEndpoint))
                    {
                        // MinIOConfig__Endpoint pode ser "minio:9000"
                        if (minioEndpoint.Contains(":"))
                        {
                            return minioEndpoint.Split(':')[0];
                        }
                        return minioEndpoint;
                    }
                    break;
                    
                case "RABBITMQ":
                    var rabbitHost = Environment.GetEnvironmentVariable("RabbitMQConfig__HostName");
                    if (!string.IsNullOrEmpty(rabbitHost))
                    {
                        return rabbitHost;
                    }
                    break;
            }
            
            // Fallback para variáveis de ambiente tradicionais
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
            // Verificar primeiro as variáveis de conexão específicas dos containers
            switch (serviceName.ToUpper())
            {
                case "POSTGRES":
                    var pgConnString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
                    if (!string.IsNullOrEmpty(pgConnString) && pgConnString.Contains("Port="))
                    {
                        var portStart = pgConnString.IndexOf("Port=") + 5;
                        var portEnd = pgConnString.IndexOf(";", portStart);
                        if (portEnd > portStart)
                        {
                            var pgPortStr = pgConnString.Substring(portStart, portEnd - portStart);
                            if (int.TryParse(pgPortStr, out int port))
                            {
                                return port;
                            }
                        }
                    }
                    break;
                    
                case "VAULT":
                    var vaultAddress = Environment.GetEnvironmentVariable("VaultConfig__Address");
                    if (!string.IsNullOrEmpty(vaultAddress))
                    {
                        var uri = new Uri(vaultAddress);
                        return uri.Port == -1 ? defaultPort : uri.Port;
                    }
                    break;
                    
                case "MINIO":
                    var minioEndpoint = Environment.GetEnvironmentVariable("MinIOConfig__Endpoint");
                    if (!string.IsNullOrEmpty(minioEndpoint))
                    {
                        // MinIOConfig__Endpoint pode ser "minio:9000"
                        if (minioEndpoint.Contains(":"))
                        {
                            var parts = minioEndpoint.Split(':');
                            if (parts.Length > 1 && int.TryParse(parts[1], out int port))
                            {
                                return port;
                            }
                        }
                    }
                    break;
                    
                case "RABBITMQ":
                    var rabbitPort = Environment.GetEnvironmentVariable("RabbitMQConfig__Port");
                    if (!string.IsNullOrEmpty(rabbitPort) && int.TryParse(rabbitPort, out int rPort))
                    {
                        return rPort;
                    }
                    break;
            }
            
            // Fallback para variáveis de ambiente tradicionais
            var envVar = $"{serviceName}_PORT";
            var portStr = Environment.GetEnvironmentVariable(envVar);
            
            if (int.TryParse(portStr, out int resultPort))
            {
                return resultPort;
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

        /// <summary>
        /// Resolve service hostname - wrapper method for compatibility
        /// </summary>
        /// <param name="serviceName">Service name (lowercase, e.g., "postgres", "minio")</param>
        /// <param name="fallbackHost">Fallback host (default: localhost)</param>
        /// <returns>Resolved hostname</returns>
        public static string ResolveServiceHostname(string serviceName, string fallbackHost = "localhost")
        {
            // Convert to uppercase for environment variable lookup
            return GetHost(serviceName.ToUpper());
        }

        /// <summary>
        /// Resolve service port - wrapper method for compatibility
        /// </summary>
        /// <param name="serviceName">Service name (lowercase, e.g., "postgres", "minio")</param>
        /// <param name="defaultPort">Default port</param>
        /// <returns>Resolved port</returns>
        public static int ResolveServicePort(string serviceName, int defaultPort)
        {
            // Convert to uppercase for environment variable lookup
            return GetPort(serviceName.ToUpper(), defaultPort);
        }

        /// <summary>
        /// Get observability service URL using environment variables or fallback to localhost
        /// </summary>
        /// <param name="serviceName">Service name (jaeger, loki, prometheus, grafana)</param>
        /// <param name="defaultPort">Default port</param>
        /// <returns>Service URL</returns>
        public static string GetObservabilityUrl(string serviceName, int defaultPort)
        {
            // Check environment variables for observability endpoints
            var envVarName = $"ObservabilityConfig__{char.ToUpper(serviceName[0])}{serviceName.Substring(1)}__Endpoint";
            var endpoint = Environment.GetEnvironmentVariable(envVarName);
            
            if (!string.IsNullOrEmpty(endpoint))
            {
                return endpoint;
            }
            
            // Fallback to host/port variables
            var host = GetHost(serviceName.ToUpper());
            var port = GetPort(serviceName.ToUpper(), defaultPort);
            
            return $"http://{host}:{port}";
        }
    }
}
