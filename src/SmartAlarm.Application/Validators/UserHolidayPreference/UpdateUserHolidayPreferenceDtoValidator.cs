using FluentValidation;
using SmartAlarm.Application.DTOs.UserHolidayPreference;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Application.Validators.UserHolidayPreference
{
    /// <summary>
    /// Validator para UpdateUserHolidayPreferenceDto.
    /// </summary>
    public class UpdateUserHolidayPreferenceDtoValidator : AbstractValidator<UpdateUserHolidayPreferenceDto>
    {
        public UpdateUserHolidayPreferenceDtoValidator()
        {
            RuleFor(x => x.Action)
                .IsInEnum()
                .WithMessage("Ação deve ser um valor válido (Disable, Delay, Skip).");

            RuleFor(x => x.DelayInMinutes)
                .Must((dto, delayInMinutes) => ValidateDelayInMinutes(dto.Action, delayInMinutes))
                .WithMessage("DelayInMinutes é obrigatório e deve estar entre 1 e 1440 quando a ação for Delay.");
        }

        private static bool ValidateDelayInMinutes(HolidayPreferenceAction action, int? delayInMinutes)
        {
            if (action == HolidayPreferenceAction.Delay)
            {
                return delayInMinutes.HasValue && delayInMinutes > 0 && delayInMinutes <= 1440;
            }
            return true; // Para outras ações, DelayInMinutes é opcional
        }
    }
}
