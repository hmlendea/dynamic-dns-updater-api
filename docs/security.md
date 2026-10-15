# Security

## Overview

This document describes the security architecture of the application, including authentication, authorisation, replay protection, scanner protection, header validation, and secure coding practices.

## Security Layers

The application implements defence-in-depth with multiple security layers:

```
Request
    │
    ▼
┌─────────────────────────────────────────────────────────────┐
│ Network Layer (External)                                    │
│   • HTTPS enforcement (HSTS in production)                  │
│   • Reverse proxy / load balancer                           │
│   • WAF (optional)                                          │
└─────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────┐
│ Middleware Pipeline (Application)                           │
│   1. ExceptionHandling — No info leakage                    │
│   2. ScannerProtection — Block known malicious clients      │
│   3. RequestLogging — Audit trail (no secrets)              │
│   4. HeaderValidation — Protocol compliance                 │
│   5. ReplayProtection — HMAC + nonce + timestamp            │
│   6. Authorization — API key validation                     │
└─────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────┐
│ Application Layer                                           │
│   • Input validation (model binding)                        │
│   • Business logic validation                               │
│   • Provider API authentication                             │
└─────────────────────────────────────────────────────────────┘
```

## Authentication

### API Key Authentication

**Mechanism:** Bearer token in `Authorization` header

**Header Format:**
```
Authorization: Bearer <api-key>
```

**Validation:**
- Performed by NuciAPI middleware (`UseAuthorization`)
- Compares against `SecuritySettings.ApiKey`
- Constant-time comparison (NuciAPI implementation)

**Configuration:**
```json
{
  "SecuritySettings": {
    "ApiKey": "your-secure-api-key"
  }
}
```

**Key Requirements:**
- Minimum 32 characters recommended
- Generated using cryptographically secure random
- Rotated periodically (e.g., every 90 days)
- Different keys per environment

### Gandi API Authentication

**Mechanism:** Bearer token in `Authorization` header to Gandi API

**Header Format:**
```
Authorization: Bearer <gandi-api-key>
```

**Validation:**
- Performed by Gandi LiveDNS API
- Key managed in Gandi account settings
- Scoped to specific DNS zones (recommended)

## Authorisation

### NuciAPI Authorisation

**Implementation:** `NuciApiAuthorisation.ApiKey(securitySettings.ApiKey)`

**Flow:**
1. Middleware extracts `Authorization` header
2. Validates Bearer scheme
3. Compares token with configured API key
4. Sets `HttpContext.User` with claims on success
5. Returns 401 on failure

**Claims Added:**
- `sub` — API key identifier (hashed)
- `scope` — `dns:update` (implicit)

## Replay Protection

### Purpose

Prevents attackers from capturing and re-sending valid requests.

### Mechanism

HMAC-SHA256 signature covering:
- Request ID
- Timestamp
- Nonce
- HTTP method
- Request path
- Request body hash

### Required Headers

| Header | Description | Format |
|--------|-------------|--------|
| `X-Nuci-Request-Id` | Unique request identifier | UUID v4 |
| `X-Nuci-Timestamp` | Unix timestamp (seconds) | Integer |
| `X-Nuci-Nonce` | Cryptographic nonce | Base64 (32 bytes) |
| `X-Nuci-Signature` | HMAC-SHA256 signature | Base64 |

### Validation Rules

| Check | Failure Response |
|-------|------------------|
| Missing header | 401 Unauthorized |
| Timestamp > 5 min old | 401 Unauthorized |
| Timestamp > 5 min future | 401 Unauthorized |
| Nonce already used | 401 Unauthorized |
| Invalid HMAC | 401 Unauthorized |

### Nonce Storage

- In-memory store (per application instance)
- Automatic cleanup of expired nonces
- Not distributed — each instance tracks its own nonces
- **Limitation:** Does not prevent replay across instances behind load balancer

### Client Implementation

