using System;
using System.Reflection;

namespace SmartAlarm.Observability
{
    /// <summary>
    /// Interface para informações de versionamento e build da biblioteca
    /// </summary>
    public interface IVersionInfo
    {
        /// <summary>
        /// Versão da biblioteca de observabilidade
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Data/hora do build
        /// </summary>
        DateTime BuildDate { get; }

        /// <summary>
        /// Obtém informações completas de versão em formato string
        /// </summary>
        string GetVersionString();
    }

    /// <summary>
    /// Implementação padrão das informações de versão
    /// </summary>
    public class VersionInfo : IVersionInfo
    {
        /// <summary>
        /// Construtor
        /// </summary>
        public VersionInfo()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyName = assembly.GetName();
            
            Version = assemblyName.Version?.ToString() ?? "1.0.0";
            
            // Tentar obter data de build do assembly
            var buildDateAttribute = assembly.GetCustomAttribute<AssemblyMetadataAttribute>();
            if (buildDateAttribute?.Key == "BuildDate" && 
                DateTime.TryParse(buildDateAttribute.Value, out var buildDate))
            {
                BuildDate = buildDate;
            }
            else
            {
                // Fallback para data de criação do arquivo
                BuildDate = System.IO.File.GetCreationTime(assembly.Location);
            }
        }

        /// <inheritdoc />
        public string Version { get; }

        /// <inheritdoc />
        public DateTime BuildDate { get; }

        /// <inheritdoc />
        public string GetVersionString()
        {
            return $"SmartAlarm.Observability v{Version} (Built: {BuildDate:yyyy-MM-dd HH:mm:ss} UTC)";
        }

        /// <summary>
        /// Obtém informações detalhadas sobre a biblioteca
        /// </summary>
        /// <returns>Informações detalhadas</returns>
        public string GetDetailedInfo()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyName = assembly.GetName();
            
            return $"""
                SmartAlarm Observability Library
                ================================
                Version: {Version}
                Build Date: {BuildDate:yyyy-MM-dd HH:mm:ss} UTC
                Assembly: {assemblyName.FullName}
                Location: {assembly.Location}
                Framework: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}
                """;
        }

        /// <summary>
        /// Obtém informações sobre as dependências principais
        /// </summary>
        /// <returns>Informações das dependências</returns>
        public string GetDependenciesInfo()
        {
            var openTelemetryVersion = GetPackageVersion("OpenTelemetry");
            var serilogVersion = GetPackageVersion("Serilog");
            
            return $"""
                Main Dependencies
                ================
                OpenTelemetry: {openTelemetryVersion ?? "Unknown"}
                Serilog: {serilogVersion ?? "Unknown"}
                """;
        }

        /// <summary>
        /// Tenta obter a versão de um pacote específico
        /// </summary>
        /// <param name="packageName">Nome do pacote</param>
        /// <returns>Versão do pacote ou null se não encontrado</returns>
        private static string? GetPackageVersion(string packageName)
        {
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    if (assembly.GetName().Name?.StartsWith(packageName) == true)
                    {
                        return assembly.GetName().Version?.ToString();
                    }
                }
            }
            catch
            {
                // Ignora erros ao tentar obter versões
            }
            
            return null;
        }
    }
}
