using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Application.DTOs.ExceptionPeriod;

/// <summary>
/// DTO para resposta de período de exceção
/// </summary>
public class ExceptionPeriodDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public ExceptionPeriodType Type { get; set; }
    public Guid UserId { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Duração em dias do período de exceção
    /// </summary>
    public int DurationDays => (EndDate.Date - StartDate.Date).Days + 1;

    /// <summary>
    /// Indica se o período está ativo na data especificada
    /// </summary>
    public bool IsActiveOnDate(DateTime date)
    {
        return IsActive && StartDate.Date <= date.Date && EndDate.Date >= date.Date;
    }

    /// <summary>
    /// Converte entidade de domínio para DTO
    /// </summary>
    public static ExceptionPeriodDto FromEntity(Domain.Entities.ExceptionPeriod entity)
    {
        return new ExceptionPeriodDto
        {
            Id = entity.Id,
            Name = entity.Name,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Type = entity.Type,
            UserId = entity.UserId,
            Description = entity.Description,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}
