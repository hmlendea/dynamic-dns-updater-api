# Integration Models

## Overview

The integration models layer defines the contracts and implementations for external DNS provider integrations. It abstracts provider-specific details behind interfaces, enabling the application service to work with multiple providers without knowing their implementation details.

## Responsibilities

- Define provider integration interfaces
- Implement provider-specific API communication
- Handle provider authentication and request formatting
- Map application data to provider API payloads
- Handle provider-specific error responses

## Components

### IGandiService

**File:** `DynamicDnsUpdater.API/Service/Integrations/Gandi/IGandiService.cs`

**Purpose:** Interface defining the Gandi LiveDNS integration contract.

**Key Characteristics:**
- Single method: `Update(string domainName, string ipAddress)`
- Returns `Task` (async)
- No return value — success indicated by absence of exception
- Colocated with implementation per project conventions

```csharp
public interface IGandiService
{
    Task Update(
        string domainName,
        string ipAddress);
}
```

### GandiService

**File:** `DynamicDnsUpdater.API/Service/Integrations/Gandi/GandiService.cs`

**Purpose:** Implementation of `IGandiService` that communicates with Gandi LiveDNS v5 API.

**Dependencies:**
- `GandiSettings` — Contains API key for authentication
- `ILogger` (NuciLog) — Structured logging

**Lifetime:** Transient (registered in `ServiceCollectionExtensions`)

## Gandi LiveDNS API Integration

### API Endpoint
- **Base URL:** `https://api.gandi.net/v5/livedns`
- **Endpoint:** `PUT /domains/{domainName}/records/{subdomain}/A`
- **Authentication:** Bearer token (API key in Authorization header)

### Request Format

```json
{
  "rrset_type": "A",
  "rrset_ttl": 300,
  "rrset_values": ["192.0.2.1"]
}
```

### Response Handling
- **2xx** — Success (record created/updated)
- **4xx/5xx** — Failure (thrown as `HttpRequestException`)

## Execution Flow

### Update Method

```
Input: domainName, ipAddress
    ↓
1. Validate domain format (must contain at least one dot)
    ├── Invalid → throw ArgumentException
    └── Valid → Continue
    ↓
2. Extract subdomain and domain from domainName
    ├── "sub.example.com" → subdomain="sub", domain="example.com"
    └── "example.com" → throw ArgumentException (no subdomain)
    ↓
3. Construct API URL: https://api.gandi.net/v5/livedns/domains/{domain}/records/{subdomain}/A
    ↓
4. Create HTTP request:
    - Method: PUT
    - Headers: Authorization: Bearer {apiKey}, Accept: application/json
    - Body: JSON payload with A record, TTL 300, IP value
    ↓
5. Send request via HttpClient
    ↓
6. Ensure success status code (throws on failure)
    ↓
7. Log success
```

### Detailed Steps

#### Step 1: Domain Validation
```csharp
if (!domainName.Contains('.'))
{
    throw new ArgumentException(
        "Domain name must contain at least one dot.",
        nameof(domainName));
}
```

**Rationale:** Gandi LiveDNS requires a subdomain (e.g., `www.example.com`, not just `example.com`). The apex domain is managed differently.

#### Step 2: Subdomain Extraction
```csharp
int firstDotIndex = domainName.IndexOf('.');
string subdomain = domainName[..firstDotIndex];
string domain = domainName[(firstDotIndex + 1)..];
```

**Examples:**
| Input | Subdomain | Domain |
|-------|-----------|--------|
| `www.example.com` | `www` | `example.com` |
| `api.v2.example.com` | `api` | `v2.example.com` |
| `example.com` | (throws) | (throws) |

#### Step 3: HTTP Request Construction
```csharp
using HttpRequestMessage request = new(HttpMethod.Put, url)
{
    Content = new StringContent(
        JsonSerializer.Serialize(payload),
        Encoding.UTF8,
        "application/json")
};

request.Headers.Authorization =
    new AuthenticationHeaderValue("Bearer", gandiSettings.ApiKey);
request.Headers.Accept.Add(
    new MediaTypeWithQualityHeaderValue("application/json"));
```

#### Step 4: Payload Construction
```csharp
var payload = new
{
    rrset_type = "A",
    rrset_ttl = 300,
    rrset_values = new[] { ipAddress }
};
```

