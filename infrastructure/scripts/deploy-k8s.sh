#!/bin/bash

# Deploy SmartAlarm to Kubernetes
# Usage: ./deploy-k8s.sh [environment] [--dry-run]
# Example: ./deploy-k8s.sh development --dry-run

set -euo pipefail

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Default values
ENVIRONMENT=${1:-development}
DRY_RUN=${2:-""}
NAMESPACE="smartalarm"
KUBECTL_CMD="kubectl"

# Add dry-run flag if specified
if [[ "$DRY_RUN" == "--dry-run" ]]; then
    KUBECTL_CMD="kubectl --dry-run=client"
    echo -e "${YELLOW}🔍 Running in DRY-RUN mode${NC}"
fi

echo -e "${BLUE}🚀 Deploying SmartAlarm to Kubernetes${NC}"
echo -e "${BLUE}Environment: ${ENVIRONMENT}${NC}"
echo -e "${BLUE}Namespace: ${NAMESPACE}${NC}"
echo ""

# Function to check if kubectl is available
check_kubectl() {
    if ! command -v kubectl &> /dev/null; then
        echo -e "${RED}❌ kubectl is not installed or not in PATH${NC}"
        exit 1
    fi
    echo -e "${GREEN}✅ kubectl is available${NC}"
}

# Function to check cluster connection
check_cluster() {
    if ! kubectl cluster-info &> /dev/null; then
        echo -e "${RED}❌ Cannot connect to Kubernetes cluster${NC}"
        echo -e "${YELLOW}💡 Make sure your kubeconfig is properly configured${NC}"
        exit 1
    fi
    echo -e "${GREEN}✅ Connected to Kubernetes cluster${NC}"
    kubectl cluster-info | head -1
}

# Function to create namespace if it doesn't exist
create_namespace() {
    echo -e "${BLUE}📁 Creating namespace...${NC}"
    if kubectl get namespace $NAMESPACE &> /dev/null; then
        echo -e "${YELLOW}⚠️  Namespace $NAMESPACE already exists${NC}"
    else
        $KUBECTL_CMD apply -f infrastructure/kubernetes/namespace.yaml
        echo -e "${GREEN}✅ Namespace created${NC}"
    fi
}

# Function to deploy services
deploy_services() {
    echo -e "${BLUE}🔧 Deploying services...${NC}"
    
    local services=("alarm-service" "ai-service" "integration-service")
    
    for service in "${services[@]}"; do
        echo -e "${YELLOW}📦 Deploying $service...${NC}"
        
        if [[ -f "infrastructure/kubernetes/$service.yaml" ]]; then
            $KUBECTL_CMD apply -f "infrastructure/kubernetes/$service.yaml"
            echo -e "${GREEN}✅ $service deployed${NC}"
        else
            echo -e "${RED}❌ Manifest file for $service not found${NC}"
            exit 1
        fi
    done
}

# Function to wait for deployments
wait_for_deployments() {
    if [[ "$DRY_RUN" == "--dry-run" ]]; then
        echo -e "${YELLOW}⏭️  Skipping deployment wait (dry-run mode)${NC}"
        return 0
    fi
    
    echo -e "${BLUE}⏳ Waiting for deployments to be ready...${NC}"
    
    local services=("alarm-service" "ai-service" "integration-service")
    
    for service in "${services[@]}"; do
        echo -e "${YELLOW}⏳ Waiting for $service deployment...${NC}"
        
        if kubectl rollout status deployment/$service -n $NAMESPACE --timeout=300s; then
            echo -e "${GREEN}✅ $service is ready${NC}"
        else
            echo -e "${RED}❌ $service deployment failed or timed out${NC}"
            echo -e "${YELLOW}📋 Deployment status:${NC}"
            kubectl get deployment $service -n $NAMESPACE
            echo -e "${YELLOW}📋 Pod logs:${NC}"
            kubectl logs -l app=$service -n $NAMESPACE --tail=50
            exit 1
        fi
    done
}

