# Deployment Guide - Smart Alarm

Este guia fornece instruções completas para deploy do Smart Alarm em diferentes ambientes, desde desenvolvimento até produção.

## 🚀 Quick Start

### Ambiente Completo (Recomendado para desenvolvimento)
```bash
# Um comando para subir tudo
./scripts/dev/start-full-env.sh
```

Este comando irá:
- ✅ Verificar pré-requisitos
- ✅ Construir todas as imagens
- ✅ Subir 20+ serviços
- ✅ Executar migrações
- ✅ Abrir dashboards

## 🌍 Ambientes de Deployment

### 1. Development Environment

#### Pré-requisitos
- Docker Desktop
- .NET 8 SDK
- Node.js 20+
- Git

#### Setup Manual
```bash
# 1. Clone e navegue
git clone https://github.com/smartalarm/smart-alarm.git
cd smart-alarm

# 2. Configure ambiente
cp .env.example .env
# Edite .env com suas configurações

# 3. Suba infraestrutura
docker compose -f docker-compose.dev.yml up -d

# 4. Execute migrações
dotnet ef database update --project src/SmartAlarm.Infrastructure

# 5. Inicie aplicações
# Terminal 1: Backend
dotnet run --project src/SmartAlarm.Api

# Terminal 2: Frontend
cd frontend && npm run dev
```

### 2. Testing Environment

#### CI/CD Pipeline
```yaml
# .github/workflows/ci.yml
name: CI/CD Pipeline

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  test:
    runs-on: ubuntu-latest
    services:
      postgres:
        image: postgres:15
        env:
          POSTGRES_PASSWORD: postgres
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
      redis:
        image: redis:7-alpine
        options: >-
          --health-cmd "redis-cli ping"
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: 8.0.x
        
    - name: Setup Node.js
      uses: actions/setup-node@v4
      with:
        node-version: '20'
        
    - name: Restore .NET dependencies
      run: dotnet restore
      
    - name: Build .NET
      run: dotnet build --no-restore
      
    - name: Run .NET tests
      run: dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage"
      
    - name: Install frontend dependencies
      run: cd frontend && npm ci
      
    - name: Build frontend
      run: cd frontend && npm run build
      
    - name: Run frontend tests
      run: cd frontend && npm test -- --coverage
      
    - name: Run E2E tests
      run: |
        docker compose -f docker-compose.test.yml up -d
        cd frontend && npm run test:e2e
        docker compose -f docker-compose.test.yml down
      
    - name: Upload coverage reports
      uses: codecov/codecov-action@v3
```

#### Testes de Integração
```bash
# Setup ambiente de teste
docker compose -f docker-compose.test.yml up -d

# Execute testes
dotnet test --filter Category=Integration
cd frontend && npm run test:e2e

# Cleanup
docker compose -f docker-compose.test.yml down -v
```

### 3. Staging Environment

#### Azure Container Instances
```bash
# Build e push das imagens
docker build -t smartalarm.azurecr.io/api:latest -f src/SmartAlarm.Api/Dockerfile .
docker build -t smartalarm.azurecr.io/frontend:latest -f frontend/Dockerfile ./frontend

az acr login --name smartalarm
docker push smartalarm.azurecr.io/api:latest
docker push smartalarm.azurecr.io/frontend:latest

# Deploy container group
az container create \
  --resource-group smartalarm-staging \
  --name smartalarm-staging \
  --file azure-container-instances.yaml
```

#### Kubernetes (AKS)
```bash
# Aplicar configurações
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/configmaps/
kubectl apply -f k8s/secrets/
kubectl apply -f k8s/deployments/
kubectl apply -f k8s/services/
kubectl apply -f k8s/ingress/

# Verificar status
kubectl get pods -n smartalarm-staging
kubectl get services -n smartalarm-staging
```

### 4. Production Environment

#### OCI Container Instances
```bash
# Build e push para OCI Registry
docker build -t iad.ocir.io/smartalarm/api:latest -f src/SmartAlarm.Api/Dockerfile .
docker push iad.ocir.io/smartalarm/api:latest

# Deploy via OCI CLI
oci container-instances container-instance create \
  --compartment-id $COMPARTMENT_ID \
  --display-name smartalarm-prod \
  --containers file://oci-containers.json
```

#### Oracle Kubernetes Engine (OKE)
```bash
# Setup cluster connection
oci ce cluster create-kubeconfig \
  --cluster-id $CLUSTER_ID \
  --file $HOME/.kube/config

# Deploy
kubectl apply -f k8s/production/
kubectl rollout status deployment/smartalarm-api -n smartalarm-prod
```

## 🔧 Configuração por Ambiente

### Environment Variables

