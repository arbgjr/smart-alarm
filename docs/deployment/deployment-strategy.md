# Smart Alarm - Deployment Strategy

## Visão Geral

Esta documentação descreve a estratégia completa de deployment para o Smart Alarm, incluindo containerização, orquestração e CI/CD.

## Arquitetura de Deployment

### Microserviços

O Smart Alarm é composto por 3 microserviços principais:

1. **Alarm Service** - Gestão de alarmes e agendamentos
2. **AI Service** - Análise inteligente e recomendações
3. **Integration Service** - Integrações externas (Spotify, Weather, etc.)

### Estratégia de Containerização

#### Docker Multi-stage Builds

Todos os serviços utilizam builds multi-stage para otimização:

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
# ... build process

# Runtime stage  
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
# ... production runtime
```

**Benefícios:**
- Imagens menores em produção
- Build reproduzível
- Separação de dependências de build/runtime

#### Segurança

- **Non-root execution**: Usuário dedicado `smartalarm:1001`
- **Read-only filesystem**: Proteção contra modificações
- **Capabilities drop**: Remoção de capabilities desnecessárias
- **Health checks**: Monitoramento automático de saúde

## Ambientes

### Development

**Orquestração:** Docker Compose
**Arquivo:** `docker-compose.services.yml`

```bash
docker-compose -f docker-compose.services.yml up -d
```

**Características:**
- Hot reload habilitado
- Logs detalhados
- Debugging ports expostos
- Configuração simplificada

### Production

**Orquestração:** Kubernetes
**Manifests:** `infrastructure/kubernetes/`

```bash
./infrastructure/scripts/deploy-k8s.sh production
```

**Características:**
- Auto-scaling (HPA)
- Rolling updates
- SSL/TLS termination
- Monitoring integrado

## CI/CD Pipeline

### GitHub Actions Workflow

**Arquivo:** `.github/workflows/ci-cd.yml`

#### Stages

1. **Build & Test**
   - Restore dependencies
   - Build solution
   - Run unit tests
   - Run integration tests
   - Generate coverage reports

2. **Security Scan**
   - Trivy vulnerability scanner
   - SARIF upload para GitHub Security

3. **Build Images**
   - Multi-platform builds (amd64/arm64)
   - Registry push (GHCR)
   - Image caching

4. **Deploy**
   - Development (branch: develop)
   - Production (branch: main)

### Triggers

- **Push** para `main` ou `develop`
- **Pull Request** para `main`
- **Paths:** Apenas mudanças relevantes

## Kubernetes Configuration

### Namespace Structure

```yaml
apiVersion: v1
kind: Namespace
metadata:
  name: smartalarm
```

### ConfigMaps & Secrets

**ConfigMap:** Configurações não-sensíveis
```yaml
data:
  observability.otlp.endpoint: "http://jaeger:14268/api/traces"
  rabbitmq.host: "rabbitmq-service"
  storage.endpoint: "minio-service:9000"
```

**Secrets:** Dados sensíveis
```yaml
data:
  database.connection: <base64>
  rabbitmq.password: <base64>
  openai.apikey: <base64>
```

### Service Configuration

#### Alarm Service
- **Replicas:** 3
- **Resources:** 256Mi-512Mi RAM, 100m-500m CPU
- **Scaling:** 3-10 pods baseado em CPU/Memory

#### AI Service
- **Replicas:** 2
- **Resources:** 512Mi-1Gi RAM, 250m-1000m CPU
- **Scaling:** 2-10 pods (workloads ML)

#### Integration Service
- **Replicas:** 3
- **Resources:** 256Mi-512Mi RAM, 100m-500m CPU
- **Scaling:** 3-15 pods (alta demanda de integrações)

### Ingress & Load Balancing

**Hostnames:**
- `alarms.smartalarm.local` → Alarm Service
- `ai.smartalarm.local` → AI Service
- `integrations.smartalarm.local` → Integration Service

**Features:**
- SSL/TLS termination
- Rate limiting
- Path-based routing

## Monitoring & Observability

### Health Checks

Todos os serviços expõem:
- `/health` - Liveness probe
- `/health/ready` - Readiness probe

### Logging

**Structured Logging** com Serilog:
```json
{
  "timestamp": "2025-01-12T10:00:00Z",
  "level": "Information",
  "message": "Alarm created successfully",
  "properties": {
    "AlarmId": "123e4567-e89b-12d3-a456-426614174000",
    "UserId": "user-123",
    "CorrelationId": "req-456"
  }
}
```

### Metrics

**OpenTelemetry** integration:
- Custom meters (`SmartAlarmMeter`)
- Activity sources (`SmartAlarmActivitySource`)
- Performance counters
- Business metrics

## Scripts de Deployment

### Linux/MacOS
```bash
# Deploy com verificações
./infrastructure/scripts/deploy-k8s.sh production

