using MediatR;
using SmartAlarm.Application.DTOs.Integration;
using SmartAlarm.Application.Queries.Integration;
using SmartAlarm.Domain.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SmartAlarm.Application.Handlers.Integration
{
    public class ListIntegrationsHandler : IRequestHandler<ListIntegrationsQuery, List<IntegrationResponseDto>>
    {
        private readonly IIntegrationRepository _integrationRepository;

        public ListIntegrationsHandler(IIntegrationRepository integrationRepository)
        {
            _integrationRepository = integrationRepository;
        }

        public async Task<List<IntegrationResponseDto>> Handle(ListIntegrationsQuery request, CancellationToken cancellationToken)
        {
            // Não existe GetAllAsync, então precisamos de uma abordagem alternativa.
            // Exemplo: retornar lista vazia ou lançar NotImplementedException.
            // TODO: Implementar método de listagem de integrações conforme domínio.
            // ReSharper disable once AsyncMethodWithoutAwait
            throw new NotImplementedException("Listagem de integrações não implementada no repositório.");
        }
    }
}
