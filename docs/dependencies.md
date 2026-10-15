# Dependencies

## Overview

This document describes the external dependencies of the Dynamic DNS Updater API, including their versions, integration boundaries, and architectural consequences.

## .NET Runtime

| Component | Version | Notes |
|-----------|---------|-------|
| .NET Runtime | 10.0 | Target framework: `net10.0` |
| ASP.NET Core | Included | Part of .NET 10.0 SDK |
| System.Text.Json | Included | Used for JSON serialization |

## NuciAPI Packages

| Package | Version | Purpose |
|---------|---------|---------|
| NuciAPI | Latest | Exception handling, scanner protection, request logging, replay protection, authorization middleware |
| NuciAPI.Authorization | Latest | API key validation and authorization |
| NuciAPI.ReplayProtection | Latest | HMAC-based replay attack prevention |
| NuciAPI.ScannerProtection | Latest | Scanner detection and IP blocking |
| NuciAPI.RequestLogging | Latest | Request/response logging middleware |
| NuciAPI.HeaderValidation | Latest | HTTP header validation |
| NuciAPI.HttpsRedirection | Latest | HTTP to HTTPS redirection |

## NuciLog Packages

| Package | Version | Purpose |
|---------|---------|---------|
| NuciLog | Latest | Structured logging framework |
| NuciLog.Core | Latest | Core logging functionality |
| NuciLog.Operations | Latest | Operation-based logging with MyOperation, MyLogInfoKey |

## DNS Libraries

| Library | Version | Purpose |
|---------|---------|---------|
| DnsClient | Latest | DNS query client with LookupClient (static, caching disabled) |
| DnsClient.LookupClient | Latest | Static DNS query client |

## External Services

| Service | API | Authentication | Notes |
|---------|-----|----------------|-------|
| Gandi LiveDNS v5 | HTTPS REST | Bearer token (API key) | PUT to `/v5/livedns/domains/{domain}/records/{subdomain}/A` |
| Quad9 DNS Resolvers | DNS query | None (public) | 9.9.9.9, 149.112.112.112; A-record lookup optimization |

## .NET Standard Libraries

| Library | Purpose | Integration |
|---------|---------|-------------|
| System.Text.Json | JSON serialization | Used with NuciExtensions (camelCase, ignore nulls) |
| System.Net.Http.HttpClient | HTTP requests | Created per-request in GandiService |
| System.Net.Dns | DNS resolution | Platform DNS fallback |

## NuciExtensions

| Feature | Purpose |
|---------|---------|
| CamelCase JSON serialization | Property naming convention |
| Ignore null values | During serialization |
| Custom converters | As needed |

## Integration Boundaries

### GandiService Integration

```
┌─────────────────────────────────────────────────────────────┐
│ DynamicDnsUpdater.API (ASP.NET Core)                       │
│                                                             │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐ │
│  │ GandiService    │  │ IDnsRecordService│ │ DnsRecordService │ │
│  │ (Provider)      │  │ (Interface)     │ │ (Orchestrator) │ │
│  └─────────────────┘  └─────────────────┘  └─────────────┘ │
│           │                     │                     │       │
│           │                     │                     │       │
│           │                     │                     │       │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐ │
│  │ IGandiService   │  │ IDnsProvider    │ │ DnsProvider   │ │
│  │ (Interface)     │  │ (Enumeration)   │ │ (Factory)     │ │
│  └─────────────────┘  └─────────────────┘  └─────────────┘ │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐ │
│  │ External: api.gandi.net/v5/livedns/                  │ │
│  └─────────────────────────────────────────────────────┘ │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### DNS Verification Integration

```
┌─────────────────────────────────────────────────────────────┐
│ DynamicDnsUpdater.API (ASP.NET Core)                       │
│                                                             │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐ │
│  │ Quad9LookupClient│ │ DnsClient        │ │ DNS Query    │ │
│  │ (static)         │  │ (LookupClient)   │ │ (Quad9 resolvers)│ │
│  └─────────────────┘  └─────────────────┘  └─────────────┘ │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐ │
│  │ External: 9.9.9.9, 149.112.112.112                  │ │
│  └─────────────────────────────────────────────────────┘ │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Configuration Integration

