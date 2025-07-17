#!/bin/bash

# Build and Deploy Smart Alarm Microservices
# Author: Smart Alarm Team
# Usage: ./build-services.sh [service-name]

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}🏗️  Smart Alarm Microservices Build Script${NC}"
echo "=============================================="

# Function to build a specific service
build_service() {
    local service=$1
    local dockerfile="services/$service/Dockerfile"
    local image_name="smartalarm/$service"
    local version=${VERSION:-"latest"}
    
    echo -e "${YELLOW}Building $service...${NC}"
    
    if [ ! -f "$dockerfile" ]; then
        echo -e "${RED}❌ Dockerfile not found: $dockerfile${NC}"
        return 1
    fi
    
    # Build the Docker image
    docker build \
        -f "$dockerfile" \
        -t "$image_name:$version" \
        -t "$image_name:latest" \
        . || {
        echo -e "${RED}❌ Failed to build $service${NC}"
        return 1
    }
    
    echo -e "${GREEN}✅ Successfully built $service${NC}"
    return 0
}

# Function to build all services
build_all_services() {
    echo -e "${YELLOW}Building all microservices...${NC}"
    
    services=("alarm-service" "ai-service" "integration-service")
    
    for service in "${services[@]}"; do
        build_service "$service" || exit 1
    done
    
    echo -e "${GREEN}✅ All services built successfully!${NC}"
}

# Function to test builds
test_builds() {
    echo -e "${YELLOW}Testing built images...${NC}"
    
    services=("alarm-service" "ai-service" "integration-service")
    
    for service in "${services[@]}"; do
        echo "Testing smartalarm/$service..."
        docker run --rm --health-timeout=10s "smartalarm/$service:latest" --version || {
            echo -e "${RED}❌ Health check failed for $service${NC}"
            exit 1
        }
    done
    
    echo -e "${GREEN}✅ All image tests passed!${NC}"
}

# Function to show help
show_help() {
    echo "Usage: $0 [service-name|all|test|help]"
    echo ""
    echo "Commands:"
    echo "  alarm-service      Build only the Alarm Service"
    echo "  ai-service         Build only the AI Service"
    echo "  integration-service Build only the Integration Service"
    echo "  all                Build all services (default)"
    echo "  test               Test all built images"
    echo "  help               Show this help message"
    echo ""
    echo "Environment Variables:"
    echo "  VERSION            Version tag for the images (default: latest)"
    echo ""
    echo "Examples:"
    echo "  $0 alarm-service           # Build only alarm service"
    echo "  VERSION=1.2.0 $0 all       # Build all with version 1.2.0"
    echo "  $0 test                     # Test all built images"
}

# Main script logic
case "${1:-all}" in
    "alarm-service"|"ai-service"|"integration-service")
        build_service "$1"
        ;;
    "all")
        build_all_services
        ;;
    "test")
        test_builds
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

echo -e "${GREEN}🎉 Build process completed successfully!${NC}"