**Fixed Values:**
- `rrset_type`: Always "A" (IPv4 address record)
- `rrset_ttl`: Always 300 seconds (5 minutes)
- `rrset_values`: Array containing the single IP address

#### Step 5: HTTP Client Usage
```csharp
using HttpClient client = new();
using HttpResponseMessage response = await client.SendAsync(request);
response.EnsureSuccessStatusCode();
```

**Key Design Decisions:**
- Creates new `HttpClient` per request (transient lifetime matches service lifetime)
- No connection pooling or reuse — acceptable for low-frequency DNS updates
- `EnsureSuccessStatusCode` throws `HttpRequestException` on non-2xx

## Configuration

### GandiSettings

**File:** `DynamicDnsUpdater.API/Configuration/GandiSettings.cs`

**Purpose:** Configuration object for Gandi integration.

```csharp
public class GandiSettings
{
    public string ApiKey { get; set; } = string.Empty;
}
```

**Binding:** Bound from configuration section `GandiSettings` in `ServiceCollectionExtensions.AddConfigurations`.

**Required Configuration:**
```json
{
  "GandiSettings": {
    "ApiKey": "your-gandi-api-key"
  }
}
```

## Error Handling

### Provider-Level Errors
- **Invalid domain format** → `ArgumentException` (validation failure)
- **Missing subdomain** → `ArgumentException` (validation failure)
- **HTTP 4xx/5xx** → `HttpRequestException` (propagated to caller)

### Logging
```csharp
logger.Info(
    MyOperation.UpdateDnsRecord,
    OperationStatus.Started,
    new LogInfo(MyLogInfoKey.DomainName, domainName),
    new LogInfo(MyLogInfoKey.IpAddress, ipAddress));

// ... on success ...
logger.Info(
    MyOperation.UpdateDnsRecord,
    OperationStatus.Success,
    new LogInfo(MyLogInfoKey.DomainName, domainName),
    new LogInfo(MyLogInfoKey.IpAddress, ipAddress));
```

## Data Transformations

| Application Data | Transformation | Provider API |
|------------------|---------------|--------------|
| `domainName` (full) | Split at first dot | `domain` (path), `subdomain` (path) |
| `ipAddress` (string) | Wrapped in array | `rrset_values: [ipAddress]` |
| `GandiSettings.ApiKey` | Bearer token | `Authorization: Bearer {key}` |
| Fixed | Hardcoded | `rrset_type: "A"`, `rrset_ttl: 300` |

## Boundary Rules

### Integration Model Boundary
- ✅ **May:** Contain provider-specific HTTP logic, URL construction, payload formatting
- ✅ **May:** Handle provider authentication
- ✅ **May:** Throw provider-specific exceptions (wrapped in standard exceptions)
- ❌ **Must not:** Contain business logic (delegated to application service)
- ❌ **Must not:** Perform DNS verification (done by application service)
- ❌ **Must not:** Access configuration directly (receives settings via DI)

### Provider Abstraction Boundary
- ✅ **May:** Implement `IGandiService` interface
- ❌ **Must not:** Expose provider-specific types to application service
- ❌ **Must not:** Require application service to know Gandi API details

## Testing

### Unit Tests
**File:** `DynamicDnsUpdater.API.UnitTests/Service/Integrations/Gandi/GandiServiceTests.cs`

Tests cover:
- Successful update with mocked `HttpMessageHandler`
- Domain validation (throws on invalid format)
- Subdomain extraction logic
- HTTP request construction (headers, body, URL)
- Error propagation from HTTP failures

### Mocking Strategy
Tests use `HttpMessageHandler` mocking to intercept HTTP calls:
```csharp
var handlerMock = new Mock<HttpMessageHandler>();
handlerMock
    .Protected()
    .Setup<Task<HttpResponseMessage>>(
        "SendAsync",
        ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>())
    .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

var client = new HttpClient(handlerMock.Object);
```

## Evolution Path

If the integration models need to evolve:
1. **Add new providers** — Create new interface/implementation pair (e.g., `ICloudflareService`, `CloudflareService`)
2. **Add new record types** — Extend payload construction (AAAA, CNAME, TXT)
3. **Add retry logic** — Wrap `SendAsync` with Polly or similar
4. **Add rate limiting** — Implement token bucket or similar per provider
5. **Add response parsing** — Extract provider-specific response data if needed
6. **Change HTTP client strategy** — Use `IHttpClientFactory` for pooled clients