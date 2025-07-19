using MediatR;
using SmartAlarm.Application.DTOs.ExceptionPeriod;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Application.Commands.ExceptionPeriod;

/// <summary>
/// Command para criar um novo período de exceção
/// </summary>
public class CreateExceptionPeriodCommand : IRequest<ExceptionPeriodDto>
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
    /// ID do usuário proprietário do período
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Descrição opcional do período
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Cria command a partir de DTO
    /// </summary>
    public static CreateExceptionPeriodCommand FromDto(CreateExceptionPeriodDto dto, Guid userId)
    {
        return new CreateExceptionPeriodCommand
        {
            Name = dto.Name,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Type = dto.Type,
            UserId = userId,
            Description = dto.Description
        };
    }
}
