# Host and Composition

## Overview

The host and composition layer is responsible for creating the ASP.NET Core application, configuring the dependency injection container, registering services, and ordering the middleware pipeline. It is the application's composition root.

## Responsibilities

- Create and configure the ASP.NET Core host
- Register all services in the DI container with appropriate lifetimes
- Configure the middleware pipeline in the correct order
- Bind configuration to strongly-typed settings objects
- Define the application's startup and shutdown behaviour

## Components

### Program.cs

**File:** `DynamicDnsUpdater.API/Program.cs`

**Purpose:** Application entry point that creates the host builder and specifies the `Startup` class.

**Key Characteristics:**
- Standard ASP.NET Core minimal entry point
- Uses `Host.CreateDefaultBuilder` for default configuration
- Delegates all configuration to `Startup` class
- No direct service registration or middleware configuration

```csharp
public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseStartup<Startup>();
        });
```

### Startup.cs

**File:** `DynamicDnsUpdater.API/Startup.cs`

**Purpose:** Configures services and the middleware pipeline.

**Key Characteristics:**
- Receives `IConfiguration` via constructor injection
- Two methods: `ConfigureServices` (DI registration) and `Configure` (middleware pipeline)
- Uses extension methods from `ServiceCollectionExtensions` for service registration
- Orders middleware according to NuciAPI requirements

**ConfigureServices:**
```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddControllers();

    services
        .AddConfigurations(Configuration)
        .AddCustomServices()
        .AddNuciApiReplayProtection()
        .AddNuciApiScannerProtection();
}
```

**Configure (Middleware Pipeline Order):**
1. `UseNuciApiExceptionHandling()` — First, catches all downstream exceptions
2. `UseNuciApiScannerProtection()` — Blocks known malicious scanners
3. `UseNuciApiRequestLogging()` — Logs incoming requests
4. `UseDeveloperExceptionPage()` — Development only, detailed errors
5. `UseHttpsRedirection()` — Redirects HTTP to HTTPS
6. `UseDefaultFiles()` / `UseStaticFiles()` — Static file serving
7. `UseRouting()` — Endpoint routing
8. `UseNuciApiHeaderValidation()` — Validates NuciAPI protocol headers
9. `UseNuciApiReplayProtection()` — HMAC-based replay protection
10. `UseAuthorization()` — API key authorisation
11. `UseEndpoints()` — Maps controllers

### ServiceCollectionExtensions.cs

**File:** `DynamicDnsUpdater.API/ServiceCollectionExtensions.cs`

**Purpose:** Extension methods for registering application services and configuration.

**Key Characteristics:**
- Separates configuration binding from service registration
- Uses `AddConfigurations` for settings binding
- Uses `AddCustomServices` for application service registration
- Returns `IServiceCollection` for fluent chaining

**AddConfigurations:**
```csharp
public static IServiceCollection AddConfigurations(
    this IServiceCollection services,
    IConfiguration configuration)
{
    SecuritySettings securitySettings = new();
    GandiSettings gandiSettings = new();

    configuration.Bind(nameof(SecuritySettings), securitySettings);
    configuration.Bind(nameof(GandiSettings), gandiSettings);

    return services
        .AddSingleton(securitySettings)
        .AddSingleton(gandiSettings)
        .AddNuciLoggerSettings(configuration);
}
```

**AddCustomServices:**
```csharp
public static IServiceCollection AddCustomServices(
    this IServiceCollection services) => services
    .AddTransient<ILogger, NuciLogger>()
    .AddTransient<IDnsRecordService, DnsRecordService>()
    .AddTransient<IGandiService, GandiService>();
```

## Service Lifetimes

| Service | Lifetime | Rationale |
|---------|----------|-----------|
| `SecuritySettings` | Singleton | Configuration bound once at startup, read-only thereafter |
| `GandiSettings` | Singleton | Configuration bound once at startup, read-only thereafter |
| `ILogger` (NuciLogger) | Transient | NuciLog design; each resolution gets a logger instance |
| `IDnsRecordService` | Transient | Stateless service; transient avoids any shared state concerns |
| `IGandiService` | Transient | Creates new `HttpClient` per request; transient matches usage |

## Configuration Binding

