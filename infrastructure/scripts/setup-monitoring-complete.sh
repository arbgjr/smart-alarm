#!/bin/bash

# Smart Alarm Complete Monitoring Setup Script
# This script sets up comprehensive monitoring with dashboards, alerts, and runbooks

set -e

echo "🚀 Setting up Smart Alarm Complete Monitoring Stack..."

# Configuration
NAMESPACE="smartalarm"
MONITORING_NAMESPACE="monitoring"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check prerequisites
check_prerequisites() {
    log_info "Checking prerequisites..."

    if ! command -v kubectl &> /dev/null; then
        log_error "kubectl is required but not installed"
        exit 1
    fi

    if ! command -v docker &> /dev/null; then
        log_error "docker is required but not installed"
        exit 1
    fi

    if ! command -v docker-compose &> /dev/null; then
        log_error "docker-compose is required but not installed"
        exit 1
    fi

    log_info "Prerequisites check passed ✓"
}

# Create namespaces
create_namespaces() {
    log_info "Creating namespaces..."

    kubectl create namespace $NAMESPACE --dry-run=client -o yaml | kubectl apply -f -
    kubectl create namespace $MONITORING_NAMESPACE --dry-run=client -o yaml | kubectl apply -f -

    log_info "Namespaces created ✓"
}

# Deploy monitoring stack with Docker Compose
deploy_monitoring_stack() {
    log_info "Deploying monitoring stack..."

    cd infrastructure/monitoring

    # Start monitoring services
    docker-compose -f docker-compose.monitoring.yml up -d

    # Wait for services to be ready
    log_info "Waiting for services to be ready..."
    sleep 30

    # Check service health
    check_service_health "Prometheus" "http://localhost:9090/-/healthy"
    check_service_health "Grafana" "http://localhost:3000/api/health"
    check_service_health "AlertManager" "http://localhost:9093/-/healthy"
    check_service_health "Jaeger" "http://localhost:16686/api/services"

    cd ../..

    log_info "Monitoring stack deployed ✓"
}

# Check service health
check_service_health() {
    local service_name=$1
    local health_url=$2
    local max_attempts=10
    local attempt=1

    log_info "Checking $service_name health..."

    while [ $attempt -le $max_attempts ]; do
        if curl -f -s $health_url > /dev/null 2>&1; then
            log_info "$service_name is healthy ✓"
            return 0
        fi

        log_warn "$service_name not ready, attempt $attempt/$max_attempts"
        sleep 10
        ((attempt++))
    done

    log_error "$service_name failed to become healthy"
    return 1
}

# Configure Grafana dashboards
configure_grafana() {
    log_info "Configuring Grafana dashboards..."

    # Wait for Grafana to be fully ready
    sleep 10

    # Import dashboards via API
    import_dashboard "System Overview" "infrastructure/monitoring/grafana/dashboards/system-overview.json"
    import_dashboard "Business Metrics" "infrastructure/monitoring/grafana/dashboards/business-metrics.json"
    import_dashboard "Infrastructure" "infrastructure/monitoring/grafana/dashboards/infrastructure.json"

    log_info "Grafana dashboards configured ✓"
}

# Import Grafana dashboard
import_dashboard() {
    local dashboard_name=$1
    local dashboard_file=$2

    log_info "Importing $dashboard_name dashboard..."

    # Create dashboard payload
    local payload=$(jq -n --argjson dashboard "$(cat $dashboard_file)" '{
        dashboard: $dashboard.dashboard,
        overwrite: true,
        inputs: []
    }')

    # Import dashboard
    curl -X POST \
        -H "Content-Type: application/json" \
        -H "Authorization: Bearer admin:smartalarm123" \
        -d "$payload" \
        http://localhost:3000/api/dashboards/import > /dev/null 2>&1

    if [ $? -eq 0 ]; then
        log_info "$dashboard_name dashboard imported ✓"
    else
        log_warn "Failed to import $dashboard_name dashboard"
    fi
}

