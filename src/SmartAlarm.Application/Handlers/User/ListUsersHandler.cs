using MediatR;
using SmartAlarm.Application.DTOs.User;
using SmartAlarm.Application.Queries.User;
using SmartAlarm.Domain.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SmartAlarm.Application.Handlers.User
{
    public class ListUsersHandler : IRequestHandler<ListUsersQuery, List<UserResponseDto>>
    {
        private readonly IUserRepository _userRepository;

        public ListUsersHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<List<UserResponseDto>> Handle(ListUsersQuery request, CancellationToken cancellationToken)
        {
            // Não existe GetAllAsync, então precisamos de uma abordagem alternativa.
            // Exemplo: retornar lista vazia ou lançar NotImplementedException.
            // TODO: Implementar método de listagem de usuários conforme domínio.
            // ReSharper disable once AsyncMethodWithoutAwait
            throw new NotImplementedException("Listagem de usuários não implementada no repositório.");
        }
    }
}
