using Serilog.Formatting;
using Serilog.Events;
using System;
using System.IO;
using System.Text.Json;

namespace SmartAlarm.Observability.Logging.Formatters
{
    /// <summary>
    /// Formatter JSON customizado para logs do Smart Alarm
    /// </summary>
    public class SmartAlarmJsonFormatter : ITextFormatter
    {
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Construtor
        /// </summary>
        public SmartAlarmJsonFormatter()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        /// <summary>
        /// Formata o evento de log
        /// </summary>
        /// <param name="logEvent">Evento de log</param>
        /// <param name="output">Stream de saída</param>
        public void Format(LogEvent logEvent, TextWriter output)
        {
            var logObject = new
            {
                timestamp = logEvent.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                level = logEvent.Level.ToString(),
                message = logEvent.RenderMessage(),
                template = logEvent.MessageTemplate.Text,
                properties = ExtractProperties(logEvent),
                exception = logEvent.Exception != null ? FormatException(logEvent.Exception) : null
            };

            var json = JsonSerializer.Serialize(logObject, _jsonOptions);
            output.WriteLine(json);
        }

        /// <summary>
        /// Extrai propriedades do evento de log
        /// </summary>
        /// <param name="logEvent">Evento de log</param>
        /// <returns>Dicionário com as propriedades</returns>
        private static Dictionary<string, object?> ExtractProperties(LogEvent logEvent)
        {
            var properties = new Dictionary<string, object?>();

            foreach (var property in logEvent.Properties)
            {
                properties[property.Key] = ExtractPropertyValue(property.Value);
            }

            return properties;
        }

        /// <summary>
        /// Extrai o valor de uma propriedade
        /// </summary>
        /// <param name="propertyValue">Valor da propriedade</param>
        /// <returns>Valor extraído</returns>
        private static object? ExtractPropertyValue(LogEventPropertyValue propertyValue)
        {
            return propertyValue switch
            {
                ScalarValue scalar => scalar.Value,
                SequenceValue sequence => sequence.Elements.Select(ExtractPropertyValue).ToArray(),
                StructureValue structure => structure.Properties.ToDictionary(
                    p => p.Name,
                    p => ExtractPropertyValue(p.Value)),
                DictionaryValue dictionary => dictionary.Elements.ToDictionary(
                    kvp => ExtractPropertyValue(kvp.Key)?.ToString() ?? "null",
                    kvp => ExtractPropertyValue(kvp.Value)),
                _ => propertyValue.ToString()
            };
        }

        /// <summary>
        /// Formata informações de exceção
        /// </summary>
        /// <param name="exception">Exceção</param>
        /// <returns>Objeto com dados da exceção</returns>
        private static object FormatException(Exception exception)
        {
            return new
            {
                type = exception.GetType().Name,
                message = exception.Message,
                stackTrace = exception.StackTrace,
                source = exception.Source,
                innerException = exception.InnerException != null ? FormatException(exception.InnerException) : null,
                data = exception.Data.Count > 0 ? exception.Data : null
            };
        }
    }

    /// <summary>
    /// Formatter compacto para console com cores
    /// </summary>
    public class SmartAlarmConsoleFormatter : ITextFormatter
    {
        /// <summary>
        /// Formata o evento de log para console
        /// </summary>
        /// <param name="logEvent">Evento de log</param>
        /// <param name="output">Stream de saída</param>
        public void Format(LogEvent logEvent, TextWriter output)
        {
            var timestamp = logEvent.Timestamp.ToString("HH:mm:ss.fff");
            var level = GetLevelString(logEvent.Level);
            var message = logEvent.RenderMessage();

            // Extrair propriedades importantes
            var correlationId = GetPropertyValue(logEvent, "CorrelationId");
            var userId = GetPropertyValue(logEvent, "UserId");

            var formattedMessage = $"[{timestamp}] {level} {message}";

            if (!string.IsNullOrEmpty(correlationId))
            {
                formattedMessage += $" (Correlation: {correlationId[..8]}...)";
            }

            if (!string.IsNullOrEmpty(userId))
            {
                formattedMessage += $" (User: {userId})";
            }

            if (logEvent.Exception != null)
            {
                formattedMessage += Environment.NewLine + logEvent.Exception.ToString();
            }

            output.WriteLine(formattedMessage);
        }

        /// <summary>
        /// Obtém representação string do nível de log
        /// </summary>
        /// <param name="level">Nível de log</param>
        /// <returns>String formatada</returns>
        private static string GetLevelString(LogEventLevel level)
        {
            return level switch
            {
                LogEventLevel.Verbose => "[VRB]",
                LogEventLevel.Debug => "[DBG]",
                LogEventLevel.Information => "[INF]",
                LogEventLevel.Warning => "[WRN]",
                LogEventLevel.Error => "[ERR]",
                LogEventLevel.Fatal => "[FTL]",
                _ => "[UNK]"
            };
        }

        /// <summary>
        /// Obtém valor de uma propriedade do evento de log
        /// </summary>
        /// <param name="logEvent">Evento de log</param>
        /// <param name="propertyName">Nome da propriedade</param>
        /// <returns>Valor da propriedade como string</returns>
        private static string? GetPropertyValue(LogEvent logEvent, string propertyName)
        {
            if (logEvent.Properties.TryGetValue(propertyName, out var property) && 
                property is ScalarValue scalar)
            {
                return scalar.Value?.ToString();
            }
            return null;
        }
    }
}
