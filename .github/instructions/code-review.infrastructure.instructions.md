---
applyTo: "**/*.{tf,tfvars,yml,yaml,sh,ps1,bat,cmd,dockerfile,Dockerfile}"
---
# Infrastructure & DevOps Code Review Instructions

## 1. Infrastructure as Code (Terraform)

### Code Organization
- **Module Structure**: Logical separation of resources into reusable modules
- **Variable Management**: Proper use of variables, locals, and outputs
- **State Management**: Remote state configuration with proper locking
- **Environment Separation**: Clear separation between dev, staging, and production

### Security & Compliance
- **Secrets Management**: No hardcoded secrets, use proper secret management services
- **Access Controls**: Principle of least privilege for all resources
- **Network Security**: Proper VPC configuration, security groups, and network ACLs
- **Encryption**: Encryption at rest and in transit for all sensitive data

### Best Practices
- **Resource Naming**: Consistent naming conventions across all resources
- **Tagging Strategy**: Comprehensive tagging for cost allocation and management
- **Version Constraints**: Proper provider and module version constraints
- **Documentation**: Clear descriptions for all modules and variables

## 2. Container Configuration (Docker)

### Dockerfile Standards
- **Multi-Stage Builds**: Use multi-stage builds to minimize image size
- **Base Images**: Use official, minimal base images (Alpine when appropriate)
- **Layer Optimization**: Minimize layers, combine RUN commands where logical
- **Security**: Run as non-root user, minimal attack surface

### Security Practices
- **Vulnerability Scanning**: Images scanned for known vulnerabilities
- **Secrets Handling**: No secrets embedded in images, use runtime secret injection
- **Minimal Packages**: Install only necessary packages, remove package managers
- **User Permissions**: Non-privileged user for application runtime

### Performance Optimization
- **Image Size**: Optimized for minimal size while maintaining functionality
- **Build Context**: Efficient use of .dockerignore to reduce build context
- **Caching**: Leverage Docker layer caching for faster builds
- **Health Checks**: Proper health check implementation

## 3. Kubernetes Manifests

### Resource Configuration
- **Resource Limits**: CPU and memory limits/requests properly configured
- **Health Checks**: Readiness and liveness probes implemented
- **Configuration Management**: ConfigMaps and Secrets used appropriately
- **Service Mesh**: Istio or similar for service-to-service communication

### Security Configuration
- **RBAC**: Role-Based Access Control properly implemented
- **Pod Security**: Pod Security Standards enforced
- **Network Policies**: Network segmentation with proper policies
- **Service Accounts**: Minimal permissions for service accounts

### Scalability & Reliability
- **Horizontal Pod Autoscaling**: HPA configured based on relevant metrics
- **Pod Disruption Budgets**: PDBs to ensure availability during updates
- **Resource Quotas**: Namespace resource quotas to prevent resource exhaustion
- **Anti-Affinity Rules**: Pod anti-affinity for high availability

## 4. CI/CD Pipeline Configuration

### Pipeline Security
- **Secret Management**: Secrets stored in secure CI/CD secret stores
- **Access Controls**: Principle of least privilege for CI/CD service accounts
- **Code Scanning**: SAST, DAST, and dependency vulnerability scanning
- **Artifact Signing**: Container images and artifacts properly signed

### Quality Gates
- **Automated Testing**: Unit, integration, and end-to-end tests in pipeline
- **Code Quality**: Linting, formatting, and code quality checks
- **Security Scanning**: Security scans as pipeline gates
- **Performance Testing**: Load and performance tests for critical paths

### Deployment Strategy
- **Blue-Green Deployment**: Zero-downtime deployment strategies
- **Rollback Capability**: Automated rollback on deployment failures
- **Environment Promotion**: Consistent promotion through environments
- **Monitoring Integration**: Deployment monitoring and alerting

## 5. Monitoring & Observability

