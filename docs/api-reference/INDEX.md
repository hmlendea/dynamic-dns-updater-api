# API Reference Index

## Endpoint Summary

| Method | Path | Controller | Action | Auth |
|--------|------|------------|--------|------|
| PUT | `/DnsRecords/{domainName}` | `DnsRecordsController` | `Update` | API Key |

## Global Authentication

All endpoints require API key authentication via the `X-Api-Key` header. The API key is validated against the configured `SecuritySettings.ApiKey` value.

## Versioning Strategy

The API is currently unversioned in the URL path. Future versions should be introduced via URL path (e.g., `/v1/DnsRecords/{domainName}`) to maintain backward compatibility.

## Rate Limiting / Throttling

No rate limiting is implemented in the application. Rate limiting should be configured at the deployment infrastructure level (reverse proxy, API gateway, or cloud provider).

## Related Documentation

- [DNS Record Update Flow](../flows/dns-record-update.md)
- [Update DNS Record Behaviour](../behaviour/update-dns-record.md)
- [Data Model](../data-model.md)
- [Configuration](../configuration.md)

## Quick Reference

### Update DNS Record

**Endpoint:** `PUT /DnsRecords/{domainName}`

**Authentication:** Required — `X-Api-Key` header

**Request Body:**
```json
{
  "ip": "192.0.2.1",
  "provider": "Gandi"
}
```

**Success Response:** `200 OK` (empty body)

**Error Responses:**
- `400 Bad Request` — Invalid request body or parameters
- `401 Unauthorized` — Missing or invalid API key
- `403 Forbidden` — Scanner protection triggered
- `409 Conflict` — Replay protection triggered
- `500 Internal Server Error` — Provider API failure or unexpected error

**See [dnsrecords.md](./dnsrecords.md) for complete endpoint documentation.**