# Configure Prometheus alerts
configure_prometheus_alerts() {
    log_info "Configuring Prometheus alert rules..."

    # Reload Prometheus configuration
    curl -X POST http://localhost:9090/-/reload

    # Verify alert rules are loaded
    local rules_count=$(curl -s http://localhost:9090/api/v1/rules | jq '.data.groups | length')

    if [ "$rules_count" -gt 0 ]; then
        log_info "Prometheus alert rules configured ✓ ($rules_count rule groups loaded)"
    else
        log_warn "No alert rules loaded"
    fi
}

# Configure AlertManager
configure_alertmanager() {
    log_info "Configuring AlertManager..."

    # Reload AlertManager configuration
    curl -X POST http://localhost:9093/-/reload

    # Verify configuration
    local config_status=$(curl -s http://localhost:9093/api/v1/status | jq -r '.data.configYAML' | wc -l)

    if [ "$config_status" -gt 10 ]; then
        log_info "AlertManager configured ✓"
    else
        log_warn "AlertManager configuration may be incomplete"
    fi
}

# Setup monitoring for applications
setup_application_monitoring() {
    log_info "Setting up application monitoring..."

    # Apply observability configuration to applications
    kubectl apply -f - <<EOF
apiVersion: v1
kind: ConfigMap
metadata:
  name: observability-config
  namespace: $NAMESPACE
data:
  appsettings.Observability.json: |
$(cat src/SmartAlarm.Api/appsettings.Observability.json | sed 's/^/    /')
EOF

    # Restart applications to pick up new configuration
    kubectl rollout restart deployment -n $NAMESPACE

    log_info "Application monitoring configured ✓"
}

# Verify monitoring setup
verify_monitoring() {
    log_info "Verifying monitoring setup..."

    # Check Prometheus targets
    local targets_up=$(curl -s http://localhost:9090/api/v1/targets | jq '.data.activeTargets | map(select(.health == "up")) | length')
    local targets_total=$(curl -s http://localhost:9090/api/v1/targets | jq '.data.activeTargets | length')

    log_info "Prometheus targets: $targets_up/$targets_total up"

    # Check Grafana datasources
    local datasources=$(curl -s -H "Authorization: Bearer admin:smartalarm123" http://localhost:3000/api/datasources | jq 'length')
    log_info "Grafana datasources: $datasources configured"

    # Check AlertManager status
    local alertmanager_status=$(curl -s http://localhost:9093/api/v1/status | jq -r '.status')
    log_info "AlertManager status: $alertmanager_status"

    # Check Jaeger services
    local jaeger_services=$(curl -s http://localhost:16686/api/services | jq '.data | length')
    log_info "Jaeger services: $jaeger_services discovered"

    log_info "Monitoring verification completed ✓"
}

# Create monitoring documentation
create_documentation() {
    log_info "Creating monitoring documentation..."

    cat > docs/monitoring-setup.md << 'EOF'
# Smart Alarm Monitoring Setup

## Services Overview

| Service | URL | Purpose |
|---------|-----|---------|
| Grafana | http://localhost:3000 | Dashboards and visualization |
| Prometheus | http://localhost:9090 | Metrics collection and alerting |
| AlertManager | http://localhost:9093 | Alert routing and notifications |
| Jaeger | http://localhost:16686 | Distributed tracing |

## Default Credentials

- **Grafana**: admin / smartalarm123
- **Prometheus**: No authentication
- **AlertManager**: No authentication
- **Jaeger**: No authentication

## Key Dashboards

1. **System Overview** - Overall system health and SLA metrics
2. **Business Metrics** - User engagement and business KPIs
3. **Infrastructure** - Resource utilization and performance

## Alert Categories

- **Critical**: Immediate response required (< 15 minutes)
- **Warning**: Response within 1 hour
- **Business**: Response within business hours

## Runbooks

All runbooks are available in the `docs/runbooks/` directory:
- Service Down
- SLA Breaches
- High Error Rate
- Performance Issues

## Troubleshooting

If monitoring services are not working:

1. Check Docker containers: `docker-compose -f infrastructure/monitoring/docker-compose.monitoring.yml ps`
2. Check logs: `docker-compose -f infrastructure/monitoring/docker-compose.monitoring.yml logs`
3. Restart services: `docker-compose -f infrastructure/monitoring/docker-compose.monitoring.yml restart`

EOF

    log_info "Documentation created ✓"
}

# Print access information
print_access_info() {
    log_info "Monitoring setup completed successfully! 🎉"
    echo
    echo "📊 Access your monitoring services:"
    echo "  Grafana:      http://localhost:3000 (admin/smartalarm123)"
    echo "  Prometheus:   http://localhost:9090"
    echo "  AlertManager: http://localhost:9093"
    echo "  Jaeger:       http://localhost:16686"
    echo
    echo "📚 Documentation:"
    echo "  Runbooks:     docs/runbooks/"
    echo "  Setup Guide:  docs/monitoring-setup.md"
    echo
    echo "🚨 Test alerts:"
    echo "  kubectl delete pod -n $NAMESPACE -l app=smartalarm-api"
    echo
}

# Main execution
main() {
    log_info "Starting Smart Alarm monitoring setup..."

    check_prerequisites
    create_namespaces
    deploy_monitoring_stack
    configure_grafana
    configure_prometheus_alerts
    configure_alertmanager
    setup_application_monitoring
    verify_monitoring
    create_documentation
    print_access_info

    log_info "Setup completed successfully! ✅"
}

# Run main function
main "$@"
