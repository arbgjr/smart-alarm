using System;

namespace SmartAlarm.Observability.Context
{
    /// <summary>
    /// Interface para contexto de correlação
    /// </summary>
    public interface ICorrelationContext
    {
        /// <summary>
        /// ID de correlação da requisição
        /// </summary>
        string CorrelationId { get; }

        /// <summary>
        /// ID da sessão do usuário
        /// </summary>
        string? SessionId { get; set; }

        /// <summary>
        /// ID do usuário
        /// </summary>
        string? UserId { get; set; }

        /// <summary>
        /// Informações adicionais do contexto
        /// </summary>
        void AddContextProperty(string key, object value);

        /// <summary>
        /// Obtém uma propriedade do contexto
        /// </summary>
        T? GetContextProperty<T>(string key);
    }

    /// <summary>
    /// Implementação do contexto de correlação
    /// </summary>
    public class CorrelationContext : ICorrelationContext
    {
        private readonly Dictionary<string, object> _properties = new();

        /// <summary>
        /// Construtor
        /// </summary>
        public CorrelationContext()
        {
            CorrelationId = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Construtor com ID de correlação específico
        /// </summary>
        /// <param name="correlationId">ID de correlação</param>
        public CorrelationContext(string correlationId)
        {
            CorrelationId = correlationId;
        }

        /// <inheritdoc />
        public string CorrelationId { get; }

        /// <inheritdoc />
        public string? SessionId { get; set; }

        /// <inheritdoc />
        public string? UserId { get; set; }

        /// <inheritdoc />
        public void AddContextProperty(string key, object value)
        {
            _properties[key] = value;
        }

        /// <inheritdoc />
        public T? GetContextProperty<T>(string key)
        {
            if (_properties.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return default(T);
        }
    }
}
