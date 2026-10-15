# Roadmap

## Vision

Build a robust, extensible Dynamic DNS Updater API that supports multiple DNS providers, offers high reliability, and provides excellent developer experience.

## Current Status (v1.0)

- ✅ Gandi LiveDNS v5 integration
- ✅ IPv4 A record updates
- ✅ DNS verification via Quad9
- ✅ API key authentication
- ✅ Replay protection
- ✅ Scanner protection
- ✅ Structured logging
- ✅ Unit and integration tests
- ✅ Comprehensive documentation

## Near Term (v1.1 - Q2 2024)

### Provider Expansion
- [ ] **Cloudflare DNS** integration
- [ ] **AWS Route 53** integration
- [ ] **Azure DNS** integration
- [ ] **Google Cloud DNS** integration

### IPv6 Support
- [ ] AAAA record updates
- [ ] IPv6 DNS verification
- [ ] Dual-stack support (A + AAAA)

### Reliability
- [ ] Retry policies with exponential backoff (Polly)
- [ ] Circuit breaker for provider failures
- [ ] Health check for provider APIs
- [ ] Request timeout configuration

### Observability
- [ ] OpenTelemetry integration
- [ ] Distributed tracing
- [ ] Custom metrics (Prometheus)
- [ ] Structured log correlation IDs

## Medium Term (v1.2 - Q3 2024)

### API Enhancements
- [ ] Batch update endpoint (multiple domains)
- [ ] Webhook notifications on update
- [ ] Update history/audit log
- [ ] Rate limiting per API key
- [ ] API versioning strategy

### Configuration
- [ ] Hot configuration reload
- [ ] Per-domain provider selection
- [ ] Custom TTL per domain
- [ ] Environment-specific configs

### Security
- [ ] OAuth2/OIDC support
- [ ] JWT token authentication
- [ ] API key rotation endpoint
- [ ] Audit logging for auth events

### Developer Experience
- [ ] OpenAPI/Swagger documentation
- [ ] Client SDKs (Python, Go, JavaScript)
- [ ] Postman collection
- [ ] Interactive API explorer

## Long Term (v2.0 - Q4 2024+)

### Architecture
- [ ] Plugin architecture for providers
- [ ] Message queue for async updates
- [ ] Multi-region deployment support
- [ ] Database for audit/history (optional)

### Advanced Features
- [ ] DNSSEC support
- [ ] GeoDNS / weighted records
- [ ] Health-based failover
- [ ] Custom record types (TXT, CNAME, etc.)

### Enterprise
- [ ] Multi-tenancy
- [ ] Role-based access control
- [ ] SSO integration
- [ ] Compliance reporting

## Maintenance

### Ongoing
- [ ] Dependency updates (monthly)
- [ ] Security patches (as needed)
- [ ] Performance optimization
- [ ] Documentation updates

### Technical Debt
- [ ] Increase test coverage to >90%
- [ ] Add mutation testing
- [ ] Add contract testing for providers
- [ ] Add chaos engineering tests

## Release Schedule

| Version | Target | Focus |
|---------|--------|-------|
| 1.1 | Q2 2024 | Providers, IPv6, Reliability |
| 1.2 | Q3 2024 | API Enhancements, Security |
| 2.0 | Q4 2024 | Architecture, Enterprise |

## Prioritization Framework

Features are prioritized by:
1. **User Impact** - How many users benefit
2. **Effort** - Development complexity
3. **Risk** - Stability impact
4. **Strategic Alignment** - Long-term vision fit

## Feedback

Roadmap is influenced by:
- GitHub Issues and Discussions
- User feedback
- Security advisories
- Technology changes
- Contributor interest

## Contributing to Roadmap

1. Open a discussion for major features
2. Submit PRs for small improvements
3. Vote on issues (👍 reactions)
4. Volunteer for implementation

---

*Roadmap is subject to change based on community feedback and project needs.*