using FluentValidation;
using SmartAlarm.Application.Queries.User;

namespace SmartAlarm.Application.Validators.User
{
    public class ListUsersQueryValidator : AbstractValidator<ListUsersQuery>
    {
        public ListUsersQueryValidator()
        {
            // Nenhuma validação obrigatória
        }
    }
}
