# Planning

## The steps to start the development of Smart Alarm, following Clean Architecture, SOLID, and the patterns defined in the project are

1. **Domain Definition (Domain Layer)**
   - Model main entities: Alarm, Routine, User, Integration. ✅
   - Define repository interfaces (e.g., IAlarmRepository, IUserRepository). ✅
   - Create Value Objects and essential business rules. ✅

2. **Application Layer**
   - Implement the first Use Cases (e.g., CreateAlarm, ListAlarms). ✅
   - Define DTOs for input/output. ✅
   - Create Handlers and commands/queries (e.g., CreateAlarmCommand, ListAlarmsQuery). ✅

3. **Validation and Error Handling**
   - Configure FluentValidation for main commands. ⚠️ (Initial configuration done, but full coverage of all commands still pending)
   - Implement exception handling and structured logging (Serilog). ✅

4. **Initial Infrastructure**
   - Implement fake/in-memory repositories for testing. ✅
   - Structure database integration (Autonomous DB) via interfaces. ⚠️ (Initial integration done, but production tests and validation still pending)

5. **API (Presentation Layer)**
   - Create basic controllers for Alarm (e.g., POST /alarms, GET /alarms). ⚠️ (Controllers structured, but main endpoints not yet implemented)
   - Configure Swagger/OpenAPI for automatic documentation. ⚠️ (Initial configuration done, but full endpoint documentation still pending)

6. **Automated Tests**
   - Write unit tests for the main use cases (xUnit, Moq). ✅ (Cobertura mínima atingida para camada de aplicação; cobertura de API/integrada segue em progresso)
   - Cover success, error, and edge scenarios. ✅ (Principais cenários cobertos na camada de aplicação)
   - Transaction rollback test temporarily disabled due to SQLite in-memory limitations. Registered as tech debt; will be re-enabled with real integration tests.

7. **Basic Security**
   - Structure JWT authentication (without external integration initially). ⚠️ (Structure planned, implementation pending)
   - Ensure endpoints are protected. ⚠️ (Endpoint protection depends on JWT implementation)

8. **Documentation**
   - Document initial architecture, endpoints, and decisions in the docs directory. ⚠️ (Initial documentation created, detailed documentation of endpoints and decisions still pending)

9. **Infrastructure Layer**
   - Implements concrete repositories (e.g., AlarmRepository) based on domain interfaces. ✅
   - Handles integration with external services: databases (Autonomous DB), messaging, storage, logging, tracing, and metrics. ⚠️ (Autonomous DB integration done, other integrations and production tests still pending)
   - Provides dependency injection configuration for all infrastructure services. ✅
   - Ensures infrastructure is decoupled from domain and application, allowing easy replacement or mocking for tests. ✅
   - Follows Clean Architecture: no direct dependencies from Domain/Application to Infrastructure. ✅

> Note: The Infrastructure Layer is a fundamental part of Clean Architecture. It must be explicitly designed and documented, ensuring all external integrations and technical concerns are isolated from business logic.

These steps follow exactly the flow recommended in the instruction files, Memory Bank, and project standards. If you wish, I can detail the execution plan for each step or start implementing a specific item. Would you like to proceed to detailed planning or start executing any of these steps?
