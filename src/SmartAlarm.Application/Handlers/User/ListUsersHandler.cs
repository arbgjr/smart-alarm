using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.DTOs.User;
using SmartAlarm.Application.DTOs.Common;
using SmartAlarm.Application.Queries.User;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Logging;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SmartAlarm.Application.Handlers.User
{
    public class ListUsersHandler : IRequestHandler<ListUsersQuery, PaginatedResponseDto<UserResponseDto>>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<ListUsersHandler> _logger;
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly SmartAlarmMeter _meter;
        private readonly BusinessMetrics _businessMetrics;
        private readonly ICorrelationContext _correlationContext;

        public ListUsersHandler(
            IUserRepository userRepository,
            ILogger<ListUsersHandler> logger,
            SmartAlarmActivitySource activitySource,
            SmartAlarmMeter meter,
            BusinessMetrics businessMetrics,
            ICorrelationContext correlationContext)
        {
            _userRepository = userRepository;
            _logger = logger;
            _activitySource = activitySource;
            _meter = meter;
            _businessMetrics = businessMetrics;
            _correlationContext = correlationContext;
        }

        public async Task<PaginatedResponseDto<UserResponseDto>> Handle(ListUsersQuery request, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var correlationId = _correlationContext.CorrelationId;
            
            _logger.LogDebug(LogTemplates.QueryStarted, 
                nameof(ListUsersQuery), 
                new { Page = request.Pagination.Page, PageSize = request.Pagination.PageSize, IsActive = request.IsActive, EmailFilter = request.EmailFilter });

            using var activity = _activitySource.StartActivity("ListUsersHandler.Handle");
            activity?.SetTag("correlation.id", correlationId);
            activity?.SetTag("operation", "ListUsers");
            activity?.SetTag("handler", "ListUsersHandler");
            activity?.SetTag("pagination.page", request.Pagination.Page.ToString());
            activity?.SetTag("pagination.pageSize", request.Pagination.PageSize.ToString());
            activity?.SetTag("filter.isActive", request.IsActive?.ToString() ?? "null");
            activity?.SetTag("filter.email", request.EmailFilter ?? "null");
            
            try
            {
                // Validar parâmetros de paginação
                if (!request.Pagination.IsValidOrderDirection)
                {
                    throw new ArgumentException($"Invalid order direction: {request.Pagination.OrderDirection}");
                }

                // Obter todos os usuários (vamos implementar paginação no repositório depois)
                var allUsers = await _userRepository.GetAllAsync();
                
                // Aplicar filtros
                var filteredUsers = allUsers.AsEnumerable();
                
                if (request.IsActive.HasValue)
                {
                    filteredUsers = filteredUsers.Where(u => u.IsActive == request.IsActive.Value);
                }
                
                if (!string.IsNullOrWhiteSpace(request.EmailFilter))
                {
                    filteredUsers = filteredUsers.Where(u => 
                        u.Email.ToString().Contains(request.EmailFilter, StringComparison.OrdinalIgnoreCase));
                }

                // Aplicar ordenação
                var orderedUsers = request.Pagination.OrderBy switch
                {
                    "email" => request.Pagination.OrderDirection.Equals("desc", StringComparison.OrdinalIgnoreCase) ?
                        filteredUsers.OrderByDescending(u => u.Email.ToString()) :
                        filteredUsers.OrderBy(u => u.Email.ToString()),
                    "name" => request.Pagination.OrderDirection.Equals("desc", StringComparison.OrdinalIgnoreCase) ?
                        filteredUsers.OrderByDescending(u => u.Name.ToString()) :
                        filteredUsers.OrderBy(u => u.Name.ToString()),
                    _ => filteredUsers.OrderBy(u => u.Id) // ordenação padrão por ID
                };

                var usersList = orderedUsers.ToList();
                var totalCount = usersList.Count;

                // Aplicar paginação
                var pagedUsers = usersList
                    .Skip(request.Pagination.Skip)
                    .Take(request.Pagination.PageSize)
                    .Select(u => new UserResponseDto
                    {
                        Id = u.Id,
                        Name = u.Name.ToString(),
                        Email = u.Email.ToString(),
                        IsActive = u.IsActive
                    })
                    .ToList();
                
                stopwatch.Stop();
                activity?.SetTag("users.total_count", totalCount.ToString());
                activity?.SetTag("users.returned_count", pagedUsers.Count.ToString());
                activity?.SetTag("users.active", pagedUsers.Count(u => u.IsActive).ToString());
                activity?.SetStatus(ActivityStatusCode.Ok);
                
                // Métricas técnicas
                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "ListUsers", "Users");
                
                // Métricas de negócio
                _businessMetrics.UpdateUsersActiveToday(pagedUsers.Count(u => u.IsActive));
                
                _logger.LogDebug(LogTemplates.QueryCompleted, 
                    nameof(ListUsersQuery), 
                    stopwatch.ElapsedMilliseconds,
                    pagedUsers.Count);

                _logger.LogInformation(LogTemplates.BusinessEventOccurred,
                    "UsersListed",
                    new { TotalCount = totalCount, ReturnedCount = pagedUsers.Count, ActiveCount = pagedUsers.Count(u => u.IsActive), Page = request.Pagination.Page },
                    correlationId);

                return new PaginatedResponseDto<UserResponseDto>(
                    items: pagedUsers,
                    totalCount: totalCount,
                    page: request.Pagination.Page,
                    pageSize: request.Pagination.PageSize,
                    orderBy: request.Pagination.OrderBy,
                    orderDirection: request.Pagination.OrderDirection
                );
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("QUERY", "Users", "ListError");
                
                _logger.LogError(LogTemplates.QueryFailed,
                    nameof(ListUsersQuery),
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                
                throw;
            }
        }
    }
}
