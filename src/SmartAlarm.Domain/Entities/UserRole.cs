using System.ComponentModel.DataAnnotations;

namespace SmartAlarm.Domain.Entities;

/// <summary>
/// Entidade de junção para RBAC (Role-Based Access Control)
/// Representa a relação Many-to-Many entre User e Role
/// Seguindo princípios de Clean Architecture e SOLID
/// </summary>
public class UserRole
{
    public Guid UserId { get; set; }

    public Guid RoleId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;

    // Navegação
    public virtual User User { get; set; } = null!;

    public virtual Role Role { get; set; } = null!;

    /// <summary>
    /// Construtor privado para EF Core
    /// </summary>
    private UserRole() { }

    /// <summary>
    /// Construtor público
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <param name="roleId">ID da role</param>
    /// <param name="expiresAt">Data de expiração (opcional)</param>
    public UserRole(Guid userId, Guid roleId, DateTime? expiresAt = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId não pode ser vazio", nameof(userId));

        if (roleId == Guid.Empty)
            throw new ArgumentException("RoleId não pode ser vazio", nameof(roleId));

        UserId = userId;
        RoleId = roleId;
        ExpiresAt = expiresAt;
    }

    /// <summary>
    /// Verifica se a role está válida (ativa e não expirada)
    /// </summary>
    /// <returns>True se válida</returns>
    public bool IsValid()
    {
        return IsActive && (ExpiresAt == null || ExpiresAt > DateTime.UtcNow);
    }

    /// <summary>
    /// Desativa a role do usuário
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Revoga role do usuário (compatibilidade)
    /// </summary>
    public void Revoke()
    {
        IsActive = false;
    }

    /// <summary>
    /// Reativa role do usuário
    /// </summary>
    public void Reactivate()
    {
        IsActive = true;
        ExpiresAt = null;
    }

    /// <summary>
    /// Define data de expiração
    /// </summary>
    /// <param name="expiresAt">Data de expiração</param>
    public void SetExpiration(DateTime expiresAt)
    {
        if (expiresAt <= DateTime.UtcNow)
            throw new ArgumentException("Data de expiração deve ser futura", nameof(expiresAt));

        ExpiresAt = expiresAt;
    }
}
