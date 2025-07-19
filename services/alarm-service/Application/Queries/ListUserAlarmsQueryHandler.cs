using MediatR;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Tracing;
using SmartAlarm.Observability.Metrics;
using FluentValidation;
using System.Diagnostics;

namespace SmartAlarm.AlarmService.Application.Queries
{
    /// <summary>
    /// Query para listar alarmes de um usuário
    /// </summary>
    public record ListUserAlarmsQuery(
        Guid UserId,
        bool? IsEnabled = null,
        int Page = 1,
        int PageSize = 10
    ) : IRequest<ListUserAlarmsResponse>;

    /// <summary>
    /// Response da listagem de alarmes
    /// </summary>
    public record ListUserAlarmsResponse(
        IEnumerable<AlarmSummary> Alarms,
        int TotalCount,
        int Page,
        int PageSize,
        int TotalPages
    );

    /// <summary>
    /// Resumo de alarme para listagem
    /// </summary>
    public record AlarmSummary(
        Guid AlarmId,
        string Name,
        DateTime Time,
        bool Enabled,
        DateTime CreatedAt
    );

    /// <summary>
    /// Validator para query de listagem
    /// </summary>
    public class ListUserAlarmsQueryValidator : AbstractValidator<ListUserAlarmsQuery>
    {
        public ListUserAlarmsQueryValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("UserId é obrigatório");

            RuleFor(x => x.Page)
                .GreaterThan(0)
                .WithMessage("Page deve ser maior que zero");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("PageSize deve estar entre 1 e 100");
        }
    }

    /// <summary>
    /// Handler para listar alarmes do usuário
    /// </summary>
    public class ListUserAlarmsQueryHandler : IRequestHandler<ListUserAlarmsQuery, ListUserAlarmsResponse>
    {
        private readonly IAlarmRepository _alarmRepository;
        private readonly IUserRepository _userRepository;
        private readonly IValidator<ListUserAlarmsQuery> _validator;
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly ILogger<ListUserAlarmsQueryHandler> _logger;

        public ListUserAlarmsQueryHandler(
            IAlarmRepository alarmRepository,
            IUserRepository userRepository,
            IValidator<ListUserAlarmsQuery> validator,
            SmartAlarmActivitySource activitySource,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext,
            ILogger<ListUserAlarmsQueryHandler> logger)
        {
            _alarmRepository = alarmRepository;
            _userRepository = userRepository;
            _validator = validator;
            _activitySource = activitySource;
            _meter = meter;
            _correlationContext = correlationContext;
            _logger = logger;
        }

        public async Task<ListUserAlarmsResponse> Handle(ListUserAlarmsQuery request, CancellationToken cancellationToken)
        {
            using var activity = _activitySource.StartActivity("ListUserAlarmsQueryHandler.Handle");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Activity tags
                activity?.SetTag("user.id", request.UserId.ToString());
                activity?.SetTag("operation", "list_user_alarms");
                activity?.SetTag("filter.enabled", request.IsEnabled?.ToString() ?? "null");
                activity?.SetTag("pagination.page", request.Page.ToString());
                activity?.SetTag("pagination.pageSize", request.PageSize.ToString());
                activity?.SetTag("correlation.id", _correlationContext.CorrelationId);

                _logger.LogInformation("Iniciando listagem de alarmes para usuário {UserId} - Page: {Page}, PageSize: {PageSize}, Enabled: {Enabled} - CorrelationId: {CorrelationId}",
                    request.UserId, request.Page, request.PageSize, request.IsEnabled, _correlationContext.CorrelationId);

                // Validação
                var validationResult = await _validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    activity?.SetTag("validation.failed", true);
                    _meter.IncrementErrorCount("query", "list_user_alarms", "validation");
                    
                    var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    _logger.LogWarning("Validação falhou para listagem de alarmes: {Errors} - CorrelationId: {CorrelationId}",
                        errors, _correlationContext.CorrelationId);
                    
                    throw new ValidationException($"Dados inválidos: {errors}");
                }

                // Verificar se usuário existe
                var user = await _userRepository.GetByIdAsync(request.UserId);
                if (user == null)
                {
                    activity?.SetTag("user.found", false);
                    _meter.IncrementErrorCount("query", "list_user_alarms", "user_not_found");
                    
                    _logger.LogWarning("Usuário {UserId} não encontrado para listagem de alarmes - CorrelationId: {CorrelationId}",
                        request.UserId, _correlationContext.CorrelationId);
                    
                    throw new InvalidOperationException($"Usuário {request.UserId} não encontrado");
                }

                activity?.SetTag("user.found", true);
                activity?.SetTag("user.email", user.Email.Address);

                // Buscar alarmes com paginação
                var alarms = await _alarmRepository.GetByUserIdAsync(request.UserId);
                
                // Aplicar filtros
                if (request.IsEnabled.HasValue)
                {
                    alarms = alarms.Where(a => a.Enabled == request.IsEnabled.Value);
                }

                var totalCount = alarms.Count();
                var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

                // Aplicar paginação
                var pagedAlarms = alarms
                    .OrderBy(a => a.Time)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(a => new AlarmSummary(
                        a.Id,
                        a.Name.Value,
                        a.Time,
                        a.Enabled,
                        a.CreatedAt
                    ))
                    .ToList();

                activity?.SetTag("alarms.total_count", totalCount.ToString());
                activity?.SetTag("alarms.returned_count", pagedAlarms.Count.ToString());
                activity?.SetTag("pagination.total_pages", totalPages.ToString());

                stopwatch.Stop();

                // Métricas de sucesso
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "list_user_alarms", "success", "200");

                // Log de sucesso
                _logger.LogInformation("Alarmes listados com sucesso para usuário {UserId} - Total: {TotalCount}, Retornados: {ReturnedCount}, Página: {Page}/{TotalPages} - Duração: {Duration}ms - CorrelationId: {CorrelationId}",
                    request.UserId, totalCount, pagedAlarms.Count, request.Page, totalPages, stopwatch.ElapsedMilliseconds, _correlationContext.CorrelationId);

                // Retornar response
                return new ListUserAlarmsResponse(
                    pagedAlarms,
                    totalCount,
                    request.Page,
                    request.PageSize,
                    totalPages
                );
            }
            catch (ValidationException)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "list_user_alarms", "validation_error", "400");
                throw;
            }
            catch (InvalidOperationException)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "list_user_alarms", "user_not_found", "404");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "list_user_alarms", "error", "500");
                _meter.IncrementErrorCount("query", "list_user_alarms", "exception");
                
                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);
                
                _logger.LogError(ex, "Erro inesperado ao listar alarmes do usuário {UserId} - CorrelationId: {CorrelationId}",
                    request.UserId, _correlationContext.CorrelationId);
                
                throw;
            }
        }
    }
}
