# Service Down Runbook

## Alert Description

**Alert Name**: ServiceDown  
**Severity**: Critical  
**Trigger**: Service health check fails for more than 1 minute

## Immediate Actions (< 5 minutes)

### 1. Verify the Alert

```bash
# Check service status in Kubernetes
kubectl get pods -n smartalarm -l app=<service-name>

# Check service logs
kubectl logs -n smartalarm -l app=<service-name> --tail=100

# Check service endpoints
kubectl get endpoints -n smartalarm <service-name>
```

### 2. Quick Health Check

```bash
# Test service directly
curl -f http://<service-name>:8080/health

# Check if service is responding
curl -f http://<service-name>:8080/health/live
```

## Diagnosis Steps

### 1. Check Pod Status

```bash
# Get detailed pod information
kubectl describe pod -n smartalarm <pod-name>

# Check pod events
kubectl get events -n smartalarm --sort-by='.lastTimestamp'
```

### 2. Review Application Logs

```bash
# Check application logs for errors
kubectl logs -n smartalarm <pod-name> --previous

# Check for specific error patterns
kubectl logs -n smartalarm <pod-name> | grep -i "error\|exception\|fatal"
```

### 3. Check Resource Usage

```bash
# Check resource consumption
kubectl top pods -n smartalarm

# Check node resources
kubectl top nodes
```

### 4. Verify Dependencies

- Database connectivity
- Redis cache availability
- External API accessibility
- Message queue status

## Common Causes and Solutions

### 1. Out of Memory (OOMKilled)

**Symptoms**: Pod restarts frequently, OOMKilled in events

```bash
# Increase memory limits
kubectl patch deployment -n smartalarm <service-name> -p '{"spec":{"template":{"spec":{"containers":[{"name":"<container-name>","resources":{"limits":{"memory":"2Gi"}}}]}}}}'
```

### 2. Database Connection Issues

**Symptoms**: Connection timeout errors in logs

```bash
# Check database connectivity
kubectl exec -n smartalarm <pod-name> -- nc -zv postgres 5432

# Check connection pool metrics in Grafana
# Navigate to Infrastructure Dashboard -> Database Connection Pool
```

### 3. Configuration Issues

**Symptoms**: Service fails to start, configuration errors

```bash
# Check ConfigMaps and Secrets
kubectl get configmap -n smartalarm
kubectl get secrets -n smartalarm

# Verify environment variables
kubectl exec -n smartalarm <pod-name> -- env | grep -i smart
```

### 4. Image Pull Issues

**Symptoms**: ImagePullBackOff, ErrImagePull

```bash
# Check image availability
docker pull <image-name>

# Update deployment with correct image
kubectl set image deployment/<service-name> <container-name>=<correct-image> -n smartalarm
```

## Recovery Actions

### 1. Restart Service

```bash
# Restart deployment
kubectl rollout restart deployment/<service-name> -n smartalarm

# Wait for rollout to complete
kubectl rollout status deployment/<service-name> -n smartalarm
```

### 2. Scale Service

```bash
# Scale up replicas temporarily
kubectl scale deployment/<service-name> --replicas=3 -n smartalarm
```

### 3. Rollback Deployment

```bash
# Check rollout history
kubectl rollout history deployment/<service-name> -n smartalarm

# Rollback to previous version
kubectl rollout undo deployment/<service-name> -n smartalarm
```

## Verification Steps

### 1. Service Health

```bash
# Verify pods are running
kubectl get pods -n smartalarm -l app=<service-name>

# Check service endpoints
curl http://<service-name>:8080/health
```

### 2. Metrics Verification

- Check Grafana System Overview dashboard
- Verify service appears as "UP" in Prometheus
- Confirm error rates have returned to normal

### 3. End-to-End Testing

```bash
# Test critical endpoints
curl -X POST http://<service-name>:8080/api/alarms \
  -H "Content-Type: application/json" \
  -d '{"name":"test","time":"09:00"}'
```

## Prevention Measures

1. **Resource Monitoring**: Set up alerts for resource usage
2. **Health Checks**: Implement comprehensive health checks
3. **Circuit Breakers**: Add circuit breakers for external dependencies
4. **Graceful Shutdown**: Ensure proper shutdown handling
5. **Resource Limits**: Set appropriate resource requests and limits

## Escalation

If service cannot be restored within 15 minutes:

1. Contact on-call engineer: +55 11 99999-9999
2. Create incident in PagerDuty
3. Notify stakeholders via Slack #incidents channel

## Post-Incident Actions

1. Document root cause in incident report
2. Update monitoring and alerting if needed
3. Review and update this runbook
4. Schedule post-mortem meeting if critical impact
