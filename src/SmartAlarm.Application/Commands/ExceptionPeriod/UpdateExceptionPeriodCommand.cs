using MediatR;
using SmartAlarm.Application.DTOs.ExceptionPeriod;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Application.Commands.ExceptionPeriod;

/// <summary>
/// Command para atualizar um período de exceção existente
/// </summary>
public class UpdateExceptionPeriodCommand : IRequest<ExceptionPeriodDto>
{
    /// <summary>
    /// ID do período de exceção a ser atualizado
    /// </summary>
    public Guid Id { get; set; }

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
    /// ID do usuário proprietário do período
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Descrição opcional do período
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Indica se o período está ativo
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Cria command a partir de DTO
    /// </summary>
    public static UpdateExceptionPeriodCommand FromDto(Guid id, UpdateExceptionPeriodDto dto, Guid userId)
    {
        return new UpdateExceptionPeriodCommand
        {
            Id = id,
            Name = dto.Name,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Type = dto.Type,
            UserId = userId,
            Description = dto.Description,
            IsActive = dto.IsActive
        };
    }
}
