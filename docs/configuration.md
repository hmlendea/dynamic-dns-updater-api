# Configuration

## Overview

This document describes how the application is configured, including configuration sources, binding to strongly-typed objects, and the configuration classes used throughout the system.

## Configuration Sources

The application uses ASP.NET Core's configuration system with the following sources (in order of priority):

1. **appsettings.json** — Base configuration file
2. **appsettings.{Environment}.json** — Environment-specific overrides
3. **Environment variables** — Highest priority for most settings
4. **Command line arguments** — Highest priority
5. **User secrets** (Development only) — Local development overrides

### appsettings.json

**File:** `DynamicDnsUpdater.API/appsettings.json`

The base configuration file containing default values and structure. In the repository, this file contains placeholder values that should be overridden in deployment.

```json
{
  "SecuritySettings": {
    "ApiKey": ""
  },
  "GandiSettings": {
    "ApiKey": ""
  },
  "NuciLoggerSettings": {
    "MinimumLevel": "Information",
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/log-.txt",
          "rollingInterval": "Day"
        }
      }
    ]
  }
}
```

### Environment Variables

Environment variables use double underscore (`__`) as a separator for nested configuration:

```
SecuritySettings__ApiKey=your-api-key
GandiSettings__ApiKey=your-gandi-api-key
NuciLoggerSettings__MinimumLevel=Warning
```

### Command Line Arguments

Can override any setting when starting the application:

```bash
dotnet DynamicDnsUpdater.API.dll \
  SecuritySettings:ApiKey=cli-key \
  GandiSettings:ApiKey=cli-gandi-key
```

## Configuration Binding

Configuration is bound to strongly-typed C# classes using the `options` pattern. This provides type safety, IntelliSense, and validation.

### Binding Process

In `ServiceCollectionExtensions.AddConfigurations`:

```csharp
public static IServiceCollection AddConfigurations(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // Create instances
    SecuritySettings securitySettings = new();
    GandiSettings gandiSettings = new();

    // Bind configuration sections to instances
    configuration.Bind(nameof(SecuritySettings), securitySettings);
    configuration.Bind(nameof(GandiSettings), gandiSettings);

    // Register as singletons
    return services
        .AddSingleton(securitySettings)
        .AddSingleton(gandiSettings)
        .AddNuciLoggerSettings(configuration);
}
```

### Binding Rules

- Property names must match configuration keys (case-insensitive)
- Nested objects are supported via colon (`:`) or double underscore (`__`)
- Arrays and collections are supported
- Default values are used when configuration is missing
- Type conversion is automatic (string → int, bool, etc.)

## Configuration Classes

### SecuritySettings

**File:** `DynamicDnsUpdater.API/Configuration/SecuritySettings.cs`

Contains settings related to API security and authentication.

```csharp
public class SecuritySettings
{
    /// <summary>
    /// API key used for client authentication via Authorization header.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}
```

**Used By:**
- `DnsRecordsController` — Validates incoming requests
- `NuciApiAuthorisation` — Creates authorisation challenge

**Validation:** None performed; empty string results in authentication failure

### GandiSettings

**File:** `DynamicDnsUpdater.API/Configuration/GandiSettings.cs`

Contains settings for the Gandi LiveDNS integration.

```csharp
public class GandiSettings
{
    /// <summary>
    /// API key used for authenticating with Gandi LiveDNS v5 API.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}
```

**Used By:**
- `GandiService` — Authenticates HTTP requests to Gandi API

**Validation:** None performed; empty string results in 401 from Gandi API

### NuciLoggerSettings

**File:** Part of NuciLog package (`NuciLoggerSettings`)

Configures the NuciLog logging framework.

```csharp
public class NuciLoggerSettings
{
    public string MinimumLevel { get; set; } = "Information";
    public IEnumerable<NuciLogSink> WriteTo { get; set; } = Array.Empty<NuciLogSink>();
}
```

**Used By:**
- `NuciLogger` — Configures log sinks and minimum level

**Default Configuration:**
- Minimum level: Information
- Sinks: Console and File (daily rolling)

## Configuration in Different Environments

### Development

- Uses `appsettings.json` and `appsettings.Development.json`
- User secrets enabled for sensitive data
- Logging includes detailed debug information
- Exception pages enabled

### Testing (Integration Tests)

- Uses in-memory configuration via `WebApplicationFactory`
- Overrides `ApiKey` values with test constants
- Logging typically directed to test output

### Staging/Production

