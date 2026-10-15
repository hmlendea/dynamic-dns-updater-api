# Startup and Rendering Flow

## Overview

This document describes the application startup sequence and the middleware pipeline that processes every HTTP request. The flow covers host creation, service registration, middleware ordering, and request processing through the pipeline.

## Startup Sequence

### 1. Host Creation (Program.cs)

```
Main()
    ↓
Host.CreateDefaultBuilder(args)
    ├── Loads configuration (appsettings.json, env vars, etc.)
    ├── Configures logging (default providers)
    ├── Sets up dependency injection container
    └── Configures Kestrel web server
    ↓
ConfigureWebHostDefaults
    ├── UseStartup<Startup>()
    └── Builds IWebHost
    ↓
Build()
    ↓
Run() / RunAsync()
```

### 2. Startup Configuration (Startup.cs)

#### ConfigureServices (DI Registration)

```
services.AddControllers()
    ↓
services.AddConfigurations(Configuration)
    ├── new SecuritySettings()
    ├── configuration.Bind("SecuritySettings", securitySettings)
    ├── services.AddSingleton(securitySettings)
    ├── new GandiSettings()
    ├── configuration.Bind("GandiSettings", gandiSettings)
    ├── services.AddSingleton(gandiSettings)
    └── services.AddNuciLoggerSettings(configuration)
    ↓
services.AddCustomServices()
    ├── services.AddTransient<ILogger, NuciLogger>()
    ├── services.AddTransient<IDnsRecordService, DnsRecordService>()
    └── services.AddTransient<IGandiService, GandiService>()
    ↓
services.AddNuciApiReplayProtection()
    └── Registers replay protection services
    ↓
services.AddNuciApiScannerProtection()
    └── Registers scanner protection services
```

#### Configure (Middleware Pipeline)

```
app.UseNuciApiExceptionHandling()
    ↓
app.UseNuciApiScannerProtection()
    ↓
app.UseNuciApiRequestLogging()
    ↓
if (env.IsDevelopment())
    app.UseDeveloperExceptionPage()
    ↓
app.UseHttpsRedirection()
    ↓
app.UseDefaultFiles()
    ↓
app.UseStaticFiles()
    ↓
app.UseRouting()
    ↓
app.UseNuciApiHeaderValidation()
    ↓
app.UseNuciApiReplayProtection()
    ↓
app.UseAuthorization()
    ↓
app.UseEndpoints(endpoints =>
    endpoints.MapControllers())
```

## Middleware Pipeline Detail

### Request Processing Flow

```
HTTP Request Received
    │
    ▼
┌─────────────────────────────────────────────────────────────┐
│ UseNuciApiExceptionHandling                                 │
│   • Catches ALL exceptions from downstream middleware       │
│   • Maps to standardised error responses                    │
│   • Logs unhandled exceptions                               │
└─────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────┐
│ UseNuciApiScannerProtection                                 │
│   • Checks User-Agent against known scanner patterns        │
│   • Checks IP against known malicious IP lists              │
│   • Returns 403 Forbidden if matched                        │
│   • Short-circuits pipeline (no further processing)         │
└─────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────┐
│ UseNuciApiRequestLogging                                    │
│   • Logs request method, path, headers, query string        │
│   • Logs response status code, duration                     │
│   • Uses NuciLog structured logging                         │
└─────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────┐
│ UseDeveloperExceptionPage (Development only)                │
│   • Detailed exception pages for local debugging            │
└─────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────┐
│ UseHttpsRedirection                                         │
│   • Redirects HTTP requests to HTTPS (307)                  │
│   • Uses HSTS in production                                 │
└─────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────┐
│ UseDefaultFiles / UseStaticFiles                            │
│   • Serves index.html for directory requests                │
│   • Serves static files from wwwroot                        │
└─────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────┐
│ UseRouting                                                  │
│   • Matches request to endpoint (controller action)         │
│   • Populates HttpContext.GetEndpoint()                     │
└─────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────┐
│ UseNuciApiHeaderValidation                                  │
│   • Validates required NuciAPI protocol headers             │
│   • Checks X-Nuci-Api-Version, X-Nuci-Request-Id, etc.      │
│   • Returns 400 Bad Request if invalid                      │
└─────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────┐
│ UseNuciApiReplayProtection                                  │
│   • Validates HMAC signature in X-Nuci-Signature header     │
│   • Checks timestamp freshness (replay window)              │
│   • Verifies nonce uniqueness (prevents replay)             │
│   • Returns 401 Unauthorized if invalid                     │
└─────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────┐
│ UseAuthorization                                            │
│   • Validates API key from Authorization header             │
│   • Uses NuciApiAuthorisation configured in controller      │
│   • Returns 401 Unauthorized if invalid/missing             │
└─────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────┐
│ UseEndpoints (Controller Execution)                         │
│   • Invokes matched controller action                       │
│   • Model binding, validation, action execution             │
│   • Returns ActionResult                                    │
└─────────────────────────────────────────────────────────────┘
    │
    ▼
HTTP Response Sent
```

## Service Resolution During Request

### Per-Request Service Resolution

