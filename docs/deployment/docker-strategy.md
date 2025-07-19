# Docker Containerization for Smart Alarm Microservices

## Overview
This document outlines the containerization strategy for the Smart Alarm microservices architecture implemented in FASE 6.

## Services to Containerize

### 1. AlarmService
- **Port**: 5001
- **Dependencies**: PostgreSQL, SmartAlarm.Observability
- **Features**: Hangfire background jobs, CQRS handlers, health checks

### 2. AI Service  
- **Port**: 5002
- **Dependencies**: SmartAlarm.Observability
- **Features**: ML pattern analysis, optimal time prediction, CQRS handlers

### 3. Integration Service
- **Port**: 5003
- **Dependencies**: SmartAlarm.Observability, External APIs
- **Features**: Calendar sync, provider integrations, CQRS handlers

## Docker Strategy

### Base Images
- **Runtime**: mcr.microsoft.com/dotnet/aspnet:8.0 (optimized for production)
- **Build**: mcr.microsoft.com/dotnet/sdk:8.0 (for compilation)

### Multi-stage Build Pattern
1. **Build Stage**: Restore packages, compile, test
2. **Runtime Stage**: Copy binaries, configure runtime

### Environment Configuration
- **Development**: Docker Compose with all dependencies
- **Production**: Kubernetes manifests with secrets management

### Observability Integration
- All containers will include SmartAlarm.Observability
- Health checks exposed on /health endpoints
- Metrics scraped by Prometheus
- Logs forwarded to centralized logging

## Next Steps
1. Create Dockerfiles for each service
2. Create Docker Compose for local development
3. Create Kubernetes manifests for production
4. Set up GitHub Actions CI/CD pipeline
