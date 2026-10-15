# Design Decisions

## Overview

This document describes the key architectural and design decisions made during the development of the Dynamic DNS Updater API, including rationale, trade-offs, and consequences.

## Decision Log

### 1. Modular Monolith Architecture

**Decision:** Implement a modular monolith with clear boundaries between HTTP Transport/Middleware → Controller → Application Service → Provider Integration → External Systems.

**Rationale:**
- **Maintainability:** Clear separation of concerns makes the codebase easier to understand and modify
- **Testability:** Each layer can be tested independently with appropriate mocking
- **Scalability:** Monolith allows for easier deployment while maintaining clean boundaries for future microservicification
- **Performance:** Single deployment with optimized internal communication

**Trade-offs:**
- **Deployment:** Requires deploying the entire application for changes to any module
- **Resource Usage:** All components run in the same process
- **Complexity:** Requires careful boundary definition to avoid coupling

**Implementation:**
```
┌─────────────────────────────────────────────────────────────┐
│ HTTP Transport/Middleware (NuciAPI)                        │
│                                                             │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐ │
│  │ ExceptionHandling│ │ ScannerProtection│ │ RequestLogging│ │
│  │ HeaderValidation │ │ ReplayProtection │ │ Authorization│ │
│  └─────────────────┘  └─────────────────┘  └─────────────┘ │
│                                                             │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐ │
│  │ HttpsRedirection│ │ StaticFiles     │ │ Routing      │ │
│  └─────────────────┘  └─────────────────┘  └─────────────┘ │
│                                                             │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐ │
│  │ Endpoints       │  │ Controllers     │  │ Services    │ │
│  │ (API Routes)    │  │ (DnsRecords)    │  │ (DnsRecord) │ │
│  └─────────────────┘  └─────────────────┘  └─────────────┘ │
│                                                             │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐ │
│  │ Integrations    │  │ Models          │  │ Providers   │ │
│  │ (Gandi)         │  │ (DnsProvider)   │  │ (Gandi)     │ │
│  └─────────────────┘  └─────────────────┘  └─────────────┘ │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 2. NuciAPI Middleware Stack

**Decision:** Use NuciAPI middleware for cross-cutting concerns instead of implementing them manually.

**Rationale:**
- **Consistency:** Leverages battle-tested, production-ready middleware
- **Security:** Benefits from established security patterns and best practices
- **Maintenance:** Reduces custom code for common concerns
- **Integration:** Works seamlessly with the existing Nuci ecosystem

**Trade-offs:**
- **Vendor Lock-in:** Tightly coupled to NuciAPI ecosystem
- **Flexibility:** Less control over middleware behavior
- **Learning Curve:** Requires understanding NuciAPI patterns

**Implementation:**
```csharp
// Startup.cs - Middleware pipeline configuration
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<ScannerProtectionMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<HeaderValidationMiddleware>();
app.UseMiddleware<ReplayProtectionMiddleware>();
app.UseMiddleware<AuthorizationMiddleware>();
```

### 3. Static Quad9 DNS Client

**Decision:** Use a static, non-caching DnsClient LookupClient for Quad9 DNS queries.

**Rationale:**
- **Performance:** Static instance avoids connection overhead
- **Simplicity:** Single point of configuration
- **Reliability:** Consistent behavior across the application
- **Optimization:** Caching disabled ensures fresh lookups

**Trade-offs:**
- **Resource Usage:** Single instance shared across all requests
- **Scalability:** Potential bottleneck under high load
- **Flexibility:** Hard to configure different DNS servers

**Implementation:**
```csharp
private static readonly LookupClient Quad9LookupClient = new LookupClient(
    new IPEndPoint[] { IPAddress.Parse("9.9.9.9"), IPAddress.Parse("149.112.112.112") });
