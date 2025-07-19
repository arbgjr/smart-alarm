using FluentValidation;
using SmartAlarm.Application.Queries.User;

namespace SmartAlarm.Application.Validators.User
{
    public class GetUserByIdQueryValidator : AbstractValidator<GetUserByIdQuery>
    {
        public GetUserByIdQueryValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