#### Development
```env
# Database
DATABASE_PROVIDER=PostgreSQL
CONNECTION_STRING=Host=localhost;Database=smartalarm;Username=dev;Password=dev

# Redis
REDIS_CONNECTION_STRING=localhost:6379

# Observability
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
```

#### Staging
```env
# Database
DATABASE_PROVIDER=PostgreSQL
CONNECTION_STRING=${DATABASE_URL}

# Redis
REDIS_CONNECTION_STRING=${REDIS_URL}

# OAuth
GOOGLE_CLIENT_ID=${VAULT:google-client-id}
GITHUB_CLIENT_ID=${VAULT:github-client-id}
```

#### Production
```env
# Database
DATABASE_PROVIDER=Oracle
CONNECTION_STRING=${OCI_VAULT:database-connection}

# Storage
STORAGE_PROVIDER=OCI
OCI_OBJECT_STORAGE_BUCKET=${OCI_VAULT:storage-bucket}

# Secrets
KEYVAULT_PROVIDER=OCI
OCI_VAULT_ENDPOINT=${OCI_VAULT_ENDPOINT}
```

## 🗄️ Database Migrations

### Entity Framework Migrations
```bash
# Criar migration
dotnet ef migrations add MigrationName --project src/SmartAlarm.Infrastructure

# Aplicar migration
dotnet ef database update --project src/SmartAlarm.Infrastructure

# Production migration
dotnet ef database update --connection "${PRODUCTION_CONNECTION_STRING}"
```

### Production Migration Script
```bash
#!/bin/bash
# scripts/deploy/migrate-production.sh

set -e

echo "🚀 Starting production database migration..."

# Backup database
echo "📦 Creating backup..."
pg_dump $PRODUCTION_CONNECTION_STRING > "backup-$(date +%Y%m%d-%H%M%S).sql"

# Run migrations
echo "🔄 Applying migrations..."
dotnet ef database update \
  --project src/SmartAlarm.Infrastructure \
  --connection "$PRODUCTION_CONNECTION_STRING"

echo "✅ Migration completed successfully"
```

## 🔐 Secrets Management

### Development (HashiCorp Vault)
```bash
# Configurar Vault
export VAULT_ADDR=http://localhost:8200
export VAULT_TOKEN=dev-token

# Armazenar secrets
vault kv put secret/smartalarm \
  google-client-id="your-client-id" \
  google-client-secret="your-client-secret"
```

### Production (OCI Vault)
```bash
# Criar secret no OCI Vault
oci vault secret create-base64 \
  --compartment-id $COMPARTMENT_ID \
  --vault-id $VAULT_ID \
  --key-id $KEY_ID \
  --secret-name "smartalarm-database-connection" \
  --secret-content-content "$(echo -n "$CONNECTION_STRING" | base64)"
```

## 📊 Monitoramento

### Health Checks
```bash
# Verificar saúde da aplicação
curl http://localhost:5000/health

# Health check detalhado
curl http://localhost:5000/health/ready

# Liveness probe
curl http://localhost:5000/health/live
```

### Prometheus Metrics
```yaml
# prometheus.yml
scrape_configs:
  - job_name: 'smartalarm-api'
    static_configs:
      - targets: ['api:80']
    metrics_path: '/metrics'
    
  - job_name: 'smartalarm-ai'
    static_configs:
      - targets: ['ai-service:80']
```

### Grafana Dashboards
```json
{
  "dashboard": {
    "title": "Smart Alarm - Production",
    "panels": [
      {
        "title": "Request Rate",
        "type": "graph",
        "targets": [
          {
            "expr": "rate(http_requests_total[5m])"
          }
        ]
      }
    ]
  }
}
```

## 🚦 Load Balancer & CDN

### NGINX Configuration
```nginx
upstream smartalarm_api {
    server api:80 max_fails=3 fail_timeout=30s;
    server api-2:80 max_fails=3 fail_timeout=30s;
}

server {
    listen 443 ssl http2;
    server_name api.smartalarm.com;
    
    ssl_certificate /etc/ssl/smartalarm.crt;
    ssl_certificate_key /etc/ssl/smartalarm.key;
    
    location / {
        proxy_pass http://smartalarm_api;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
    
    location /health {
        proxy_pass http://smartalarm_api;
        access_log off;
    }
}
```

### CloudFlare Configuration
```yaml
# cloudflare-config.yml
rules:
  - pattern: "*.smartalarm.com/api/*"
    cache_level: "bypass"
    
  - pattern: "*.smartalarm.com/static/*"
    cache_level: "cache_everything"
    edge_cache_ttl: 604800  # 1 week
    
  - pattern: "*.smartalarm.com/"
    cache_level: "standard"
    security_level: "medium"
```

