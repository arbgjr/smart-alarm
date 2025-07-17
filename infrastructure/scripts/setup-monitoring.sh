#!/bin/bash

# SmartAlarm Monitoring Stack Setup
# Usage: ./setup-monitoring.sh [start|stop|restart|status]

set -euo pipefail

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

MONITORING_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
COMPOSE_FILE="$MONITORING_DIR/docker-compose.monitoring.yml"

echo -e "${BLUE}🔍 SmartAlarm Monitoring Stack Manager${NC}"
echo ""

# Function to check if Docker is running
check_docker() {
    if ! docker info &> /dev/null; then
        echo -e "${RED}❌ Docker is not running or not accessible${NC}"
        exit 1
    fi
    echo -e "${GREEN}✅ Docker is running${NC}"
}

# Function to check if docker-compose is available
check_compose() {
    if ! command -v docker-compose &> /dev/null; then
        echo -e "${RED}❌ docker-compose is not installed${NC}"
        exit 1
    fi
    echo -e "${GREEN}✅ docker-compose is available${NC}"
}

# Function to create required directories
create_directories() {
    echo -e "${BLUE}📁 Creating monitoring directories...${NC}"
    
    local dirs=(
        "grafana/dashboards"
        "grafana/datasources"
        "prometheus/alerts"
        "prometheus/rules"
        "alertmanager"
        "loki"
        "promtail"
    )
    
    for dir in "${dirs[@]}"; do
        mkdir -p "$MONITORING_DIR/$dir"
        echo -e "${GREEN}✅ Created $dir${NC}"
    done
}

# Function to create Grafana datasource configuration
create_grafana_datasources() {
    echo -e "${BLUE}⚙️  Creating Grafana datasources...${NC}"
    
    cat > "$MONITORING_DIR/grafana/datasources/datasources.yml" << EOF
apiVersion: 1

datasources:
  - name: Prometheus
    type: prometheus
    access: proxy
    url: http://prometheus:9090
    isDefault: true
    editable: true

  - name: Loki
    type: loki
    access: proxy
    url: http://loki:3100
    editable: true

  - name: Jaeger
    type: jaeger
    access: proxy
    url: http://jaeger:16686
    editable: true
EOF

    echo -e "${GREEN}✅ Grafana datasources configured${NC}"
}

# Function to create Grafana dashboard provisioning
create_grafana_dashboards_config() {
    echo -e "${BLUE}⚙️  Creating Grafana dashboard provisioning...${NC}"
    
    mkdir -p "$MONITORING_DIR/grafana/dashboards/configs"
    
    cat > "$MONITORING_DIR/grafana/dashboards/configs/dashboards.yml" << EOF
apiVersion: 1

providers:
  - name: 'SmartAlarm Dashboards'
    orgId: 1
    folder: 'SmartAlarm'
    type: file
    disableDeletion: false
    updateIntervalSeconds: 10
    allowUiUpdates: true
    options:
      path: /etc/grafana/provisioning/dashboards
EOF

    echo -e "${GREEN}✅ Grafana dashboard provisioning configured${NC}"
}

# Function to create Loki configuration
create_loki_config() {
    echo -e "${BLUE}⚙️  Creating Loki configuration...${NC}"
    
    cat > "$MONITORING_DIR/loki/loki.yml" << EOF
auth_enabled: false

server:
  http_listen_port: 3100
  grpc_listen_port: 9096

ingester:
  wal:
    enabled: true
    dir: /loki/wal
  lifecycler:
    address: 127.0.0.1
    ring:
      kvstore:
        store: inmemory
      replication_factor: 1
    final_sleep: 0s
  chunk_idle_period: 1h
  max_chunk_age: 1h
  chunk_target_size: 1048576
  chunk_retain_period: 30s
  max_transfer_retries: 0

schema_config:
  configs:
    - from: 2020-10-24
      store: boltdb-shipper
      object_store: filesystem
      schema: v11
      index:
        prefix: index_
        period: 24h

storage_config:
  boltdb_shipper:
    active_index_directory: /loki/boltdb-shipper-active
    cache_location: /loki/boltdb-shipper-cache
    cache_ttl: 24h
    shared_store: filesystem
  filesystem:
    directory: /loki/chunks

compactor:
  working_directory: /loki/boltdb-shipper-compactor
  shared_store: filesystem

limits_config:
  reject_old_samples: true
  reject_old_samples_max_age: 168h

chunk_store_config:
  max_look_back_period: 0s

table_manager:
  retention_deletes_enabled: false
  retention_period: 0s

ruler:
  storage:
    type: local
    local:
      directory: /loki/rules
  rule_path: /loki/rules-temp
  alertmanager_url: http://alertmanager:9093
  ring:
    kvstore:
      store: inmemory
  enable_api: true
EOF

    echo -e "${GREEN}✅ Loki configuration created${NC}"
}

