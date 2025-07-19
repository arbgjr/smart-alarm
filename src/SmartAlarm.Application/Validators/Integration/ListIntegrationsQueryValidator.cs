using FluentValidation;
using SmartAlarm.Application.Queries.Integration;

namespace SmartAlarm.Application.Validators.Integration
{
    public class ListIntegrationsQueryValidator : AbstractValidator<ListIntegrationsQuery>
    {
        public ListIntegrationsQueryValidator()
        {
            // Nenhuma validação obrigatória
        }
    }
}