## 🔄 Blue-Green Deployment

### Script de Deploy
```bash
#!/bin/bash
# scripts/deploy/blue-green.sh

CURRENT_COLOR=$(kubectl get service smartalarm-api -o jsonpath='{.metadata.labels.version}')
NEW_COLOR=$([ "$CURRENT_COLOR" = "blue" ] && echo "green" || echo "blue")

echo "Current: $CURRENT_COLOR, Deploying: $NEW_COLOR"

# 1. Deploy nova versão
kubectl apply -f k8s/deployments/api-$NEW_COLOR.yaml

# 2. Aguardar healthy
kubectl rollout status deployment/smartalarm-api-$NEW_COLOR

# 3. Executar smoke tests
./scripts/test/smoke-tests.sh $NEW_COLOR

# 4. Switch traffic
kubectl patch service smartalarm-api -p '{"spec":{"selector":{"version":"'$NEW_COLOR'"}}}'

# 5. Verificar métricas por 5 minutos
./scripts/deploy/monitor-metrics.sh 300

# 6. Cleanup versão anterior
kubectl delete deployment smartalarm-api-$CURRENT_COLOR
```

## 📱 PWA Deployment

### Service Worker Cache Strategy
```javascript
// public/service-worker.js
const CACHE_NAME = 'smartalarm-v1.0.0';

self.addEventListener('install', (event) => {
  event.waitUntil(
    caches.open(CACHE_NAME)
      .then(cache => cache.addAll([
        '/',
        '/static/js/main.js',
        '/static/css/main.css',
        '/manifest.json'
      ]))
  );
});
```

### CDN + Service Worker
```yaml
# CDN rules for PWA
patterns:
  - path: "/service-worker.js"
    cache_control: "no-cache"
    
  - path: "/manifest.json" 
    cache_control: "max-age=3600"
    
  - path: "/static/*"
    cache_control: "max-age=31536000"
```

## 📈 Scaling Strategy

### Horizontal Pod Autoscaler
```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: smartalarm-api-hpa
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: smartalarm-api
  minReplicas: 2
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
```

## 🚨 Disaster Recovery

### Backup Strategy
```bash
#!/bin/bash
# scripts/backup/daily-backup.sh

# Database backup
pg_dump $DATABASE_URL | gzip > "db-backup-$(date +%Y%m%d).sql.gz"

# Upload to cloud storage
aws s3 cp "db-backup-$(date +%Y%m%d).sql.gz" s3://smartalarm-backups/

# Retention: manter 30 dias
find /backups -name "db-backup-*.sql.gz" -mtime +30 -delete
```

### Recovery Procedure
1. **Identificar problema**: Monitoramento + alertas
2. **Avaliar escopo**: Logs + métricas
3. **Executar rollback**: Blue-green switch
4. **Restaurar dados**: Backup mais recente
5. **Validar sistema**: Smoke tests
6. **Post-mortem**: Análise e melhorias

## 📋 Checklist de Deployment

### Pré-deployment
- [ ] Testes passando (unit, integration, E2E)
- [ ] Code review aprovado
- [ ] Secrets atualizados
- [ ] Database migrations testadas
- [ ] Performance tests executados
- [ ] Security scan executado

### During deployment
- [ ] Backup database realizado
- [ ] Migration executada com sucesso
- [ ] Health checks passing
- [ ] Smoke tests passando
- [ ] Metrics/logs funcionando
- [ ] SSL certificates válidos

### Pós-deployment
- [ ] Monitoramento ativo por 30 minutos
- [ ] Error rate < baseline
- [ ] Response time < SLA
- [ ] Business metrics normais
- [ ] User acceptance tests passando
- [ ] Documentation atualizada

## 🆘 Troubleshooting

### Problemas Comuns

#### Database Connection Issues
```bash
# Verificar conectividade
pg_isready -h $DB_HOST -p $DB_PORT -U $DB_USER

# Verificar pooling
kubectl logs deployment/smartalarm-api | grep -i "connection pool"
```

#### High Memory Usage
```bash
# Verificar usage
kubectl top pods -n smartalarm-prod

# Ajustar limits
kubectl patch deployment smartalarm-api -p '{"spec":{"template":{"spec":{"containers":[{"name":"api","resources":{"limits":{"memory":"1Gi"}}}]}}}}'
```

#### OAuth Issues
```bash
# Verificar secrets
vault kv get secret/smartalarm

# Testar endpoints
curl -X POST "https://api.smartalarm.com/api/oauth/google" -d '{"code":"test"}'
```

Esta documentação fornece um guia completo para deploy em todos os ambientes, desde desenvolvimento até produção enterprise.