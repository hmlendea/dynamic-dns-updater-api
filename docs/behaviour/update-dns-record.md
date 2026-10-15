# Update DNS Record Behaviour

## Overview

This document describes the user-facing behaviour of the DNS record update endpoint. It covers the HTTP contract, request/response formats, validation rules, error responses, and expected client behaviour.

## Endpoint

**Method:** `PUT`
**Path:** `/DnsRecords/{domainName}`
**Content-Type:** `application/json`

## Request

### Route Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `domainName` | string | Yes | Fully qualified domain name including subdomain (e.g., `www.example.com`) |

### Headers

| Header | Required | Description |
|--------|----------|-------------|
| `Authorization` | Yes | Bearer token: `Bearer <api-key>` |
| `X-Nuci-Api-Version` | Yes | API version (e.g., `1.0`) |
| `X-Nuci-Request-Id` | Yes | Unique request identifier (UUID) |
| `X-Nuci-Timestamp` | Yes | Unix timestamp (seconds) |
| `X-Nuci-Nonce` | Yes | Cryptographic nonce (base64) |
| `X-Nuci-Signature` | Yes | HMAC-SHA256 signature |

### Request Body

```json
{
  "ip": "192.0.2.1",
  "provider": "Gandi"
}
```

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `ip` | string | Yes | Target IP address (IPv4 or IPv6) |
| `provider` | string | Yes | DNS provider name (currently only `Gandi`) |

### Request Examples

**IPv4 Update:**
```http
PUT /DnsRecords/www.example.com HTTP/1.1
Host: api.example.com
Authorization: Bearer abc123
X-Nuci-Api-Version: 1.0
X-Nuci-Request-Id: 550e8400-e29b-41d4-a716-446655440000
X-Nuci-Timestamp: 1699900000
X-Nuci-Nonce: dGhpcyBpcyBhIG5vbmNl
X-Nuci-Signature: base64-hmac-signature
Content-Type: application/json

{
  "ip": "192.0.2.1",
  "provider": "Gandi"
}
```

**IPv6 Update:**
```json
{
  "ip": "2001:db8::1",
  "provider": "Gandi"
}
```

**Case-Insensitive Provider:**
```json
{
  "ip": "192.0.2.1",
  "provider": "gandi"
}
```

## Response

### Success Response

**Status:** `200 OK`
**Body:** Empty (no content)

```http
HTTP/1.1 200 OK
Content-Length: 0
```

### Error Responses

All error responses follow the NuciAPI standardised error format:

```json
{
  "error": {
    "code": "ERROR_CODE",
    "message": "Human-readable description",
    "details": {}
  }
}
```

| Scenario | Status | Error Code | Message |
|----------|--------|------------|---------|
| Missing/invalid API key | 401 | `UNAUTHORIZED` | Invalid or missing API key |
| Missing NuciAPI headers | 400 | `BAD_REQUEST` | Required header missing or invalid |
| Invalid HMAC signature | 401 | `UNAUTHORIZED` | Invalid request signature |
| Replay detected (nonce reuse) | 401 | `UNAUTHORIZED` | Request replay detected |
| Timestamp outside window | 401 | `UNAUTHORIZED` | Request timestamp expired |
| Scanner detected | 403 | `FORBIDDEN` | Request blocked by scanner protection |
| Invalid JSON body | 400 | `BAD_REQUEST` | Request body is not valid JSON |
| Missing `ip` property | 400 | `BAD_REQUEST` | Required property 'ip' is missing |
| Missing `provider` property | 400 | `BAD_REQUEST` | Required property 'provider' is missing |
| Unknown provider | 500 | `INTERNAL_SERVER_ERROR` | The requested DNS provider is not supported |
| Domain lacks subdomain | 500 | `INTERNAL_SERVER_ERROR` | Domain name must contain at least one dot |
| Gandi API error | 502 | `BAD_GATEWAY` | Upstream DNS provider error |
| Internal server error | 500 | `INTERNAL_SERVER_ERROR` | An unexpected error occurred |

## Validation Rules

### Domain Name (`domainName` route parameter)

| Rule | Enforcement | Error |
|------|-------------|-------|
| Must be present in route | ASP.NET Core routing | 404 Not Found |
| Must contain at least one dot | GandiService validation | 500 Internal Server Error |
| Must have subdomain (not apex) | GandiService validation | 500 Internal Server Error |
| URL-encoded characters decoded | ASP.NET Core | Automatic |

**Valid Examples:**
- `www.example.com`
- `api.v2.example.com`
- `sub.domain.example.com`

**Invalid Examples:**
- `example.com` (no subdomain)
- `localhost` (no dot)
- `` (empty)

### IP Address (`ip` body property)

| Rule | Enforcement | Error |
|------|-------------|-------|
| Must be present in JSON | Model binding | 400 Bad Request |
| Must be valid IP (v4 or v6) | Service parsing | 500* (if unparsable) |
| IPv4 → DNS verification | Service logic | No error (optimisation) |
| IPv6 → Skip verification | Service logic | No error (by design) |

* Unparsable IPs skip verification but still attempt provider update

**Valid Examples:**
- `192.0.2.1` (IPv4)
- `2001:db8::1` (IPv6)
- `10.0.0.1` (private IPv4)

### Provider (`provider` body property)

