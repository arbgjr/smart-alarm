using FluentValidation;
using SmartAlarm.Application.Commands.UserHolidayPreference;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Abstractions;

namespace SmartAlarm.Application.Validators.UserHolidayPreference
{
    /// <summary>
    /// Validator para CreateUserHolidayPreferenceCommand.
    /// </summary>
    public class CreateUserHolidayPreferenceCommandValidator : AbstractValidator<CreateUserHolidayPreferenceCommand>
    {
        private readonly IUserHolidayPreferenceRepository _userHolidayPreferenceRepository;

        public CreateUserHolidayPreferenceCommandValidator(IUserHolidayPreferenceRepository userHolidayPreferenceRepository)
        {
            _userHolidayPreferenceRepository = userHolidayPreferenceRepository;

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("ID do usuário é obrigatório.");

            RuleFor(x => x.HolidayId)
                .NotEmpty()
                .WithMessage("ID do feriado é obrigatório.");

            RuleFor(x => x.Action)
                .IsInEnum()
                .WithMessage("Ação deve ser um valor válido (Disable, Delay, Skip).");

            RuleFor(x => x.DelayInMinutes)
                .Must((command, delayInMinutes) => ValidateDelayInMinutes(command.Action, delayInMinutes))
                .WithMessage("DelayInMinutes é obrigatório e deve estar entre 1 e 1440 quando a ação for Delay.");

            RuleFor(x => x)
                .MustAsync(async (command, cancellation) => await NotHaveDuplicatePreference(command.UserId, command.HolidayId))
                .WithMessage("Já existe uma preferência para este usuário e feriado.");
        }

        private static bool ValidateDelayInMinutes(HolidayPreferenceAction action, int? delayInMinutes)
        {
            if (action == HolidayPreferenceAction.Delay)
            {
                return delayInMinutes.HasValue && delayInMinutes > 0 && delayInMinutes <= 1440;
            }
            return true; // Para outras ações, DelayInMinutes é opcional
        }

        private async Task<bool> NotHaveDuplicatePreference(Guid userId, Guid holidayId)
        {
            var existingPreference = await _userHolidayPreferenceRepository.GetByUserAndHolidayAsync(userId, holidayId);
            return existingPreference == null;
        }
    }
}
