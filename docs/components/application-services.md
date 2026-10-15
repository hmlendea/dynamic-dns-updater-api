# Application Services

## Overview

The application service layer contains the core business logic for the DNS update use case. It orchestrates the interaction between the controller, DNS verification, and provider integrations.

## Responsibilities

- Orchestrate the DNS record update workflow
- Apply the DNS resolution short-circuit optimisation
- Select the appropriate DNS provider based on the provider name
- Log operation status (started, success, failure) with structured context
- Propagate exceptions after logging

## Components

### IDnsRecordService

**File:** `DynamicDnsUpdater.API/Service/IDnsRecordService.cs`

**Purpose:** Interface defining the DNS record update contract.

**Key Characteristics:**
- Single method: `Update(string domainName, string ipAddress, string dnsProviderName)`
- Returns `Task` (async)
- No return value — success is indicated by absence of exception
- Colocated interface per project conventions

```csharp
public interface IDnsRecordService
{
    Task Update(
        string domainName,
        string ipAddress,
        string dnsProviderName);
}
```

### DnsRecordService

**File:** `DynamicDnsUpdater.API/Service/DnsRecordService.cs`

**Purpose:** Implementation of `IDnsRecordService` that orchestrates the DNS update workflow.

**Dependencies:**
- `IGandiService` — Provider integration for Gandi LiveDNS
- `ILogger` (NuciLog) — Structured operation logging

**Lifetime:** Transient (registered in `ServiceCollectionExtensions`)

## Execution Flow

### Update Method

```
Input: domainName, ipAddress, dnsProviderName
    ↓
1. Log operation started (with provider, domain, IP context)
    ↓
2. Check if IP is IPv4 and parseable
    ├── Yes → Query Quad9 for A records
    │   ├── If domain already resolves to requested IP → Log success, return (no-op)
    │   └── Otherwise → Continue to provider update
    └── No (IPv6, invalid, or unparsable) → Skip verification, continue
    ↓
3. Resolve provider name via DnsProvider.FromString()
    ↓
4. Select provider and call adapter
    ├── Gandi → gandiService.Update(domainName, ipAddress)
    └── Unknown → throw NotImplementedException
    ↓
5. Log operation success
    ↓
Exception path: Log operation failure, rethrow
```

### Detailed Steps

#### Step 1: Operation Logging
```csharp
IEnumerable<LogInfo> logInfos =
[
    new(MyLogInfoKey.Provider, dnsProviderName),
    new(MyLogInfoKey.DomainName, domainName),
    new(MyLogInfoKey.IpAddress, ipAddress)
];

logger.Info(
    MyOperation.UpdateDnsRecord,
    OperationStatus.Started,
    logInfos);
```

#### Step 2: DNS Resolution Short-Circuit
```csharp
static async Task<bool> IsAlreadyResolvedToRequestedIpAddress(
    string domainName,
    string ipAddress)
{
    // Only IPv4 addresses are checked
    if (!IPAddress.TryParse(ipAddress, out IPAddress requestedAddress) ||
        requestedAddress.AddressFamily != AddressFamily.InterNetwork)
    {
        return false;
    }

    try
    {
        IDnsQueryResponse response = await Quad9LookupClient.QueryAsync(
            domainName, QueryType.A);

        return response.Answers
            .ARecords()
            .Any(record => record.Address.Equals(requestedAddress));
    }
    catch (DnsResponseException)
    {
        return false;  // DNS failure → proceed to update
    }
    catch (SocketException)
    {
        return false;  // Network failure → proceed to update
    }
}
```

**Key Design Decisions:**
- Uses static `LookupClient` configured for Quad9 (9.9.9.9, 149.112.112.112)
- Caching disabled (`UseCache = false`) for fresh results
- Only IPv4 (A records) checked — IPv6 (AAAA) bypasses verification
- DNS failures are non-fatal — update proceeds regardless

#### Step 3: Provider Selection
```csharp
DnsProvider provider = DnsProvider.FromString(dnsProviderName);

if (provider == DnsProvider.Gandi)
{
    await gandiService.Update(domainName, ipAddress);
}
else
{
    throw new NotImplementedException(
        "The requested DNS provider is not supported.");
}
```

**Key Design Decisions:**
- `DnsProvider.FromString` returns `null` for unknown providers
- Null comparison with `DnsProvider.Gandi` throws `NullReferenceException` (not `NotImplementedException`)
- Only Gandi is currently supported

#### Step 4: Error Handling
```csharp
catch (Exception exception)
{
    logger.Error(
        MyOperation.UpdateDnsRecord,
        OperationStatus.Failure,
        exception,
        logInfos);

    throw;  // Re-throw to preserve stack trace
}
```

## Data Transformations

| Input | Transformation | Output |
|-------|---------------|--------|
| `domainName` (string) | Passed through to provider | `GandiService.Update(domainName, ipAddress)` |
| `ipAddress` (string) | Parsed for IPv4 check | `IPAddress` object or skip verification |
| `dnsProviderName` (string) | `DnsProvider.FromString()` | `DnsProvider` object or null |
| `DnsProvider` | Compared to `DnsProvider.Gandi` | Provider selection decision |

## State Management

- **No persistent state** — The service is stateless
- **Static state** — `Quad9LookupClient` is a static readonly field (shared across all requests)
- **Transient state** — `logInfos` is created per request
- **No caching** — DNS lookups use `UseCache = false`

## Boundary Rules

### Application Service Boundary
- ✅ **May:** Orchestrate workflows, apply business rules, log operations
- ✅ **May:** Depend on provider contracts (`IGandiService`)
- ✅ **May:** Use DNS client for verification
- ❌ **Must not:** Contain provider-specific HTTP calls or URL construction
- ❌ **Must not:** Directly access configuration (receives settings via DI)

### Provider Selection Boundary
- ✅ **May:** Select provider based on `DnsProvider` model
- ❌ **Must not:** Duplicate provider-specific logic (delegated to adapters)

## Logging Integration

The service uses NuciLog structured logging with:

**Custom Log Info Keys:**
- `MyLogInfoKey.Provider` — DNS provider name
- `MyLogInfoKey.DomainName` — Domain being updated
- `MyLogInfoKey.IpAddress` — Target IP address

**Custom Operations:**
- `MyOperation.UpdateDnsRecord` — Operation identifier

**Log Levels Used:**
- `Info` — Operation started, success, no-op (already resolved)
- `Error` — Operation failure with exception

## Testing

### Unit Tests
**File:** `DynamicDnsUpdater.API.UnitTests/Service/DnsRecordServiceTests.cs`

Tests cover:
- Gandi provider invocation (IPv6 bypasses verification)
- Exception propagation from Gandi service
- Unknown provider handling (throws exception)

### Integration Tests
**File:** `DynamicDnsUpdater.API.IntegrationTests/DnsRecordsApiTests.cs`

Tests cover:
- Request forwarding with varied domain/IP/provider values
- JSON body binding with various property name cases
- Route decoding (URL-encoded domain names)
- HMAC header handling

## Evolution Path

If the application service needs to evolve:
1. **Add new providers** — Extend `DnsProvider` model, add provider interface and implementation, update selection logic
2. **Add new verification methods** — Extend `IsAlreadyResolvedToRequestedIpAddress` or add new verification strategies
3. **Add new operations** — Add new `MyOperation` values and log info keys
4. **Add retry logic** — Wrap provider calls with retry policies
5. **Add metrics** — Emit metrics alongside log entries for observability