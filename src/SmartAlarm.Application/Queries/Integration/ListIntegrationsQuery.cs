using MediatR;
using SmartAlarm.Application.DTOs.Integration;
using System.Collections.Generic;

namespace SmartAlarm.Application.Queries.Integration
{
    public class ListIntegrationsQuery : IRequest<List<IntegrationResponseDto>>
    {
    }
}