# Function to check service health
check_health() {
    if [[ "$DRY_RUN" == "--dry-run" ]]; then
        echo -e "${YELLOW}⏭️  Skipping health checks (dry-run mode)${NC}"
        return 0
    fi
    
    echo -e "${BLUE}🏥 Checking service health...${NC}"
    
    local services=("alarm-service" "ai-service" "integration-service")
    
    for service in "${services[@]}"; do
        echo -e "${YELLOW}🔍 Checking $service health...${NC}"
        
        local service_ip=$(kubectl get service $service -n $NAMESPACE -o jsonpath='{.spec.clusterIP}')
        local service_port=$(kubectl get service $service -n $NAMESPACE -o jsonpath='{.spec.ports[0].port}')
        
        if kubectl run temp-curl-$service --rm -i --restart=Never --image=curlimages/curl -- \
           curl -f -s "http://$service_ip:$service_port/health" &> /dev/null; then
            echo -e "${GREEN}✅ $service is healthy${NC}"
        else
            echo -e "${YELLOW}⚠️  Health check for $service failed or not yet ready${NC}"
        fi
    done
}

# Function to show deployment status
show_status() {
    echo -e "${BLUE}📊 Deployment Status${NC}"
    echo "=========================="
    
    echo -e "${YELLOW}📦 Deployments:${NC}"
    kubectl get deployments -n $NAMESPACE
    
    echo ""
    echo -e "${YELLOW}🐳 Pods:${NC}"
    kubectl get pods -n $NAMESPACE
    
    echo ""
    echo -e "${YELLOW}🌐 Services:${NC}"
    kubectl get services -n $NAMESPACE
    
    if kubectl get ingress -n $NAMESPACE &> /dev/null; then
        echo ""
        echo -e "${YELLOW}🌍 Ingresses:${NC}"
        kubectl get ingress -n $NAMESPACE
    fi
    
    echo ""
    echo -e "${YELLOW}📈 HPA Status:${NC}"
    kubectl get hpa -n $NAMESPACE
}

# Function to show access information
show_access_info() {
    echo -e "${BLUE}🔗 Access Information${NC}"
    echo "=========================="
    
    echo -e "${YELLOW}🌐 Service URLs:${NC}"
    echo "• Alarm Service: http://alarms.smartalarm.local"
    echo "• AI Service: http://ai.smartalarm.local"
    echo "• Integration Service: http://integrations.smartalarm.local"
    
    echo ""
    echo -e "${YELLOW}📋 Port Forward Commands:${NC}"
    echo "• kubectl port-forward svc/alarm-service 8080:80 -n $NAMESPACE"
    echo "• kubectl port-forward svc/ai-service 8081:80 -n $NAMESPACE"
    echo "• kubectl port-forward svc/integration-service 8082:80 -n $NAMESPACE"
    
    echo ""
    echo -e "${YELLOW}📊 Monitoring Commands:${NC}"
    echo "• kubectl logs -f deployment/alarm-service -n $NAMESPACE"
    echo "• kubectl describe pod -l app=alarm-service -n $NAMESPACE"
    echo "• kubectl top pods -n $NAMESPACE"
}

# Main execution
main() {
    echo -e "${BLUE}🔍 Pre-flight checks...${NC}"
    check_kubectl
    check_cluster
    
    echo ""
    create_namespace
    
    echo ""
    deploy_services
    
    echo ""
    wait_for_deployments
    
    echo ""
    check_health
    
    echo ""
    show_status
    
    echo ""
    show_access_info
    
    echo ""
    echo -e "${GREEN}🎉 SmartAlarm deployment completed successfully!${NC}"
    echo -e "${GREEN}Environment: $ENVIRONMENT${NC}"
    echo -e "${GREEN}Namespace: $NAMESPACE${NC}"
}

# Trap to handle script interruption
trap 'echo -e "${RED}❌ Deployment interrupted${NC}"; exit 1' INT TERM

# Run main function
main "$@"