```csharp
// Pseudocode
string requestId = Guid.NewGuid().ToString();
long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
string nonce = Convert.ToBase64String(RandomBytes(32));
string bodyHash = ComputeSha256(requestBody);
string signature = ComputeHmacSha256(
    secretKey,
    $"{requestId}\n{timestamp}\n{nonce}\n{method}\n{path}\n{bodyHash}");
```

## Scanner Protection

### Purpose

Blocks requests from known vulnerability scanners and malicious bots.

### Mechanism

**User-Agent Blocking:**
- Checks `User-Agent` header against known scanner patterns
- Patterns include: sqlmap, nikto, nessus, openvas, etc.

**IP Blocking:**
- Checks client IP against known malicious IP lists
- Lists updated periodically (NuciAPI package)

### Response

- **403 Forbidden** — Request blocked
- No detailed error information (prevents fingerprinting)
- Logged for audit purposes

### Configuration

Built into NuciAPI.Middleware.Security package. No application-specific configuration.

## Header Validation

### Purpose

Ensures all required NuciAPI protocol headers are present and valid.

### Required Headers

| Header | Validation |
|--------|------------|
| `X-Nuci-Api-Version` | Must match supported version (e.g., `1.0`) |
| `X-Nuci-Request-Id` | Must be valid UUID |
| `X-Nuci-Timestamp` | Must be valid Unix timestamp |
| `X-Nuci-Nonce` | Must be valid Base64 |
| `X-Nuci-Signature` | Must be valid Base64 |

### Response

- **400 Bad Request** — Missing or malformed headers
- Standardised error format

## Exception Handling

### Purpose

Prevents information leakage through error responses.

### Mechanism

**NuciAPI Exception Handling Middleware:**
- Catches all unhandled exceptions
- Maps to standardised error responses
- Logs full exception details internally
- Returns minimal information to client

### Error Response Format

```json
{
  "error": {
    "code": "INTERNAL_SERVER_ERROR",
    "message": "An unexpected error occurred",
    "details": {}
  }
}
```

### Information Leakage Prevention

| Scenario | Client Response | Internal Log |
|----------|-----------------|--------------|
| Unhandled exception | 500 + generic message | Full stack trace |
| Validation error | 400 + field details | Full context |
| Auth failure | 401 + generic message | Auth details |
| Provider error | 502 + generic message | Provider response |

## Transport Security

### HTTPS Enforcement

**Middleware:** `UseHttpsRedirection()`

**Behaviour:**
- Redirects HTTP → HTTPS (307 Temporary Redirect)
- In production: HSTS header added

**Production Recommendations:**
```csharp
// In Startup.Configure (production only)
app.UseHsts(); // Adds Strict-Transport-Security header
```

### Secure Headers

The application relies on NuciAPI middleware and ASP.NET Core defaults for security headers. Consider adding:

```csharp
// Optional: Add to Startup.Configure
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});
```

## Input Validation

### Request Body Validation

**Model Binding:**
- ASP.NET Core model binding validates JSON structure
- Missing properties → default values (empty strings)
- Type mismatches → 400 Bad Request

**Application-Level Validation:**
- `DnsRecordService` — Validates IP address parsing
- `GandiService` — Validates domain format (subdomain required)
- `DnsProvider.FromString` — Validates provider name

### Domain Name Validation

**Rules:**
- Must contain at least one dot (`.`)
- Must have subdomain (not apex domain)
- URL-decoded by ASP.NET Core routing

**Enforcement:** `GandiService` throws `ArgumentException` for invalid domains

### IP Address Validation

**Rules:**
- Parsed using `IPAddress.TryParse()`
- IPv4 → DNS verification performed
- IPv6 → DNS verification skipped
- Invalid → DNS verification skipped, provider update attempted

## Secrets Management

### Application Secrets

