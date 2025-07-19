---
applyTo: "**"
---
# Pull Request Instructions

## 1. PR Purpose & Context

### Clear Objective Statement
Every PR must start with a clear, business-focused purpose that explains the value delivered:

```markdown
## 🎯 Purpose

This PR implements JWT token revocation to enhance security by preventing the use of
compromised or logged-out user tokens. Users can now log out securely, and administrators
can revoke access for specific tokens when necessary.
```

### Business Value Context
- **User Impact**: How does this change affect the end user experience?
- **Technical Improvement**: What technical debt is resolved or system capability added?
- **Security Enhancement**: What security vulnerabilities are addressed?
- **Performance Gains**: What performance improvements are achieved?

## 2. Technical Implementation Details

### Architectural Changes
Document how the change fits within Clean Architecture:

```markdown
## 🔧 Technical Implementation

### Architecture Layer Changes
- **Domain**: Added `JwtToken` value object with revocation capabilities
- **Application**: Implemented `RevokeTokenCommandHandler` for token lifecycle management
- **Infrastructure**: Added Redis-based `IJwtBlocklistService` for distributed token storage
- **API**: Updated authentication middleware to check token blacklist

### Design Patterns Applied
- **Repository Pattern**: `IJwtBlocklistService` abstracts token storage implementation
- **Command Pattern**: Token revocation handled via MediatR command
- **Strategy Pattern**: Multiple blacklist providers (Redis, In-Memory) based on environment
```

### Code Quality Measures
- **Clean Architecture Compliance**: Verify layer boundaries are respected
- **SOLID Principles**: Demonstrate adherence to design principles
- **Error Handling**: Document exception handling and failure modes
- **Observability**: Include logging, tracing, and metrics implementation

## 3. Testing & Quality Assurance

### Comprehensive Test Coverage
```markdown
## ✅ Testing Strategy

### Unit Tests (95% Coverage)
- **Domain Logic**: JWT token validation and revocation rules
- **Command Handlers**: Token revocation workflow and error scenarios  
- **Services**: Blacklist service implementations with different providers

### Integration Tests
- **Database Integration**: Token persistence and retrieval from Redis
- **API Endpoints**: Authentication middleware with blacklist validation
- **Message Flow**: Event publishing when tokens are revoked

### Manual Testing Scenarios
- **Happy Path**: User logout revokes token successfully
- **Edge Cases**: Concurrent token revocation, expired token handling
- **Security**: Attempt to use revoked tokens fails appropriately
```

### Performance & Security Validation
- **Performance Impact**: Measure authentication latency with blacklist checking
- **Security Testing**: Verify revoked tokens cannot access protected resources
- **Load Testing**: Confirm system handles high token revocation volume
- **Memory Usage**: Monitor Redis memory consumption for token storage

## 4. Database & Migration Changes

### Schema Changes
```markdown
## 🗄️ Database Impact

### New Tables/Indexes
- No schema changes - using Redis for token blacklist storage

### Data Migration
- No data migration required
- Existing tokens continue to work until expiration

### Rollback Strategy
- Disable blacklist checking in configuration to rollback
- Clear Redis blacklist keys if needed: `redis-cli FLUSHDB`
```

### Migration Testing
- Test migrations in development environment first
- Verify rollback procedures work correctly  
- Document any manual steps required for deployment
- Validate performance impact of schema changes

## 5. Configuration & Deployment

### Environment Configuration
```markdown
## ⚙️ Configuration Changes

### New Environment Variables
```bash
# Redis connection for JWT blacklist
REDIS_CONNECTION_STRING=localhost:6379
JWT_BLACKLIST_ENABLED=true
JWT_BLACKLIST_TTL=86400  # 24 hours in seconds
```

### Feature Flags
- `JWT_BLACKLIST_ENABLED`: Allows gradual rollout and quick rollback
- Environment-specific defaults: enabled in prod, optional in dev
```

### Deployment Considerations
- **Zero-Downtime**: Explain how changes deploy without service interruption
- **Configuration Updates**: List required configuration changes per environment
- **Service Dependencies**: Document any new external service dependencies
- **Rollback Plan**: Clear steps to rollback if issues arise

## 6. External Integrations & Dependencies  

### Third-Party Services
```markdown
## 🔗 External Dependencies

### New Service Dependencies
- **Redis**: Required for distributed token blacklist storage
- **Health Check**: Added Redis connectivity health check endpoint

### API Changes
- No breaking changes to existing API contracts
- New optional `X-Token-Revoked-At` header in error responses
```

### Backward Compatibility
- Ensure existing API clients continue to work
- Document any deprecated endpoints or parameters
- Provide migration guide for breaking changes
- Maintain support for previous versions where possible

## 7. Security & Privacy Impact

