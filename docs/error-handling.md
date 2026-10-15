# Error Handling

## Overview

This document describes the application's error handling strategy, including exception types, handling patterns, error response formats, and recovery mechanisms.

## Exception Taxonomy

### Application Exceptions

| Exception | Source | HTTP Status | Description |
|-----------|--------|-------------|-------------|
| `ArgumentException` | `GandiService` | 500* | Invalid domain format or missing subdomain |
| `FormatException` | `GandiService` | 500* | Invalid domain format |
| `NotImplementedException` | `DnsRecordService` | 500* | Unknown DNS provider |
| `HttpRequestException` | `GandiService` | 502* | Gandi API call failed |
| `DnsResponseException` | `DnsClient` | N/A | DNS query failed (non-fatal) |
| `SocketException` | `DnsClient` | N/A | Network failure (non-fatal) |

* Mapped by NuciAPI exception handling middleware

### Middleware Exceptions (NuciAPI)

| Exception | Middleware | HTTP Status | Description |
|-----------|------------|-------------|-------------|
| `NuciApiException` | ExceptionHandling | 400-500 | Standardised API errors |
| `UnauthorizedAccessException` | Authorization | 401 | Invalid/missing API key |
| `SecurityException` | ReplayProtection | 401 | Invalid HMAC, replay, timestamp |
| `ForbiddenException` | ScannerProtection | 403 | Scanner detected |

## Error Handling Flow

```
Exception Thrown
    │
    ▼
┌─────────────────────────────────────────────────────────────┐
│ NuciAPI ExceptionHandling Middleware (First in Pipeline)   │
│                                                             │
│   Catches ALL unhandled exceptions from downstream         │
│                                                             │
│   Mapping Rules:                                            │
│   ├── ArgumentException → 400 Bad Request                  │
│   ├── FormatException → 400 Bad Request                    │
│   ├── NotImplementedException → 501 Not Implemented        │
│   ├── HttpRequestException → 502 Bad Gateway               │
│   ├── UnauthorizedAccessException → 401 Unauthorized       │
│   ├── SecurityException → 401 Unauthorized                 │
│   ├── ForbiddenException → 403 Forbidden                   │
│   └── Exception → 500 Internal Server Error                │
│                                                             │
│   Response Format:                                          │
│   {                                                         │
│     "error": {                                              │
│       "code": "ERROR_CODE",                                 │
│       "message": "Human-readable message",                  │
│       "details": {}                                         │
│     }                                                       │
│   }                                                         │
│                                                             │
│   Logging: Full exception with stack trace logged          │
└─────────────────────────────────────────────────────────────┘
    │
    ▼
HTTP Response to Client
```

## Error Response Format

All error responses follow the NuciAPI standardised format:

```json
{
  "error": {
    "code": "ERROR_CODE",
    "message": "Human-readable description",
    "details": {}
  }
}
```

### Common Error Codes

| Code | HTTP Status | Description |
|------|-------------|-------------|
| `BAD_REQUEST` | 400 | Invalid request format or parameters |
| `UNAUTHORIZED` | 401 | Authentication failed |
| `FORBIDDEN` | 403 | Access denied (scanner, IP block) |
| `NOT_FOUND` | 404 | Endpoint not found |
| `CONFLICT` | 409 | Replay attack detected |
| `INTERNAL_SERVER_ERROR` | 500 | Unexpected application error |
| `NOT_IMPLEMENTED` | 501 | Feature not implemented |
| `BAD_GATEWAY` | 502 | Upstream service error |
| `SERVICE_UNAVAILABLE` | 503 | Service temporarily unavailable |

## Handling Patterns

### Application Service Pattern (DnsRecordService)

```csharp
public async Task Update(string domainName, string ipAddress, string dnsProviderName)
{
    var logInfos = new[]
    {
        new LogInfo(MyLogInfoKey.Provider, dnsProviderName),
        new LogInfo(MyLogInfoKey.DomainName, domainName),
        new LogInfo(MyLogInfoKey.IpAddress, ipAddress)
    };

    // 1. Log operation started
    logger.Info(MyOperation.UpdateDnsRecord, OperationStatus.Started, logInfos);

    try
    {
        // 2. Perform operation
        // ... DNS verification ...
        // ... Provider call ...

        // 3. Log success
        logger.Info(MyOperation.UpdateDnsRecord, OperationStatus.Success, logInfos);
    }
    catch (Exception ex)
    {
        // 4. Log failure with full context
        logger.Error(MyOperation.UpdateDnsRecord, OperationStatus.Failure, ex, logInfos);

        // 5. Re-throw to preserve stack trace
        throw;
    }
}
```

