using System.Collections.Generic;

namespace SmartAlarm.Api.Models
{
    /// <summary>
    /// Modelo padronizado de resposta de erro da API.
    /// </summary>
    public class ErrorResponse
    {
        /// <summary>
        /// Código HTTP do erro.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Título resumido do erro.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Descrição detalhada do erro.
        /// </summary>
        public string Detail { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de erro (ex: ValidationError, BusinessError, SystemError).
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// ID único para rastreamento do erro.
        /// </summary>
        public string TraceId { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp do erro.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Lista de erros de validação, quando aplicável.
        /// </summary>
        public List<ValidationError> ValidationErrors { get; set; } = new();

        /// <summary>
        /// Dados adicionais sobre o erro.
        /// </summary>
        public Dictionary<string, object> Extensions { get; set; } = new();
    }

    /// <summary>
    /// Representação de um erro de validação específico.
    /// </summary>
    public class ValidationError
    {
        /// <summary>
        /// Campo que falhou na validação.
        /// </summary>
        public string Field { get; set; } = string.Empty;

        /// <summary>
        /// Mensagem de erro para o campo.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Código de erro específico.
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Valor que foi rejeitado.
        /// </summary>
        public object? AttemptedValue { get; set; }
    }
}