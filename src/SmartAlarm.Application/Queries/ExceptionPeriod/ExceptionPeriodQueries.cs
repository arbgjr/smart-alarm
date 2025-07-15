using MediatR;
using SmartAlarm.Application.DTOs.ExceptionPeriod;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Application.Queries.ExceptionPeriod;

/// <summary>
/// Query para obter um período de exceção por ID
/// </summary>
public class GetExceptionPeriodByIdQuery : IRequest<ExceptionPeriodDto?>
{
    /// <summary>
    /// ID do período de exceção
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ID do usuário solicitante (para validação de propriedade)
    /// </summary>
    public Guid UserId { get; set; }

    public GetExceptionPeriodByIdQuery(Guid id, Guid userId)
    {
        Id = id;
        UserId = userId;
    }
}

/// <summary>
/// Query para listar períodos de exceção de um usuário
/// </summary>
public class ListExceptionPeriodsQuery : IRequest<IEnumerable<ExceptionPeriodDto>>
{
    /// <summary>
    /// ID do usuário
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Filtro por tipo (opcional)
    /// </summary>
    public ExceptionPeriodType? Type { get; set; }

    /// <summary>
    /// Incluir apenas períodos ativos
    /// </summary>
    public bool OnlyActive { get; set; } = true;

    /// <summary>
    /// Data para filtrar períodos ativos (opcional)
    /// </summary>
    public DateTime? ActiveOnDate { get; set; }

    public ListExceptionPeriodsQuery(Guid userId)
    {
        UserId = userId;
    }
}

/// <summary>
/// Query para obter períodos ativos em uma data específica
/// </summary>
public class GetActiveExceptionPeriodsOnDateQuery : IRequest<IEnumerable<ExceptionPeriodDto>>
{
    /// <summary>
    /// ID do usuário
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Data para verificar períodos ativos
    /// </summary>
    public DateTime Date { get; set; }

    public GetActiveExceptionPeriodsOnDateQuery(Guid userId, DateTime date)
    {
        UserId = userId;
        Date = date;
    }
}
