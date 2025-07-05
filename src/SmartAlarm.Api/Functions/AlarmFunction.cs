using System.Threading.Tasks;
using SmartAlarm.Application.Commands;
using SmartAlarm.Application.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using SmartAlarm.Infrastructure.KeyVault;
using MediatR;

namespace SmartAlarm.Api.Functions
{
    /// <summary>
    /// Handler serverless para criação de alarme (OCI Functions).
    /// </summary>
    public class AlarmFunction
    {
        private readonly IMediator _mediator;
        private readonly ILogger<AlarmFunction> _logger;
        private readonly IConfiguration _configuration;
        private readonly IKeyVaultProvider _keyVaultProvider;

        public AlarmFunction(IMediator mediator, ILogger<AlarmFunction> logger, IConfiguration configuration, IKeyVaultProvider keyVaultProvider)
        {
            _mediator = mediator;
            _logger = logger;
            _configuration = configuration;
            _keyVaultProvider = keyVaultProvider;
        }

        /// <summary>
        /// Handler principal para criação de alarme via OCI Function.
        /// </summary>
        public async Task<AlarmResponseDto> HandleAsync(CreateAlarmCommand command)
        {
            _logger.LogInformation("[AlarmFunction] Iniciando criação de alarme para {Name}", command.Alarm?.Name);
            // Exemplo de uso seguro de segredo
            var dbPassword = await _keyVaultProvider.GetSecretAsync("DbPassword");
            // Chama o handler de aplicação
            var result = await _mediator.Send(command);
            _logger.LogInformation("[AlarmFunction] Alarme criado com sucesso: {AlarmId}", result.Id);
            return result;
        }
    }
}
