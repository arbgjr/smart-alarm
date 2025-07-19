# Alarm Service - Smart Alarm

The Alarm Service is responsible for all alarm operations, business rules, and notifications, being implemented exclusively in C#/.NET, following Clean Architecture, SOLID, and modern practices for testability and security.

## Technologies and Standards

- **C# (.NET 8+):** All alarm logic
- **Entity Framework Core:** Data persistence
- **Azure Functions:** Serverless deployment
- **FluentValidation:** Input/output validation
- **Structured logging:** Serilog and Application Insights
- **Automated tests:** xUnit, Moq

## Notes

- There are no dependencies on Go, Python, or Node.js
- The entire backend is testable, secure, and observable
