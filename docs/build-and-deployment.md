# Build and Deployment

## Overview

This document describes the build process, deployment options, and operational procedures for the Dynamic DNS Updater API.

## Build Process

### Prerequisites

| Tool | Version | Purpose |
|------|---------|---------|
| .NET SDK | 10.0 | Build and run |
| Git | Latest | Source control |

### Build Commands

```bash
# Restore dependencies
dotnet restore DynamicDnsUpdater.API.slnx

# Build solution
dotnet build DynamicDnsUpdater.API.slnx --configuration Release --no-restore

# Run tests
dotnet test DynamicDnsUpdater.API.slnx --configuration Release --no-build --verbosity normal

# Publish API
dotnet publish DynamicDnsUpdater.API/DynamicDnsUpdater.API.csproj \
  --configuration Release \
  --output ./publish \
  --no-restore
```

### Build Output

```
publish/
├── DynamicDnsUpdater.API.dll
├── DynamicDnsUpdater.API.deps.json
├── DynamicDnsUpdater.API.runtimeconfig.json
├── appsettings.json
├── appsettings.Production.json
├── NuciAPI.*.dll
├── NuciLog.*.dll
├── DnsClient.*.dll
└── System.*.dll
```

## Configuration

### Environment Variables

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `ASPNETCORE_ENVIRONMENT` | No | `Production` | Environment name |
| `ASPNETCORE_URLS` | No | `http://+:8080` | URLs to listen on |
| `SecuritySettings__ApiKey` | Yes | - | API key for authorization |
| `GandiSettings__ApiKey` | Yes | - | Gandi LiveDNS API key |
| `NuciLoggerSettings__MinimumLevel` | No | `Information` | Minimum log level |

### Configuration Files

**appsettings.json** (base):
```json
{
  "SecuritySettings": {
    "ApiKey": ""
  },
  "GandiSettings": {
    "ApiKey": ""
  },
  "NuciLoggerSettings": {
    "MinimumLevel": "Information"
  }
}
```

**appsettings.Production.json** (override):
```json
{
  "SecuritySettings": {
    "ApiKey": "PRODUCTION_API_KEY"
  },
  "GandiSettings": {
    "ApiKey": "PRODUCTION_GANDI_API_KEY"
  }
}
```

### Configuration Priority

1. Environment variables (highest)
2. `appsettings.{Environment}.json`
3. `appsettings.json`
4. Defaults (lowest)

## Deployment Options

### 1. Docker Container

**Dockerfile:**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["DynamicDnsUpdater.API/DynamicDnsUpdater.API.csproj", "DynamicDnsUpdater.API/"]
RUN dotnet restore "DynamicDnsUpdater.API/DynamicDnsUpdater.API.csproj"
COPY . .
WORKDIR "/src/DynamicDnsUpdater.API"
RUN dotnet build "DynamicDnsUpdater.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DynamicDnsUpdater.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DynamicDnsUpdater.API.dll"]
```

**Build and Run:**
```bash
# Build image
docker build -t dynamic-dns-updater-api .

# Run container
docker run -d \
  --name dynamic-dns-updater \
  -p 8080:8080 \
  -e SecuritySettings__ApiKey=your-api-key \
  -e GandiSettings__ApiKey=your-gandi-key \
  dynamic-dns-updater-api
```

### 2. Systemd Service (Linux)

**Service File:** `/etc/systemd/system/dynamic-dns-updater.service`
```ini
[Unit]
Description=Dynamic DNS Updater API
After=network.target

[Service]
Type=notify
WorkingDirectory=/opt/dynamic-dns-updater
ExecStart=/usr/bin/dotnet DynamicDnsUpdater.API.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=dynamic-dns-updater
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://+:8080
Environment=SecuritySettings__ApiKey=your-api-key
Environment=GandiSettings__ApiKey=your-gandi-key

[Install]
WantedBy=multi-user.target
```

**Commands:**
```bash
# Enable and start
sudo systemctl enable dynamic-dns-updater
sudo systemctl start dynamic-dns-updater

# Check status
sudo systemctl status dynamic-dns-updater

