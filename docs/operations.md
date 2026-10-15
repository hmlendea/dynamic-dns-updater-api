# Operations

## Overview

This document describes operational procedures for running the Dynamic DNS Updater API in production.

## Daily Operations

### Health Checks

```bash
# Check API health
curl -f http://localhost:8080/health

# Check with authentication
curl -f -H "X-Api-Key: your-api-key" http://localhost:8080/health
```

**Expected Response:**
```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "self",
      "status": "Healthy"
    }
  ]
}
```

### Log Monitoring

```bash
# View recent logs (systemd)
journalctl -u dynamic-dns-updater --since "1 hour ago"

# View recent logs (docker)
docker logs dynamic-dns-updater --since 1h

# Follow logs in real-time
journalctl -u dynamic-dns-updater -f
```

### Key Log Patterns

| Pattern | Meaning | Action |
|---------|---------|--------|
| `OperationStatus.Started` | Update initiated | Normal |
| `OperationStatus.Success` | Update completed | Normal |
| `OperationStatus.Failure` | Update failed | Investigate |
| `ScannerProtection` | Scanner detected | Monitor |
| `ReplayProtection` | Replay attack | Monitor |
| `UnauthorizedAccessException` | Auth failure | Check API keys |

## Weekly Operations

### Log Rotation

```bash
# systemd handles rotation automatically
# Check disk usage
du -sh /var/log/journal/

# Clean old logs if needed
journalctl --vacuum-time=30d
```

### Certificate Renewal

If using Let's Encrypt with reverse proxy:

```bash
# Check certificate expiry
openssl x509 -enddate -noout -in /etc/letsencrypt/live/yourdomain.com/cert.pem

# Renew (certbot handles automatically)
certbot renew --dry-run
```

### Dependency Updates

```bash
# Check for outdated packages
dotnet list package --outdated

# Update packages
dotnet add package PackageName --version LatestVersion

# Test after updates
dotnet test
```

## Monthly Operations

### Security Review

1. **API Key Rotation:**
   - Generate new API keys
   - Update configuration
   - Deploy with zero-downtime
   - Revoke old keys

2. **Access Audit:**
   - Review who has access to keys
   - Check deployment logs
   - Verify no unauthorized access

3. **Vulnerability Scan:**
   ```bash
   # Scan container image
   docker scan dynamic-dns-updater-api:latest

   # Scan dependencies
   dotnet list package --vulnerable
   ```

### Performance Review

1. **Metrics Analysis:**
   - Request latency (p50, p95, p99)
   - Error rate trends
   - Throughput patterns

2. **Resource Usage:**
   - CPU utilization
   - Memory consumption
   - Network I/O

3. **Capacity Planning:**
   - Project growth
   - Plan scaling

## Incident Response

### Common Incidents

#### 1. API Returns 500 Errors

**Symptoms:** Health check fails, requests return 500

**Diagnosis:**
```bash
# Check logs
journalctl -u dynamic-dns-updater -f

# Check process
systemctl status dynamic-dns-updater
```

**Common Causes:**
- Gandi API key expired
- Network connectivity to Gandi/Quad9
- Application crash

**Resolution:**
- Verify API keys
- Check network connectivity
- Restart service

#### 2. API Returns 401 Unauthorized

**Symptoms:** Valid requests return 401

**Diagnosis:**
```bash
# Check API key in config
grep -r "SecuritySettings__ApiKey" /etc/systemd/system/dynamic-dns-updater.service

# Test with known good key
curl -H "X-Api-Key: known-good-key" http://localhost:8080/health
```

**Resolution:**
- Update API key in configuration
- Reload service

#### 3. DNS Updates Not Propagating

**Symptoms:** API returns 200 but DNS doesn't change

**Diagnosis:**
```bash
# Check Gandi API directly
curl -H "Authorization: Bearer your-gandi-key" \
  "https://api.gandi.net/v5/livedns/domains/example.com"

# Check Quad9
dig @9.9.9.9 www.example.com A
```

**Resolution:**
- Verify Gandi API key permissions
- Check domain ownership in Gandi
- Wait for DNS propagation (TTL 300s)

#### 4. High Latency

**Symptoms:** Requests take >5 seconds

**Diagnosis:**
```bash
# Check Gandi API latency
time curl -H "Authorization: Bearer your-gandi-key" \
  "https://api.gandi.net/v5/livedns/domains/example.com"

# Check Quad9 latency
time dig @9.9.9.9 www.example.com A
```

**Resolution:**
- Check network connectivity
- Consider caching (future enhancement)
- Scale horizontally

### Escalation Procedures

| Severity | Response Time | Escalation |
|----------|---------------|------------|
| Critical (API down) | 15 min | Page on-call |
| High (500 errors) | 1 hour | Notify team |
| Medium (slow) | 4 hours | Create ticket |
| Low (cosmetic) | Next business day | Backlog |

## Backup and Recovery

### Configuration Backup

