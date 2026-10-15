# Quick Start

## Overview

This guide gets you running the Dynamic DNS Updater API in under 5 minutes.

## Prerequisites

- .NET 10.0 SDK
- Gandi LiveDNS API key
- API key for authorization

## 1. Clone and Build

```bash
git clone <repository-url>
cd dynamic-dns-updater-api
dotnet restore
dotnet build
```

## 2. Configure

Create `appsettings.Development.json`:

```json
{
  "SecuritySettings": {
    "ApiKey": "your-api-key-here"
  },
  "GandiSettings": {
    "ApiKey": "your-gandi-api-key-here"
  }
}
```

## 3. Run

```bash
dotnet run --project DynamicDnsUpdater.API
```

API starts at `http://localhost:5000` (or `https://localhost:5001`).

## 4. Test

```bash
curl -X PUT http://localhost:5000/DnsRecords/www.example.com \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: your-api-key-here" \
  -d '{"ipAddress": "192.0.2.1"}'
```

**Success Response:** `200 OK`

## 5. Verify DNS

```bash
dig @9.9.9.9 www.example.com A
```

Should return `192.0.2.1` after propagation.

## Docker Quick Start

```bash
docker build -t dynamic-dns-updater-api .
docker run -d -p 8080:8080 \
  -e SecuritySettings__ApiKey=your-api-key \
  -e GandiSettings__ApiKey=your-gandi-key \
  dynamic-dns-updater-api
```

Test: `curl -X PUT http://localhost:8080/DnsRecords/www.example.com ...`

## Next Steps

- [Configuration](configuration.md) - Detailed configuration options
- [API Reference](api-reference/dnsrecords.md) - Full API documentation
- [Deployment](build-and-deployment.md) - Production deployment guide