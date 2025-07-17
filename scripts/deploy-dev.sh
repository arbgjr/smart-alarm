#!/bin/bash

# Deploy Smart Alarm Microservices to Development Environment
# Author: Smart Alarm Team
# Usage: ./deploy-dev.sh [start|stop|restart|status|logs]

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${GREEN}🚀 Smart Alarm Development Deployment Script${NC}"
echo "================================================="

# Configuration
COMPOSE_FILE="docker-compose.dev.yml"
SERVICES_COMPOSE_FILE="docker-compose.services.yml"
PROJECT_NAME="smartalarm"

# Function to check if Docker is running
check_docker() {
    if ! docker info >/dev/null 2>&1; then
        echo -e "${RED}❌ Docker is not running. Please start Docker and try again.${NC}"
        exit 1
    fi
}

# Function to start infrastructure services
start_infrastructure() {
    echo -e "${YELLOW}🏗️  Starting infrastructure services...${NC}"
    
    docker-compose -f "$COMPOSE_FILE" -p "$PROJECT_NAME" up -d \
        postgres rabbitmq vault minio prometheus loki jaeger grafana
    
    echo -e "${BLUE}⏳ Waiting for infrastructure services to be healthy...${NC}"
    
    # Wait for key services to be ready
    echo "Waiting for PostgreSQL..."
    timeout 60 bash -c 'until docker exec smartalarm-postgres pg_isready -U smartalarm >/dev/null 2>&1; do sleep 2; done'
    
    echo "Waiting for RabbitMQ..."
    timeout 60 bash -c 'until docker exec smartalarm-rabbitmq rabbitmq-diagnostics ping >/dev/null 2>&1; do sleep 2; done'
    
    echo "Waiting for MinIO..."
    timeout 60 bash -c 'until curl -f http://localhost:9000/minio/health/live >/dev/null 2>&1; do sleep 2; done'
    
    echo "Waiting for Vault..."
    timeout 60 bash -c 'until curl -f http://localhost:8200/v1/sys/health >/dev/null 2>&1; do sleep 2; done'
    
    echo -e "${GREEN}✅ Infrastructure services are ready!${NC}"
}