```

### 4. Per-Request HttpClient

**Decision:** Create HttpClient per request in GandiService instead of using a singleton.

**Rationale:**
- **DNS Resolution:** Avoids SocketException with DNS resolution
- **Best Practices:** Follows Microsoft's recommended pattern for ASP.NET Core
- **Simplicity:** No need for connection pooling configuration
- **Reliability:** Each request gets a fresh connection

**Trade-offs:**
- **Performance:** Higher connection overhead
- **Resource Usage:** More connections under load
- **Complexity:** Need to manage disposable clients

**Implementation:**
```csharp
public async Task Update(string domainName, string ipAddress)
{
    using var client = new HttpClient();
    using var request = new HttpRequestMessage(HttpMethod.Put, url)
    {
        Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
    };
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", gandiSettings.ApiKey);

    using var response = await client.SendAsync(request);
    response.EnsureSuccessStatusCode();
}
```

### 5. DnsProvider Enumeration with Factory

**Decision:** Use DnsProvider enumeration with FromString factory method for case-insensitive provider selection.

**Rationale:**
- **Extensibility:** Easy to add new providers
- **Type Safety:** Compile-time checking of provider names
- **Simplicity:** Single point of provider logic
- **User-Friendly:** Case-insensitive parsing

**Trade-offs:**
- **Limited to Enum:** Cannot easily add dynamic providers
- **Hardcoded Values:** Requires code changes for new providers
- **Testing:** Requires mocking enum values

**Implementation:**
```csharp
public enum DnsProvider
{
    Gandi
}

public static class DnsProvider
{
    public static DnsProvider FromString(string providerName)
        => Enum.TryParse(providerName, true, out var result) ? result : Gandi;
}
```

### 6. No Local Persistence

**Decision:** No local caching of DNS records or provider state.

**Rationale:**
- **Statelessness:** Enables horizontal scaling
- **Simplicity:** Single source of truth (provider API)
- **Reliability:** No stale data issues
- **Deployment:** Simpler deployment and maintenance

**Trade-offs:**
- **Performance:** Each request requires provider API call
- **Cost:** Provider API calls may have limits
- **Availability:** Dependent on provider API availability

**Implementation:**
- No local database or cache
- Each update operation calls provider API directly
- DNS verification is read-only, non-authoritative

### 7. IPv4-Only DNS Verification

**Decision:** DNS verification only for IPv4 A records, not a correctness requirement.

**Rationale:**
- **Current Use Case:** Requires IPv4 only
- **Simplicity:** Simpler verification logic
- **Optimization:** DNS verification is an optimisation, not a correctness requirement
- **Reliability:** Provider update is the authoritative action

**Trade-offs:**
- **Limited Coverage:** Cannot verify IPv6 records
- **False Sense of Security:** DNS verification doesn't guarantee correctness
- **Performance:** Still requires DNS lookup

**Implementation:**
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

### 8. Structured Logging with NuciLog

**Decision:** Use NuciLog with operation-based structured logging.

**Rationale:**
- **Consistency:** Standardized logging across the application
- **Structure:** Structured logs enable better analysis
- **Operations:** Operation-based logging tracks business processes
- **Integration:** Works with Nuci ecosystem

**Trade-offs:**
- **Learning Curve:** Requires understanding NuciLog patterns
- **Vendor Lock-in:** Tightly coupled to NuciLog
- **Performance:** Structured logging may have overhead

**Implementation:**
```csharp
logger.Info(
    MyOperation.UpdateDnsRecord,
    OperationStatus.Started,
    new LogInfo(MyLogInfoKey.Provider, "Gandi"),
    new LogInfo(MyLogInfoKey.DomainName, "www.example.com"),
    new LogInfo(MyLogInfoKey.IpAddress, "192.0.2.1"));
