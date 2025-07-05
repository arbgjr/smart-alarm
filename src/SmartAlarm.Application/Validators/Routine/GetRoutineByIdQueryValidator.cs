using FluentValidation;
using SmartAlarm.Application.Queries.Routine;

namespace SmartAlarm.Application.Validators.Routine
{
    public class GetRoutineByIdQueryValidator : AbstractValidator<GetRoutineByIdQuery>
    {
        public GetRoutineByIdQueryValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
