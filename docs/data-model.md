# Data Model

## Overview

This document describes the data models used throughout the application, including domain models, DTOs, configuration objects, and provider-specific models. It covers their structure, relationships, serialization, and validation rules.

## Model Categories

### Domain Models

Core business entities that represent the application's domain concepts.

### Data Transfer Objects (DTOs)

Transport-layer objects for HTTP request/response serialization.

### Configuration Objects

Strongly-typed settings bound from configuration sources.

### Provider Models

External API request/response models for DNS provider integrations.

## Domain Models

### DnsProvider

**File:** `DynamicDnsUpdater.API/Service/Models/DnsProvider.cs`

**Purpose:** Enumeration class representing supported DNS providers.

**Structure:**
```csharp
public sealed class DnsProvider : Enumeration
{
    public static readonly DnsProvider Gandi = new(1, nameof(Gandi));

    private DnsProvider(int id, string name) : base(id, name) { }

    public static DnsProvider FromString(string name)
    {
        return name?.ToLowerInvariant() switch
        {
            "gandi" => Gandi,
            _ => null
        };
    }
}
```

**Inheritance:** `Enumeration` (base class providing value-object semantics)

**Key Characteristics:**
- Immutable value object
- Implements equality by `Id` and `Name`
- Factory method `FromString` for case-insensitive parsing
- Returns `null` for unknown providers (not exception)
- Extensible for new providers

**Equality Implementation:**
```csharp
// From Enumeration base class
public override bool Equals(object obj) =>
    obj is Enumeration other && Id == other.Id;

public override int GetHashCode() => Id.GetHashCode();

public static bool operator ==(Enumeration left, Enumeration right) =>
    left?.Equals(right) ?? right is null;

public static bool operator !=(Enumeration left, Enumeration right) =>
    !(left == right);
```

**Usage:**
```csharp
// In DnsRecordService
DnsProvider provider = DnsProvider.FromString(dnsProviderName);
if (provider == DnsProvider.Gandi) { ... }
```

**Serialization:** Not directly serialized (used internally only)

### Enumeration (Base Class)

**Purpose:** Base class for enumeration classes providing value-object semantics.

**Key Features:**
- `Id` (int) — Unique identifier
- `Name` (string) — Display name
- Value-based equality
- Implicit conversion from derived types

## Data Transfer Objects (DTOs)

### PutDnsRecordRequest

**File:** `DynamicDnsUpdater.API/Requests/PutDnsRecordRequest.cs`

**Purpose:** Request body for DNS record update endpoint.

**Structure:**
```csharp
public class PutDnsRecordRequest : NuciApiRequest
{
    [JsonPropertyName("ip")]
    public string IpAddress { get; set; } = string.Empty;

    [JsonPropertyName("provider")]
    public string DnsProvider { get; set; } = string.Empty;
}
```

**Inheritance:** `NuciApiRequest` (NuciAPI base class for request validation)

**JSON Serialization:**
```json
{
  "ip": "192.0.2.1",
  "provider": "Gandi"
}
```

**Property Mapping:**
| C# Property | JSON Property | Type | Required |
|-------------|---------------|------|----------|
| `IpAddress` | `ip` | string | Yes |
| `DnsProvider` | `provider` | string | Yes |

**Validation:**
- No built-in validation attributes
- Validation occurs in service layer
- Empty strings accepted by model binder

**NuciApiRequest Base Class:**
Provides common request metadata and validation infrastructure from NuciAPI.

## Configuration Objects

### SecuritySettings

**File:** `DynamicDnsUpdater.API/Configuration/SecuritySettings.cs`

**Purpose:** API authentication configuration.

**Structure:**
```csharp
public class SecuritySettings
{
    public string ApiKey { get; set; } = string.Empty;
}
```

**Binding Source:** `SecuritySettings` configuration section

**Usage:**
- `DnsRecordsController` — Creates `NuciApiAuthorisation`
- Validated by NuciAPI middleware

### GandiSettings

**File:** `DynamicDnsUpdater.API/Configuration/GandiSettings.cs`

**Purpose:** Gandi LiveDNS API configuration.

**Structure:**
```csharp
public class GandiSettings
{
    public string ApiKey { get; set; } = string.Empty;
}
```

**Binding Source:** `GandiSettings` configuration section

**Usage:**
- `GandiService` — Bearer token for Gandi API requests