- Relies on environment variables for secrets
- `appsettings.json` may contain non-sensitive defaults
- Logging configured for external aggregation (e.g., Seq, ELK)
- Minimum level often Warning or Error

## Best Practices

### Secret Management

1. **Never commit secrets** — Use `.gitignore` to exclude `appsettings.{Environment}.json`
2. **Use environment variables** — Preferred for production secrets
3. **Use user secrets** — For development only (`dotnet user-secrets`)
4. **Consider Azure Key Vault** — For production secret management

### Configuration Structure

1. **Group related settings** — Use classes like `SecuritySettings`, `GandiSettings`
2. **Use descriptive property names** — Self-documenting configuration
3. **Provide sensible defaults** — Where appropriate (e.g., logging levels)
4. **Document required settings** — In deployment documentation

### Validation

1. **Validate at startup** — Check required settings in `Startup.ConfigureServices`
2. **Fail fast** — Throw meaningful exceptions if configuration is invalid
3. **Consider data annotations** — For more complex validation scenarios

## Accessing Configuration

### Via Dependency Injection

Preferred method — receive settings through constructor injection:

```csharp
public class GandiService : IGandiService
{
    private readonly GandiSettings _gandiSettings;
    private readonly ILogger _logger;

    public GandiService(
        GandiSettings gandiSettings,
        ILogger logger)
    {
        _gandiSettings = gandiSettings;
        _logger = logger;
    }
}
```

### Via IConfiguration (Discouraged)

Direct access should be avoided in favor of strongly-typed options:

```csharp
// Discouraged - breaks encapsulation and testability
public class SomeService
{
    private readonly IConfiguration _configuration;

    public SomeService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // Later: _configuration["GandiSettings:ApiKey"]
}
```

## Configuration Flow

```
Configuration Sources
    │
    ▼
IConfiguration (built by Host)
    │
    ▼
ServiceCollectionExtensions.AddConfigurations()
    │
    ├── Creates SecuritySettings instance
    │   Binds "SecuritySettings" section
    │   Registers as singleton
    │
    ├── Creates GandiSettings instance
    │   Binds "GandiSettings" section
    │   Registers as singleton
    │
    └── Adds NuciLoggerSettings
        │
        ▼
    NuciLog configuration
    │
    ▼
Registered Services
    │
    ▼
Consumed via DI
    │
    ▼
Application Components
```

## Deployment Considerations

### Azure App Service

1. Use Application Settings for environment variables
2. Set `ASPNETCORE_ENVIRONMENT=Production`
3. Mount `appsettings.json` via Kudu if needed

### Docker

1. Use `-e` flags for environment variables
2. Or create custom `appsettings.Production.json`
3. Consider multi-stage builds to exclude secrets

### Kubernetes

1. Use ConfigMaps for non-sensitive configuration
2. Use Secrets for sensitive data (API keys)
3. Mount as environment variables or files

## Changing Configuration at Runtime

The application does **not** support reloading configuration at runtime. Configuration is read once at startup and remains constant for the lifetime of the application. To change configuration:

1. Modify the configuration source
2. Restart the application
3. New configuration is loaded on startup

## Testing Configuration

### Unit Tests

Create instances directly with test values:

```csharp
var settings = new SecuritySettings { ApiKey = "test-key" };
```

### Integration Tests

Override configuration in `ApiTestHost.cs`:

```csharp
builder.ConfigureAppConfiguration((hostingContext, config) =>
{
    config.AddInMemoryCollection(new[]
    {
        new KeyValuePair<string, string>("SecuritySettings:ApiKey", "test-key"),
        new KeyValuePair<string, string>("GandiSettings:ApiKey", "test-gandi-key")
    });
});
```

## Security Implications

### API Keys

- Treated as secrets — should never be logged or exposed
- Gandi API key provides full DNS zone control — protect accordingly
- API key for this service should be rotated periodically
- Consider using different keys per environment

### Configuration Exposure

- Configuration classes contain only API keys in this application
- No personal data or sensitive user information in configuration
- Logging configuration does not expose secrets by default

## Evolution Path

If the configuration system needs to evolve:

1. **Add new settings** — Create new class, bind in `AddConfigurations`, register as singleton
2. **Add validation** — Use data annotations or custom validation in binding
3. **Add runtime reloading** — Use `IOptionsMonitor<T>` instead of `IOptions<T>`
4. **Add encrypted configuration** — Use Azure Key Vault or AWS Secrets Manager
5. **Add feature flags** — Consider adding a `FeatureSettings` class
6. **Add hierarchical configuration** — Support for per-environment overrides beyond current model