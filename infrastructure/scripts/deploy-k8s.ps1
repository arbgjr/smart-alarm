# Deploy SmartAlarm to Kubernetes (PowerShell)
# Usage: .\deploy-k8s.ps1 [-Environment development] [-DryRun]
# Example: .\deploy-k8s.ps1 -Environment production -DryRun

[CmdletBinding()]
param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("development", "staging", "production")]
    [string]$Environment = "development",
    
    [Parameter(Mandatory=$false)]
    [switch]$DryRun,
    
    [Parameter(Mandatory=$false)]
    [string]$Namespace = "smartalarm"
)

# Error handling
$ErrorActionPreference = "Stop"

# Colors for output
function Write-ColorOutput {
    param(
        [string]$Message,
        [ValidateSet("Red", "Green", "Yellow", "Blue", "Cyan", "Magenta", "White")]
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

Write-ColorOutput "🚀 Deploying SmartAlarm to Kubernetes" -Color Blue
Write-ColorOutput "Environment: $Environment" -Color Blue
Write-ColorOutput "Namespace: $Namespace" -Color Blue

if ($DryRun) {
    Write-ColorOutput "🔍 Running in DRY-RUN mode" -Color Yellow
}

Write-Host ""

# Function to check if kubectl is available
function Test-Kubectl {
    try {
        kubectl version --client --short | Out-Null
        Write-ColorOutput "✅ kubectl is available" -Color Green
        return $true
    }
    catch {
        Write-ColorOutput "❌ kubectl is not installed or not in PATH" -Color Red
        exit 1
    }
}

# Function to check cluster connection
function Test-Cluster {
    try {
        kubectl cluster-info | Out-Null
        Write-ColorOutput "✅ Connected to Kubernetes cluster" -Color Green
        $clusterInfo = kubectl cluster-info | Select-Object -First 1
        Write-Host $clusterInfo
        return $true
    }
    catch {
        Write-ColorOutput "❌ Cannot connect to Kubernetes cluster" -Color Red
        Write-ColorOutput "💡 Make sure your kubeconfig is properly configured" -Color Yellow
        exit 1
    }
}

# Function to create namespace if it doesn't exist
function New-NamespaceIfNotExists {
    Write-ColorOutput "📁 Creating namespace..." -Color Blue
    
    try {
        kubectl get namespace $Namespace | Out-Null
        Write-ColorOutput "⚠️  Namespace $Namespace already exists" -Color Yellow
    }
    catch {
        if ($DryRun) {
            kubectl apply -f infrastructure/kubernetes/namespace.yaml --dry-run=client
        } else {
            kubectl apply -f infrastructure/kubernetes/namespace.yaml
        }
        Write-ColorOutput "✅ Namespace created" -Color Green
    }
}

# Function to deploy services
function Deploy-Services {
    Write-ColorOutput "🔧 Deploying services..." -Color Blue
    
    $services = @("alarm-service", "ai-service", "integration-service")
    
    foreach ($service in $services) {
        Write-ColorOutput "📦 Deploying $service..." -Color Yellow
        
        $manifestPath = "infrastructure/kubernetes/$service.yaml"
        
        if (Test-Path $manifestPath) {
            if ($DryRun) {
                kubectl apply -f $manifestPath --dry-run=client
            } else {
                kubectl apply -f $manifestPath
            }
            Write-ColorOutput "✅ $service deployed" -Color Green
        } else {
            Write-ColorOutput "❌ Manifest file for $service not found" -Color Red
            exit 1
        }
    }
}

# Function to wait for deployments
function Wait-ForDeployments {
    if ($DryRun) {
        Write-ColorOutput "⏭️  Skipping deployment wait (dry-run mode)" -Color Yellow
        return
    }
    
    Write-ColorOutput "⏳ Waiting for deployments to be ready..." -Color Blue
    
    $services = @("alarm-service", "ai-service", "integration-service")
    
    foreach ($service in $services) {
        Write-ColorOutput "⏳ Waiting for $service deployment..." -Color Yellow
        
        try {
            kubectl rollout status deployment/$service -n $Namespace --timeout=300s
            Write-ColorOutput "✅ $service is ready" -Color Green
        }
        catch {
            Write-ColorOutput "❌ $service deployment failed or timed out" -Color Red
            Write-ColorOutput "📋 Deployment status:" -Color Yellow
            kubectl get deployment $service -n $Namespace
            Write-ColorOutput "📋 Pod logs:" -Color Yellow
            kubectl logs -l app=$service -n $Namespace --tail=50
            exit 1
        }
    }
}

# Function to check service health
function Test-ServiceHealth {
    if ($DryRun) {
        Write-ColorOutput "⏭️  Skipping health checks (dry-run mode)" -Color Yellow
        return
    }
    
    Write-ColorOutput "🏥 Checking service health..." -Color Blue
    
    $services = @("alarm-service", "ai-service", "integration-service")
    
    foreach ($service in $services) {
        Write-ColorOutput "🔍 Checking $service health..." -Color Yellow
        
        try {
            $serviceIp = kubectl get service $service -n $Namespace -o jsonpath='{.spec.clusterIP}'
            $servicePort = kubectl get service $service -n $Namespace -o jsonpath='{.spec.ports[0].port}'
            
            $curlResult = kubectl run temp-curl-$service --rm -i --restart=Never --image=curlimages/curl -- curl -f -s "http://$serviceIp`:$servicePort/health" 2>$null
            
            if ($LASTEXITCODE -eq 0) {
                Write-ColorOutput "✅ $service is healthy" -Color Green
            } else {
                Write-ColorOutput "⚠️  Health check for $service failed or not yet ready" -Color Yellow
            }
        }
        catch {
            Write-ColorOutput "⚠️  Health check for $service failed or not yet ready" -Color Yellow
        }
    }
}

# Function to show deployment status
function Show-DeploymentStatus {
    Write-ColorOutput "📊 Deployment Status" -Color Blue
    Write-Host "=========================="
    
    Write-ColorOutput "📦 Deployments:" -Color Yellow
    kubectl get deployments -n $Namespace
    
    Write-Host ""
    Write-ColorOutput "🐳 Pods:" -Color Yellow
    kubectl get pods -n $Namespace
    
    Write-Host ""
    Write-ColorOutput "🌐 Services:" -Color Yellow
    kubectl get services -n $Namespace
    
    try {
        kubectl get ingress -n $Namespace | Out-Null
        Write-Host ""
        Write-ColorOutput "🌍 Ingresses:" -Color Yellow
        kubectl get ingress -n $Namespace
    } catch {
        # Ingress not found, skip
    }
    
    Write-Host ""
    Write-ColorOutput "📈 HPA Status:" -Color Yellow
    kubectl get hpa -n $Namespace
}

# Function to show access information
function Show-AccessInfo {
    Write-ColorOutput "🔗 Access Information" -Color Blue
    Write-Host "=========================="
    
    Write-ColorOutput "🌐 Service URLs:" -Color Yellow
    Write-Host "• Alarm Service: http://alarms.smartalarm.local"
    Write-Host "• AI Service: http://ai.smartalarm.local"
    Write-Host "• Integration Service: http://integrations.smartalarm.local"
    
    Write-Host ""
    Write-ColorOutput "📋 Port Forward Commands:" -Color Yellow
    Write-Host "• kubectl port-forward svc/alarm-service 8080:80 -n $Namespace"
    Write-Host "• kubectl port-forward svc/ai-service 8081:80 -n $Namespace"
    Write-Host "• kubectl port-forward svc/integration-service 8082:80 -n $Namespace"
    
    Write-Host ""
    Write-ColorOutput "📊 Monitoring Commands:" -Color Yellow
    Write-Host "• kubectl logs -f deployment/alarm-service -n $Namespace"
    Write-Host "• kubectl describe pod -l app=alarm-service -n $Namespace"
    Write-Host "• kubectl top pods -n $Namespace"
}

# Main execution
try {
    Write-ColorOutput "🔍 Pre-flight checks..." -Color Blue
    Test-Kubectl
    Test-Cluster
    
    Write-Host ""
    New-NamespaceIfNotExists
    
    Write-Host ""
    Deploy-Services
    
    Write-Host ""
    Wait-ForDeployments
    
    Write-Host ""
    Test-ServiceHealth
    
    Write-Host ""
    Show-DeploymentStatus
    
    Write-Host ""
    Show-AccessInfo
    
    Write-Host ""
    Write-ColorOutput "🎉 SmartAlarm deployment completed successfully!" -Color Green
    Write-ColorOutput "Environment: $Environment" -Color Green
    Write-ColorOutput "Namespace: $Namespace" -Color Green
}
catch {
    Write-ColorOutput "❌ Deployment failed: $($_.Exception.Message)" -Color Red
    exit 1
}
