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

3. **Validation and Error Handling** ✅
   - Configure FluentValidation for main commands. ✅ (Full validation coverage for all main commands and DTOs, including standardized error response.)
   - Implement exception handling and structured logging (Serilog). ✅

4. **API (Presentation Layer)**
   - Create basic controllers for Alarm (e.g., POST /alarms, GET /alarms). ✅ (Main endpoints implemented, tested and validated with automated coverage)
   - Configure Swagger/OpenAPI for automatic documentation. ✅ (Swagger UI available and configured)
   - Provide detailed endpoint documentation (Swagger/OpenAPI, markdown, or equivalent). ✅ (See: docs/api/alarms.endpoints.md for full reference)

5. **Basic Security**
   - Structure JWT authentication (without external integration initially). ✅ (Implemented: JWT authentication, login endpoint, DTOs, validation, and tests)
   - Ensure endpoints are protected. ✅ (All sensitive endpoints protected with [Authorize], covered by automated tests)

6. **Infrastructure Layer**
   - Implements concrete repositories (e.g., AlarmRepository) based on domain interfaces. ✅
   - Handles integration with external services: databases (Autonomous DB), messaging, storage, logging, tracing, and metrics. ⚠️ (Autonomous DB integration ready; other integrations and production tests documented as tech debt)
   - Add tracing to routines and layers with potential bottlenecks. ⚠️ (Detailed instrumentation pending, registered as tech debt)
   - Provides dependency injection configuration for all infrastructure services. ✅
   - Ensures infrastructure is decoupled from domain and application, allowing easy replacement or mocking for tests. ✅
   - Follows Clean Architecture: no direct dependencies from Domain/Application to Infrastructure. ✅
   - **Pending for production**: real integrations for messaging, storage, tracing, metrics, integrated tests, and detailed documentation — all registered in `docs/tech-debt/techDebt.md`.

7. **Automated Tests**
   - Write unit tests for the main use cases (xUnit, Moq). ✅ (Minimum coverage achieved for application and API; integration coverage in progress)
   - Cover success, error, and edge scenarios. ✅ (Main scenarios covered in the application layer)
   - Transaction rollback test temporarily disabled due to SQLite in-memory limitations. Registered as tech debt; will be re-enabled with real integration tests.

8. **Documentation**
   - Document initial architecture, endpoints, and decisions in the docs directory. ✅

9. **Initial Infrastructure**

- Implement fake/in-memory repositories for testing. ✅
- Structure database integration (Autonomous DB) via interfaces. ⚠️ (Initial integration done, but production tests and validation still pending)5.

> Note: The Infrastructure Layer is a fundamental part of Clean Architecture. It must be explicitly designed and documented, ensuring all external integrations and technical concerns are isolated from business logic.

These steps follow exactly the flow recommended in the instruction files, Memory Bank, and project standards. If you wish, I can detail the execution plan for each step or start implementing a specific item. Would you like to proceed to detailed planning or start executing any of these steps?
