#!/bin/bash

# Smart Alarm - Script de Build para Produção
# Este script compila todos os serviços e APIs para produção

set -e

echo "🚀 Iniciando build para produção..."

# Configurações
SOLUTION_PATH="SmartAlarm.sln"
CONFIGURATION="Release"
OUTPUT_DIR="./publish"
SERVICES=(
    "src/SmartAlarm.Api"
    "services/ai-service"
    "services/alarm-service"
    "services/integration-service"
)

# Limpar diretório de output
echo "🧹 Limpando diretório de output..."
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"

# Restaurar dependências
echo "📦 Restaurando dependências..."
dotnet restore "$SOLUTION_PATH"

# Build da solution
echo "🔨 Compilando solution..."
dotnet build "$SOLUTION_PATH" --configuration "$CONFIGURATION" --no-restore

# Executar testes
echo "🧪 Executando testes..."
dotnet test "$SOLUTION_PATH" --configuration "$CONFIGURATION" --no-build --logger "trx;LogFileName=test-results.trx"

# Publicar cada serviço
for service in "${SERVICES[@]}"; do
    service_name=$(basename "$service")
    echo "📤 Publicando $service_name..."
    
    dotnet publish "$service" \
        --configuration "$CONFIGURATION" \
        --no-build \
        --output "$OUTPUT_DIR/$service_name" \
        --runtime linux-x64 \
        --self-contained false
done

# Copiar arquivos de configuração para produção
echo "⚙️ Copiando arquivos de configuração..."
cp appsettings.Production.json "$OUTPUT_DIR/"

for service in "${SERVICES[@]}"; do
    service_name=$(basename "$service")
    if [ -f "$service/appsettings.Production.json" ]; then
        cp "$service/appsettings.Production.json" "$OUTPUT_DIR/$service_name/"
    fi
done

# Criar script de inicialização
echo "📋 Criando scripts de inicialização..."
cat > "$OUTPUT_DIR/start-services.sh" << 'EOF'
#!/bin/bash

# Script de inicialização dos serviços Smart Alarm

set -e

echo "🚀 Iniciando serviços Smart Alarm..."

# Variáveis de ambiente necessárias
export ASPNETCORE_ENVIRONMENT=Production
export ASPNETCORE_URLS="http://+:5000"

# Função para iniciar serviço em background
start_service() {
    local service_name=$1
    local port=$2
    
    echo "▶️ Iniciando $service_name na porta $port..."
    cd "$service_name"
    ASPNETCORE_URLS="http://+:$port" dotnet "$service_name.dll" &
    cd ..
}

# Verificar se todos os arquivos existem
services=("SmartAlarm.Api" "ai-service" "alarm-service" "integration-service")
for service in "${services[@]}"; do
    if [ ! -d "$service" ]; then
        echo "❌ Serviço $service não encontrado!"
        exit 1
    fi
done

# Iniciar serviços
start_service "SmartAlarm.Api" 5000
start_service "ai-service" 5001
start_service "alarm-service" 5002
start_service "integration-service" 5003

echo "✅ Todos os serviços foram iniciados!"
echo "📊 API principal: http://localhost:5000"
echo "🤖 AI Service: http://localhost:5001"
echo "⏰ Alarm Service: http://localhost:5002"
echo "🔗 Integration Service: http://localhost:5003"

# Aguardar
wait
EOF

chmod +x "$OUTPUT_DIR/start-services.sh"

# Criar script de parada
cat > "$OUTPUT_DIR/stop-services.sh" << 'EOF'
#!/bin/bash

echo "🛑 Parando serviços Smart Alarm..."

# Encontrar e parar processos dos serviços
pkill -f "SmartAlarm.Api.dll" || true
pkill -f "ai-service.dll" || true  
pkill -f "alarm-service.dll" || true
pkill -f "integration-service.dll" || true

echo "✅ Todos os serviços foram parados!"
EOF

chmod +x "$OUTPUT_DIR/stop-services.sh"

# Criar dockerfile para produção
echo "🐳 Criando Dockerfile para produção..."
cat > "$OUTPUT_DIR/Dockerfile" << 'EOF'
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Instalar dependências do sistema se necessário
RUN apt-get update && apt-get install -y \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Copiar arquivos publicados
COPY . /app/

# Configurar health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

# Expor portas
EXPOSE 5000 5001 5002 5003

# Script de inicialização
CMD ["./start-services.sh"]
EOF

# Criar docker-compose para produção
cat > "$OUTPUT_DIR/docker-compose.production.yml" << 'EOF'
version: '3.8'

services:
  smartalarm:
    build: .
    ports:
      - "5000:5000"
      - "5001:5001"
      - "5002:5002"
      - "5003:5003"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - DB_HOST=${DB_HOST:-postgres}
      - DB_PORT=${DB_PORT:-5432}
      - DB_NAME=${DB_NAME:-smartalarm}
      - DB_USER=${DB_USER:-postgres}
      - DB_PASSWORD=${DB_PASSWORD}
      - JWT_SECRET_KEY=${JWT_SECRET_KEY}
      - REDIS_CONNECTION_STRING=${REDIS_CONNECTION_STRING}
      - OCI_OBJECT_STORAGE_NAMESPACE=${OCI_OBJECT_STORAGE_NAMESPACE}
      - OCI_OBJECT_STORAGE_BUCKET_NAME=${OCI_OBJECT_STORAGE_BUCKET_NAME}
      - AZURE_KEYVAULT_URI=${AZURE_KEYVAULT_URI}
    depends_on:
      - postgres
      - redis
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5000/health"]
      interval: 30s
      timeout: 10s
      retries: 3

  postgres:
    image: postgres:15
    environment:
      - POSTGRES_DB=${DB_NAME:-smartalarm}
      - POSTGRES_USER=${DB_USER:-postgres}
      - POSTGRES_PASSWORD=${DB_PASSWORD}
    volumes:
      - postgres_data:/var/lib/postgresql/data
    restart: unless-stopped

  redis:
    image: redis:7-alpine
    restart: unless-stopped
    volumes:
      - redis_data:/data

