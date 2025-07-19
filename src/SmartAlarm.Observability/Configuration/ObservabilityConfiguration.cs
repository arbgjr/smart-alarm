using System;

namespace SmartAlarm.Observability.Configuration
{
    /// <summary>
    /// Configuração geral de observabilidade
    /// </summary>
    public class ObservabilityConfiguration
    {
        /// <summary>
        /// Ambiente de execução
        /// </summary>
        public string? Environment { get; set; }

        /// <summary>
        /// Configurações de logging
        /// </summary>
        public LoggingConfiguration Logging { get; set; } = new();

        /// <summary>
        /// Configurações de tracing
        /// </summary>
        public TracingConfiguration Tracing { get; set; } = new();

        /// <summary>
        /// Configurações de métricas
        /// </summary>
        public MetricsConfiguration Metrics { get; set; } = new();
    }

    /// <summary>
    /// Configuração de logging
    /// </summary>
    public class LoggingConfiguration
    {
        /// <summary>
        /// Nível de log mínimo
        /// </summary>
        public string Level { get; set; } = "Information";

        /// <summary>
        /// Caminho do arquivo de log
        /// </summary>
        public string FilePath { get; set; } = "logs/app-.log";

        /// <summary>
        /// Configuração do Graylog
        /// </summary>
        public GraylogConfiguration? Graylog { get; set; }
    }

    /// <summary>
    /// Configuração do Graylog
    /// </summary>
    public class GraylogConfiguration
    {
        /// <summary>
        /// Indica se o Graylog está habilitado
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Host do Graylog
        /// </summary>
        public string Host { get; set; } = string.Empty;

        /// <summary>
        /// Porta do Graylog
        /// </summary>
        public int Port { get; set; } = 12201;
    }

    /// <summary>
    /// Configuração de tracing
    /// </summary>
    public class TracingConfiguration
    {
        /// <summary>
        /// Taxa de amostragem (0.0 a 1.0)
        /// </summary>
        public double SamplingRatio { get; set; } = 1.0;

        /// <summary>
        /// Configuração do exportador de console
        /// </summary>
        public ConsoleExporterConfiguration Console { get; set; } = new();

        /// <summary>
        /// Configuração do exportador OTLP
        /// </summary>
        public OtlpExporterConfiguration? Otlp { get; set; }
    }

    /// <summary>
    /// Configuração de métricas
    /// </summary>
    public class MetricsConfiguration
    {
        /// <summary>
        /// Configuração do exportador de console
        /// </summary>
        public ConsoleExporterConfiguration Console { get; set; } = new();

        /// <summary>
        /// Configuração do exportador Prometheus
        /// </summary>
        public PrometheusExporterConfiguration Prometheus { get; set; } = new();
    }

    /// <summary>
    /// Configuração do exportador de console
    /// </summary>
    public class ConsoleExporterConfiguration
    {
        /// <summary>
        /// Indica se o exportador está habilitado
        /// </summary>
        public bool Enabled { get; set; } = true;
    }

    /// <summary>
    /// Configuração do exportador OTLP
    /// </summary>
    public class OtlpExporterConfiguration
    {
        /// <summary>
        /// Indica se o exportador está habilitado
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Endpoint do coletor OTLP
        /// </summary>
        public string Endpoint { get; set; } = "http://localhost:4317";
    }

    /// <summary>
    /// Configuração do exportador Prometheus
    /// </summary>
    public class PrometheusExporterConfiguration
    {
        /// <summary>
        /// Indica se o exportador está habilitado
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Endpoint para scraping das métricas
        /// </summary>
        public string Endpoint { get; set; } = "/metrics";
    }
}