# View logs
sudo journalctl -u dynamic-dns-updater -f
```

### 3. Kubernetes Deployment

**Deployment YAML:**
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: dynamic-dns-updater
  labels:
    app: dynamic-dns-updater
spec:
  replicas: 2
  selector:
    matchLabels:
      app: dynamic-dns-updater
  template:
    metadata:
      labels:
        app: dynamic-dns-updater
    spec:
      containers:
      - name: api
        image: dynamic-dns-updater-api:latest
        ports:
        - containerPort: 8080
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ASPNETCORE_URLS
          value: "http://+:8080"
        - name: SecuritySettings__ApiKey
          valueFrom:
            secretKeyRef:
              name: dynamic-dns-secrets
              key: api-key
        - name: GandiSettings__ApiKey
          valueFrom:
            secretKeyRef:
              name: dynamic-dns-secrets
              key: gandi-api-key
        livenessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 5
---
apiVersion: v1
kind: Service
metadata:
  name: dynamic-dns-updater
spec:
  selector:
    app: dynamic-dns-updater
  ports:
  - port: 80
    targetPort: 8080
  type: ClusterIP
```

**Secret:**
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: dynamic-dns-secrets
type: Opaque
stringData:
  api-key: "your-api-key"
  gandi-api-key: "your-gandi-api-key"
```

### 4. Azure App Service

**Deployment:**
```bash
# Create resource group
az group create --name dynamic-dns-rg --location eastus

# Create App Service plan
az appservice plan create --name dynamic-dns-plan --resource-group dynamic-dns-rg --sku B1 --is-linux

# Create web app
az webapp create --resource-group dynamic-dns-rg --plan dynamic-dns-plan --name dynamic-dns-updater --runtime "DOTNET|10.0"

# Configure settings
az webapp config appsettings set --resource-group dynamic-dns-rg --name dynamic-dns-updater \
  --settings SecuritySettings__ApiKey=your-api-key GandiSettings__ApiKey=your-gandi-key

# Deploy
az webapp deploy --resource-group dynamic-dns-rg --name dynamic-dns-updater --src-path ./publish.zip --type zip
```

## CI/CD Pipeline

### GitHub Actions (dotnet.yml)

```yaml
name: .NET

on:
  push:
    branches: [ master ]
  pull_request:
    branches: [ master ]

jobs:
  build:
    name: Build
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v7

    - name: Setup .NET
      uses: actions/setup-dotnet@v5
      with:
        dotnet-version: 10.0.x

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build --no-restore

    - name: Test
      run: dotnet test --no-build --verbosity normal

  deploy:
    name: Deploy
    needs: build
    if: github.ref == 'refs/heads/master'
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v7

    - name: Setup .NET
      uses: actions/setup-dotnet@v5
      with:
        dotnet-version: 10.0.x

    - name: Publish
      run: dotnet publish DynamicDnsUpdater.API/DynamicDnsUpdater.API.csproj -c Release -o ./publish

    - name: Deploy to Azure
      uses: azure/webapps-deploy@v2
      with:
        app-name: dynamic-dns-updater
        publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
        package: ./publish
```

## Health Checks

### Endpoint

```
GET /health
```

### Response

```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "self",
      "status": "Healthy",
      "description": "Application is running"
    }
  ]
}
```

### Implementation

```csharp
// Program.cs
builder.Services.AddHealthChecks();

app.MapHealthChecks("/health");
```

## Monitoring

### Logging

- **Framework:** NuciLog
- **Output:** Console (stdout/stderr)
- **Format:** Structured JSON
- **Levels:** Debug, Information, Warning, Error, Critical

### Metrics

- **Framework:** Built-in ASP.NET Core metrics
- **Endpoint:** `/metrics` (if enabled)
- **Key Metrics:**
  - Request count
  - Request duration
  - Error rate
  - Active connections

### Tracing

- **Framework:** OpenTelemetry (if configured)
- **Exporters:** Console, Jaeger, Zipkin
- **Correlation:** Request ID propagation

## Rollback Procedure

### Docker
```bash
# Tag current version
docker tag dynamic-dns-updater-api:latest dynamic-dns-updater-api:previous

# Deploy new version
docker build -t dynamic-dns-updater-api:latest .