# Function to create Promtail configuration
create_promtail_config() {
    echo -e "${BLUE}⚙️  Creating Promtail configuration...${NC}"
    
    cat > "$MONITORING_DIR/promtail/promtail.yml" << EOF
server:
  http_listen_port: 9080
  grpc_listen_port: 0

positions:
  filename: /tmp/positions.yaml

clients:
  - url: http://loki:3100/loki/api/v1/push

scrape_configs:
  - job_name: containers
    static_configs:
      - targets:
          - localhost
        labels:
          job: containerlogs
          __path__: /var/lib/docker/containers/*/*log

    pipeline_stages:
      - json:
          expressions:
            output: log
            stream: stream
            attrs:
      - json:
          expressions:
            tag:
          source: attrs
      - regex:
          expression: (?P<container_name>(?:[^|]*))/(?P<container_id>(?:[^|]*))
          source: tag
      - timestamp:
          format: RFC3339Nano
          source: time
      - labels:
          stream:
          container_name:
          container_id:
      - output:
          source: output

  - job_name: syslog
    static_configs:
      - targets:
          - localhost
        labels:
          job: syslog
          __path__: /var/log/syslog
EOF

    echo -e "${GREEN}✅ Promtail configuration created${NC}"
}

# Function to start monitoring stack
start_monitoring() {
    echo -e "${BLUE}🚀 Starting SmartAlarm monitoring stack...${NC}"
    
    check_docker
    check_compose
    create_directories
    create_grafana_datasources
    create_grafana_dashboards_config
    create_loki_config
    create_promtail_config
    
    echo -e "${BLUE}📦 Starting containers...${NC}"
    docker-compose -f "$COMPOSE_FILE" up -d
    
    echo -e "${BLUE}⏳ Waiting for services to be ready...${NC}"
    sleep 10
    
    # Check service health
    local services=("prometheus:9090" "grafana:3000" "alertmanager:9093" "loki:3100" "jaeger:16686")
    
    for service in "${services[@]}"; do
        local name=$(echo $service | cut -d':' -f1)
        local port=$(echo $service | cut -d':' -f2)
        
        if curl -f -s "http://localhost:$port" &> /dev/null; then
            echo -e "${GREEN}✅ $name is ready at http://localhost:$port${NC}"
        else
            echo -e "${YELLOW}⚠️  $name might not be fully ready yet${NC}"
        fi
    done
    
    echo ""
    echo -e "${GREEN}🎉 Monitoring stack started successfully!${NC}"
    echo ""
    echo -e "${BLUE}📊 Access URLs:${NC}"
    echo "• Grafana: http://localhost:3000 (admin/smartalarm123)"
    echo "• Prometheus: http://localhost:9090"
    echo "• Alertmanager: http://localhost:9093"
    echo "• Jaeger: http://localhost:16686"
    echo "• Loki: http://localhost:3100"
}

# Function to stop monitoring stack
stop_monitoring() {
    echo -e "${BLUE}🛑 Stopping SmartAlarm monitoring stack...${NC}"
    
    docker-compose -f "$COMPOSE_FILE" down
    
    echo -e "${GREEN}✅ Monitoring stack stopped${NC}"
}

# Function to restart monitoring stack
restart_monitoring() {
    echo -e "${BLUE}🔄 Restarting SmartAlarm monitoring stack...${NC}"
    
    stop_monitoring
    sleep 5
    start_monitoring
}

# Function to show monitoring stack status
show_status() {
    echo -e "${BLUE}📊 SmartAlarm Monitoring Stack Status${NC}"
    echo "==========================================="
    
    docker-compose -f "$COMPOSE_FILE" ps
    
    echo ""
    echo -e "${BLUE}🔍 Service Health Checks:${NC}"
    
    local services=("prometheus:9090" "grafana:3000" "alertmanager:9093" "loki:3100" "jaeger:16686")
    
    for service in "${services[@]}"; do
        local name=$(echo $service | cut -d':' -f1)
        local port=$(echo $service | cut -d':' -f2)
        
        if curl -f -s "http://localhost:$port" &> /dev/null; then
            echo -e "${GREEN}✅ $name: UP${NC}"
        else
            echo -e "${RED}❌ $name: DOWN${NC}"
        fi
    done
}

# Main execution
case "${1:-help}" in
    "start")
        start_monitoring
        ;;
    "stop")
        stop_monitoring
        ;;
    "restart")
        restart_monitoring
        ;;
    "status")
        show_status
        ;;
    "help"|*)
        echo "Usage: $0 [start|stop|restart|status]"
        echo ""
        echo "Commands:"
        echo "  start    - Start the monitoring stack"
        echo "  stop     - Stop the monitoring stack"
        echo "  restart  - Restart the monitoring stack"
        echo "  status   - Show current status"
        ;;
esac
