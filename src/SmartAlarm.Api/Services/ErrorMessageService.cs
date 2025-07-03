using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SmartAlarm.Api.Services
{
    /// <summary>
    /// Implementação do serviço de mensagens de erro.
    /// </summary>
    public class ErrorMessageService : IErrorMessageService
    {
        private readonly Dictionary<string, object> _messages;
        private readonly ILogger<ErrorMessageService> _logger;

        public ErrorMessageService(IWebHostEnvironment environment, ILogger<ErrorMessageService> logger)
        {
            _logger = logger;
            _messages = LoadMessages(environment);
        }

        public string GetMessage(string keyPath, params object[] parameters)
        {
            try
            {
                var message = GetMessageFromPath(keyPath);
                if (string.IsNullOrEmpty(message))
                {
                    _logger.LogWarning("Mensagem não encontrada para o caminho: {KeyPath}", keyPath);
                    return $"Erro: {keyPath}";
                }

                return FormatMessage(message, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter mensagem para o caminho: {KeyPath}", keyPath);
                return $"Erro: {keyPath}";
            }
        }

        public bool HasMessage(string keyPath)
        {
            return !string.IsNullOrEmpty(GetMessageFromPath(keyPath));
        }

        private Dictionary<string, object> LoadMessages(IWebHostEnvironment environment)
        {
            try
            {
                var filePath = Path.Combine(environment.ContentRootPath, "Resources", "ErrorMessages.json");
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("Arquivo de mensagens de erro não encontrado: {FilePath}", filePath);
                    return new Dictionary<string, object>();
                }

                var json = File.ReadAllText(filePath);
                var messages = JsonSerializer.Deserialize<Dictionary<string, object>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return messages ?? new Dictionary<string, object>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar mensagens de erro");
                return new Dictionary<string, object>();
            }
        }

        private string GetMessageFromPath(string keyPath)
        {
            var keys = keyPath.Split('.');
            object current = _messages;

            foreach (var key in keys)
            {
                if (current is Dictionary<string, object> dict && dict.TryGetValue(key, out var value))
                {
                    current = value;
                }
                else if (current is JsonElement element)
                {
                    if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(key, out var property))
                    {
                        current = property;
                    }
                    else
                    {
                        return string.Empty;
                    }
                }
                else
                {
                    return string.Empty;
                }
            }

            return current switch
            {
                string str => str,
                JsonElement element when element.ValueKind == JsonValueKind.String => element.GetString() ?? string.Empty,
                _ => string.Empty
            };
        }

        private static string FormatMessage(string message, object[] parameters)
        {
            if (parameters == null || parameters.Length == 0)
                return message;

            // Suporte para placeholders nomeados como {MaxLength}, {MinLength}, etc.
            var result = message;
            var regex = new Regex(@"\{(\w+)\}");
            var matches = regex.Matches(message);

            for (int i = 0; i < matches.Count && i < parameters.Length; i++)
            {
                var placeholder = matches[i].Value;
                result = result.Replace(placeholder, parameters[i]?.ToString() ?? string.Empty);
            }

            return result;
        }
    }
}