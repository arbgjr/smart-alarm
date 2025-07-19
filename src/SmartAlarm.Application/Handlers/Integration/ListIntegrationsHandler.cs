using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.DTOs.Integration;
using SmartAlarm.Application.DTOs.Common;
using SmartAlarm.Application.Queries.Integration;
using SmartAlarm.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SmartAlarm.Application.Handlers.Integration
{
    /// <summary>
    /// Handler para listar integrações com suporte a paginação, filtros e ordenação.
    /// </summary>
    public class ListIntegrationsHandler : IRequestHandler<ListIntegrationsQuery, PaginatedResponseDto<IntegrationResponseDto>>
    {
        private readonly IIntegrationRepository _integrationRepository;
        private readonly ILogger<ListIntegrationsHandler> _logger;

        public ListIntegrationsHandler(
            IIntegrationRepository integrationRepository,
            ILogger<ListIntegrationsHandler> logger)
        {
            _integrationRepository = integrationRepository;
            _logger = logger;
        }

        public async Task<PaginatedResponseDto<IntegrationResponseDto>> Handle(ListIntegrationsQuery request, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation("Starting to list integrations with pagination - Page: {Page}, Size: {PageSize}",
                request.Pagination.Page, request.Pagination.PageSize);

            try
            {
                // Validar parâmetros de paginação
                if (!request.Pagination.IsValidOrderDirection)
                {
                    throw new ArgumentException($"Invalid order direction: {request.Pagination.OrderDirection}");
                }

                // Obter todas as integrações (implementaremos paginação no repositório depois)
                var integrations = await _integrationRepository.GetAllAsync();

                // Aplicar filtros
                var filteredIntegrations = integrations.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(request.Provider))
                {
                    filteredIntegrations = filteredIntegrations.Where(i =>
                        i.Provider.Contains(request.Provider, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(request.NameFilter))
                {
                    filteredIntegrations = filteredIntegrations.Where(i =>
                        i.Name.ToString().Contains(request.NameFilter, StringComparison.OrdinalIgnoreCase));
                }

                if (request.AlarmId.HasValue)
                {
                    filteredIntegrations = filteredIntegrations.Where(i => i.AlarmId == request.AlarmId.Value);
                }

                if (request.IsActive.HasValue)
                {
                    filteredIntegrations = filteredIntegrations.Where(i => i.IsActive == request.IsActive.Value);
                }

                // Aplicar ordenação
                var orderedIntegrations = (request.Pagination.OrderBy ?? "name") switch
                {
                    "provider" => (request.Pagination.OrderDirection ?? "asc").Equals("desc", StringComparison.OrdinalIgnoreCase) ?
                        filteredIntegrations.OrderByDescending(i => i.Provider) :
                        filteredIntegrations.OrderBy(i => i.Provider),
                    "name" => (request.Pagination.OrderDirection ?? "asc").Equals("desc", StringComparison.OrdinalIgnoreCase) ?
                        filteredIntegrations.OrderByDescending(i => i.Name.ToString()) :
                        filteredIntegrations.OrderBy(i => i.Name.ToString()),
                    "isActive" => (request.Pagination.OrderDirection ?? "asc").Equals("desc", StringComparison.OrdinalIgnoreCase) ?
                        filteredIntegrations.OrderByDescending(i => i.IsActive) :
                        filteredIntegrations.OrderBy(i => i.IsActive),
                    _ => filteredIntegrations.OrderBy(i => i.Name.ToString()) // ordenação padrão por nome
                };

                var integrationsList = orderedIntegrations.ToList();
                var totalCount = integrationsList.Count;

                // Aplicar paginação
                var pagedIntegrations = integrationsList
                    .Skip(request.Pagination.Skip)
                    .Take(request.Pagination.PageSize)
                    .Select(i => new IntegrationResponseDto
                    {
                        Id = i.Id,
                        Name = i.Name.ToString(),
                        Provider = i.Provider,
                        Configuration = string.IsNullOrWhiteSpace(i.Configuration)
                            ? new Dictionary<string, string>()
                            : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(i.Configuration) ?? new Dictionary<string, string>(),
                        IsActive = i.IsActive
                    })
                    .ToList();

                stopwatch.Stop();

                _logger.LogInformation("Found {TotalCount} integrations, returning {ReturnedCount} for page {Page}",
                    totalCount, pagedIntegrations.Count, request.Pagination.Page);

                return new PaginatedResponseDto<IntegrationResponseDto>(
                    items: pagedIntegrations,
                    totalCount: totalCount,
                    page: request.Pagination.Page,
                    pageSize: request.Pagination.PageSize,
                    orderBy: request.Pagination.OrderBy,
                    orderDirection: request.Pagination.OrderDirection ?? "asc"
                );
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex, "Error listing integrations");

                throw;
            }
        }
    }
}
