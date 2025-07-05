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
            var integrations = await _integrationRepository.GetAllAsync();
            return integrations.Select(i => new IntegrationResponseDto
            {
                Id = i.Id,
                Name = i.Name.ToString(),
                Provider = i.Provider,
                Configuration = string.IsNullOrWhiteSpace(i.Configuration)
                    ? new Dictionary<string, string>()
                    : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(i.Configuration) ?? new Dictionary<string, string>(),
                IsActive = i.IsActive
            }).ToList();
        }
    }
}