| Rule | Enforcement | Error |
|------|-------------|-------|
| Must be present in JSON | Model binding | 400 Bad Request |
| Must match known provider | DnsProvider.FromString | 500 Internal Server Error |
| Case-insensitive matching | DnsProvider.FromString | N/A |

**Valid Values:**
- `Gandi`
- `gandi`
- `GANDI`

**Invalid Values:**
- `Cloudflare`
- `Route53`
- `""` (empty)
- `null`

## Behavioural Guarantees

### Idempotency

The operation is **idempotent**:
- Multiple identical requests produce the same end state
- Gandi LiveDNS PUT is idempotent (create or update)
- Short-circuit on already-resolved IP still returns 200 OK

### DNS Verification Short-Circuit

**Behaviour:** If the domain already resolves to the requested IPv4 address, the provider update is skipped.

**Conditions:**
- Requested IP is valid IPv4
- Quad9 DNS query succeeds
- At least one A record matches the requested IP

**Result:** Returns 200 OK without calling provider API

**Note:** This is an optimisation, not a correctness requirement. If DNS query fails, update proceeds.

### IPv6 Handling

**Behaviour:** IPv6 addresses bypass DNS verification entirely.

**Rationale:** Quad9 lookup only checks A records (IPv4). AAAA record verification not implemented.

**Result:** Provider update always attempted for IPv6.

## Client Implementation Guide

### Required Headers Generation

Clients must generate NuciAPI protocol headers:

```csharp
// Pseudocode for header generation
string requestId = Guid.NewGuid().ToString();
long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
string nonce = Convert.ToBase64String(RandomBytes(32));
string signature = ComputeHmacSha256(
    secretKey,
    $"{requestId}\n{timestamp}\n{nonce}\n{httpMethod}\n{path}\n{bodyHash}");
```

### Retry Logic

| Error | Retryable | Recommended Backoff |
|-------|-----------|---------------------|
| 400 Bad Request | No | N/A (fix request) |
| 401 Unauthorized | No | N/A (fix auth) |
| 403 Forbidden | No | N/A (IP blocked) |
| 404 Not Found | No | N/A (wrong endpoint) |
| 429 Too Many Requests | Yes | Exponential (respect Retry-After) |
| 500 Internal Server Error | Yes | Exponential (max 3 retries) |
| 502 Bad Gateway | Yes | Exponential (max 3 retries) |
| 503 Service Unavailable | Yes | Exponential (max 3 retries) |

### Client Example (cURL)

```bash
curl -X PUT "https://api.example.com/DnsRecords/www.example.com" \
  -H "Authorization: Bearer YOUR_API_KEY" \
  -H "X-Nuci-Api-Version: 1.0" \
  -H "X-Nuci-Request-Id: $(uuidgen)" \
  -H "X-Nuci-Timestamp: $(date +%s)" \
  -H "X-Nuci-Nonce: $(openssl rand -base64 32)" \
  -H "X-Nuci-Signature: $(compute_signature)" \
  -H "Content-Type: application/json" \
  -d '{"ip": "192.0.2.1", "provider": "Gandi"}'
```

## Edge Cases

### Concurrent Updates

- Multiple clients updating same domain → Last write wins (Gandi behaviour)
- No client-side coordination provided
- Consider external locking if strict ordering required

### DNS Propagation Delay

- Gandi TTL is 300 seconds (5 minutes)
- Quad9 cache may delay verification visibility
- Short-circuit may not trigger immediately after update

### Provider API Limits

- Gandi LiveDNS has rate limits (not enforced by this API)
- Clients should implement their own rate limiting
- 429 responses from Gandi → 502 to client

### Network Partitions

- Quad9 unreachable → Update proceeds (non-fatal)
- Gandi unreachable → 502 Bad Gateway
- Client should retry with backoff

## Monitoring and Observability

### Log Events (Structured)

| Operation | Status | Key Fields |
|-----------|--------|------------|
| UpdateDnsRecord | Started | provider, domainName, ipAddress |
| UpdateDnsRecord | Success | provider, domainName, ipAddress |
| UpdateDnsRecord | Failure | provider, domainName, ipAddress, exception |

### Metrics to Track

- Request rate (req/s)
- Success rate (2xx / total)
- Error rate by code (4xx, 5xx)
- Latency percentiles (p50, p95, p99)
- Short-circuit rate (DNS already resolved)
- Provider error rate

## Versioning and Compatibility

### Current Version

- API Version: `1.0` (via `X-Nuci-Api-Version` header)
- No breaking changes since initial release

### Future Changes

| Change Type | Strategy |
|-------------|----------|
| New provider | Add to `DnsProvider`, new service implementation |
| New record types | Extend request body, provider adapters |
| New IP version | Already supports IPv6 (no verification) |
| Breaking change | New endpoint path or API version header |

## Security Considerations

### Authentication

- API key in `Authorization: Bearer` header
- Key validated by NuciAPI middleware
- Keys should be rotated periodically

### Replay Protection

- HMAC signature covers method, path, body, timestamp, nonce
- Timestamp window: ±5 minutes (configurable)
- Nonce tracked to prevent reuse

### Transport Security

- HTTPS enforced via `UseHttpsRedirection`
- HSTS recommended in production
- No sensitive data in URLs or logs (IP addresses logged)

### Input Validation

- Domain name validated by provider (not at API boundary)
- IP address parsed but not strictly validated (provider validates)
- JSON deserialization handles malformed input