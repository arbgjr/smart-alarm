# [TASK014] - Routine Management API

**Status:** Pending  
**Added:** July 19, 2025  
**Updated:** July 19, 2025

## Original Request
Create REST endpoints for routine CRUD operations to expose existing domain logic through a public API interface. This addresses the highest priority gap (Priority: 10.00) identified in the comprehensive gap analysis.

## Thought Process
After systematic analysis, this emerged as the #1 priority feature because:
- **High User Impact (4/5)**: Core feature needed for routine automation
- **High Strategic Alignment (5/5)**: Central to Smart Alarm's mission
- **Low Implementation Effort (2/5)**: Domain logic already exists, only need API layer
- **Low Risk Level (1/5)**: Well-understood patterns, existing infrastructure
- **Priority Score: 10.00** - Highest score in prioritization matrix

The domain entities and MediatR handlers already exist - we just need to create the REST API controller and DTOs to expose this functionality.

## Implementation Plan

### 1. Create API Controller (2-3 hours)
- Implement `RoutineController` in `src/SmartAlarm.Api/Controllers/`
- Follow existing controller patterns (AlarmController as reference)
- Add proper routing, authentication, and authorization
- Implement all CRUD endpoints with consistent error handling

### 2. Create Query/Response DTOs (1-2 hours)  
- Add Query DTOs in `src/SmartAlarm.Application/Queries/Routine/`
- Add Response DTOs in `src/SmartAlarm.Application/DTOs/Routine/`
- Ensure proper validation using FluentValidation patterns
- Map between domain entities and DTOs

### 3. Add Comprehensive Tests (3-4 hours)
- Unit tests for controller actions and validation
- Integration tests for end-to-end API functionality
- Test authentication/authorization scenarios
- Test error handling and edge cases

### 4. Update Documentation (1 hour)
- OpenAPI documentation will be generated automatically
- Update any relevant API documentation
- Add examples and usage notes

## Progress Tracking

**Overall Status:** Pending - 0%

### Subtasks
| ID | Description | Status | Updated | Notes |
|----|-------------|--------|---------|-------|
| 14.1 | Create RoutineController with all CRUD endpoints | Not Started | - | Follow AlarmController patterns |
| 14.2 | Implement Query DTOs (GetRoutineByIdQuery, ListRoutinesQuery) | Not Started | - | Use existing MediatR patterns |
| 14.3 | Create Response DTOs with proper validation | Not Started | - | Consistent with other API DTOs |
| 14.4 | Add comprehensive unit tests (>90% coverage) | Not Started | - | Test all endpoints and scenarios |
| 14.5 | Add integration tests for end-to-end validation | Not Started | - | Use TestContainers if needed |
| 14.6 | Verify OpenAPI documentation generation | Not Started | - | Ensure proper Swagger docs |

## Technical Requirements
- **Framework**: ASP.NET Core 8 with existing Clean Architecture patterns
- **Authentication**: JWT Bearer token authentication (existing infrastructure)
- **Authorization**: User-scoped access (users see only their routines, admins see all)
- **Validation**: FluentValidation for all input DTOs
- **Observability**: OpenTelemetry tracing and Serilog structured logging
- **Testing**: xUnit with Moq, TestContainers for integration tests

## Acceptance Criteria
- [ ] GET /api/v1/routines - List user's routines with pagination
- [ ] POST /api/v1/routines - Create new routine with validation
- [ ] GET /api/v1/routines/{id} - Get routine by ID with proper authorization
- [ ] PUT /api/v1/routines/{id} - Update routine with validation and authorization  
- [ ] DELETE /api/v1/routines/{id} - Delete routine with proper authorization
- [ ] All endpoints require JWT authentication
- [ ] Users can only access their own routines (role-based authorization)
- [ ] Comprehensive error handling with consistent response format
- [ ] Unit tests achieve >90% code coverage
- [ ] Integration tests validate complete API functionality
- [ ] OpenAPI documentation automatically generated

## Dependencies
- ✅ Domain entities already exist (Routine, RoutineAction, etc.)
- ✅ Application handlers already implemented (CreateRoutineCommand, etc.)
- ✅ Authentication infrastructure already in place
- ✅ Validation infrastructure (FluentValidation) already configured
- ✅ Observability stack already implemented

## Estimated Effort
**Total: 6-9 hours (1-2 days)**
- Implementation: 3-5 hours
- Testing: 3-4 hours  
- Documentation/Polish: 1 hour

## Progress Log
### July 19, 2025
- Task created based on comprehensive gap analysis
- Identified as highest priority feature (Priority: 10.00)
- Implementation plan developed with clear acceptance criteria
- Ready for development start
