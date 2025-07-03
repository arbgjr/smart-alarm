# Artificial Intelligence in Smart Alarm

All AI and behavioral analysis logic in Smart Alarm is implemented in C# using ML.NET. The backend does not depend on Python, Go, or Node.js for AI processing, ensuring security, performance, and native integration with the rest of the .NET stack.

## Technologies Used

- **ML.NET:** Training, validation, and inference of machine learning models in C#
- **Azure Functions:** Serverless deployment of AI services
- **Application Insights:** Monitoring and logging of models in production

## Principles

- Testability and reproducibility of models
- Data security and privacy
- Observability and continuous monitoring

## Notes

- Integrations with Python for AI are only allowed via .NET libraries, never exposing sensitive data outside the C# environment.
