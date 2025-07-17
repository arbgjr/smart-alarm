#!/bin/bash

# SmartAlarm Deployment Validation Script
# Usage: ./validate-deployment.sh [namespace]
# Example: ./validate-deployment.sh smartalarm

set -euo pipefail

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Default values
NAMESPACE=${1:-smartalarm}
SERVICES=("alarm-service" "ai-service" "integration-service")

echo -e "${BLUE}🔍 SmartAlarm Deployment Validation${NC}"
echo -e "${BLUE}Namespace: ${NAMESPACE}${NC}"
echo ""

# Function to check if namespace exists
check_namespace() {
    echo -e "${BLUE}📁 Checking namespace...${NC}"
    if kubectl get namespace $NAMESPACE &> /dev/null; then
        echo -e "${GREEN}✅ Namespace $NAMESPACE exists${NC}"
    else
        echo -e "${RED}❌ Namespace $NAMESPACE not found${NC}"
        exit 1
    fi
}

# Function to validate deployments
validate_deployments() {
    echo -e "${BLUE}🚀 Validating deployments...${NC}"
    
    for service in "${SERVICES[@]}"; do
        echo -e "${YELLOW}🔍 Checking $service deployment...${NC}"
        
        # Check if deployment exists
        if ! kubectl get deployment $service -n $NAMESPACE &> /dev/null; then
            echo -e "${RED}❌ Deployment $service not found${NC}"
            continue
        fi
        
        # Check deployment status
        local ready_replicas=$(kubectl get deployment $service -n $NAMESPACE -o jsonpath='{.status.readyReplicas}')
        local desired_replicas=$(kubectl get deployment $service -n $NAMESPACE -o jsonpath='{.spec.replicas}')
        
        if [[ "$ready_replicas" == "$desired_replicas" ]]; then
            echo -e "${GREEN}✅ $service: $ready_replicas/$desired_replicas replicas ready${NC}"
        else
            echo -e "${RED}❌ $service: $ready_replicas/$desired_replicas replicas ready${NC}"
        fi
    done
}

# Function to validate services
validate_services() {
    echo -e "${BLUE}🌐 Validating services...${NC}"
    
    for service in "${SERVICES[@]}"; do
        echo -e "${YELLOW}🔍 Checking $service service...${NC}"
        
        if kubectl get service $service -n $NAMESPACE &> /dev/null; then
            local service_type=$(kubectl get service $service -n $NAMESPACE -o jsonpath='{.spec.type}')
            local cluster_ip=$(kubectl get service $service -n $NAMESPACE -o jsonpath='{.spec.clusterIP}')
            echo -e "${GREEN}✅ $service: $service_type at $cluster_ip${NC}"
        else
            echo -e "${RED}❌ Service $service not found${NC}"
        fi
    done
}

# Function to validate pods
validate_pods() {
    echo -e "${BLUE}🐳 Validating pods...${NC}"
    
    local total_pods=0
    local ready_pods=0
    
    for service in "${SERVICES[@]}"; do
        echo -e "${YELLOW}🔍 Checking $service pods...${NC}"
        
        local pods=$(kubectl get pods -l app=$service -n $NAMESPACE --no-headers)
        
        if [[ -z "$pods" ]]; then
            echo -e "${RED}❌ No pods found for $service${NC}"
            continue
        fi
        
        while IFS= read -r pod_line; do
            local pod_name=$(echo $pod_line | awk '{print $1}')
            local pod_status=$(echo $pod_line | awk '{print $3}')
            local pod_ready=$(echo $pod_line | awk '{print $2}')
            
            total_pods=$((total_pods + 1))
            
            if [[ "$pod_status" == "Running" && "$pod_ready" == *"/"* ]]; then
                local ready_containers=$(echo $pod_ready | cut -d'/' -f1)
                local total_containers=$(echo $pod_ready | cut -d'/' -f2)
                
                if [[ "$ready_containers" == "$total_containers" ]]; then
                    ready_pods=$((ready_pods + 1))
                    echo -e "${GREEN}✅ $pod_name: $pod_status ($pod_ready)${NC}"
                else
                    echo -e "${YELLOW}⚠️  $pod_name: $pod_status ($pod_ready)${NC}"
                fi
            else
                echo -e "${RED}❌ $pod_name: $pod_status ($pod_ready)${NC}"
            fi
        done <<< "$pods"
    done
    
    echo -e "${BLUE}📊 Pod Summary: $ready_pods/$total_pods pods ready${NC}"
}

# Function to validate health endpoints
validate_health() {
    echo -e "${BLUE}🏥 Validating health endpoints...${NC}"
    
    for service in "${SERVICES[@]}"; do
        echo -e "${YELLOW}🔍 Checking $service health...${NC}"
        
        local service_ip=$(kubectl get service $service -n $NAMESPACE -o jsonpath='{.spec.clusterIP}' 2>/dev/null)
        local service_port=$(kubectl get service $service -n $NAMESPACE -o jsonpath='{.spec.ports[0].port}' 2>/dev/null)
        
        if [[ -n "$service_ip" && -n "$service_port" ]]; then
            # Create a temporary curl pod
            if kubectl run temp-health-check-$service --rm -i --restart=Never --image=curlimages/curl:latest -- \
               curl -f -s --max-time 10 "http://$service_ip:$service_port/health" &> /dev/null; then
                echo -e "${GREEN}✅ $service health endpoint responding${NC}"
            else
                echo -e "${RED}❌ $service health endpoint not responding${NC}"
            fi
        else
            echo -e "${RED}❌ Cannot get service details for $service${NC}"
        fi
    done
}

