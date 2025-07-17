using Microsoft.AspNetCore.Mvc;
using MediatR;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Tracing;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.AlarmService.Application.Commands;
using SmartAlarm.AlarmService.Application.Queries;
using FluentValidation;
using Hangfire;
using System.Diagnostics;

namespace SmartAlarm.AlarmService.Controllers
{
    /// <summary>
    /// Controller para gerenciamento de alarmes
    /// </summary>
    [ApiController]
    [Route("api/v1/alarms")]
    [Produces("application/json")]
    public class AlarmsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<AlarmsController> _logger;
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public AlarmsController(
            IMediator mediator,
            ILogger<AlarmsController> logger,
            SmartAlarmActivitySource activitySource,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext,
            IBackgroundJobClient backgroundJobClient)
        {
            _mediator = mediator;
            _logger = logger;
            _activitySource = activitySource;
            _meter = meter;
            _correlationContext = correlationContext;
            _backgroundJobClient = backgroundJobClient;
        }

        /// <summary>
        /// Lista alarmes do usuário usando MediatR
        /// </summary>
        /// <param name="userId">ID do usuário</param>
        /// <param name="active">Filtrar apenas alarmes ativos</param>
        /// <param name="page">Número da página</param>
        /// <param name="pageSize">Tamanho da página</param>
        /// <returns>Lista de alarmes</returns>
        [HttpGet("user/{userId:guid}")]
        public async Task<IActionResult> GetUserAlarms(
            Guid userId, 
            [FromQuery] bool? active = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            using var activity = _activitySource.StartActivity("AlarmsController.GetUserAlarms");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                activity?.SetTag("user.id", userId.ToString());
                activity?.SetTag("filter.active", active?.ToString() ?? "null");
                activity?.SetTag("pagination.page", page.ToString());
                activity?.SetTag("pagination.pageSize", pageSize.ToString());
                activity?.SetTag("operation", "get_user_alarms");
                
                _logger.LogInformation("Listando alarmes do usuário {UserId} - Filtro ativo: {Active}, Page: {Page}, PageSize: {PageSize} - CorrelationId: {CorrelationId}", 
                    userId, active, page, pageSize, _correlationContext.CorrelationId);

                // Usar MediatR para processar a query
                var query = new ListUserAlarmsQuery(
                    UserId: userId,
                    IsEnabled: active,
                    Page: page,
                    PageSize: pageSize
                );

                var response = await _mediator.Send(query);

                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "get_user_alarms", "success", "200");
                
                _logger.LogInformation("Alarmes do usuário {UserId} listados com sucesso - Total: {TotalCount}, Retornados: {ReturnedCount} em {Duration}ms", 
                    userId, response.TotalCount, response.Alarms.Count(), stopwatch.ElapsedMilliseconds);