## Provider Models

### Gandi LiveDNS Request Payload

**Constructed in:** `GandiService.Update()`

**Structure:**
```csharp
var payload = new
{
    rrset_type = "A",
    rrset_ttl = 300,
    rrset_values = new[] { ipAddress }
};
```

**JSON Serialization:**
```json
{
  "rrset_type": "A",
  "rrset_ttl": 300,
  "rrset_values": ["192.0.2.1"]
}
```

**Field Descriptions:**
| Field | Type | Value | Description |
|-------|------|-------|-------------|
| `rrset_type` | string | `"A"` | Record type (always A for IPv4) |
| `rrset_ttl` | integer | `300` | Time-to-live in seconds (5 minutes) |
| `rrset_values` | string[] | `[ip]` | Array of IP addresses |

**Constraints:**
- Only A records supported (no AAAA, CNAME, TXT)
- TTL fixed at 300 seconds
- Single value in array (no round-robin)

### Gandi LiveDNS API Endpoint

**URL Pattern:** `https://api.gandi.net/v5/livedns/domains/{domain}/records/{subdomain}/A`

**Path Parameters:**
| Parameter | Source | Example |
|-----------|--------|---------|
| `domain` | Domain part after first dot | `example.com` |
| `subdomain` | Part before first dot | `www` |

**HTTP Method:** `PUT`

**Headers:**
| Header | Value |
|--------|-------|
| `Authorization` | `Bearer {GandiSettings.ApiKey}` |
| `Accept` | `application/json` |
| `Content-Type` | `application/json` |

## Data Flow Transformations

### Request to Domain

```
HTTP Request
    │
    ├── Route: domainName (string)
    ├── Body: { "ip": "192.0.2.1", "provider": "Gandi" }
    │
    ▼
Controller Binding
    │
    ├── domainName → string
    ├── request.IpAddress → "192.0.2.1"
    └── request.DnsProvider → "Gandi"
    │
    ▼
Service.Update(domainName, "192.0.2.1", "Gandi")
    │
    ├── DnsProvider.FromString("Gandi") → DnsProvider.Gandi
    │
    ▼
GandiService.Update(domainName, "192.0.2.1")
    │
    ├── Split domainName at first '.'
    │   "www.example.com" → subdomain="www", domain="example.com"
    │
    ├── Construct Gandi payload
    │   { rrset_type: "A", rrset_ttl: 300, rrset_values: ["192.0.2.1"] }
    │
    ▼
HTTP PUT to Gandi API
```

### IP Address Handling

| Input IP | Parsed | Family | Verified |
|----------|--------|--------|----------|
| `192.0.2.1` (IPv4) | Yes | InterNetwork | Yes |
| `2001:db8::1` (IPv6) | Yes | InterNetworkV6 | No |
| `256.0.0.1` | No | — | No |
| `not-an-ip` | No | — | No |

## Validation Rules

### DnsProvider

| Input | Result |
|-------|--------|
| `"Gandi"` | `DnsProvider.Gandi` |
| `"gandi"` | `DnsProvider.Gandi` |
| `"GANDI"` | `DnsProvider.Gandi` |
| `"Cloudflare"` | `null` |
| `""` | `null` |
| `null` | `null` |

### Domain Name (GandiService)

| Input | Valid | Subdomain | Domain |
|-------|-------|-----------|--------|
| `www.example.com` | Yes | `www` | `example.com` |
| `api.v2.example.com` | Yes | `api` | `v2.example.com` |
| `example.com` | No | — | — |
| `localhost` | No | — | — |
| `sub` | No | — | — |

### IP Address (Service)

| Input | Parsed | Family | Verified |
|-------|--------|--------|----------|
| `192.0.2.1` | Yes | InterNetwork | Yes |
| `2001:db8::1` | Yes | InterNetworkV6 | No |
| `256.0.0.1` | No | — | No |
| `not-an-ip` | No | — | No |

## Serialization

### JSON Serialization Settings

The application uses `NuciExtensions.Json` (NuciExtensions package) which configures:

- CamelCase property naming
- Ignore null values (default)
- ISO 8601 date formatting
- Enum as string (for custom enums)

### Custom Converters

No custom JSON converters are used in this application. All models use standard serialization.

## Model Relationships

