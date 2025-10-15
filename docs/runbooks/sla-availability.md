# SLA Availability Breach Runbook

## Alert Description

**Alert Name**: SLAAvailabilityBreach  
**Severity**: Critical  
**Trigger**: System availability drops below 99.9% over 5-minute window  
**SLA Target**: 99.9% availability (43.2 minutes downtime per month)

## Immediate Actions (< 2 minutes)

### 1. Assess Impact Scope

```bash
# Check which services are affected
curl -s http://prometheus:9090/api/v1/query?query='up{job=~"smartalarm-.*"}' | jq '.data.result[] | select(.value[1] == "0")'

# Check error rates by service
curl -s http://prometheus:9090/api/v1/query?query='rate(smartalarm_requests_total{status_code=~"5.."}[5m])' | jq
```

### 2. Quick Status Check

- Open Grafana System Overview Dashboard
- Check service status panel
- Identify failing components

## Diagnosis Steps

### 1. Identify Root Cause

```bash
# Check recent deployments
kubectl rollout history deployment -n smartalarm

# Check system events
kubectl get events -n smartalarm --sort-by='.lastTimestamp' | head -20

# Check load balancer status
kubectl get ingress -n smartalarm
```

### 2. Analyze Error Patterns

```bash
# Get error breakdown by service
curl -s "http://prometheus:9090/api/v1/query?query=rate(smartalarm_requests_total{status_code=~'5..'}[5m]) by (service)" | jq

# Check response time distribution
curl -s "http://prometheus:9090/api/v1/query?query=histogram_quantile(0.95, rate(smartalarm_request_duration_seconds_bucket[5m]))" | jq
```

### 3. Infrastructure Health

```bash
# Check node status
kubectl get nodes

# Check cluster resources
kubectl top nodes
kubectl top pods -n smartalarm

# Check persistent volumes
kubectl get pv,pvc -n smartalarm
```

## Common Scenarios and Solutions

### 1. Database Outage

**Symptoms**: All services returning 500 errors, database connection failures

```bash
# Check database pod status
kubectl get pods -n smartalarm -l app=postgres

# Check database logs
kubectl logs -n smartalarm -l app=postgres --tail=50

# Immediate mitigation: Enable read-only mode
kubectl patch configmap smartalarm-config -n smartalarm --patch '{"data":{"READONLY_MODE":"true"}}'
kubectl rollout restart deployment -n smartalarm
```

### 2. Load Balancer Issues

**Symptoms**: External traffic not reaching services, timeouts

```bash
# Check ingress controller
kubectl get pods -n ingress-nginx

# Check ingress configuration
kubectl describe ingress -n smartalarm smartalarm-ingress

# Restart ingress controller if needed
kubectl rollout restart deployment/nginx-ingress-controller -n ingress-nginx
```

### 3. Resource Exhaustion

**Symptoms**: Pods being evicted, high resource usage

```bash
# Check resource usage
kubectl describe nodes | grep -A 5 "Allocated resources"

# Scale critical services
kubectl scale deployment/smartalarm-api --replicas=5 -n smartalarm
kubectl scale deployment/alarm-service --replicas=3 -n smartalarm
```

### 4. Network Issues

**Symptoms**: Service-to-service communication failures

```bash
# Test internal connectivity
kubectl exec -n smartalarm deployment/smartalarm-api -- nc -zv postgres 5432
kubectl exec -n smartalarm deployment/smartalarm-api -- nc -zv redis 6379

# Check network policies
kubectl get networkpolicies -n smartalarm
```

## Recovery Actions

### 1. Immediate Mitigation

```bash
# Enable maintenance mode if needed
kubectl create configmap maintenance-mode --from-literal=enabled=true -n smartalarm

# Scale up healthy services
kubectl scale deployment/smartalarm-api --replicas=10 -n smartalarm

# Redirect traffic to backup region (if available)
# Update DNS or load balancer configuration
```

### 2. Service Recovery

```bash
# Restart failed services
kubectl rollout restart deployment -n smartalarm

# Monitor recovery progress
watch kubectl get pods -n smartalarm

# Check service health
for service in api alarm-service ai-service integration-service; do
  echo "Checking $service..."
  kubectl exec -n smartalarm deployment/smartalarm-api -- curl -f http://$service:8080/health
done
```

### 3. Data Integrity Check

```bash
# Verify database consistency
kubectl exec -n smartalarm deployment/postgres -- psql -U smartalarm -c "SELECT COUNT(*) FROM alarms;"

# Check for data corruption
kubectl exec -n smartalarm deployment/postgres -- psql -U smartalarm -c "VACUUM ANALYZE;"
```

## SLA Recovery Calculation

### Current Availability Calculation

```bash
# Get current availability percentage
curl -s "http://prometheus:9090/api/v1/query?query=(1 - (rate(smartalarm_requests_total{status_code=~'5..'}[24h]) / rate(smartalarm_requests_total[24h]))) * 100" | jq '.data.result[0].value[1]'
```

### Time to SLA Recovery

- **Current downtime**: Calculate from alert start time
- **Remaining monthly budget**: 43.2 minutes - current month downtime
- **Recovery time needed**: Time to get back above 99.9%

## Verification Steps

### 1. Service Health Verification

```bash
# Check all services are healthy
kubectl get pods -n smartalarm | grep -v Running

# Verify endpoints respond
curl -f http://smartalarm.com/health
curl -f http://smartalarm.com/api/health
```

### 2. Performance Verification

- Check Grafana dashboards show normal metrics
- Verify response times are below SLA thresholds
- Confirm error rates are below 1%

### 3. End-to-End Testing

```bash
# Test critical user journeys
./scripts/e2e-health-check.sh

# Verify alarm creation and triggering
curl -X POST http://smartalarm.com/api/alarms \
  -H "Authorization: Bearer $TEST_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"SLA Test","time":"'$(date -d "+1 minute" +%H:%M)'"}'
```

## Communication

### 1. Immediate Notification

```bash
# Update status page
curl -X POST https://status.smartalarm.com/api/incidents \
  -H "Authorization: Bearer $STATUS_TOKEN" \
  -d '{"name":"Service Degradation","status":"investigating"}'

# Notify stakeholders
# Send message to #incidents Slack channel
# Email executive team if downtime > 10 minutes
```

### 2. Regular Updates

- Update status page every 15 minutes during incident
- Provide ETA for resolution
- Communicate impact and mitigation steps

## Prevention Measures

1. **Redundancy**: Implement multi-region deployment
2. **Circuit Breakers**: Add circuit breakers for all external dependencies
3. **Graceful Degradation**: Implement feature flags for non-critical features
4. **Capacity Planning**: Regular load testing and capacity reviews
5. **Monitoring**: Enhanced monitoring for early detection

## Escalation

### Immediate Escalation (< 5 minutes)

- On-call engineer: +55 11 99999-9999
- Engineering manager: +55 11 88888-8888

### Executive Escalation (> 15 minutes downtime)

- CTO: +55 11 77777-7777
- CEO: +55 11 66666-6666

## Post-Incident Actions

1. **Incident Report**: Document timeline, root cause, and impact
2. **SLA Analysis**: Calculate actual availability impact
3. **Customer Communication**: Notify affected customers
4. **Process Improvement**: Update runbooks and monitoring
5. **Post-Mortem**: Schedule blameless post-mortem within 48 hours
