using FluentValidation;
using SmartAlarm.Application.Commands.UserHolidayPreference;
using SmartAlarm.Domain.Abstractions;

namespace SmartAlarm.Application.Validators.UserHolidayPreference
{
    /// <summary>
    /// Validator para DeleteUserHolidayPreferenceCommand.
    /// </summary>
    public class DeleteUserHolidayPreferenceCommandValidator : AbstractValidator<DeleteUserHolidayPreferenceCommand>
    {
        private readonly IUserHolidayPreferenceRepository _userHolidayPreferenceRepository;

        public DeleteUserHolidayPreferenceCommandValidator(IUserHolidayPreferenceRepository userHolidayPreferenceRepository)
        {
            _userHolidayPreferenceRepository = userHolidayPreferenceRepository;

            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("ID da preferência é obrigatório.")
                .MustAsync(async (id, cancellation) => await PreferenceExists(id))
                .WithMessage("Preferência não encontrada.");
        }

        private async Task<bool> PreferenceExists(Guid id)
        {
            var preference = await _userHolidayPreferenceRepository.GetByIdAsync(id);
            return preference != null;
        }
    }
}
