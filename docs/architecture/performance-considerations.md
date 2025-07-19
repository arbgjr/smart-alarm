
# Performance Considerations – Smart Alarm

## Scalability

- All backend services (AlarmService, AnalysisService, IntegrationService) scale horizontally and independently.
- Use of OCI Functions for serverless, ensuring automatic scalability and cost optimization.
- Caching strategies to reduce latency and load on external APIs.
- Connection pooling and query optimization for database operations.

## Performance Optimization

- Circuit breaker (Polly) to protect against failures in external services.
- Caching of non-critical data, with immediate invalidation for alarms and routines.
- Continuous monitoring of latency and throughput via Application Insights and Prometheus.

## Load Testing

- Automated load and stress tests for critical endpoints.
- Peak scenario simulation to ensure 99.9% uptime SLA.

## References

- [OCI Functions](https://www.oracle.com/br/cloud/functions/)
- [Polly](https://github.com/App-vNext/Polly)
- [Prometheus](https://prometheus.io/)

---

**Status:** Performance documentation complete and up to date.