```
┌─────────────────────────────────────────────────────────────┐
│ DynamicDnsUpdater.API (ASP.NET Core)                       │
│                                                             │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐ │
│  │ appsettings.json  │  │ SecuritySettings │ │ GandiSettings │ │
│  │ (Configuration)   │  │ (ApiKey)         │ │ (ApiKey)      │ │
│  └─────────────────┘  └─────────────────┘  └─────────────┘ │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐ │
│  │ Binds to IServiceCollection as singletons             │ │
│  └─────────────────────────────────────────────────────┘ │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## Architectural Consequences

### Per-Request HttpClient

**Decision:** HttpClient is created per-request in GandiService.

**Rationale:**
- Avoids SocketException with DNS resolution
- Follows Microsoft's recommended pattern for ASP.NET Core
- Simplifies connection management

**Implementation:**
```csharp
using var client = new HttpClient();
using var request = new HttpRequestMessage(...);
using var response = await client.SendAsync(request);
```

### Static DNS Client

**Decision:** DnsClient LookupClient is static with caching disabled.

**Rationale:**
- Single instance shared across the application
- Caching disabled to ensure fresh lookups
- Minimal overhead for occasional DNS queries

**Implementation:**
```csharp
private static readonly LookupClient Quad9LookupClient = new LookupClient(
    new IPEndPoint[] { IPAddress.Parse("9.9.9.9"), IPAddress.Parse("149.112.112.112") });
```

### Provider Abstraction

**Decision:** DnsProvider enumeration with Gandi integration.

**Rationale:**
- Enables extensibility for additional providers
- Factory pattern (FromString) for case-insensitive parsing
- Single point of provider logic

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

### No Local Persistence

**Decision:** No local caching of DNS records or provider state.

**Rationale:**
- Stateless design enables horizontal scaling
- Provider API is the source of truth
- Simplifies deployment and maintenance

### IPv4-Only Verification

**Decision:** DNS verification only for IPv4 A records.

**Rationale:**
- Current use case requires IPv4 only
- Simplifies verification logic
- DNS verification is optimization, not correctness requirement

## Version Requirements

| Dependency | Minimum Version | Recommended |
|-----------|----------------|-------------|
| .NET SDK | 10.0 | Latest |
| NuciAPI | 1.x | Latest |
| NuciLog | 1.x | Latest |
| DnsClient | 3.x | Latest |
| NUnit | 4.x | Latest |
| Moq | 4.x | Latest |
| FluentAssertions | 6.x | Latest |

## Build Dependencies

### project.assets.json

```json
{
  "assets": {
    "projectName": "DynamicDnsUpdater.API",
    "here": "bin/Debug/net10.0/",
    "assets": {
      "referencePaths": [
        "obj/Debug/net10.0/DynamicDnsUpdater.API.csproj.FileList.txt"
      ]
    }
  }
}
```

### NuGet Packages (from .csproj)

```xml
<ItemGroup>
  <PackageReference Include="NuciAPI">
    <Version>1.0.0</Version>
  </PackageReference>
  <PackageReference Include="NuciLog">
    <Version>1.0.0</Version>
  </PackageReference>
  <PackageReference Include="NuciExtensions">
    <Version>1.0.0</Version>
  </PackageReference>
  <PackageReference Include="DnsClient">
    <Version>3.0.0</Version>
  </PackageReference>
  <PackageReference Include="NUnit">
    <Version>4.0.0</Version>
  </PackageReference>
  <PackageReference Include="Moq">
    <Version>4.0.0</Version>
  </PackageReference>
  <PackageReference Include="FluentAssertions">
    <Version>6.0.0</Version>
  </PackageReference>
</ItemGroup>
```