# Dry-run para validação
./infrastructure/scripts/deploy-k8s.sh production --dry-run
```

### Windows
```powershell
# Deploy com verificações
.\infrastructure\scripts\deploy-k8s.ps1 -Environment production

# Dry-run para validação  
.\infrastructure\scripts\deploy-k8s.ps1 -Environment production -DryRun
```

### Features dos Scripts

- **Pre-flight checks**: kubectl, cluster connectivity
- **Health verification**: Verificação pós-deploy
- **Status reporting**: Informações de acesso
- **Error handling**: Rollback automático em falhas
- **Colored output**: Feedback visual detalhado

## Rollback Strategy

### Kubernetes Rolling Updates

```bash
# Verificar status do rollout
kubectl rollout status deployment/alarm-service -n smartalarm

# Rollback para versão anterior
kubectl rollout undo deployment/alarm-service -n smartalarm

# Rollback para versão específica
kubectl rollout undo deployment/alarm-service --to-revision=2 -n smartalarm
```

### Docker Tags

**Estratégia de versionamento:**
- `latest` - Última versão estável (main branch)
- `develop` - Versão de desenvolvimento
- `sha-{commit}` - Versão específica do commit
- `v{version}` - Releases taggeadas

## Performance & Scaling

### Horizontal Pod Autoscaler (HPA)

**Métricas:**
- CPU: 70% threshold
- Memory: 80% threshold

**Behavior:**
- **Scale Up**: 100% increase a cada 15s
- **Scale Down**: 10% decrease a cada 60s
- **Stabilization**: 5 minutos

### Resource Optimization

**Requests vs Limits:**
- **Requests**: Garantia mínima de recursos
- **Limits**: Máximo permitido (evita resource starvation)

**Memory:**
- Alarm Service: 256Mi-512Mi
- AI Service: 512Mi-1Gi (ML workloads)
- Integration Service: 256Mi-512Mi

## Security Best Practices

### Container Security

- **Non-root execution**: UID/GID 1001
- **Read-only filesystem**: Prevenção de modificações
- **Minimal attack surface**: Imagens Alpine Linux
- **No secrets in images**: Configuração via environment

### Kubernetes Security

- **RBAC**: Service accounts com permissões mínimas
- **Network Policies**: Isolamento de tráfego
- **Pod Security Standards**: Restricted profile
- **Secrets management**: Encrypted at rest

### CI/CD Security

- **Vulnerability scanning**: Trivy integration
- **Dependency scanning**: GitHub Dependabot
- **SARIF reports**: Security findings integration
- **Private registry**: GHCR com autenticação

## Troubleshooting

### Common Issues

**Pod CrashLoopBackOff:**
```bash
kubectl logs -f pod/alarm-service-xxx -n smartalarm
kubectl describe pod alarm-service-xxx -n smartalarm
```

**Service não acessível:**
```bash
kubectl get svc -n smartalarm
kubectl port-forward svc/alarm-service 8080:80 -n smartalarm
```

**Database connectivity:**
```bash
kubectl exec -it deployment/alarm-service -n smartalarm -- env | grep CONNECTION
```

### Debugging Commands

```bash
# Status geral
kubectl get all -n smartalarm

# Logs em tempo real
kubectl logs -f deployment/alarm-service -n smartalarm

# Métricas de recursos
kubectl top pods -n smartalarm

# Events do namespace
kubectl get events -n smartalarm --sort-by='.lastTimestamp'
```

## Próximos Passos

1. **GitOps Integration**: ArgoCD para deployment declarativo
2. **Service Mesh**: Istio para comunicação segura
3. **Advanced Monitoring**: Prometheus + Grafana
4. **Backup Strategy**: Velero para backup/restore
5. **Multi-cluster**: Deployment em múltiplos clusters
