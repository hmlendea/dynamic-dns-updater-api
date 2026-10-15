# Repository Overview

## Purpose

The Dynamic DNS Updater API provides a RESTful endpoint to update DNS A records for a domain name to point to a specified IP address. It is designed for dynamic DNS scenarios where a client's public IP address changes periodically and the DNS record needs to be updated automatically.

## Scope

This repository contains:
- **DynamicDnsUpdater.API** — The main ASP.NET Core web API project
- **DynamicDnsUpdater.API.UnitTests** — Unit tests for domain logic and services
- **DynamicDnsUpdater.API.IntegrationTests** — Integration tests for API endpoints and middleware

## Entry Points

| Entry Point | Location | Description |
|-------------|----------|-------------|
| Application startup | `DynamicDnsUpdater.API/Program.cs` | Configures host and uses `Startup` class |
| Request pipeline | `DynamicDnsUpdater.API/Startup.cs` | Configures services and middleware pipeline |
| API endpoint | `DynamicDnsUpdater.API/Controllers/DnsRecordsController.cs` | `PUT /DnsRecords/{domainName}` |
| Service registration | `DynamicDnsUpdater.API/ServiceCollectionExtensions.cs` | DI container configuration |

## Principal Capabilities

1. **Update DNS A record** — Accepts domain name, IP address, and DNS provider; updates the record via the provider's API
2. **Pre-update verification** — Queries Quad9 DNS servers to check if the record already points to the requested IP; skips update if already correct
3. **Provider abstraction** — `IDnsRecordService` and `IGandiService` interfaces allow adding new DNS providers
4. **API key authentication** — Requests authenticated via `X-Api-Key` header validated by NuciAPI middleware
5. **Replay protection** — HMAC-based request signing prevents replay attacks
6. **Scanner protection** — Blocks requests from known malicious scanners
7. **Structured logging** — Uses NuciLog for operation-scoped logging with correlation IDs

## Supported DNS Providers

Currently only **Gandi.net** (LiveDNS v5 API) is implemented. The `DnsProvider` enumeration class is designed for extensibility.

## Technology Stack

- **Runtime**: .NET 10.0
- **Web Framework**: ASP.NET Core
- **DNS Client**: DnsClient (Quad9 lookup)
- **HTTP Client**: HttpClient (Gandi API calls)
- **Logging**: NuciLog / NuciLog.Core
- **API Middleware**: NuciAPI (authentication, replay protection, scanner protection, exception handling, request logging)
- **Testing**: NUnit 4.x, Moq, FluentAssertions, Microsoft.AspNetCore.Mvc.Testing
- **JSON**: System.Text.Json with NuciExtensions for serialization

## Runtime Topology

```
Client → HTTPS → ASP.NET Core (Kestrel)
  → Middleware Pipeline:
      ExceptionHandling → ScannerProtection → RequestLogging
      → HeaderValidation → ReplayProtection → Authorization
      → Controller (DnsRecordsController)
        → IDnsRecordService (DnsRecordService)
          → Quad9 DNS (DnsClient) — verification query
          → IGandiService (GandiService) — HTTP PUT to api.gandi.net
```

## Major State

The application is **stateless** — no persistent state is maintained between requests. Configuration is read at startup from `appsettings.json` and environment variables.

## Major Integrations

| Integration | Protocol | Purpose |
|-------------|----------|---------|
| Gandi LiveDNS API | HTTPS REST | Update DNS A records |
| Quad9 DNS | DNS over UDP/TCP | Verify current DNS resolution |

## System-Wide Invariants

1. **Idempotency** — Updating a record to the same IP address it already resolves to is a no-op (verified via Quad9 before calling provider)
2. **Authentication required** — All endpoints require valid API key
3. **Replay protection** — Each request must have a unique HMAC token (when enabled)
4. **Subdomain required** — Gandi integration requires a subdomain (e.g., `sub.domain.tld`, not `domain.tld`)
5. **IPv4 only for verification** — Quad9 verification only checks A records (IPv4); AAAA records are not verified