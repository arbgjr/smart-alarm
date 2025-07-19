using System.ComponentModel.DataAnnotations;

namespace SmartAlarm.Domain.Entities
{
    /// <summary>
    /// Entidade que representa um webhook registrado no sistema
    /// </summary>
    public class Webhook
    {
        public Guid Id { get; set; }
        
        [Required]
        [Url]
        public string Url { get; set; } = string.Empty;
        
        [Required]
        public string[] Events { get; set; } = Array.Empty<string>();
        
        [Required]
        public string Secret { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime? UpdatedAt { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// ID do usuário que criou o webhook
        /// </summary>
        public Guid UserId { get; set; }
        
        /// <summary>
        /// Número de tentativas de entrega falhas consecutivas
        /// </summary>
        public int FailureCount { get; set; } = 0;
        
        /// <summary>
        /// Data da última tentativa de entrega
        /// </summary>
        public DateTime? LastDeliveryAttempt { get; set; }
        
        /// <summary>
        /// Data da última entrega bem-sucedida
        /// </summary>
        public DateTime? LastSuccessfulDelivery { get; set; }
    }
}
