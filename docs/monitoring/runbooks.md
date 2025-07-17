# SmartAlarm Monitoring Runbooks

## 🚨 Critical Alerts

### ServiceDown

**Symptom**: Um ou mais microserviços estão completamente indisponíveis

**Immediate Actions**:
1. Check Kubernetes pod status:
   ```bash
   kubectl get pods -n smartalarm
   kubectl describe pod <pod-name> -n smartalarm
   ```

2. Check recent logs:
   ```bash
   kubectl logs -f deployment/<service-name> -n smartalarm --tail=100
   ```

3. Check resource usage:
   ```bash
   kubectl top pods -n smartalarm
   ```

**Common Causes & Solutions**:
- **OOMKilled**: Increase memory limits in deployment manifest
- **CrashLoopBackOff**: Check application logs for startup errors
- **ImagePullBackOff**: Verify image exists in registry
- **Node issues**: Check node status with `kubectl get nodes`

**Escalation**: If service doesn't recover in 5 minutes, escalate to SRE team

---

### HighErrorRate

**Symptom**: Taxa de erro 5xx acima de 5% por mais de 2 minutos

**Immediate Actions**:
1. Check error distribution by endpoint:
   ```bash
   # In Grafana, filter by service and status_code=~"5.."
   ```

2. Check recent deployments:
   ```bash
   kubectl rollout history deployment/<service-name> -n smartalarm
   ```

3. Check database connectivity:
   ```bash
   kubectl exec -it deployment/<service-name> -n smartalarm -- env | grep CONNECTION
   ```

**Common Causes & Solutions**:
- **Database timeouts**: Check database performance and connections
- **Bad deployment**: Consider rollback with `kubectl rollout undo`
- **External service failures**: Check integration service health
- **Resource exhaustion**: Scale up with `kubectl scale deployment`

**Rollback Procedure**:
```bash
kubectl rollout undo deployment/<service-name> -n smartalarm
kubectl rollout status deployment/<service-name> -n smartalarm
```

---

### SLOAvailabilityBreached

**Symptom**: 30-day availability below 99.9% SLO

**Immediate Actions**:
1. Calculate current availability:
   ```promql
   avg_over_time(up{job="<service>"}[30d]) * 100
   ```

2. Identify recent outages:
   ```promql
   changes(up{job="<service>"}[30d])
   ```

3. Review incident timeline in Grafana

**Root Cause Analysis**:
- Gather metrics for the last 30 days
- Identify patterns in downtime
- Document lessons learned
- Update runbooks and monitoring

**Business Impact**:
- Customer SLA may be affected
- Notify product and business teams
- Prepare customer communication if needed

---

## ⚠️ Warning Alerts

### HighResponseTime

**Symptom**: P95 response time above 2 seconds for 3+ minutes

**Investigation Steps**:
1. Check slow endpoints:
   ```promql
   topk(10, histogram_quantile(0.95, rate(smartalarm_request_duration_seconds_bucket[5m])))
   ```

2. Database query performance:
   ```promql
   histogram_quantile(0.95, rate(smartalarm_database_query_duration_seconds_bucket[5m]))
   ```

3. Check resource utilization:
   ```bash
   kubectl top pods -n smartalarm
   kubectl top nodes
   ```

**Optimization Actions**:
- Scale horizontally if CPU/memory bound
- Optimize slow database queries
- Check for memory leaks
- Review external service dependencies

---

### HighMemoryUsage

**Symptom**: Memory usage above 80% of limits

**Immediate Actions**:
1. Check memory trends:
   ```bash
   kubectl top pods -n smartalarm --sort-by=memory
   ```

2. Look for memory leaks:
   ```promql
   increase(process_resident_memory_bytes[1h])
   ```

3. Check garbage collection metrics (if available)

**Solutions**:
- Increase memory limits if legitimate growth
- Investigate memory leaks in application code
- Scale horizontally to distribute load
- Review caching strategies

---

### PodRestartingFrequently

**Symptom**: Pod restarted 3+ times in the last hour

**Investigation**:
1. Check restart reasons:
   ```bash
   kubectl describe pod <pod-name> -n smartalarm
   ```

2. Review application logs around restart times:
   ```bash
   kubectl logs <pod-name> -n smartalarm --previous
   ```

3. Check resource limits and requests