```
Request starts
    │
    ▼
Controller instantiated (per request)
    ├── IDnsRecordService → DnsRecordService (transient)
    │   ├── IGandiService → GandiService (transient)
    │   │   └── GandiSettings (singleton, captured at registration)
    │   └── ILogger → NuciLogger (transient)
    └── SecuritySettings (singleton, captured at registration)
    │
    ▼
Action executes
    │
    ▼
Services disposed (if IDisposable)
```

### Singleton Services (Application Lifetime)

| Service | Created | Disposed |
|---------|---------|----------|
| `SecuritySettings` | Startup | App shutdown |
| `GandiSettings` | Startup | App shutdown |
| `NuciLoggerSettings` | Startup | App shutdown |
| `Quad9LookupClient` (static) | First use | App domain unload |

### Transient Services (Per Request)

| Service | Created | Disposed |
|---------|---------|----------|
| `DnsRecordService` | Controller ctor | End of request |
| `GandiService` | DnsRecordService ctor | End of request |
| `NuciLogger` | Controller/Service ctor | End of request |
| `HttpClient` (in GandiService) | GandiService.Update() | End of Update() |

## Configuration Loading

### Configuration Sources (Priority Order)

1. **appsettings.json** — Base configuration
2. **appsettings.{Environment}.json** — Environment-specific overrides
3. **Environment variables** — Highest priority (e.g., `SecuritySettings__ApiKey`)
4. **Command line arguments** — Highest priority
5. **User secrets** (Development) — Local development overrides

### Settings Binding

```csharp
// In ServiceCollectionExtensions.AddConfigurations
SecuritySettings securitySettings = new();
configuration.Bind(nameof(SecuritySettings), securitySettings);
// Binds: SecuritySettings:ApiKey → securitySettings.ApiKey

GandiSettings gandiSettings = new();
configuration.Bind(nameof(GandiSettings), gandiSettings);
// Binds: GandiSettings:ApiKey → gandiSettings.ApiKey
```

### Example Configuration

```json
{
  "SecuritySettings": {
    "ApiKey": "your-api-key-here"
  },
  "GandiSettings": {
    "ApiKey": "your-gandi-api-key-here"
  },
  "NuciLoggerSettings": {
    "MinimumLevel": "Information",
    "WriteTo": ["Console", "File"]
  }
}
```

## Rendering (Response Generation)

### Controller Response Flow

```
Controller Action
    │
    ▼
ProcessRequestAsync(request, serviceCall, authorisation)
    │
    ├── Authorisation check (NuciApiAuthorisation)
    │   ├── Valid → Continue
    │   └── Invalid → 401 Unauthorized
    │
    ├── Model validation (if applicable)
    │   ├── Valid → Continue
    │   └── Invalid → 400 Bad Request
    │
    ├── Service call execution
    │   ├── Success → 200 OK (empty body)
    │   └── Exception → Caught by UseNuciApiExceptionHandling
    │
    ▼
ActionResult returned
    │
    ▼
Middleware pipeline (reverse order)
    │
    ▼
Response sent to client
```

### Response Types

| Scenario | Status Code | Body |
|----------|-------------|------|
| Success | 200 OK | Empty |
| Invalid API key | 401 Unauthorized | NuciAPI error format |
| Invalid request body | 400 Bad Request | NuciAPI error format |
| Missing required headers | 400 Bad Request | NuciAPI error format |
| Replay attack detected | 401 Unauthorized | NuciAPI error format |
| Scanner detected | 403 Forbidden | NuciAPI error format |
| Service exception | 500 Internal Server Error | NuciAPI error format |
| Provider HTTP error | 502 Bad Gateway | NuciAPI error format |

## Shutdown Sequence

```
SIGTERM / Ctrl+C received
    ↓
Host.StopAsync()
    ├── Stops accepting new requests
    ├── Waits for in-flight requests (configurable timeout)
    ├── Disposes scoped services
    ├── Disposes singleton services (IDisposable)
    ├── Shuts down Kestrel
    └── Flushes logs
    ↓
Process exits
```

## Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| Startup class over minimal API | Clear separation of services vs middleware; familiar pattern |
| Transient services | Stateless services; no shared state concerns |
| Static Quad9 client | Single UDP socket reused; thread-safe; no disposal needed |
| Middleware order | Security first (scanner, replay), then auth, then business logic |
| Exception handling first | Catches everything downstream; consistent error format |
| HTTPS redirection after static files | Allows HTTP for static assets in dev; prod uses reverse proxy |

## Testing the Pipeline

### Integration Test Host (ApiTestHost.cs)

```csharp
builder.ConfigureTestServices(services =>
{
    // Replace real services with mocks
    services.RemoveAll<IDnsRecordService>();
    services.AddSingleton(DnsRecordServiceMock.Object);

    services.RemoveAll<ILogger>();
    services.AddSingleton(LoggerMock.Object);
});

builder.ConfigureAppConfiguration(config =>
{
    // Override with test configuration
    config.AddInMemoryCollection(new[]
    {
        new KeyValuePair<string, string>("SecuritySettings:ApiKey", "test-key"),
        new KeyValuePair<string, string>("GandiSettings:ApiKey", "test-gandi-key")
    });
});
```

This allows testing the full pipeline with controlled dependencies.