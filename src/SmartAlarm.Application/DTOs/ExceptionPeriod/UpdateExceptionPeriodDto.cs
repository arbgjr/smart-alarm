using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Application.DTOs.ExceptionPeriod;

/// <summary>
/// DTO para atualização de período de exceção
/// </summary>
public class UpdateExceptionPeriodDto
{
    /// <summary>
    /// Nome do período de exceção
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Data de início do período
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Data de fim do período
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Tipo do período de exceção
    /// </summary>
    public ExceptionPeriodType Type { get; set; }

    /// <summary>
    /// Descrição opcional do período
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Indica se o período está ativo
    /// </summary>
    public bool IsActive { get; set; } = true;
}