# If issues, rollback
docker stop dynamic-dns-updater
docker rm dynamic-dns-updater
docker run -d --name dynamic-dns-updater dynamic-dns-updater-api:previous
```

### Systemd
```bash
# Backup current binary
cp /opt/dynamic-dns-updater/DynamicDnsUpdater.API.dll /opt/dynamic-dns-updater/DynamicDnsUpdater.API.dll.backup

# Deploy new version
cp ./publish/* /opt/dynamic-dns-updater/

# If issues, rollback
cp /opt/dynamic-dns-updater/DynamicDnsUpdater.API.dll.backup /opt/dynamic-dns-updater/DynamicDnsUpdater.API.dll
sudo systemctl restart dynamic-dns-updater
```

### Kubernetes
```bash
# Rollback to previous revision
kubectl rollout undo deployment/dynamic-dns-updater

# Check rollout status
kubectl rollout status deployment/dynamic-dns-updater
```

## Security Considerations

### Secrets Management

- **Never** commit secrets to source control
- Use environment variables or secret managers
- Rotate API keys regularly
- Use different keys per environment

### Network Security

- Run behind reverse proxy (nginx, Traefik)
- Enable HTTPS/TLS termination at proxy
- Restrict inbound traffic to necessary ports
- Use firewall rules

### Container Security

- Run as non-root user
- Use distroless or minimal base images
- Scan images for vulnerabilities
- Keep base images updated

## Troubleshooting

### Common Issues

| Issue | Cause | Solution |
|-------|-------|----------|
| Port 8080 in use | Another process | Change `ASPNETCORE_URLS` |
| API key invalid | Wrong key in config | Verify `SecuritySettings__ApiKey` |
| Gandi API fails | Invalid Gandi key | Verify `GandiSettings__ApiKey` |
| DNS lookup fails | Network issues | Check Quad9 connectivity |
| High memory | Memory leak | Check for disposed resources |

### Debug Commands

```bash
# Check process
ps aux | grep dotnet

# Check ports
netstat -tlnp | grep 8080

# Check logs (systemd)
journalctl -u dynamic-dns-updater -f

# Check logs (docker)
docker logs dynamic-dns-updater -f

# Check health
curl http://localhost:8080/health

# Test API
curl -X PUT http://localhost:8080/DnsRecords/www.example.com \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: your-api-key" \
  -d '{"ipAddress": "192.0.2.1"}'
```

## Performance Tuning

### Application Settings

```json
{
  "Kestrel": {
    "Limits": {
      "MaxConcurrentConnections": 1000,
      "MaxConcurrentUpgradedConnections": 1000,
      "MaxRequestBodySize": 1048576
    }
  }
}
```

### Container Resources

```yaml
resources:
  requests:
    memory: "256Mi"
    cpu: "250m"
  limits:
    memory: "512Mi"
    cpu: "500m"
```

## Backup and Recovery

### Configuration Backup

```bash
# Backup configuration
cp appsettings.Production.json appsettings.Production.json.backup

# Restore configuration
cp appsettings.Production.json.backup appsettings.Production.json
```

### No Data Persistence

The application has no local data persistence - all state is in external systems (Gandi DNS, Quad9 DNS). No database backups needed.

## Versioning

### Semantic Versioning

- **Major:** Breaking API changes
- **Minor:** New features, backward compatible
- **Patch:** Bug fixes, backward compatible

### Release Process

1. Update version in `.csproj`
2. Create release branch
3. Run full test suite
4. Build and publish artifacts
5. Create GitHub release
6. Deploy to staging
7. Validate in staging
8. Deploy to production
9. Monitor for issues

## Maintenance Windows

- **Scheduled:** Weekly (Sunday 02:00 UTC)
- **Emergency:** As needed
- **Notification:** 24 hours advance for scheduled
- **Rollback Plan:** Always have rollback ready

## Disaster Recovery

### RTO/RPO

| Metric | Target |
|--------|--------|
| RTO (Recovery Time Objective) | < 15 minutes |
| RPO (Recovery Point Objective) | 0 (no local data) |

### Recovery Steps

1. Provision new infrastructure
2. Deploy application
3. Configure secrets
4. Verify health checks
5. Update DNS/load balancer
6. Validate functionality