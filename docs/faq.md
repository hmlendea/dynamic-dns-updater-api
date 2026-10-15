# Frequently Asked Questions

## General

### What is this API for?

The Dynamic DNS Updater API updates DNS A records for a given subdomain to point to a specified IPv4 address. It's designed for dynamic DNS scenarios where a client's IP address changes periodically.

### Which DNS providers are supported?

Currently only **Gandi LiveDNS v5** is supported. The architecture allows adding more providers via the `DnsProvider` enumeration and corresponding service implementations.

### What IP versions are supported?

**IPv4 only** (A records). IPv6 (AAAA records) is not currently supported.

### Is this a DDNS client?

No. This is an **API server** that receives update requests. You need a separate client (script, router, device) to call this API when your IP changes.

## Configuration

### Where do I get a Gandi API key?

1. Log into your Gandi account
2. Go to **Security** → **API Keys**
3. Create a new key with **LiveDNS v5** permissions
4. Copy the key to your configuration

### Where do I get the API key for authorization?

Generate a secure random string (32+ characters) and configure it in both:
- Server: `SecuritySettings__ApiKey`
- Client: `X-Api-Key` header

### Can I use environment variables instead of config files?

Yes. All settings support environment variables with double underscore notation:
- `SecuritySettings__ApiKey`
- `GandiSettings__ApiKey`
- `NuciLoggerSettings__MinimumLevel`

### How do I enable debug logging?

Set `NuciLoggerSettings__MinimumLevel=Debug` in configuration or environment.

## API Usage

### What's the endpoint?

```
PUT /DnsRecords/{domainName}
```

### What headers are required?

| Header | Value |
|--------|-------|
| `Content-Type` | `application/json` |
| `X-Api-Key` | Your configured API key |

### What's the request body?

```json
{
  "ipAddress": "192.0.2.1"
}
```

### What domain name format is expected?

**Subdomain required:** `www.example.com` (not `example.com`)

The subdomain is the part before the first dot. The rest is the domain registered at Gandi.

### What's a successful response?

**200 OK** with empty body.

### What are common error responses?

| Status | Code | Cause |
|--------|------|-------|
| 400 | `BAD_REQUEST` | Missing/invalid IP, domain format |
| 401 | `UNAUTHORIZED` | Missing/invalid API key, replay attack |
| 403 | `FORBIDDEN` | Scanner detected |
| 500 | `INTERNAL_SERVER_ERROR` | Unexpected error |
| 502 | `BAD_GATEWAY` | Gandi API error |

## DNS Verification

### What is the DNS verification step?

Before calling Gandi, the API checks Quad9 (9.9.9.9) to see if the domain already resolves to the requested IP. If yes, it skips the Gandi API call.

### Why does verification sometimes fail?

Verification failures (network issues, Quad9 downtime) are **non-fatal**. The API proceeds to call Gandi anyway. Verification is an optimization, not a correctness requirement.

### Can I disable DNS verification?

Not currently. It's a fast, lightweight check that reduces unnecessary Gandi API calls.

### Does verification work for IPv6?

No. Verification only runs for IPv4 addresses. IPv6 addresses skip verification.

## Gandi Integration

### What Gandi API permissions are needed?

The API key needs **LiveDNS v5** read/write permissions for the target domain.

### What record type and TTL are used?

- **Type:** A (IPv4)
- **TTL:** 300 seconds (5 minutes)
- **Values:** Single IP address array

### Can I customize TTL or record type?

Not currently. These are hardcoded in `GandiService`. To change, modify the payload construction.

### What if Gandi returns an error?

The API returns **502 Bad Gateway** with error details logged. The client should retry with exponential backoff.

## Security

### How does authentication work?

**API Key** in `X-Api-Key` header. Validated by NuciAPI Authorization middleware.

### How does replay protection work?

**HMAC-SHA256** of request body + timestamp + nonce. Validated by NuciAPI ReplayProtection middleware. Prevents duplicate requests.

