using System.ComponentModel.DataAnnotations;

namespace SmartAlarm.Domain.Entities;

/// <summary>
/// Entidade de Role para RBAC (Role-Based Access Control)
/// Seguindo princípios de Clean Architecture e SOLID
/// </summary>
public class Role
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    // Navegação
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    /// <summary>
    /// Desativa role
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Ativa role
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }
}