                return Ok(response);
            }
            catch (ValidationException ex)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "get_user_alarms", "validation_error", "400");
                
                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);
                
                _logger.LogWarning("Erro de validação ao listar alarmes do usuário {UserId}: {Error} - CorrelationId: {CorrelationId}", 
                    userId, ex.Message, _correlationContext.CorrelationId);

                return BadRequest(new { error = ex.Message, correlationId = _correlationContext.CorrelationId });
            }
            catch (InvalidOperationException ex)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "get_user_alarms", "not_found", "404");
                
                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);
                
                _logger.LogWarning("Usuário não encontrado ao listar alarmes {UserId}: {Error} - CorrelationId: {CorrelationId}", 
                    userId, ex.Message, _correlationContext.CorrelationId);

                return NotFound(new { error = ex.Message, correlationId = _correlationContext.CorrelationId });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "get_user_alarms", "error", "500");
                _meter.IncrementErrorCount("controller", "user_alarms", "exception");
                
                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);
                
                _logger.LogError(ex, "Erro ao listar alarmes do usuário {UserId} - CorrelationId: {CorrelationId}", 
                    userId, _correlationContext.CorrelationId);

                return StatusCode(500, new { error = "Erro interno do servidor", correlationId = _correlationContext.CorrelationId });
            }
        }

        /// <summary>
        /// Busca um alarme por ID usando MediatR
        /// </summary>
        /// <param name="alarmId">ID do alarme</param>
        /// <returns>Dados do alarme</returns>
        [HttpGet("{alarmId:guid}")]
        public async Task<IActionResult> GetAlarmById(Guid alarmId)
        {
            using var activity = _activitySource.StartActivity("AlarmsController.GetAlarmById");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                activity?.SetTag("alarm.id", alarmId.ToString());
                activity?.SetTag("operation", "get_alarm_by_id");
                
                _logger.LogInformation("Buscando alarme por ID {AlarmId} - CorrelationId: {CorrelationId}", 
                    alarmId, _correlationContext.CorrelationId);

                // Usar MediatR para processar a query
                var query = new GetAlarmByIdQuery(alarmId);
                var response = await _mediator.Send(query);

                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "get_alarm_by_id", "success", "200");
                
                _logger.LogInformation("Alarme {AlarmId} encontrado com sucesso em {Duration}ms", 
                    alarmId, stopwatch.ElapsedMilliseconds);

                return Ok(response);
            }
            catch (ValidationException ex)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "get_alarm_by_id", "validation_error", "400");
                
                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);
                
                _logger.LogWarning("Erro de validação ao buscar alarme {AlarmId}: {Error} - CorrelationId: {CorrelationId}", 
                    alarmId, ex.Message, _correlationContext.CorrelationId);

                return BadRequest(new { error = ex.Message, correlationId = _correlationContext.CorrelationId });
            }
            catch (InvalidOperationException ex)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "get_alarm_by_id", "not_found", "404");
                
                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);
                
                _logger.LogWarning("Alarme não encontrado {AlarmId}: {Error} - CorrelationId: {CorrelationId}", 
                    alarmId, ex.Message, _correlationContext.CorrelationId);

                return NotFound(new { error = ex.Message, correlationId = _correlationContext.CorrelationId });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "get_alarm_by_id", "error", "500");
                _meter.IncrementErrorCount("controller", "get_alarm", "exception");
                
                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);
                
                _logger.LogError(ex, "Erro ao buscar alarme {AlarmId} - CorrelationId: {CorrelationId}", 
                    alarmId, _correlationContext.CorrelationId);

                return StatusCode(500, new { error = "Erro interno do servidor", correlationId = _correlationContext.CorrelationId });
            }
        }

        /// <summary>
        /// Cria um novo alarme usando MediatR
        /// </summary>
        /// <param name="request">Dados do alarme</param>
        /// <returns>Alarme criado</returns>
        [HttpPost]
        public async Task<IActionResult> CreateAlarm([FromBody] CreateAlarmRequest request)
        {
            using var activity = _activitySource.StartActivity("AlarmsController.CreateAlarm");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                activity?.SetTag("user.id", request.UserId.ToString());
                activity?.SetTag("alarm.name", request.Name);
                activity?.SetTag("operation", "create_alarm");
                
                _logger.LogInformation("Criando alarme '{Name}' para usuário {UserId} - CorrelationId: {CorrelationId}", 
                    request.Name, request.UserId, _correlationContext.CorrelationId);

                // Usar MediatR para processar o comando
                var command = new CreateAlarmCommand(
                    UserId: request.UserId,
                    Name: request.Name,
                    Time: DateTime.Now.Date.Add(TimeSpan.Parse(request.Time)),
                    Enabled: true,
                    Description: $"Alarme: {request.Name}"
                );

                var response = await _mediator.Send(command);

                // Agendar job em background para o alarme
                var jobId = _backgroundJobClient.Schedule(
                    () => TriggerAlarm(response.AlarmId, response.UserId),
                    response.Time);

                _logger.LogInformation("Job de alarme agendado: {JobId} para {Time}", 
                    jobId, response.Time);

                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "create_alarm", "success", "201");
                
                _logger.LogInformation("Alarme '{Name}' criado com sucesso para usuário {UserId} em {Duration}ms", 
                    request.Name, request.UserId, stopwatch.ElapsedMilliseconds);

                return Created($"/api/v1/alarms/{response.AlarmId}", response);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "create_alarm", "error", "500");
                _meter.IncrementErrorCount("controller", "alarm_creation", "exception");
                
                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);
                
                _logger.LogError(ex, "Erro ao criar alarme '{Name}' para usuário {UserId} - CorrelationId: {CorrelationId}", 
                    request.Name, request.UserId, _correlationContext.CorrelationId);

                return StatusCode(500, new { error = "Erro interno do servidor", correlationId = _correlationContext.CorrelationId });
            }
        }

        /// <summary>
        /// Ativa ou desativa um alarme
        /// </summary>
        /// <param name="alarmId">ID do alarme</param>
        /// <param name="request">Estado desejado</param>
        /// <returns>Status da operação</returns>
        [HttpPatch("{alarmId:guid}/status")]
        public async Task<IActionResult> UpdateAlarmStatus(Guid alarmId, [FromBody] UpdateAlarmStatusRequest request)
        {
            using var activity = _activitySource.StartActivity("AlarmsController.UpdateAlarmStatus");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                activity?.SetTag("alarm.id", alarmId.ToString());
                activity?.SetTag("alarm.active", request.IsActive.ToString());
                activity?.SetTag("operation", "update_alarm_status");
                
                _logger.LogInformation("Atualizando status do alarme {AlarmId} para {Status} - CorrelationId: {CorrelationId}", 
                    alarmId, request.IsActive ? "ativo" : "inativo", _correlationContext.CorrelationId);

                // TODO: Implementar comando real
                // await _mediator.Send(new UpdateAlarmStatusCommand(alarmId, request.IsActive));

                var result = new
                {
                    AlarmId = alarmId,
                    IsActive = request.IsActive,
                    UpdatedAt = DateTime.UtcNow,
                    Message = request.IsActive ? "Alarme ativado com sucesso" : "Alarme desativado com sucesso"
                };

                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "update_alarm_status", "success", "200");
                
                _logger.LogInformation("Status do alarme {AlarmId} atualizado para {Status} em {Duration}ms", 
                    alarmId, request.IsActive ? "ativo" : "inativo", stopwatch.ElapsedMilliseconds);

                return Ok(result);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "update_alarm_status", "error", "500");
                _meter.IncrementErrorCount("controller", "alarm_status_update", "exception");
                
                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);
                
                _logger.LogError(ex, "Erro ao atualizar status do alarme {AlarmId} - CorrelationId: {CorrelationId}", 
                    alarmId, _correlationContext.CorrelationId);

                return StatusCode(500, new { error = "Erro interno do servidor", correlationId = _correlationContext.CorrelationId });
            }
        }

        /// <summary>
        /// Método executado pelo Hangfire para disparar o alarme
        /// </summary>
        [NonAction]
        public async Task TriggerAlarm(Guid alarmId, Guid userId)
        {
            using var activity = _activitySource.StartActivity("AlarmsController.TriggerAlarm");
            
            try
            {
                activity?.SetTag("alarm.id", alarmId.ToString());
                activity?.SetTag("user.id", userId.ToString());
                activity?.SetTag("operation", "trigger_alarm");
                
                _logger.LogInformation("Disparando alarme {AlarmId} para usuário {UserId}", alarmId, userId);

                // TODO: Implementar lógica de disparo real
                // - Comunicar com AI Service para obter recomendações personalizadas
                // - Comunicar com Integration Service para enviar notificações
                // - Registrar evento de disparo
                
                _meter.IncrementAlarmTriggered("scheduled", userId.ToString(), "success");
                
                _logger.LogInformation("Alarme {AlarmId} disparado com sucesso para usuário {UserId}", alarmId, userId);
            }
            catch (Exception ex)
            {
                _meter.IncrementErrorCount("background_job", "alarm_trigger", "exception");
                
                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);
                
                _logger.LogError(ex, "Erro ao disparar alarme {AlarmId} para usuário {UserId}", alarmId, userId);
                throw;
            }
        }

        private static DateTime CalculateNextExecution(string time, string[] daysOfWeek)
        {
            // Lógica simplificada para calcular próxima execução
            if (TimeSpan.TryParse(time, out var timeSpan))
            {
                var today = DateTime.Today;
                var nextExecution = today.Add(timeSpan);
                
                if (nextExecution <= DateTime.Now)
                {
                    nextExecution = nextExecution.AddDays(1);
                }
                
                return nextExecution;
            }
            
            return DateTime.Now.AddDays(1);
        }
    }

    /// <summary>
    /// Modelo para criação de alarme
    /// </summary>
    public class CreateAlarmRequest
    {
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string[] DaysOfWeek { get; set; } = Array.Empty<string>();
        public AlarmSmartFeatures SmartFeatures { get; set; } = new();
    }

    /// <summary>
    /// Funcionalidades inteligentes do alarme
    /// </summary>
    public class AlarmSmartFeatures
    {
        public bool AdaptToCalendar { get; set; }
        public bool WeatherAdjustment { get; set; }
        public bool SleepPatternOptimization { get; set; }
        public bool TrafficConsideration { get; set; }
    }

    /// <summary>
    /// Modelo para atualização de status
    /// </summary>
    public class UpdateAlarmStatusRequest
    {
        public bool IsActive { get; set; }
        public string? Reason { get; set; }
    }
}