volumes:
  postgres_data:
  redis_data:
EOF

# Criar arquivo de variáveis de ambiente template
cat > "$OUTPUT_DIR/.env.template" << 'EOF'
# Banco de Dados
DB_HOST=localhost
DB_PORT=5432
DB_NAME=smartalarm
DB_USER=postgres
DB_PASSWORD=your_postgres_password

# JWT
JWT_SECRET_KEY=your_jwt_secret_key_here_minimum_32_characters
JWT_ISSUER=SmartAlarm
JWT_AUDIENCE=SmartAlarm.Api
JWT_EXPIRATION_MINUTES=60

# Redis
REDIS_CONNECTION_STRING=localhost:6379

# OCI (Oracle Cloud Infrastructure)
OCI_OBJECT_STORAGE_NAMESPACE=your_oci_namespace
OCI_OBJECT_STORAGE_BUCKET_NAME=smartalarm-storage
OCI_REGION=us-ashburn-1
OCI_STREAMING_STREAM_OCID=your_stream_ocid
OCI_STREAMING_ENDPOINT=your_streaming_endpoint
OCI_VAULT_ID=your_vault_id
OCI_COMPARTMENT_ID=your_compartment_id

# Azure (opcional)
AZURE_KEYVAULT_URI=https://your-keyvault.vault.azure.net/

# AWS (opcional)
AWS_REGION=us-east-1

# APIs Externas
GOOGLE_CLIENT_ID=your_google_client_id
GOOGLE_CLIENT_SECRET=your_google_client_secret
MICROSOFT_CLIENT_ID=your_microsoft_client_id
MICROSOFT_CLIENT_SECRET=your_microsoft_client_secret
MICROSOFT_TENANT_ID=your_microsoft_tenant_id
HOLIDAY_API_KEY=your_holiday_api_key

# Observabilidade
OTLP_ENDPOINT=http://localhost:4317
JAEGER_ENDPOINT=http://localhost:14268/api/traces
PROMETHEUS_ENDPOINT=http://localhost:9090
ENABLE_TRACING=true
ENABLE_METRICS=true
ENABLE_LOGGING=true

# Segurança
CORS_ALLOWED_ORIGINS=https://yourdomain.com
RATE_LIMIT_PER_MINUTE=100
EOF

# Criar README para produção
cat > "$OUTPUT_DIR/README.md" << 'EOF'
# Smart Alarm - Deploy de Produção

Este diretório contém todos os arquivos necessários para executar o Smart Alarm em produção.

## Pré-requisitos

- .NET 8.0 Runtime
- PostgreSQL 15+
- Redis 7+
- Configuração adequada das variáveis de ambiente

## Configuração

1. Copie `.env.template` para `.env` e configure todas as variáveis
2. Configure as credenciais dos provedores de cloud (OCI, Azure, AWS)
3. Configure as chaves das APIs externas (Google, Microsoft, Holiday API)

## Execução Local

```bash
# Dar permissão aos scripts
chmod +x start-services.sh stop-services.sh

# Iniciar todos os serviços
./start-services.sh

# Parar todos os serviços
./stop-services.sh
```

## Execução com Docker

```bash
# Configurar variáveis de ambiente
cp .env.template .env
# Editar .env com suas configurações

# Executar com docker-compose
docker-compose -f docker-compose.production.yml up -d

# Verificar logs
docker-compose -f docker-compose.production.yml logs -f

# Parar
docker-compose -f docker-compose.production.yml down
```

## Endpoints de Saúde

- API Principal: http://localhost:5000/health
- AI Service: http://localhost:5001/health
- Alarm Service: http://localhost:5002/health
- Integration Service: http://localhost:5003/health

## Documentação da API

- Swagger UI: http://localhost:5000/swagger

## Monitoramento

Os serviços estão configurados com:
- Health checks automáticos
- Logging estruturado
- Métricas para Prometheus
- Tracing distribuído para Jaeger

## Segurança

- HTTPS obrigatório em produção
- Autenticação JWT
- Rate limiting configurado
- CORS configurado para domínios específicos
EOF

echo "✅ Build para produção concluído!"
echo "📂 Arquivos de produção em: $OUTPUT_DIR"
echo ""
echo "📋 Próximos passos:"
echo "1. Configure as variáveis de ambiente em $OUTPUT_DIR/.env"
echo "2. Execute $OUTPUT_DIR/start-services.sh para testar localmente"
echo "3. Use docker-compose.production.yml para deploy containerizado"
echo ""
echo "🔍 Verifique a documentação em $OUTPUT_DIR/README.md"
