using FluentValidation;
using SmartAlarm.Application.Queries.Routine;

namespace SmartAlarm.Application.Validators.Routine
{
    public class ListRoutinesQueryValidator : AbstractValidator<ListRoutinesQuery>
    {
        public ListRoutinesQueryValidator()
        {
            // Nenhuma validação obrigatória, mas pode ser expandida futuramente
        }
    }
}