# Function to validate ingress
validate_ingress() {
    echo -e "${BLUE}🌍 Validating ingress...${NC}"
    
    local ingresses=$(kubectl get ingress -n $NAMESPACE --no-headers 2>/dev/null)
    
    if [[ -z "$ingresses" ]]; then
        echo -e "${YELLOW}⚠️  No ingress resources found${NC}"
        return
    fi
    
    while IFS= read -r ingress_line; do
        local ingress_name=$(echo $ingress_line | awk '{print $1}')
        local hosts=$(echo $ingress_line | awk '{print $3}')
        
        echo -e "${GREEN}✅ Ingress $ingress_name: $hosts${NC}"
    done <<< "$ingresses"
}

# Function to validate HPA
validate_hpa() {
    echo -e "${BLUE}📈 Validating HPA...${NC}"
    
    for service in "${SERVICES[@]}"; do
        local hpa_name="${service}-hpa"
        
        if kubectl get hpa $hpa_name -n $NAMESPACE &> /dev/null; then
            local targets=$(kubectl get hpa $hpa_name -n $NAMESPACE -o jsonpath='{.status.currentMetrics}')
            local min_replicas=$(kubectl get hpa $hpa_name -n $NAMESPACE -o jsonpath='{.spec.minReplicas}')
            local max_replicas=$(kubectl get hpa $hpa_name -n $NAMESPACE -o jsonpath='{.spec.maxReplicas}')
            
            echo -e "${GREEN}✅ $hpa_name: $min_replicas-$max_replicas replicas${NC}"
        else
            echo -e "${YELLOW}⚠️  HPA for $service not found${NC}"
        fi
    done
}

# Function to validate ConfigMaps and Secrets
validate_config() {
    echo -e "${BLUE}⚙️  Validating configuration...${NC}"
    
    # Check ConfigMap
    if kubectl get configmap smartalarm-config -n $NAMESPACE &> /dev/null; then
        echo -e "${GREEN}✅ ConfigMap smartalarm-config exists${NC}"
    else
        echo -e "${RED}❌ ConfigMap smartalarm-config not found${NC}"
    fi
    
    # Check Secrets
    if kubectl get secret smartalarm-secrets -n $NAMESPACE &> /dev/null; then
        echo -e "${GREEN}✅ Secret smartalarm-secrets exists${NC}"
    else
        echo -e "${RED}❌ Secret smartalarm-secrets not found${NC}"
    fi
}

# Function to show resource usage
show_resource_usage() {
    echo -e "${BLUE}📊 Resource Usage${NC}"
    echo "=================="
    
    echo -e "${YELLOW}💾 Pod Resource Usage:${NC}"
    if command -v kubectl &> /dev/null && kubectl top pods -n $NAMESPACE &> /dev/null; then
        kubectl top pods -n $NAMESPACE
    else
        echo "Resource metrics not available (metrics-server required)"
    fi
    
    echo ""
    echo -e "${YELLOW}🖥️  Node Resource Usage:${NC}"
    if kubectl top nodes &> /dev/null; then
        kubectl top nodes
    else
        echo "Node metrics not available (metrics-server required)"
    fi
}

# Function to generate deployment report
generate_report() {
    echo ""
    echo -e "${BLUE}📋 Deployment Report${NC}"
    echo "===================="
    
    # Count resources
    local deployment_count=$(kubectl get deployments -n $NAMESPACE --no-headers | wc -l)
    local service_count=$(kubectl get services -n $NAMESPACE --no-headers | wc -l)
    local pod_count=$(kubectl get pods -n $NAMESPACE --no-headers | wc -l)
    local running_pods=$(kubectl get pods -n $NAMESPACE --no-headers | grep Running | wc -l)
    
    echo -e "${YELLOW}📦 Deployments: $deployment_count${NC}"
    echo -e "${YELLOW}🌐 Services: $service_count${NC}"
    echo -e "${YELLOW}🐳 Pods: $running_pods/$pod_count running${NC}"
    
    # Overall status
    echo ""
    if [[ "$running_pods" -eq "$pod_count" && "$pod_count" -gt 0 ]]; then
        echo -e "${GREEN}🎉 Overall Status: HEALTHY${NC}"
    elif [[ "$running_pods" -gt 0 ]]; then
        echo -e "${YELLOW}⚠️  Overall Status: PARTIALLY HEALTHY${NC}"
    else
        echo -e "${RED}❌ Overall Status: UNHEALTHY${NC}"
    fi
}

# Main execution
main() {
    check_namespace
    echo ""
    
    validate_deployments
    echo ""
    
    validate_services
    echo ""
    
    validate_pods
    echo ""
    
    validate_health
    echo ""
    
    validate_ingress
    echo ""
    
    validate_hpa
    echo ""
    
    validate_config
    echo ""
    
    show_resource_usage
    
    generate_report
}

# Run main function
main "$@"