**Key Principles:**
- Log at operation boundaries (start, success, failure)
- Include structured context (provider, domain, IP)
- Log exception with full details internally
- Re-throw to let middleware handle HTTP response
- Never catch and swallow exceptions

### Provider Adapter Pattern (GandiService)

```csharp
public async Task Update(string domainName, string ipAddress)
{
    // 1. Validate input (fail fast)
    if (!domainName.Contains('.'))
    {
        throw new ArgumentException("Domain name must contain at least one dot.", nameof(domainName));
    }

    // 2. Extract components
    int firstDotIndex = domainName.IndexOf('.');
    string subdomain = domainName[..firstDotIndex];
    string domain = domainName[(firstDotIndex + 1)..];

    if (string.IsNullOrEmpty(subdomain))
    {
        throw new ArgumentException("The domain must include a subdomain.", nameof(domainName));
    }

    // 3. Build and send request
    using var client = new HttpClient();
    using var request = new HttpRequestMessage(HttpMethod.Put, url)
    {
        Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
    };
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", gandiSettings.ApiKey);

    // 4. Let HttpClient throw on failure
    using var response = await client.SendAsync(request);
    response.EnsureSuccessStatusCode(); // Throws HttpRequestException on non-2xx

    // 5. Log success
    logger.Info(MyOperation.UpdateDnsRecord, OperationStatus.Success, logInfos);
}
```