### Metrics Collection
- **Prometheus Integration**: Proper metrics exposure for all services
- **Custom Metrics**: Business-relevant metrics beyond infrastructure metrics
- **Alerting Rules**: Meaningful alerts with proper thresholds
- **Dashboard Configuration**: Grafana dashboards for operational visibility

### Log Management
- **Centralized Logging**: Logs aggregated in central system (ELK, Loki)
- **Structured Logging**: JSON structured logs for better parsing
- **Log Retention**: Appropriate retention policies for different log types
- **Sensitive Data**: No sensitive information in logs

### Distributed Tracing
- **OpenTelemetry**: Consistent tracing implementation across services
- **Correlation IDs**: Request correlation across service boundaries
- **Performance Monitoring**: Application performance monitoring integration
- **Error Tracking**: Comprehensive error tracking and alerting

## 6. Database Infrastructure

### High Availability
- **Multi-AZ Deployment**: Database deployed across availability zones
- **Backup Strategy**: Automated backups with proper retention
- **Disaster Recovery**: Documented and tested DR procedures
- **Monitoring**: Database performance and health monitoring

### Security Configuration
- **Encryption**: Database encryption at rest and in transit
- **Access Controls**: Minimal database access permissions
- **Network Isolation**: Database in private subnets with security groups
- **Audit Logging**: Database audit logs enabled and monitored

## 7. Networking & Security

### Network Architecture
- **VPC Design**: Proper subnet design with public/private separation
- **Load Balancing**: Appropriate load balancer configuration
- **CDN Integration**: Content delivery network for static assets
- **DNS Management**: Proper DNS configuration and health checks

### Security Controls
- **WAF Configuration**: Web Application Firewall properly configured
- **DDoS Protection**: DDoS mitigation services enabled
- **Certificate Management**: Automated SSL certificate management
- **Security Groups**: Minimal required access in security group rules

## 8. Backup & Disaster Recovery

### Backup Strategy
- **Automated Backups**: Regular automated backups for all critical data
- **Cross-Region Replication**: Critical data replicated across regions
- **Backup Testing**: Regular restore testing to verify backup integrity
- **Documentation**: Clear backup and restore procedures documented

### Disaster Recovery
- **RTO/RPO Targets**: Clear Recovery Time and Recovery Point Objectives
- **Failover Procedures**: Automated failover where possible
- **DR Testing**: Regular disaster recovery testing and documentation
- **Communication Plan**: Clear communication procedures during incidents

## 9. Cost Optimization

### Resource Management
- **Right-Sizing**: Resources appropriately sized for workload requirements
- **Auto-Scaling**: Automatic scaling based on demand
- **Reserved Instances**: Use of reserved instances for predictable workloads
- **Spot Instances**: Use of spot instances where appropriate for cost savings

### Cost Monitoring
- **Cost Allocation**: Proper tagging for cost allocation and tracking
- **Budget Alerts**: Automated alerts for budget overruns
- **Cost Optimization Reviews**: Regular reviews for cost optimization opportunities
- **Unused Resources**: Regular cleanup of unused or orphaned resources

## 10. Compliance & Governance

### Regulatory Compliance
- **LGPD Compliance**: Data protection requirements implemented
- **Security Standards**: Compliance with relevant security frameworks
- **Audit Trails**: Comprehensive audit logging for compliance requirements
- **Data Residency**: Data stored in appropriate geographic regions

### Governance Controls
- **Policy as Code**: Infrastructure policies enforced as code
- **Change Management**: Proper change management procedures
- **Access Reviews**: Regular access reviews and cleanup
- **Documentation**: Comprehensive documentation of infrastructure and procedures

## Review Checklist

- [ ] Infrastructure code follows best practices
- [ ] Security controls properly implemented
- [ ] No hardcoded secrets or credentials
- [ ] Proper resource sizing and cost optimization
- [ ] Monitoring and alerting configured
- [ ] Backup and disaster recovery procedures in place
- [ ] CI/CD pipeline security and quality gates
- [ ] Compliance requirements addressed
- [ ] Documentation complete and current
- [ ] Testing procedures for infrastructure changes