Configuration is bound from ASP.NET Core configuration sources (appsettings.json, environment variables, etc.) using `configuration.Bind()`.

**Settings Classes:**
- `SecuritySettings` — Contains `ApiKey` for API authentication
- `GandiSettings` — Contains `ApiKey` for Gandi LiveDNS authentication
- `NuciLoggerSettings` — Configured via `AddNuciLoggerSettings` (NuciLog package)

**Binding Pattern:**
```csharp
SecuritySettings securitySettings = new();
configuration.Bind(nameof(SecuritySettings), securitySettings);
services.AddSingleton(securitySettings);
```

## Middleware Pipeline Ordering

The middleware order is critical for correct behaviour:

```
Request
    ↓
ExceptionHandling (catches all)
    ↓
ScannerProtection (blocks malicious clients early)
    ↓
RequestLogging (logs all requests that pass scanner)
    ↓
DeveloperExceptionPage (dev only)
    ↓
HttpsRedirection
    ↓
StaticFiles / DefaultFiles
    ↓
Routing
    ↓
HeaderValidation (validates NuciAPI headers)
    ↓
ReplayProtection (checks HMAC token)
    ↓
Authorization (API key validation)
    ↓
Endpoints (Controller execution)
    ↓
Response
```

**Rationale:**
- Exception handling first → catches everything downstream
- Scanner protection early → blocks malicious traffic before logging
- Request logging after scanner → doesn't log blocked requests
- Header validation before replay → replay needs valid headers
- Replay protection before authorisation → replay check is protocol-level
- Authorisation last before endpoints → all protocol checks passed

## NuciAPI Middleware Integration

The application uses several NuciAPI middleware packages:

| Middleware | Package | Purpose |
|------------|---------|---------|
| `UseNuciApiExceptionHandling` | NuciAPI.Middleware.ExceptionHandling | Maps exceptions to HTTP responses |
| `UseNuciApiScannerProtection` | NuciAPI.Middleware.Security | Blocks known scanner user agents/IPs |
| `UseNuciApiRequestLogging` | NuciAPI.Middleware.Logging | Logs request/response metadata |
| `UseNuciApiHeaderValidation` | NuciAPI.Middleware.Security | Validates required NuciAPI headers |
| `UseNuciApiReplayProtection` | NuciAPI.Middleware.Security | HMAC-based replay attack prevention |

## Boundary Rules

### Composition Root Boundary
- ✅ **May:** Reference concrete implementations for registration
- ✅ **May:** Configure third-party middleware and packages
- ✅ **May:** Bind configuration to settings objects
- ❌ **Must not:** Contain business logic or request handling

### Service Registration Boundary
- ✅ **May:** Register interfaces with implementations
- ✅ **May:** Choose appropriate lifetimes (singleton, transient, scoped)
- ❌ **Must not:** Register services conditionally based on runtime state (use configuration instead)

## Testing Considerations

### Integration Test Host
**File:** `DynamicDnsUpdater.API.IntegrationTests/ApiTestHost.cs`

The integration tests use a custom `WebApplicationFactory` that:
- Overrides configuration with test values (in-memory)
- Replaces `IDnsRecordService` and `ILogger` with mocks
- Creates HTTPS client with test base address
- Adds `X-Forwarded-For` header for consistent IP

```csharp
protected override void ConfigureWebHost(IWebHostBuilder builder)
{
    builder.UseEnvironment("IntegrationTesting");
    builder.ConfigureAppConfiguration(configurationBuilder =>
    {
        // In-memory test configuration
    });
    builder.ConfigureTestServices(services =>
    {
        services.RemoveAll<IDnsRecordService>();
        services.AddSingleton(DnsRecordServiceMock.Object);
        services.RemoveAll<ILogger>();
        services.AddSingleton(LoggerMock.Object);
    });
}
```

## Evolution Path

If the host and composition layer needs to evolve:
1. **Add new services** — Register in `ServiceCollectionExtensions.AddCustomServices`
2. **Add new configuration** — Create settings class, bind in `AddConfigurations`
3. **Change middleware order** — Modify `Startup.Configure` with clear rationale
4. **Add new middleware** — Insert at appropriate position in pipeline
5. **Change service lifetimes** — Evaluate statefulness and thread safety first