**Key Principles:**
- Validate early with descriptive exceptions
- Use `EnsureSuccessStatusCode()` for HTTP errors
- Let exceptions propagate (don't wrap)
- Log success after successful completion

### Middleware Pattern (NuciAPI)

The NuciAPI middleware handles cross-cutting concerns:

```csharp
// ExceptionHandling middleware (simplified)
public async Task InvokeAsync(HttpContext context, RequestDelegate next)
{
    try
    {
        await next(context);
    }
    catch (Exception ex)
    {
        // Map to standardised response
        var (statusCode, errorCode, message) = MapException(ex);

        // Log full exception
        logger.Error(ex, "Unhandled exception");

        // Write standardised response
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new { error = new { code = errorCode, message } });
    }
}
```

## Non-Fatal Errors

### DNS Verification Failures

**Behaviour:** DNS lookup failures do NOT fail the request.

```csharp
private static async Task<bool> IsAlreadyResolvedToRequestedIpAddress(string domainName, string ipAddress)
{
    if (!IPAddress.TryParse(ipAddress, out var requestedAddress) ||
        requestedAddress.AddressFamily != AddressFamily.InterNetwork)
    {
        return false; // Skip verification for IPv6/invalid
    }

    try
    {
        var response = await Quad9LookupClient.QueryAsync(domainName, QueryType.A);
        return response.Answers.ARecords().Any(r => r.Address.Equals(requestedAddress));
    }
    catch (DnsResponseException)
    {
        return false; // DNS error → proceed to update
    }
    catch (SocketException)
    {
        return false; // Network error → proceed to update
    }
}
```

**Rationale:**
- DNS is an optimisation, not a correctness requirement
- Provider update is the authoritative action
- Network partitions shouldn't block legitimate updates

### Logging Failures

**Behaviour:** Logging failures do not affect request processing.

- NuciLog is designed to be non-blocking
- Log sink failures are handled internally
- Application continues regardless of log output

## Recovery Mechanisms

### Retry Logic

**Current State:** No application-level retry logic.

**Provider Level:**
- Gandi API: No retry (single attempt)
- DNS Query: No retry (single attempt)

**Recommended (Future):**
```csharp
// Example: Polly retry policy for Gandi
var retryPolicy = Policy
    .Handle<HttpRequestException>()
    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
```

### Circuit Breaker

**Current State:** Not implemented.

**Recommended (Future):**
```csharp
// Example: Polly circuit breaker for Gandi
var circuitBreaker = Policy
    .Handle<HttpRequestException>()
    .CircuitBreakerAsync(5, TimeSpan.FromMinutes(1));
```

### Fallback

**Current State:** No fallback for provider failures.

**DNS Verification:** Implicit fallback — if DNS fails, proceed to provider.

## Error Logging

### Structured Error Logging

All errors are logged with structured context:

```csharp
logger.Error(
    MyOperation.UpdateDnsRecord,
    OperationStatus.Failure,
    exception,
    new LogInfo(MyLogInfoKey.Provider, "Gandi"),
    new LogInfo(MyLogInfoKey.DomainName, "www.example.com"),
    new LogInfo(MyLogInfoKey.IpAddress, "192.0.2.1"));
```

### Log Fields for Errors

| Field | Source | Purpose |
|-------|--------|---------|
| `Operation` | `MyOperation` | Identifies the failed operation |
| `Status` | `OperationStatus.Failure` | Indicates failure state |
| `Exception` | Caught exception | Full exception details |
| `Provider` | `MyLogInfoKey.Provider` | Which provider was used |
| `DomainName` | `MyLogInfoKey.DomainName` | Target domain |
| `IpAddress` | `MyLogInfoKey.IpAddress` | Target IP |

## Client Error Handling Guide

### Retryable Errors

| Status | Code | Retry | Backoff |
|--------|------|-------|---------|
| 429 | `TOO_MANY_REQUESTS` | Yes | Exponential (respect Retry-After) |
| 500 | `INTERNAL_SERVER_ERROR` | Yes | Exponential (max 3) |
| 502 | `BAD_GATEWAY` | Yes | Exponential (max 3) |
| 503 | `SERVICE_UNAVAILABLE` | Yes | Exponential (max 3) |

### Non-Retryable Errors

| Status | Code | Action |
|--------|------|--------|
| 400 | `BAD_REQUEST` | Fix request format |
| 401 | `UNAUTHORIZED` | Fix authentication |
| 403 | `FORBIDDEN` | Contact administrator |
| 404 | `NOT_FOUND` | Check endpoint URL |
| 409 | `CONFLICT` | Generate new request ID/nonce |
| 501 | `NOT_IMPLEMENTED` | Check provider name |

## Testing Error Handling

### Unit Test Patterns

**Service Exception Propagation:**
```csharp
[Test]
public void GivenGandiServiceThrows_WhenUpdateIsCalled_ThenRethrowsException()
{
    gandiServiceMock
        .Setup(x => x.Update(It.IsAny<string>(), It.IsAny<string>()))
        .ThrowsAsync(new HttpRequestException("API failed"));

    Assert.That(
        async () => await service.Update("www.example.com", "192.0.2.1", "Gandi"),
        Throws.InstanceOf<HttpRequestException>());
}
```

**Error Logging Verification:**
```csharp
[Test]
public async Task Update_GandiThrows_LogsFailure()
{
    gandiServiceMock
        .Setup(x => x.Update(It.IsAny<string>(), It.IsAny<string>()))
        .ThrowsAsync(new HttpRequestException("API error"));

    await Assert.ThrowsAsync<HttpRequestException>(
        () => service.Update("www.example.com", "192.0.2.1", "Gandi"));

    loggerMock.Verify(
        x => x.Error(
            MyOperation.UpdateDnsRecord,
            OperationStatus.Failure,
            It.IsAny<Exception>(),
            It.IsAny<IEnumerable<LogInfo>>()),
        Times.Once);
}
```

### Integration Test Patterns

**Exception Handling Middleware:**
```csharp
[Test]
public async Task GivenServiceThrows_WhenRequestReceived_ThenReturns500()
{
    // Arrange
    serviceMock
        .Setup(x => x.Update(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
        .ThrowsAsync(new HttpRequestException("Provider error"));

    // Act
    var response = await client.SendAsync(CreateValidRequest());

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    var body = await response.Content.ReadAsStringAsync();
    body.Should().Contain("INTERNAL_SERVER_ERROR");
}
```

## Evolution Path

If error handling needs to evolve:

1. **Add retry policies** — Use Polly for transient failures
2. **Add circuit breakers** — Prevent cascade failures
3. **Add fallback responses** — Graceful degradation
4. **Add error codes** — More granular error codes for clients
5. **Add problem details** — RFC 7807 Problem Details format
6. **Add distributed tracing** — Correlation IDs across services
7. **Add dead letter queue** — For failed async operations