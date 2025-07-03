# AI Service - Smart Alarm

The AI Service is responsible for all artificial intelligence logic and behavioral analysis of Smart Alarm, being implemented exclusively in C#/.NET, using ML.NET and following Clean Architecture, SOLID, and modern practices for testability and security.

## Technologies and Standards

- **C# (.NET 8+):** All AI logic
- **ML.NET:** Model training and inference
- **Azure Functions:** Serverless deployment
- **Structured logging:** Serilog and Application Insights
- **Automated tests:** xUnit, Moq

## Notes

- There are no dependencies on Go, Python, or Node.js
- Integrations with Python for AI are only allowed via .NET libraries, never exposing sensitive data outside the C# environment
- The entire backend is testable, secure, and observable