```

### 9. Configuration Binding

**Decision:** Bind SecuritySettings and GandiSettings from appsettings.json as singletons.

**Rationale:**
- **Performance:** Singleton instances avoid repeated binding
- **Simplicity:** Simple configuration management
- **Reliability:** Configuration available throughout application
- **Testability:** Easy to mock in tests

**Trade-offs:**
- **Mutability:** Configuration changes require application restart
- **Memory:** Configuration objects stored in memory
- **Thread Safety:** Singletons must be thread-safe

**Implementation:**
```csharp
services.AddSingleton<SecuritySettings>();
services.AddSingleton<GandiSettings>();
```

### 10. Exception Handling Strategy

**Decision:** Let exceptions propagate to NuciAPI middleware for standardised error responses.

**Rationale:**
- **Consistency:** Standardised error responses across the application
- **Simplicity:** No custom exception handling logic
- **Security:** Prevents information leakage
- **User Experience:** Consistent error format

**Trade-offs:**
- **Flexibility:** Less control over error handling
- **Debugging:** May hide underlying issues
- **Performance:** Middleware overhead for all exceptions

**Implementation:**
```csharp
// Service catches and logs, then re-throws
logger.Error(ex, "Failed to update DNS record");
throw; // Let middleware handle HTTP response
```

## Decision Matrix

| Decision | Alternative | Chosen | Impact |
|----------|-------------|--------|--------|
| Modular Monolith | Microservices | Monolith | High maintainability, easier deployment |
| NuciAPI Middleware | Custom Implementation | NuciAPI | High consistency, lower maintenance |
| Static DNS Client | Per-Request Client | Static | High performance, simple configuration |
| Per-Request HttpClient | Singleton HttpClient | Per-Request | High reliability, follows best practices |
| DnsProvider Enum | Dictionary<string, Func<DnsProvider>> | Enum | High type safety, simple extension |
| No Local Persistence | Local Cache | No Persistence | High reliability, simpler deployment |
| IPv4-Only Verification | Full DNS Verification | IPv4-Only | High simplicity, optimization focus |
| NuciLog | Standard Logging | NuciLog | High consistency, structured logs |
| Singleton Configuration | Scoped Configuration | Singleton | High performance, simple access |
| Propagate Exceptions | Catch and Handle | Propagate | High consistency, simpler code |

## Decision Evolution

### Current State
- All decisions are implemented as described
- No significant changes since initial design

### Future Considerations
1. **Add Retry Logic:** Consider adding Polly retry policies for transient failures
2. **Add Circuit Breaker:** Implement circuit breaker for provider failures
3. **Add Fallback:** Consider fallback mechanisms for provider unavailability
4. **Add Local Cache:** Consider caching for performance optimization
5. **Add IPv6 Support:** Consider extending DNS verification to IPv6
6. **Add Configuration Reload:** Consider hot configuration reloading
7. **Add Distributed Tracing:** Consider adding correlation IDs for distributed systems
8. **Add Problem Details:** Consider RFC 7807 Problem Details format for errors

### Decision Review Process

**When to Review Decisions:**
- New requirements emerge
- Performance issues arise
- Security vulnerabilities are discovered
- Technology stack changes
- Team knowledge gaps appear

**Review Criteria:**
- Does the decision still meet original requirements?
- Are there better alternatives?
- Has the impact changed?
- Are there new constraints?

**Review Process:**
1. Document the problem
2. Evaluate current solution
3. Identify alternatives
4. Analyze trade-offs
5. Make decision
6. Implement
7. Validate
8. Document

## Documentation References

- [Dependencies](dependencies.md) - External dependencies and integration boundaries
- [Error Handling](error-handling.md) - Exception handling patterns and error responses
- [Testing](testing.md) - Test strategy and testing patterns
- [Configuration](configuration.md) - Configuration management and binding
- [Logging](logging.md) - Logging strategy and implementation
- [Security](security.md) - Security architecture and controls

## Architecture Decision Records (ADRs)

This document serves as the primary Architecture Decision Record (ADR) for the Dynamic DNS Updater API. Future ADRs can be added as new decisions are made.

**ADR Format:**
- Title: Brief description of the decision
- Decision: What was decided
- Rationale: Why it was chosen
- Trade-offs: Pros and cons
- Implementation: How it was implemented
- Consequences: Impact and future considerations

**ADR Status:**
- **Accepted:** Decision has been implemented
- **Rejected:** Decision was made but later rejected
- **Deprecated:** Decision is no longer valid
- **Superseded:** Decision has been replaced by a newer one