### Security Enhancements
```markdown
## 🔒 Security Impact

### Vulnerabilities Addressed
- **Token Reuse**: Prevents compromised tokens from being reused
- **Logout Security**: Ensures user logout truly invalidates session
- **Administrative Control**: Allows token revocation for security incidents

### Privacy Considerations
- Token IDs stored in Redis, not token contents
- Automatic cleanup after token expiration
- No additional user data collected or stored
```

### Compliance Requirements
- **LGPD Compliance**: Document how changes affect data processing
- **Security Standards**: Confirm adherence to security frameworks
- **Audit Trail**: Ensure security events are properly logged
- **Access Controls**: Verify proper authorization for administrative functions

## 8. Monitoring & Observability

### Observability Implementation
```markdown
## 📊 Monitoring & Alerts

### New Metrics
- `jwt_tokens_revoked_total`: Counter of revoked tokens
- `jwt_blacklist_check_duration`: Histogram of blacklist lookup times
- `jwt_blacklist_size`: Gauge of current blacklist size

### Structured Logging
- Token revocation events with correlation IDs
- Authentication failures with revocation context
- Performance metrics for blacklist operations

### Health Checks
- Redis connectivity check in `/health` endpoint
- Blacklist service availability monitoring
```

### Alerting Strategy
- **Performance**: Alert if blacklist checks exceed SLA thresholds
- **Availability**: Alert if Redis connectivity fails
- **Security**: Alert on unusual token revocation patterns
- **Capacity**: Alert when blacklist size approaches limits

## 9. Documentation Updates

### User-Facing Documentation
```markdown
## 📖 Documentation Changes

### API Documentation
- Updated Swagger/OpenAPI specs with new error responses
- Added authentication flow documentation with revocation

### User Guides  
- Added "How to Log Out Securely" section to user documentation
- Updated troubleshooting guide for authentication issues

### Administrator Guides
- Added token revocation procedures for security incidents
- Updated monitoring and alerting documentation
```

### Technical Documentation
- Update architecture diagrams with new security flow
- Document Redis configuration and maintenance procedures
- Add runbook for token-related security incidents
- Update deployment procedures with new dependencies

## 10. Quality Gates & Acceptance Criteria

### Definition of Done Checklist
```markdown
## ✅ Acceptance Criteria

### Functional Requirements
- [ ] Users can log out and invalidate their tokens
- [ ] Revoked tokens cannot access protected endpoints  
- [ ] Administrators can revoke tokens via admin API
- [ ] Token revocation works across all service instances

### Technical Requirements  
- [ ] Code follows Clean Architecture principles
- [ ] Unit test coverage ≥ 90% for new code
- [ ] Integration tests cover token revocation workflows
- [ ] Performance impact ≤ 10ms additional latency

### Operations Requirements
- [ ] Health checks include Redis dependency
- [ ] Monitoring and alerting configured  
- [ ] Deployment runbook updated
- [ ] Rollback procedure tested and documented
```

### Performance Benchmarks
- Authentication response time remains under SLA
- Token revocation completes within 100ms
- System handles 1000+ concurrent authentications
- Redis memory usage stays within allocated limits

## 11. Risk Assessment & Mitigation

### Identified Risks
```markdown
## ⚠️ Risk Analysis

### High-Risk Areas
- **Redis Dependency**: Single point of failure for authentication
- **Performance Impact**: Additional network call for each authentication
- **Memory Usage**: Token blacklist could consume significant Redis memory

### Mitigation Strategies
- **Fallback Mode**: System continues without blacklist if Redis unavailable
- **Connection Pooling**: Use connection pooling to minimize latency impact  
- **TTL Management**: Automatic cleanup prevents unbounded memory growth
- **Circuit Breaker**: Fail open if blacklist service is degraded
```

### Rollback Strategy
- Feature flag allows immediate disable of blacklist checking
- Configuration change can switch to in-memory fallback
- Redis data can be cleared without affecting other functionality
- Full rollback possible by reverting to previous deployment

## PR Review Checklist

### Code Quality Review
- [ ] Clean Architecture boundaries respected
- [ ] SOLID principles followed
- [ ] Error handling comprehensive and consistent
- [ ] Observability (logging, tracing, metrics) implemented
- [ ] Security best practices followed
- [ ] Performance considerations addressed

### Testing Review
- [ ] Unit tests cover all business logic paths
- [ ] Integration tests verify end-to-end functionality  
- [ ] Edge cases and error conditions tested
- [ ] Performance tests validate SLA requirements
- [ ] Security tests confirm protection mechanisms

### Documentation Review  
- [ ] Code changes documented with clear comments
- [ ] API documentation updated for any endpoint changes
- [ ] Architecture diagrams reflect new components
- [ ] Deployment procedures updated
- [ ] User-facing documentation updated where needed

### Operational Review
- [ ] Monitoring and alerting configured
- [ ] Health checks include new dependencies
- [ ] Configuration management updated
- [ ] Deployment tested in staging environment
- [ ] Rollback procedures validated

---

**Remember**: Each PR should be a complete, deployable unit of value that moves the product forward while maintaining system quality and reliability.

