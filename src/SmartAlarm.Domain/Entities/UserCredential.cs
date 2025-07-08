using System.ComponentModel.DataAnnotations;

namespace SmartAlarm.Domain.Entities;

/// <summary>
/// Credencial FIDO2/WebAuthn do usuário
/// Seguindo princípios de Clean Architecture e SOLID
/// </summary>
public class UserCredential
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [StringLength(500)]
    public string CredentialId { get; set; } = string.Empty;

    [Required]
    public byte[] PublicKey { get; set; } = Array.Empty<byte>();

    [Required]
    public byte[] UserHandle { get; set; } = Array.Empty<byte>();

    public uint SignatureCounter { get; set; }

    [StringLength(100)]
    public string? CredType { get; set; }

    [StringLength(100)]
    public string? AaGuid { get; set; }

    [StringLength(200)]
    public string? DeviceName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastUsedAt { get; set; }

    public bool IsActive { get; set; } = true;

    // Navegação
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Atualiza timestamp de último uso
    /// </summary>
    public void UpdateLastUsed()
    {
        LastUsedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Incrementa contador de assinatura
    /// </summary>
    public void IncrementCounter()
    {
        SignatureCounter++;
        UpdateLastUsed();
    }

    /// <summary>
    /// Desativa credencial
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }
}