```
PutDnsRecordRequest (DTO)
    │
    ├── IpAddress (string) ──────────────────┐
    ├── DnsProvider (string) ────────────────┤
    │                                        ▼
    ▼                              DnsRecordService.Update()
DnsProvider.FromString() ──────────────► DnsProvider (domain model)
    │                                        │
    │                                        ├── GandiService.Update()
    │                                        │       │
    │                                        │       ├── domainName (string)
    │                                        │       ├── ipAddress (string)
    │                                        │       │
    │                                        │       ▼
    │                                        │   Split at first '.'
    │                                        │       │
    │                                        │       ├── subdomain (string)
    │                                        │       └── domain (string)
    │                                        │       │
    │                                        │       ▼
    │                                        │   Gandi Payload (anonymous)
    │                                        │       │
    │                                        │       ├── rrset_type: "A"
    │                                        │       ├── rrset_ttl: 300
    │                                        │       └── rrset_values: [ipAddress]
    │                                        │       │
    │                                        │       ▼
    │                                        │   HTTP PUT to Gandi API
    │                                        │
    ▼                                        ▼
SecuritySettings (config)            GandiSettings (config)
    │                                        │
    ├── ApiKey ────────────────────────────┤
    │                                        ├── ApiKey ──────────────────┐
    ▼                                        ▼                          ▼
NuciApiAuthorisation              GandiService                    Gandi API
```

## Extensibility Points

### Adding New DNS Providers

1. Add new value to `DnsProvider`:
```csharp
public static readonly DnsProvider Cloudflare = new(2, nameof(Cloudflare));
```

2. Extend `FromString`:
```csharp
"cloudflare" => Cloudflare,
```

3. Create `ICloudflareService` and `CloudflareService`

4. Update `DnsRecordService` selection logic:
```csharp
if (provider == DnsProvider.Gandi) { ... }
else if (provider == DnsProvider.Cloudflare) { ... }
```

### Adding New Record Types

1. Extend request DTO:
```csharp
[JsonPropertyName("type")]
public string RecordType { get; set; } = "A";
```

2. Update provider payload construction

3. Update Gandi API endpoint (supports AAAA, CNAME, TXT, etc.)

### Adding IPv6 Verification

1. Add AAAA query to `IsAlreadyResolvedToRequestedIpAddress`
2. Use `QueryType.AAAA` with DnsClient
3. Compare against `AddressFamily.InterNetworkV6`

## Testing Data Models

### Unit Test Patterns

**DnsProvider Tests:**
```csharp
[Test]
public void FromString_Gandi_ReturnsGandi()
{
    DnsProvider.FromString("Gandi").Should().Be(DnsProvider.Gandi);
    DnsProvider.FromString("gandi").Should().Be(DnsProvider.Gandi);
}

[Test]
public void FromString_Unknown_ReturnsNull()
{
    DnsProvider.FromString("Cloudflare").Should().BeNull();
}
```

**Request DTO Tests:**
```csharp
[Test]
public void Deserialization_MapsJsonProperties()
{
    string json = """{"ip": "1.2.3.4", "provider": "Gandi"}""";
    var request = JsonSerializer.Deserialize<PutDnsRecordRequest>(json);

    request.IpAddress.Should().Be("1.2.3.4");
    request.DnsProvider.Should().Be("Gandi");
}
```

**Configuration Tests:**
```csharp
[Test]
public void SecuritySettings_BindsFromConfiguration()
{
    var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new[] { new KeyValuePair<string, string>("SecuritySettings:ApiKey", "test") })
        .Build();

    var settings = new SecuritySettings();
    config.Bind(nameof(SecuritySettings), settings);

    settings.ApiKey.Should().Be("test");
}
```

## Evolution Guidelines

### Breaking Changes

| Change | Impact | Migration |
|--------|--------|-----------|
| Remove `DnsProvider` value | Breaks existing clients | Deprecate first, then remove |
| Change JSON property names | Breaks clients | Use `JsonPropertyName` for alias |
| Change configuration structure | Breaks deployments | Support both old and new keys |
| Change Gandi payload | Breaks provider integration | Version provider API calls |

### Non-Breaking Changes

| Change | Impact |
|--------|--------|
| Add new `DnsProvider` value | Safe (clients ignore unknown) |
| Add optional DTO properties | Safe (ignored if missing) |
| Add new configuration properties | Safe (defaults used) |
| Add new provider implementation | Safe (opt-in via provider name) |