# Function to start microservices
start_microservices() {
    echo -e "${YELLOW}🔧 Starting Smart Alarm microservices...${NC}"
    
    # Create network if it doesn't exist
    docker network create smartalarm-network 2>/dev/null || true
    
    # Start the microservices
    docker-compose -f "$SERVICES_COMPOSE_FILE" -p "$PROJECT_NAME" up -d
    
    echo -e "${BLUE}⏳ Waiting for microservices to be healthy...${NC}"
    
    # Wait for services to be ready
    services=("alarm-service:5001" "ai-service:5002" "integration-service:5003")
    
    for service_port in "${services[@]}"; do
        service=${service_port%:*}
        port=${service_port#*:}
        echo "Waiting for $service..."
        timeout 60 bash -c "until curl -f http://localhost:$port/health >/dev/null 2>&1; do sleep 2; done"
    done
    
    echo -e "${GREEN}✅ All microservices are ready!${NC}"
}

# Function to start all services
start_all() {
    check_docker
    start_infrastructure
    start_microservices
    show_status
    show_endpoints
}

# Function to stop all services
stop_all() {
    echo -e "${YELLOW}🛑 Stopping Smart Alarm services...${NC}"
    
    # Stop microservices
    docker-compose -f "$SERVICES_COMPOSE_FILE" -p "$PROJECT_NAME" down
    
    # Stop infrastructure
    docker-compose -f "$COMPOSE_FILE" -p "$PROJECT_NAME" down
    
    echo -e "${GREEN}✅ All services stopped!${NC}"
}

# Function to restart all services
restart_all() {
    echo -e "${YELLOW}🔄 Restarting Smart Alarm services...${NC}"
    stop_all
    sleep 2
    start_all
}

# Function to show service status
show_status() {
    echo -e "${BLUE}📊 Service Status:${NC}"
    echo "==================="
    
    # Check infrastructure services
    services=("postgres:5432" "rabbitmq:15672" "vault:8200" "minio:9000" "prometheus:9090" "grafana:3000" "jaeger:16686")
    
    for service_port in "${services[@]}"; do
        service=${service_port%:*}
        port=${service_port#*:}
        if curl -f "http://localhost:$port" >/dev/null 2>&1; then
            echo -e "  ${GREEN}✅ $service (port $port)${NC}"
        else
            echo -e "  ${RED}❌ $service (port $port)${NC}"
        fi
    done
    
    # Check microservices
    microservices=("alarm-service:5001" "ai-service:5002" "integration-service:5003")
    
    for service_port in "${microservices[@]}"; do
        service=${service_port%:*}
        port=${service_port#*:}
        if curl -f "http://localhost:$port/health" >/dev/null 2>&1; then
            echo -e "  ${GREEN}✅ $service (port $port)${NC}"
        else
            echo -e "  ${RED}❌ $service (port $port)${NC}"
        fi
    done
}

# Function to show important endpoints
show_endpoints() {
    echo -e "${BLUE}🌐 Important Endpoints:${NC}"
    echo "======================="
    echo "Microservices:"
    echo "  Alarm Service:       http://localhost:5001"
    echo "  AI Service:          http://localhost:5002"
    echo "  Integration Service: http://localhost:5003"
    echo ""
    echo "Infrastructure:"
    echo "  RabbitMQ Management: http://localhost:15672 (guest/guest)"
    echo "  MinIO Console:       http://localhost:9001 (minioadmin/minioadmin)"
    echo "  Vault UI:            http://localhost:8200 (token: dev-token)"
    echo ""
    echo "Monitoring:"
    echo "  Prometheus:          http://localhost:9090"
    echo "  Grafana:             http://localhost:3000 (admin/admin)"
    echo "  Jaeger:              http://localhost:16686"
    echo ""
    echo "Health Checks:"
    echo "  Alarm Service:       http://localhost:5001/health"
    echo "  AI Service:          http://localhost:5002/health"
    echo "  Integration Service: http://localhost:5003/health"
}

# Function to show logs
show_logs() {
    local service=${1:-}
    
    if [ -z "$service" ]; then
        echo -e "${YELLOW}📋 Showing logs for all services...${NC}"
        docker-compose -f "$COMPOSE_FILE" -f "$SERVICES_COMPOSE_FILE" -p "$PROJECT_NAME" logs -f
    else
        echo -e "${YELLOW}📋 Showing logs for $service...${NC}"
        docker-compose -f "$COMPOSE_FILE" -f "$SERVICES_COMPOSE_FILE" -p "$PROJECT_NAME" logs -f "$service"
    fi
}

# Function to show help
show_help() {
    echo "Usage: $0 [start|stop|restart|status|logs|endpoints|help]"
    echo ""
    echo "Commands:"
    echo "  start       Start all services (infrastructure + microservices)"
    echo "  stop        Stop all services"
    echo "  restart     Restart all services"
    echo "  status      Show status of all services"
    echo "  logs [svc]  Show logs (optionally for specific service)"
    echo "  endpoints   Show important endpoints"
    echo "  help        Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0 start                    # Start all services"
    echo "  $0 logs alarm-service       # Show logs for alarm service"
    echo "  $0 status                   # Check service status"
}

# Main script logic
case "${1:-start}" in
    "start")
        start_all
        ;;
    "stop")
        stop_all
        ;;
    "restart")
        restart_all
        ;;
    "status")
        show_status
        ;;
    "logs")
        show_logs "$2"
        ;;
    "endpoints")
        show_endpoints
        ;;
    "help"|"--help"|"-h")
        show_help
        ;;
    *)
        echo -e "${RED}❌ Unknown command: $1${NC}"
        show_help
        exit 1
        ;;
esac
