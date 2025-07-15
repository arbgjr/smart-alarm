using FluentValidation;
using SmartAlarm.Application.Commands.UserHolidayPreference;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Abstractions;

namespace SmartAlarm.Application.Validators.UserHolidayPreference
{
    /// <summary>
    /// Validator para UpdateUserHolidayPreferenceCommand.
    /// </summary>
    public class UpdateUserHolidayPreferenceCommandValidator : AbstractValidator<UpdateUserHolidayPreferenceCommand>
    {
        private readonly IUserHolidayPreferenceRepository _userHolidayPreferenceRepository;

        public UpdateUserHolidayPreferenceCommandValidator(IUserHolidayPreferenceRepository userHolidayPreferenceRepository)
        {
            _userHolidayPreferenceRepository = userHolidayPreferenceRepository;

            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("ID da preferência é obrigatório.");

            RuleFor(x => x.Action)
                .IsInEnum()
                .WithMessage("Ação deve ser um valor válido (Disable, Delay, Skip).");

            RuleFor(x => x.DelayInMinutes)
                .Must((command, delayInMinutes) => ValidateDelayInMinutes(command.Action, delayInMinutes))
                .WithMessage("DelayInMinutes é obrigatório e deve estar entre 1 e 1440 quando a ação for Delay.");

            RuleFor(x => x.Id)
                .MustAsync(async (id, cancellation) => await PreferenceExists(id))
                .WithMessage("Preferência não encontrada.");
        }

        private static bool ValidateDelayInMinutes(HolidayPreferenceAction action, int? delayInMinutes)
        {
            if (action == HolidayPreferenceAction.Delay)
            {
                return delayInMinutes.HasValue && delayInMinutes > 0 && delayInMinutes <= 1440;
            }
            return true; // Para outras ações, DelayInMinutes é opcional
        }

        private async Task<bool> PreferenceExists(Guid id)
        {
            var preference = await _userHolidayPreferenceRepository.GetByIdAsync(id);
            return preference != null;
        }
    }
}
