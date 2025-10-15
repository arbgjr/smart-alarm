# High Error Rate Runbook

## Alert Description

**Alert Name**: HighErrorRate  
**Severity**: Critical  
**Trigger**: Error rate > 5% for more than 2 minutes  
**Normal Range**: < 1% error rate

## Immediate Actions (< 3 minutes)

### 1. Identify Error Sources

```bash
# Check error rate by service
curl -s "http://prometheus:9090/api/v1/query?query=rate(smartalarm_requests_total{status_code=~'5..'}[5m]) by (service)" | jq

# Check error rate by endpoint
curl -s "http://prometheus:9090/api/v1/query?query=rate(smartalarm_requests_total{status_code=~'5..'}[5m]) by (path)" | jq

# Get specific error codes
curl -s "http://prometheus:9090/api/v1/query?query=rate(smartalarm_requests_total{status_code=~'5..'}[5m]) by (status_code)" | jq
```

### 2. Quick Impact Assessment

- Check Grafana Error Rate panel
- Identify affected services and endpoints
- Determine if errors are widespread or localized

## Diagnosis Steps

### 1. Analyze Error Patterns

```bash
# Check recent error logs
kubectl logs -n smartalarm -l app=smartalarm-api --tail=100 | grep -i "error\|exception"

# Check error distribution over time
curl -s "http://prometheus:9090/api/v1/query_range?query=rate(smartalarm_requests_total{status_code=~'5..'}[5m])&start=$(date -d '1 hour ago' +%s)&end=$(date +%s)&step=60" | jq
```

### 2. Check Recent Changes

```bash
# Check recent deployments
kubectl rollout history deployment -n smartalarm

# Check recent configuration changes
kubectl get events -n smartalarm --sort-by='.lastTimestamp' | grep -i "config\|secret"

# Check recent scaling events
kubectl get events -n smartalarm --sort-by='.lastTimestamp' | grep -i "scale"
```

### 3. Examine Dependencies

```bash
# Check database health
kubectl exec -n smartalarm deployment/smartalarm-api -- curl -f http://postgres:5432/health

# Check Redis connectivity
kubectl exec -n smartalarm deployment/smartalarm-api -- redis-cli -h redis ping

# Check external API status
kubectl exec -n smartalarm deployment/smartalarm-api -- curl -f https://api.google.com/calendar/v3/
```

## Common Error Scenarios

### 1. Database Connection Issues (500 errors)

**Symptoms**: High 500 error rate, database timeout logs

```bash
# Check database connection pool
curl -s "http://prometheus:9090/api/v1/query?query=smartalarm_database_connections_active" | jq

# Check database performance
curl -s "http://prometheus:9090/api/v1/query?query=histogram_quantile(0.95, rate(smartalarm_database_query_duration_seconds_bucket[5m]))" | jq

# Immediate mitigation: Restart database connections
kubectl rollout restart deployment/smartalarm-api -n smartalarm
```

### 2. Memory Issues (500 errors)

**Symptoms**: OutOfMemory exceptions, pod restarts

```bash
# Check memory usage
kubectl top pods -n smartalarm

# Check for OOM kills
kubectl get events -n smartalarm | grep -i "oom\|killed"

# Scale up memory limits
kubectl patch deployment smartalarm-api -n smartalarm -p '{"spec":{"template":{"spec":{"containers":[{"name":"smartalarm-api","resources":{"limits":{"memory":"2Gi"}}}]}}}}'
```

### 3. External API Failures (502/503 errors)

**Symptoms**: Timeout errors, external service unavailable

```bash
# Check external service status
curl -I https://www.googleapis.com/calendar/v3/
curl -I https://graph.microsoft.com/v1.0/

# Enable circuit breaker mode
kubectl patch configmap smartalarm-config -n smartalarm --patch '{"data":{"CIRCUIT_BREAKER_ENABLED":"true"}}'
```

### 4. Rate Limiting Issues (429 errors)

**Symptoms**: High 429 error rate, rate limit exceeded logs