| Secret | Purpose | Storage |
|--------|---------|---------|
| `SecuritySettings.ApiKey` | Client authentication | Environment variable / User secrets |
| `GandiSettings.ApiKey` | Gandi API authentication | Environment variable / User secrets |

### Secret Handling Rules

1. **Never in source control** — Use `.gitignore` for `appsettings.*.json`
2. **Never in logs** — Not logged by application or middleware
3. **Never in error responses** — Exception handling strips secrets
4. **Environment-specific** — Different keys per environment
5. **Rotation** — Documented rotation procedure

### Development Secrets

**User Secrets (Development):**
```bash
dotnet user-secrets set SecuritySettings:ApiKey "dev-key"
dotnet user-secrets set GandiSettings:ApiKey "dev-gandi-key"
```

### Production Secrets

**Environment Variables:**
```bash
SecuritySettings__ApiKey=prod-key
GandiSettings__ApiKey=prod-gandi-key
```

**Azure Key Vault / AWS Secrets Manager:**
- Recommended for production
- Integrate via configuration providers

## Secure Coding Practices

### Dependency Injection

- Settings injected as singletons (immutable after startup)
- Services transient (no shared state)
- No static access to configuration

### HTTP Client Usage

- New `HttpClient` per request in `GandiService`
- No connection pooling (acceptable for low frequency)
- `EnsureSuccessStatusCode()` throws on non-2xx

### Exception Handling

- Catch specific exceptions where possible
- Log full context internally
- Return generic messages to clients
- Preserve stack trace when re-throwing

### Logging

- No sensitive data in log messages
- Structured logging with explicit keys
- API keys never logged

## Threat Model

### STRIDE Analysis

| Threat | Mitigation |
|--------|------------|
| **Spoofing** | API key auth, HMAC replay protection |
| **Tampering** | HTTPS, HMAC signature validation |
| **Repudiation** | Request logging with request IDs |
| **Information Disclosure** | Generic error responses, no secrets in logs |
| **Denial of Service** | Scanner protection, rate limiting (external) |
| **Elevation of Privilege** | Least privilege API keys, scoped Gandi keys |

### Attack Scenarios

| Attack | Mitigation |
|--------|------------|
| Credential stuffing | Strong API keys, rate limiting |
| Replay attack | HMAC + nonce + timestamp |
| Man-in-the-middle | HTTPS enforcement, HSTS |
| Scanner reconnaissance | Scanner protection middleware |
| DNS hijacking | Gandi API key scoped to zones |
| Log injection | Structured logging, no user input in log format |

## Compliance Considerations

### Data Protection

- No personal data processed
- Domain names and IPs are public routing information
- No GDPR/CCPA implications

### Audit Requirements

- All requests logged with request ID
- Authentication events logged
- Authorization decisions logged
- Provider API calls logged

## Security Testing

### Automated Checks

- Dependency vulnerability scanning (NuGet audit)
- Static analysis (Roslyn analyzers)
- Secret scanning (git-secrets, truffleHog)

### Manual Testing

- Penetration testing of API endpoints
- Replay attack simulation
- Scanner detection verification
- Error response inspection

## Incident Response

### Compromised API Key

1. Rotate `SecuritySettings.ApiKey` immediately
2. Rotate `GandiSettings.ApiKey` in Gandi console
3. Review logs for unauthorized access
4. Notify affected clients

### Compromised Gandi Key

1. Revoke key in Gandi account
2. Generate new key with minimal scope
3. Update `GandiSettings.ApiKey`
4. Verify DNS records unchanged

## Evolution Path

If security needs to evolve:

1. **Add OAuth2/OIDC** — Replace API key with token validation
2. **Add mTLS** — Mutual TLS for client authentication
3. **Add rate limiting** — Per-client rate limiting middleware
4. **Add IP allowlisting** — Restrict to known client IPs
5. **Add distributed nonce store** — Redis for replay protection across instances
6. **Add request signing** — Sign responses for non-repudiation
7. **Add audit logging** — Immutable audit trail to external store