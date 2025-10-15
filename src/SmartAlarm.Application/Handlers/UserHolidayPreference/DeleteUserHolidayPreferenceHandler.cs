using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Commands.UserHolidayPreference;
using SmartAlarm.Domain.Repositories;

namespace SmartAlarm.Application.Handlers.UserHolidayPreference
{
    /// <summary>
    /// Handler para deletar uma preferência de feriado do usuário.
    /// </summary>
    public class DeleteUserHolidayPreferenceHandler : IRequestHandler<DeleteUserHolidayPreferenceCommand, bool>
    {
        private readonly IUserHolidayPreferenceRepository _userHolidayPreferenceRepository;
        private readonly ILogger<DeleteUserHolidayPreferenceHandler> _logger;

        public DeleteUserHolidayPreferenceHandler(
            IUserHolidayPreferenceRepository userHolidayPreferenceRepository,
            ILogger<DeleteUserHolidayPreferenceHandler> logger)
        {
            _userHolidayPreferenceRepository = userHolidayPreferenceRepository;
            _logger = logger;
        }

        public async Task<bool> Handle(DeleteUserHolidayPreferenceCommand request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.Id == Guid.Empty)
                throw new ArgumentException("ID não pode ser vazio.", nameof(request.Id));

            _logger.LogInformation("Deleting user holiday preference with ID: {PreferenceId}", request.Id);

            // Buscar preferência existente
            var preference = await _userHolidayPreferenceRepository.GetByIdAsync(request.Id);
            if (preference == null)
            {
                _logger.LogWarning("User holiday preference with ID {PreferenceId} not found", request.Id);
                return false;
            }

            await _userHolidayPreferenceRepository.DeleteAsync(preference);

            _logger.LogInformation("Deleted UserHolidayPreference successfully with ID: {PreferenceId}", request.Id);
            return true;
        }
    }
}