```bash
# Check rate limiting metrics
curl -s "http://prometheus:9090/api/v1/query?query=rate(smartalarm_requests_total{status_code='429'}[5m])" | jq

# Temporarily increase rate limits
kubectl patch configmap smartalarm-config -n smartalarm --patch '{"data":{"RATE_LIMIT_PER_MINUTE":"200"}}'
```

### 5. Authentication Issues (401/403 errors)

**Symptoms**: High 401/403 error rate, JWT validation failures

```bash
# Check JWT token validation
kubectl logs -n smartalarm -l app=smartalarm-api | grep -i "jwt\|auth\|token"

# Check Key Vault connectivity
kubectl exec -n smartalarm deployment/smartalarm-api -- curl -f http://keyvault:8080/health

# Restart authentication service
kubectl rollout restart deployment/smartalarm-api -n smartalarm
```

## Recovery Actions

### 1. Immediate Mitigation

```bash
# Enable graceful degradation
kubectl patch configmap smartalarm-config -n smartalarm --patch '{"data":{"GRACEFUL_DEGRADATION":"true"}}'

# Scale up healthy services
kubectl scale deployment/smartalarm-api --replicas=5 -n smartalarm

# Route traffic away from failing instances
kubectl patch service smartalarm-api -n smartalarm --patch '{"spec":{"selector":{"version":"stable"}}}'
```

### 2. Service Recovery

```bash
# Restart affected services
kubectl rollout restart deployment/smartalarm-api -n smartalarm

# Monitor error rate during recovery
watch 'curl -s "http://prometheus:9090/api/v1/query?query=rate(smartalarm_requests_total{status_code=~\"5..\"}[5m])" | jq ".data.result[0].value[1]"'
```

### 3. Rollback if Necessary

```bash
# Check if errors started after recent deployment
kubectl rollout history deployment/smartalarm-api -n smartalarm

# Rollback to previous version
kubectl rollout undo deployment/smartalarm-api -n smartalarm

# Wait for rollback to complete
kubectl rollout status deployment/smartalarm-api -n smartalarm
```

## Verification Steps

### 1. Error Rate Verification

```bash
# Check current error rate
curl -s "http://prometheus:9090/api/v1/query?query=(rate(smartalarm_requests_total{status_code=~'5..'}[5m]) / rate(smartalarm_requests_total[5m])) * 100" | jq

# Verify error rate is below threshold
# Target: < 1% error rate
```

### 2. Service Health Check

```bash
# Test critical endpoints
curl -f http://smartalarm.com/api/health
curl -f http://smartalarm.com/api/alarms
curl -f http://smartalarm.com/api/users/profile

# Check response times
curl -w "@curl-format.txt" -o /dev/null -s http://smartalarm.com/api/alarms
```

### 3. End-to-End Testing

```bash
# Run automated health checks
./scripts/health-check.sh

# Test user workflows
./scripts/test-user-journey.sh
```

## Monitoring and Alerting

### 1. Enhanced Monitoring

- Set up detailed error tracking by endpoint
- Monitor error rate trends
- Track error recovery time

### 2. Alert Tuning

- Adjust thresholds based on normal patterns
- Add error rate prediction alerts
- Implement error budget alerts

## Prevention Measures

1. **Error Budgets**: Implement SLO-based error budgets
2. **Circuit Breakers**: Add circuit breakers for all external calls
3. **Retry Logic**: Implement exponential backoff retry
4. **Input Validation**: Enhance request validation
5. **Load Testing**: Regular load testing to identify breaking points
6. **Chaos Engineering**: Implement chaos testing

## Escalation

### If error rate remains > 5% after 10 minutes:

1. Contact on-call engineer: +55 11 99999-9999
2. Escalate to engineering manager
3. Consider enabling maintenance mode

### If error rate > 10%:

1. Immediate executive notification
2. Consider full service rollback
3. Activate incident response team

## Post-Incident Actions

1. **Root Cause Analysis**: Identify why errors occurred
2. **Error Pattern Analysis**: Look for trends and patterns
3. **Monitoring Enhancement**: Add monitoring for identified gaps
4. **Code Review**: Review recent changes that may have caused errors
5. **Process Improvement**: Update deployment and testing processes
