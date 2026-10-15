# Repository Structure

## Source Tree Layout

```
dynamic-dns-updater-api/
├── ARCHITECTURE.md
├── CODE_OF_CONDUCT.md
├── CONTRIBUTING.md
├── DynamicDnsUpdater.API.slnx
├── LICENSE
├── PRIVACY.md
├── README.md
├── release.sh
├── SECURITY.md
├── docs/                          # Generated documentation
│   ├── INDEX.md
│   ├── api-reference/
│   ├── behaviour/
│   ├── components/
│   ├── flows/
│   ├── ...
├── DynamicDnsUpdater.API/         # Main API project
│   ├── appsettings.json
│   ├── DynamicDnsUpdater.API.csproj
│   ├── Program.cs
│   ├── ServiceCollectionExtensions.cs
│   ├── Startup.cs
│   ├── Configuration/
│   │   ├── GandiSettings.cs
│   │   └── SecuritySettings.cs
│   ├── Controllers/
│   │   └── DnsRecordsController.cs
│   ├── Logging/
│   │   ├── MyLogInfoKey.cs
│   │   └── MyOperation.cs
│   ├── Requests/
│   │   └── PutDnsRecordRequest.cs
│   ├── Service/
│   │   ├── DnsRecordService.cs
│   │   ├── IDnsRecordService.cs
│   │   ├── Integrations/
│   │   │   └── Gandi/
│   │   │       ├── GandiService.cs
│   │   │       └── IGandiService.cs
│   │   └── Models/
│   │       └── DnsProvider.cs
├── DynamicDnsUpdater.API.IntegrationTests/  # Integration tests
│   ├── ApiIntegrationTestBase.cs
│   ├── ApiRequestFactory.cs
│   ├── ApiResponseAssertions.cs
│   ├── ApiTestHost.cs
│   ├── DnsRecordsApiTests.cs
│   ├── ExceptionHandlingApiTests.cs
│   ├── ReplayProtectionApiTests.cs
│   ├── RequestBodyApiTests.cs
│   ├── RequestSecurityApiTests.cs
│   ├── ScannerProtectionApiTests.cs
│   └── DynamicDnsUpdater.API.IntegrationTests.csproj
└── DynamicDnsUpdater.API.UnitTests/         # Unit tests
    ├── DynamicDnsUpdater.API.UnitTests.csproj
    ├── Service/
    │   ├── DnsRecordServiceTests.cs
    │   ├── Integrations/
    │   │   └── Gandi/
    │   │       └── GandiServiceTests.cs
    │   └── Models/
    │       └── DnsProviderTests.cs
```

## Module Organisation

### DynamicDnsUpdater.API (Main Project)

| Namespace | Folder | Responsibility |
|-----------|--------|----------------|
| `DynamicDnsUpdater.API` | Root | Application entry point, host configuration |
| `DynamicDnsUpdater.API.Configuration` | `Configuration/` | Settings classes bound from `appsettings.json` |
| `DynamicDnsUpdater.API.Controllers` | `Controllers/` | API endpoints (controllers) |
| `DynamicDnsUpdater.API.Logging` | `Logging/` | Custom log info keys and operations for NuciLog |
| `DynamicDnsUpdater.API.Requests` | `Requests/` | Request DTOs for API endpoints |
| `DynamicDnsUpdater.API.Service` | `Service/` | Application services (business logic) |
| `DynamicDnsUpdater.API.Service.Integrations.Gandi` | `Service/Integrations/Gandi/` | Gandi LiveDNS API integration |
| `DynamicDnsUpdater.API.Service.Models` | `Service/Models/` | Domain models (enumeration classes) |

### DynamicDnsUpdater.API.UnitTests

| Namespace | Folder | Responsibility |
|-----------|--------|----------------|
| `DynamicDnsUpdater.API.UnitTests.Service` | `Service/` | Unit tests for `DnsRecordService` |
| `DynamicDnsUpdater.API.UnitTests.Service.Integrations.Gandi` | `Service/Integrations/Gandi/` | Unit tests for `GandiService` |
| `DynamicDnsUpdater.API.UnitTests.Service.Models` | `Service/Models/` | Unit tests for `DnsProvider` |

### DynamicDnsUpdater.API.IntegrationTests

| Namespace | Folder | Responsibility |
|-----------|--------|----------------|
| `DynamicDnsUpdater.API.IntegrationTests` | Root | Integration tests for API endpoints, middleware, and security |

## Project Files

| File | Purpose |
|------|---------|
| `DynamicDnsUpdater.API.slnx` | SDK-style solution file referencing all three projects |
| `DynamicDnsUpdater.API/DynamicDnsUpdater.API.csproj` | Main API project — targets net10.0, references NuciAPI, NuciLog, DnsClient |
| `DynamicDnsUpdater.API.UnitTests/DynamicDnsUpdater.API.UnitTests.csproj` | Unit test project — NUnit, Moq, references main project |
| `DynamicDnsUpdater.API.IntegrationTests/DynamicDnsUpdater.API.IntegrationTests.csproj` | Integration test project — NUnit, Moq, FluentAssertions, Microsoft.AspNetCore.Mvc.Testing |

## Configuration Files

| File | Purpose |
|------|---------|
| `appsettings.json` | Base configuration (API keys, logging settings) — uses placeholders for secrets |
| `release.sh` | Release script that downloads and executes a shared deployment script |

## Naming Conventions

- **Projects**: PascalCase with `.API`, `.UnitTests`, `.IntegrationTests` suffixes
- **Namespaces**: Match folder structure exactly (block namespaces)
- **Classes**: PascalCase, one per file, file name matches class name
- **Interfaces**: `I` prefix (e.g., `IDnsRecordService`, `IGandiService`)
- **Enumeration classes**: Plain noun, static factory properties (e.g., `DnsProvider.Gandi`)
- **Settings classes**: Noun + `Settings` suffix (e.g., `GandiSettings`, `SecuritySettings`)
- **Request DTOs**: Verb + Noun + `Request` (e.g., `PutDnsRecordRequest`)
- **Controllers**: Plural noun + `Controller` (e.g., `DnsRecordsController`)
- **Test classes**: Subject + `Tests` (e.g., `DnsRecordServiceTests`)
- **Test methods**: `Given[Context]_When[Action]_Then[Outcome]` pattern