### How does scanner protection work?

NuciAPI ScannerProtection middleware detects and blocks automated scanning tools based on request patterns.

### Is HTTPS required?

**Yes in production.** The API enforces HTTPS via NuciAPI HttpsRedirection middleware. Development can use HTTP.

### Are secrets logged?

**No.** NuciLog masks sensitive values. API keys never appear in logs.

## Troubleshooting

### "Domain must include a subdomain"

**Cause:** Domain name has no dot (e.g., `example.com`).
**Fix:** Use `www.example.com` or `sub.example.com`.

### "Invalid domain format"

**Cause:** Domain name parsing failed.
**Fix:** Ensure domain contains at least one dot and valid characters.

### 401 Unauthorized with valid key

**Causes:**
- Key mismatch between client and server
- Replay protection (duplicate request ID/nonce)
- Timestamp outside allowed window

**Fix:** Verify key matches, ensure unique request IDs, check clock sync.

### 502 Bad Gateway

**Cause:** Gandi API call failed.
**Fix:** Check Gandi API key permissions, network connectivity, Gandi status page.

### DNS not updating

**Checks:**
1. API returns 200 OK
2. Gandi API key has correct permissions
3. Domain exists in Gandi account
4. Wait for TTL (300s) + propagation
5. Verify with `dig @9.9.9.9 www.example.com A`

### High latency

**Causes:**
- Gandi API latency
- Quad9 DNS latency
- Network issues

**Fix:** Check external service latency, consider caching (future feature).

## Development

### How do I run tests?

```bash
# Unit tests
dotnet test DynamicDnsUpdater.API.UnitTests

# Integration tests
dotnet test DynamicDnsUpdater.API.IntegrationTests

# All tests
dotnet test DynamicDnsUpdater.API.slnx
```

### How do I add a new DNS provider?

1. Add value to `DnsProvider` enum
2. Create `I{Provider}Service` interface
3. Implement `{Provider}Service` class
4. Register in `ServiceCollectionExtensions.cs`
5. Add case in `DnsRecordService.Update()`
6. Add tests

### What's the project structure?

```
DynamicDnsUpdater.API/           # Main API project
├── Controllers/                 # HTTP endpoints
├── Service/                     # Business logic
│   ├── Integrations/           # Provider implementations
│   └── Models/                 # Domain models
├── Configuration/              # Settings classes
└── Logging/                    # Logging extensions

DynamicDnsUpdater.API.UnitTests/      # Unit tests
DynamicDnsUpdater.API.IntegrationTests/ # Integration tests
```

### What .NET version is required?

**.NET 10.0** (net10.0 target framework).

## Deployment

### Can I run this in Kubernetes?

Yes. See [Build and Deployment](build-and-deployment.md#3-kubernetes-deployment) for deployment YAML.

### Can I run this in Azure/AWS/GCP?

Yes. Any platform supporting .NET 10.0 containers or processes.

### What ports does it use?

Default: **8080** (HTTP) via `ASPNETCORE_URLS`. Configurable.

### Does it need a database?

**No.** Stateless design - no local persistence. All state in Gandi DNS.

### How do I scale horizontally?

Run multiple instances behind a load balancer. No sticky sessions needed.

## Limits and Quotas

### What are Gandi API limits?

Check Gandi's current API rate limits. The API makes one call per update request.

### What are Quad9 limits?

Quad9 is a public resolver with generous limits. The API makes one query per update request.

### Is there a request size limit?

Default ASP.NET Core limits apply (~30MB). The request body is tiny (~50 bytes).

## Support

### Where do I report bugs?

Create an issue in the GitHub repository.

### Where do I request features?

Create a feature request issue in the GitHub repository.

### Is there a SLA?

No formal SLA. This is an open-source project.

### Can I contribute?

Yes! See [Contributing](contributing.md) for guidelines.