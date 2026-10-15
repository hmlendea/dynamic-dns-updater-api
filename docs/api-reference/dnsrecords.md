# DnsRecordsController

Base path: `/DnsRecords`

## Update (PUT /{domainName})

### HTTP Method and Path

`PUT /DnsRecords/{domainName}`

### Controller Action

`DynamicDnsUpdater.API.Controllers.DnsRecordsController.Update`

### Authentication / Authorisation

- **Scheme:** API Key
- **Header:** `X-Api-Key`
- **Validation:** NuciAPI middleware validates the API key against `SecuritySettings.ApiKey`
- **Required:** Yes

### Request

#### Path Parameters

| Name | Type | Constraints | Example |
|------|------|-------------|---------|
| `domainName` | string | Must be a valid domain name with subdomain (e.g., `sub.example.com`) | `home.nucilandia.ro` |

#### Query Parameters

None.

#### Headers

| Name | Required | Example |
|------|----------|---------|
| `X-Api-Key` | Yes | `NucileRullz!` |
| `X-Client-Id` | Yes (NuciAPI) | `client-123` |
| `X-Request-Id` | Yes (NuciAPI) | `req-456` |
| `X-Timestamp` | Yes (NuciAPI) | `2026-10-15T10:30:00.0000000Z` |
| `X-Hmac-Token` | Optional (replay protection) | `hmac-value` |

#### Body Schema

```json
{
  "ip": "192.0.2.1",
  "provider": "Gandi"
}
```

| Field | Type | Required | Description | Validation |
|-------|------|----------|-------------|------------|
| `ip` | string | Yes | Target IP address (IPv4 or IPv6) | Must be a valid IP address format |
| `provider` | string | Yes | DNS provider name | Must be a supported provider (currently only `Gandi`) |

**JSON Property Names:** `ip` and `provider` (camelCase)

### Response

#### Success

| Status Code | Description | Body |
|-------------|-------------|------|
| `200 OK` | DNS record updated successfully or already pointed to the requested IP | Empty |

#### Error Status Codes

| Status Code | Condition | Example Response |
|-------------|-----------|------------------|
| `400 Bad Request` | Invalid JSON, missing fields, invalid domain format, domain without subdomain | `{"error": "Invalid domain format."}` |
| `401 Unauthorized` | Missing or invalid `X-Api-Key` header | `{"error": "Unauthorized"}` |
| `403 Forbidden` | Scanner protection triggered (known malicious client) | `{"error": "Forbidden"}` |
| `409 Conflict` | Replay protection triggered (duplicate request) | `{"error": "Conflict"}` |
| `500 Internal Server Error` | Gandi API failure, DNS resolution error, unexpected exception | `{"error": "Failed to update DNS record. Status code: 401, Response: ..."}` |

#### Response Headers

None specific to this endpoint.

### Behaviour Summary

1. **Request validation** — NuciAPI middleware validates protocol headers (client ID, request ID, timestamp), replay protection (HMAC token), and scanner protection
2. **Authorisation** — API key validated against configured `SecuritySettings.ApiKey`
3. **Model binding** — Route parameter `domainName` and JSON body bound to `PutDnsRecordRequest`
4. **Service invocation** — Controller calls `IDnsRecordService.Update(domainName, ipAddress, dnsProvider)`
5. **DNS verification** — `DnsRecordService` queries Quad9 (9.9.9.9, 149.112.112.112) for A records if the IP is IPv4
   - If the domain already resolves to the requested IPv4 address, returns success without calling provider
   - IPv6 addresses and unparsable IPs skip this verification
   - DNS lookup failures are treated as "not already resolved" and update proceeds
6. **Provider selection** — `DnsProvider.FromString(providerName)` resolves provider
   - Currently only `Gandi` is supported
   - Unknown provider throws `NotImplementedException`
7. **Provider update** — `GandiService.Update(domainName, ipAddress)` called
   - Validates domain format (must have subdomain: `sub.domain.tld`)
   - Constructs Gandi LiveDNS v5 API payload: A record, TTL 300
   - Sends authenticated PUT request to `https://api.gandi.net/v5/livedns/domains/{domain}/records/{subdomain}/A`
8. **Response** — Success returns 200 OK; exceptions propagate to NuciAPI exception handling middleware

### Side Effects

- Outbound HTTPS request to Gandi LiveDNS API
- Outbound DNS query to Quad9 (for IPv4 addresses)
- Structured log entries emitted via NuciLog (operation: `UpdateDnsRecord`)

### Idempotency

The endpoint is **idempotent** for the same inputs:
- If the DNS record already points to the requested IP, no provider call is made
- If the provider is called, Gandi's API is idempotent for identical A record updates

### Flow References

- [DNS Record Update Flow](../flows/dns-record-update.md)
- [Startup and Request Pipeline](../flows/startup-and-rendering.md)

### Error Scenarios

| Scenario | HTTP Status | Error Message | Logged |
|----------|-------------|---------------|--------|
| Missing API key | 401 | Unauthorized | Warning |
| Invalid API key | 401 | Unauthorized | Warning |
| Replay attack detected | 409 | Conflict | Warning |
| Scanner detected | 403 | Forbidden | Warning |
| Invalid domain format | 400 | Invalid domain format. | Error |
| Domain without subdomain | 400 | The domain must include a subdomain. | Error |
| Unknown provider | 500 | The requested DNS provider is not supported. | Error |
| Gandi API authentication failure | 500 | Failed to update DNS record... (provider error) | Error |
| Gandi API rate limited | 500 | Failed to update DNS record... (provider error) | Error |
| Network failure to Gandi | 500 | The HTTP call to the Gandi API has failed. | Error |
| Quad9 DNS query failure | — | (Proceeds to provider update) | Info (optimisation miss) |