```bash
# Backup current config
cp /opt/dynamic-dns-updater/appsettings.Production.json \
   /opt/dynamic-dns-updater/appsettings.Production.json.$(date +%Y%m%d)

# Backup systemd service
cp /etc/systemd/system/dynamic-dns-updater.service \
   /opt/dynamic-dns-updater/dynamic-dns-updater.service.$(date +%Y%m%d)
```

### Recovery Procedure

```bash
# 1. Restore configuration
cp /opt/dynamic-dns-updater/appsettings.Production.json.backup \
   /opt/dynamic-dns-updater/appsettings.Production.json

# 2. Restore binary (if needed)
cp /opt/dynamic-dns-updater/DynamicDnsUpdater.API.dll.backup \
   /opt/dynamic-dns-updater/DynamicDnsUpdater.API.dll

# 3. Restart service
systemctl restart dynamic-dns-updater

# 4. Verify
curl -f http://localhost:8080/health
```

## Scaling

### Horizontal Scaling

The application is stateless - can run multiple instances behind a load balancer.

**Requirements:**
- Shared configuration (API keys)
- Load balancer with sticky sessions not required
- Health checks configured

### Vertical Scaling

**Resource Limits:**
- CPU: 500m (0.5 cores) typical, 1000m max
- Memory: 512Mi typical, 1Gi max

**Scaling Triggers:**
- CPU > 70% for 5 minutes
- Memory > 80% for 5 minutes
- Request latency p95 > 2s

## Maintenance Windows

### Scheduled Maintenance

- **When:** Sunday 02:00-04:00 UTC
- **Duration:** Up to 2 hours
- **Notification:** 24 hours via status page
- **Activities:**
  - OS updates
  - Dependency updates
  - Configuration changes
  - Certificate renewal

### Emergency Maintenance

- **When:** As needed for critical issues
- **Notification:** Immediate via status page
- **Activities:**
  - Security patches
  - Critical bug fixes
  - Infrastructure issues

## Monitoring Dashboard

### Key Metrics to Watch

| Metric | Warning | Critical |
|--------|---------|----------|
| Request Rate | > 1000/min | > 5000/min |
| Error Rate | > 1% | > 5% |
| Latency p95 | > 2s | > 5s |
| CPU Usage | > 70% | > 90% |
| Memory Usage | > 80% | > 95% |
| Disk Usage | > 70% | > 90% |

### Alerting Rules

```yaml
# Example Prometheus alerts
groups:
- name: dynamic-dns-updater
  rules:
  - alert: HighErrorRate
    expr: rate(http_requests_total{status=~"5.."}[5m]) > 0.05
    for: 2m
    labels:
      severity: critical
    annotations:
      summary: "High error rate on Dynamic DNS Updater"

  - alert: HighLatency
    expr: histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m])) > 2
    for: 5m
    labels:
      severity: warning
    annotations:
      summary: "High latency on Dynamic DNS Updater"

  - alert: ServiceDown
    expr: up{job="dynamic-dns-updater"} == 0
    for: 1m
    labels:
      severity: critical
    annotations:
      summary: "Dynamic DNS Updater is down"
```

## Runbooks

### Runbook: Deploy New Version

1. Build and test locally
2. Create release tag
3. CI/CD builds and publishes
4. Deploy to staging
5. Run smoke tests
6. Deploy to production (blue/green or rolling)
7. Monitor for 15 minutes
8. Close release

### Runbook: Rotate API Keys

1. Generate new keys in Gandi and auth system
2. Update configuration in staging
3. Test in staging
4. Update configuration in production
5. Reload service (zero-downtime)
6. Verify with test request
7. Revoke old keys
8. Document rotation

### Runbook: Scale Up

1. Check current resource usage
2. Determine scaling need (horizontal vs vertical)
3. For horizontal: add instances to load balancer
4. For vertical: increase container resources
5. Monitor for 10 minutes
6. Document scaling event

### Runbook: Rollback

1. Identify issue requiring rollback
2. Execute rollback procedure for deployment method
3. Verify health checks pass
4. Run smoke tests
5. Monitor for 15 minutes
6. Document incident and rollback

## Contact Information

| Role | Contact | Escalation |
|------|---------|------------|
| Primary On-Call | [Name] | [Phone/Slack] |
| Secondary On-Call | [Name] | [Phone/Slack] |
| Team Lead | [Name] | [Phone/Slack] |
| Infrastructure | [Team] | [Channel] |

## Useful Commands Reference

```bash
# Service management
systemctl status dynamic-dns-updater
systemctl restart dynamic-dns-updater
systemctl reload dynamic-dns-updater

# Logs
journalctl -u dynamic-dns-updater -f
journalctl -u dynamic-dns-updater --since "1 hour ago" -o json

# Configuration
systemctl show dynamic-dns-updater --property=Environment
cat /opt/dynamic-dns-updater/appsettings.Production.json

# Network
ss -tlnp | grep 8080
curl -v http://localhost:8080/health

# Process
ps aux | grep DynamicDnsUpdater
top -p $(pgrep -f DynamicDnsUpdater)

# Disk
df -h /opt/dynamic-dns-updater
du -sh /opt/dynamic-dns-updater
```