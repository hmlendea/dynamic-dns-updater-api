# Architecture (Documentation Complement)

This document complements the root [ARCHITECTURE.md](../ARCHITECTURE.md) by providing additional implementation detail, rationale, and cross-references to the generated documentation under `docs/`.

## Overview

The Dynamic DNS Updater API follows a **modular monolith** architecture hosted as a single ASP.NET Core process. The architecture is organised around clear boundaries:

1. **HTTP Transport & Middleware** — NuciAPI middleware pipeline handles cross-cutting concerns (auth, replay protection, scanner protection, logging, exception handling)
2. **Controller Layer** — Thin transport binding layer that forwards to application services
3. **Application Service Layer** — Orchestrates the DNS update use case, including verification and provider selection
4. **Provider Integration Layer** — Adapter pattern for DNS provider APIs (currently Gandi LiveDNS v5)
5. **External Integrations** — Quad9 DNS for verification, Gandi LiveDNS for mutations

## Architectural Style Rationale

The modular monolith was chosen because:
- Single deployment unit simplifies operations
- Clear internal boundaries enable future extraction if needed
- Provider abstraction (`IGandiService` / `IDnsRecordService`) allows adding providers without changing controllers
- NuciAPI middleware provides battle-tested cross-cutting concerns

## Component Relationships

```
┌─────────────────────────────────────────────────────────────────┐
│                        ASP.NET Core Host                         │
├─────────────────────────────────────────────────────────────────┤
│  NuciAPI Middleware Pipeline                                     │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌───────────┐  │
│  │ Exception   │ │ Scanner     │ │ Request     │ │ Header    │  │
│  │ Handling    │ │ Protection  │ │ Logging     │ │ Validation│  │
│  └─────────────┘ └─────────────┘ └─────────────┘ └───────────┘  │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐                │
│  │ Replay      │ │ Auth        │ │ Routing     │                │
│  │ Protection  │ │ (API Key)   │ │ → Controller│                │
│  └─────────────┘ └─────────────┘ └─────────────┘                │
├─────────────────────────────────────────────────────────────────┤
│  DnsRecordsController                                            │
│  └─→ IDnsRecordService (DnsRecordService)                       │
│       ├─→ Quad9 DNS (LookupClient) — verification               │
│       └─→ IGandiService (GandiService) — mutation               │
│            └─→ Gandi LiveDNS API (HTTPS)                        │
└─────────────────────────────────────────────────────────────────┘
```

## Key Architectural Decisions

| Decision | Rationale | Trade-off |
|----------|-----------|-----------|
| **NuciAPI middleware for auth/replay/scanner** | Reuses battle-tested security middleware; consistent with other Nuci projects | Coupled to NuciAPI package contracts and versioning |
| **Static Quad9 LookupClient** | Avoids per-request allocation; caching disabled for fresh results | Shared state; not configurable per-request |
| **Per-request HttpClient in GandiService** | Simple, avoids connection pooling complexity | No connection reuse; potential socket exhaustion under high load |
| **DnsProvider as enumeration class** | Type-safe provider selection; extensible without enum limitations | Requires code change to add providers (not config-driven) |
| **No local persistence** | Stateless design; DNS is the source of truth | No audit trail or history of updates |
| **IPv4-only verification** | A records are the primary use case; Quad9 A query is simple | AAAA (IPv6) updates skip verification optimisation |

## Dependency Direction

```
Composition Root (Startup, ServiceCollectionExtensions)
    ↓
Controllers (DnsRecordsController)
    ↓
Application Services (IDnsRecordService → DnsRecordService)
    ↓
Provider Contracts (IGandiService)
    ↓
Provider Implementations (GandiService)
    ↓
External Systems (Quad9, Gandi LiveDNS)
```

**Rules:**
- Composition root may reference concrete types for registration
- Controllers depend only on `IDnsRecordService` and settings
- Application services depend on provider contracts and logging
- Provider adapters own provider-specific HTTP/URL/payload logic
- External integrations (Quad9, Gandi) only called from adapters/services

## Cross-References

| Topic | Document |
|-------|----------|
| Root architecture (authoritative) | [../ARCHITECTURE.md](../ARCHITECTURE.md) |
| Component deep dives | [components/](./components/) |
| Execution flows | [flows/](./flows/) |
| API reference | [api-reference/](./api-reference/) |
| Data model | [data-model.md](./data-model.md) |
| Dependencies | [dependencies.md](./dependencies.md) |
| Configuration | [configuration.md](./configuration.md) |
| Security | [security.md](./security.md) |
| Error handling | [error-handling.md](./error-handling.md) |
| Logging | [logging.md](./logging.md) |
| Testing | [testing.md](./testing.md) |
| Deployment | [build-and-deployment.md](./build-and-deployment.md) |