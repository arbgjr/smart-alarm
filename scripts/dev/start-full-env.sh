#!/bin/bash

# ================================================
# Smart Alarm - Full Development Environment Setup
# ================================================

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$(dirname "$(dirname "$SCRIPT_DIR")")"
COMPOSE_FILE="$PROJECT_ROOT/docker-compose.full.yml"
ENV_FILE="$PROJECT_ROOT/.env"

# Function to print colored messages
print_message() {
    local color=$1
    local message=$2
    echo -e "${color}${message}${NC}"
}

# Function to check prerequisites
check_prerequisites() {
    print_message "$BLUE" "\n📋 Checking prerequisites..."
    
    local missing_deps=()
    
    # Check Docker
    if ! command -v docker &> /dev/null; then
        missing_deps+=("docker")
    fi
    
    # Check Docker Compose
    if ! command -v docker-compose &> /dev/null && ! docker compose version &> /dev/null; then
        missing_deps+=("docker-compose")
    fi
    
    # Check .NET SDK
    if ! command -v dotnet &> /dev/null; then
        missing_deps+=("dotnet")
    fi
    
    # Check Node.js
    if ! command -v node &> /dev/null; then
        missing_deps+=("node")
    fi
    
    # Check npm
    if ! command -v npm &> /dev/null; then
        missing_deps+=("npm")
    fi
    
    if [ ${#missing_deps[@]} -ne 0 ]; then
        print_message "$RED" "❌ Missing dependencies: ${missing_deps[*]}"
        print_message "$YELLOW" "Please install the missing dependencies and try again."
        exit 1
    fi
    
    print_message "$GREEN" "✅ All prerequisites met"
}

# Function to create .env file if not exists
create_env_file() {
    if [ ! -f "$ENV_FILE" ]; then
        print_message "$YELLOW" "\n📝 Creating .env file..."
        cat > "$ENV_FILE" << EOF
# Database
DB_PASSWORD=smartalarm123
REPLICATION_PASSWORD=replicator123

# Redis
REDIS_PASSWORD=redis123

# RabbitMQ
RABBITMQ_USER=smartalarm
RABBITMQ_PASSWORD=rabbitmq123

# MinIO
MINIO_ACCESS_KEY=minioadmin
MINIO_SECRET_KEY=minioadmin123

# Vault
VAULT_TOKEN=vault-dev-token

# Grafana
GRAFANA_PASSWORD=grafana123

# PgAdmin
PGADMIN_EMAIL=admin@smartalarm.com
PGADMIN_PASSWORD=pgadmin123

# Health Dashboard
HEALTH_PASSWORD=health123

# OAuth Providers (configure with real values)
GOOGLE_CLIENT_ID=your-google-client-id
GOOGLE_CLIENT_SECRET=your-google-client-secret
GITHUB_CLIENT_ID=your-github-client-id
GITHUB_CLIENT_SECRET=your-github-client-secret
FACEBOOK_CLIENT_ID=your-facebook-client-id
FACEBOOK_CLIENT_SECRET=your-facebook-client-secret
MICROSOFT_CLIENT_ID=your-microsoft-client-id
MICROSOFT_CLIENT_SECRET=your-microsoft-client-secret

# JWT
JWT_SECRET_KEY=your-super-secret-jwt-key-min-32-characters-long
JWT_ISSUER=SmartAlarm
JWT_AUDIENCE=SmartAlarmUsers
EOF
        print_message "$GREEN" "✅ .env file created"
    else
        print_message "$GREEN" "✅ .env file already exists"
    fi
}

# Function to build application images
build_images() {
    print_message "$BLUE" "\n🔨 Building application images..."
    
    cd "$PROJECT_ROOT"
    
    # Build .NET projects
    print_message "$YELLOW" "Building .NET services..."
    dotnet build SmartAlarm.sln --configuration Release
    
    # Build frontend
    print_message "$YELLOW" "Building frontend..."
    cd "$PROJECT_ROOT/frontend"
    npm install
    npm run build
    
    cd "$PROJECT_ROOT"
    
    # Build Docker images
    print_message "$YELLOW" "Building Docker images..."
    docker-compose -f "$COMPOSE_FILE" build --parallel
    
    print_message "$GREEN" "✅ All images built successfully"
}

# Function to create necessary directories and files
setup_infrastructure() {
    print_message "$BLUE" "\n📁 Setting up infrastructure files..."
    
    # Create monitoring config directories
    mkdir -p "$PROJECT_ROOT/infrastructure/monitoring/prometheus/alerts"
    mkdir -p "$PROJECT_ROOT/infrastructure/monitoring/grafana/dashboards"
    mkdir -p "$PROJECT_ROOT/infrastructure/monitoring/grafana/datasources"
    mkdir -p "$PROJECT_ROOT/infrastructure/monitoring/alertmanager"
    mkdir -p "$PROJECT_ROOT/infrastructure/monitoring/loki"
    mkdir -p "$PROJECT_ROOT/infrastructure/monitoring/tempo"
    mkdir -p "$PROJECT_ROOT/infrastructure/monitoring/promtail"
    mkdir -p "$PROJECT_ROOT/infrastructure/monitoring/health-dashboard"
    mkdir -p "$PROJECT_ROOT/config/redis"
    mkdir -p "$PROJECT_ROOT/scripts/db"
    
    # Create basic Prometheus config if not exists
    if [ ! -f "$PROJECT_ROOT/infrastructure/monitoring/prometheus/prometheus.yml" ]; then
        cat > "$PROJECT_ROOT/infrastructure/monitoring/prometheus/prometheus.yml" << 'EOF'
global:
  scrape_interval: 15s
  evaluation_interval: 15s

alerting:
  alertmanagers:
    - static_configs:
        - targets:
          - alertmanager:9093

rule_files:
  - /etc/prometheus/alerts/*.yml

scrape_configs:
  - job_name: 'prometheus'
    static_configs:
      - targets: ['localhost:9090']

  - job_name: 'smartalarm-api'
    static_configs:
      - targets: ['smartalarm-api:80']
    metrics_path: '/metrics'

  - job_name: 'postgres'
    static_configs:
      - targets: ['postgres-exporter:9187']

  - job_name: 'redis'
    static_configs:
      - targets: ['redis-exporter:9121']

  - job_name: 'rabbitmq'
    static_configs:
      - targets: ['rabbitmq:15692']
EOF
    fi
    
    # Create basic Grafana datasource config
    if [ ! -f "$PROJECT_ROOT/infrastructure/monitoring/grafana/datasources/prometheus.yml" ]; then
        cat > "$PROJECT_ROOT/infrastructure/monitoring/grafana/datasources/prometheus.yml" << 'EOF'
apiVersion: 1

datasources:
  - name: Prometheus
    type: prometheus
    access: proxy
    url: http://prometheus:9090
    isDefault: true
    
  - name: Loki
    type: loki
    access: proxy
    url: http://loki:3100
    
  - name: Jaeger
    type: jaeger
    access: proxy
    url: http://jaeger:16686
    
  - name: Tempo
    type: tempo
    access: proxy
    url: http://tempo:3200
EOF
    fi
    
    # Create Redis sentinel config
    if [ ! -f "$PROJECT_ROOT/config/redis/sentinel.conf" ]; then
        cat > "$PROJECT_ROOT/config/redis/sentinel.conf" << 'EOF'
port 26379
sentinel monitor mymaster redis-master 6379 1
sentinel auth-pass mymaster redis123
sentinel down-after-milliseconds mymaster 5000
sentinel parallel-syncs mymaster 1
sentinel failover-timeout mymaster 10000
EOF
    fi
    
    # Create database init script
    if [ ! -f "$PROJECT_ROOT/scripts/db/init.sql" ]; then
        cat > "$PROJECT_ROOT/scripts/db/init.sql" << 'EOF'
-- Create replication user
CREATE USER replicator WITH REPLICATION ENCRYPTED PASSWORD 'replicator123';

-- Create application schema
CREATE SCHEMA IF NOT EXISTS smartalarm;

-- Grant permissions
GRANT ALL PRIVILEGES ON SCHEMA smartalarm TO smartalarm;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA smartalarm TO smartalarm;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA smartalarm TO smartalarm;

-- Create extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";
EOF
    fi
    
    print_message "$GREEN" "✅ Infrastructure files ready"
}

# Function to start services
start_services() {
    print_message "$BLUE" "\n🚀 Starting all services..."
    
    cd "$PROJECT_ROOT"
    
    # Start infrastructure services first
    print_message "$YELLOW" "Starting infrastructure services..."
    docker-compose -f "$COMPOSE_FILE" up -d \
        postgres-primary \
        postgres-replica \
        redis-master \
        redis-sentinel \
        rabbitmq \
        minio \
        vault
    
    # Wait for infrastructure to be ready
    print_message "$YELLOW" "Waiting for infrastructure services to be healthy..."
    sleep 10
    
    # Start monitoring stack
    print_message "$YELLOW" "Starting monitoring stack..."
    docker-compose -f "$COMPOSE_FILE" up -d \
        prometheus \
        grafana \
        alertmanager \
        jaeger \
        loki \
        promtail \
        tempo
    
    # Start application services
    print_message "$YELLOW" "Starting application services..."
    docker-compose -f "$COMPOSE_FILE" up -d \
        smartalarm-api \
        smartalarm-ai-service \
        smartalarm-integration-service \
        smartalarm-alarm-service \
        smartalarm-frontend
    
    # Start management tools
    print_message "$YELLOW" "Starting management tools..."
    docker-compose -f "$COMPOSE_FILE" up -d \
        pgadmin \
        redis-commander \
        swagger-ui \
        health-dashboard
    
    print_message "$GREEN" "✅ All services started"
}

# Function to wait for services to be healthy
wait_for_health() {
    print_message "$BLUE" "\n⏳ Waiting for all services to be healthy..."
    
    local max_attempts=60
    local attempt=0
    
    while [ $attempt -lt $max_attempts ]; do
        if docker-compose -f "$COMPOSE_FILE" ps | grep -q "unhealthy\|starting"; then
            print_message "$YELLOW" "Some services are still starting... (attempt $((attempt+1))/$max_attempts)"
            sleep 5
            ((attempt++))
        else
            print_message "$GREEN" "✅ All services are healthy!"
            return 0
        fi
    done
    
    print_message "$RED" "⚠️ Some services failed to become healthy"
    docker-compose -f "$COMPOSE_FILE" ps
    return 1
}

# Function to run database migrations
run_migrations() {
    print_message "$BLUE" "\n🗄️ Running database migrations..."
    
    cd "$PROJECT_ROOT"
    
    # Run EF Core migrations
    dotnet ef database update \
        --project src/SmartAlarm.Infrastructure \
        --startup-project src/SmartAlarm.Api \
        --connection "Host=localhost;Database=smartalarm;Username=smartalarm;Password=smartalarm123"
    
    print_message "$GREEN" "✅ Database migrations completed"
}

# Function to seed test data
seed_test_data() {
    print_message "$BLUE" "\n🌱 Seeding test data..."
    
    # This would normally call a seeding script or API endpoint
    # For now, we'll just show a placeholder
    print_message "$YELLOW" "Test data seeding would go here..."
    
    print_message "$GREEN" "✅ Test data seeded"
}

# Function to open dashboards
open_dashboards() {
    print_message "$BLUE" "\n🌐 Opening dashboards..."
    
    local os=$(uname -s)
    local open_cmd=""
    
    case "$os" in
        Linux*)     open_cmd="xdg-open";;
        Darwin*)    open_cmd="open";;
        CYGWIN*|MINGW*|MSYS*) open_cmd="start";;
        *)          open_cmd="";;
    esac
    
    if [ -n "$open_cmd" ]; then
        print_message "$YELLOW" "Opening dashboards in browser..."
        $open_cmd "http://localhost:5000/swagger" 2>/dev/null || true  # API Swagger
        $open_cmd "http://localhost:3001" 2>/dev/null || true          # Frontend
        $open_cmd "http://localhost:3000" 2>/dev/null || true          # Grafana
        $open_cmd "http://localhost:16686" 2>/dev/null || true         # Jaeger
        $open_cmd "http://localhost:15672" 2>/dev/null || true         # RabbitMQ
        $open_cmd "http://localhost:9001" 2>/dev/null || true          # MinIO Console
        $open_cmd "http://localhost:5050" 2>/dev/null || true          # PgAdmin
        $open_cmd "http://localhost:8081" 2>/dev/null || true          # Redis Commander
    fi
    
    print_message "$GREEN" "\n✅ Environment is ready!"
    print_message "$BLUE" "\n📊 Available services:"
    echo "  • API:             http://localhost:5000/swagger"
    echo "  • Frontend:        http://localhost:3001"
    echo "  • Grafana:         http://localhost:3000 (admin/grafana123)"
    echo "  • Jaeger:          http://localhost:16686"
    echo "  • Prometheus:      http://localhost:9090"
    echo "  • RabbitMQ:        http://localhost:15672 (smartalarm/rabbitmq123)"
    echo "  • MinIO:           http://localhost:9001 (minioadmin/minioadmin123)"
    echo "  • PgAdmin:         http://localhost:5050 (admin@smartalarm.com/pgadmin123)"
    echo "  • Redis Commander: http://localhost:8081"
    echo "  • Vault:           http://localhost:8200 (token: vault-dev-token)"
    echo "  • Health Dashboard: http://localhost:3003"
}

# Function to show logs
show_logs() {
    print_message "$BLUE" "\n📜 Showing logs (press Ctrl+C to stop)..."
    docker-compose -f "$COMPOSE_FILE" logs -f --tail=100
}

# Main execution
main() {
    print_message "$GREEN" "========================================="
    print_message "$GREEN" "   Smart Alarm - Full Environment Setup"
    print_message "$GREEN" "========================================="
    
    check_prerequisites
    create_env_file
    setup_infrastructure
    build_images
    start_services
    wait_for_health
    run_migrations
    seed_test_data
    open_dashboards
    
    print_message "$YELLOW" "\n💡 Tips:"
    echo "  • To view logs:        $0 logs"
    echo "  • To stop all:        $0 stop"
    echo "  • To restart:         $0 restart"
    echo "  • To clean up:        $0 clean"
    
    # Ask if user wants to see logs
    read -p "Do you want to see the logs now? (y/n) " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        show_logs
    fi
}

# Handle script arguments
case "${1:-}" in
    logs)
        show_logs
        ;;
    stop)
        print_message "$YELLOW" "Stopping all services..."
        docker-compose -f "$COMPOSE_FILE" down
        print_message "$GREEN" "✅ All services stopped"
        ;;
    restart)
        print_message "$YELLOW" "Restarting all services..."
        docker-compose -f "$COMPOSE_FILE" restart
        print_message "$GREEN" "✅ All services restarted"
        ;;
    clean)
        print_message "$RED" "⚠️ This will remove all containers, volumes, and data!"
        read -p "Are you sure? (yes/no) " -r
        if [[ $REPLY == "yes" ]]; then
            docker-compose -f "$COMPOSE_FILE" down -v --remove-orphans
            print_message "$GREEN" "✅ Environment cleaned"
        fi
        ;;
    *)
        main
        ;;
esac