**Common Causes**:
- Memory limits too low (OOMKilled)
- Application crashes due to bugs
- Liveness probe failures
- Node issues or evictions

---

## 📊 Business Alerts

### LowUserActivity

**Symptom**: Less than 10 active users during business hours

**Investigation**:
1. Check user registration trends
2. Review application performance issues
3. Check for service degradation affecting user experience

**Actions**:
- Alert product team for business analysis
- Check marketing campaigns or system communications
- Verify no blocking issues for user access

---

### AlarmCreationFailures

**Symptom**: High failure rate in alarm creation

**Investigation**:
1. Check validation errors in logs
2. Review database constraints and conflicts
3. Check external service dependencies

**Common Issues**:
- Database constraint violations
- Invalid input data validation
- External calendar service failures
- Authentication/authorization issues

---

## 🔧 Infrastructure Troubleshooting

### Database Connection Issues

**Symptoms**: Database timeout errors, connection pool exhaustion

**Investigation**:
```bash
# Check database connections
kubectl exec -it deployment/<service> -n smartalarm -- netstat -an | grep 5432

# Check connection pool metrics
# Look for smartalarm_database_connections_active in Grafana
```

**Solutions**:
- Increase connection pool size
- Optimize long-running queries
- Check database server performance
- Review connection leaks in application code

---

### Message Queue Issues

**Symptoms**: Messages not being processed, queue size growing

**Investigation**:
```bash
# Check RabbitMQ management UI
curl -u guest:guest http://localhost:15672/api/queues

# Check queue metrics in Grafana
# smartalarm_messaging_queue_size
```

**Solutions**:
- Scale consumer instances
- Check for poison messages
- Review message processing logic
- Check RabbitMQ server health

---

### Storage Issues

**Symptoms**: File upload/download failures, storage errors

**Investigation**:
```bash
# Check MinIO health
curl http://localhost:9000/minio/health/live

# Check storage metrics
# smartalarm_storage_operations_total
```

**Solutions**:
- Check storage server connectivity
- Verify authentication credentials
- Check storage space availability
- Review bucket permissions

---

## 📋 Standard Operating Procedures

### Deployment Rollback

```bash
# Check rollout history
kubectl rollout history deployment/<service-name> -n smartalarm

# Rollback to previous version
kubectl rollout undo deployment/<service-name> -n smartalarm

# Rollback to specific revision
kubectl rollout undo deployment/<service-name> --to-revision=<revision> -n smartalarm

# Wait for rollback completion
kubectl rollout status deployment/<service-name> -n smartalarm
```

### Scaling Services

```bash
# Scale up
kubectl scale deployment <service-name> --replicas=<count> -n smartalarm

# Check HPA status
kubectl get hpa -n smartalarm

# Manual override of HPA (if needed)
kubectl patch hpa <service-name>-hpa -n smartalarm -p '{"spec":{"minReplicas":<new-min>}}'
```

### Log Analysis

```bash
# Real-time logs
kubectl logs -f deployment/<service-name> -n smartalarm

# Logs from specific time
kubectl logs deployment/<service-name> -n smartalarm --since=1h

# Previous container logs (after restart)
kubectl logs <pod-name> -n smartalarm --previous

# Logs with grep
kubectl logs deployment/<service-name> -n smartalarm | grep ERROR
```

### Health Check Verification

```bash
# Direct health check
kubectl port-forward svc/<service-name> 8080:80 -n smartalarm
curl http://localhost:8080/health

# Health check from within cluster
kubectl run debug --rm -i --restart=Never --image=curlimages/curl -- \
  curl -f http://<service-name>.<namespace>.svc.cluster.local/health
```

---

## 🔍 Monitoring Best Practices

### Alert Fatigue Prevention
- Tune alert thresholds based on historical data
- Use inhibition rules to prevent cascading alerts
- Regular review of alert frequency and usefulness

### Incident Response
1. **Immediate**: Stop the bleeding (rollback, scale, etc.)
2. **Investigation**: Root cause analysis
3. **Communication**: Update stakeholders
4. **Documentation**: Update runbooks and postmortems

### Capacity Planning
- Review resource utilization trends weekly
- Plan scaling before hitting limits
- Monitor business growth vs infrastructure capacity

### Regular Maintenance
- Weekly review of alert rules and thresholds
- Monthly dashboard and query optimization
- Quarterly runbook updates and incident reviews
