# Logging

## Overview

This document describes the application's logging infrastructure, including the logging framework (NuciLog), structured logging patterns, custom log keys, operation definitions, and log output configuration.

## Logging Framework

### NuciLog

**Packages:**
- `NuciLog` (1.2.1) — Main logging package
- `NuciLog.Core` (3.1.0) — Core logging infrastructure

**Purpose:** Structured, operation-based logging framework designed for NuciAPI applications.

**Key Features:**
- Operation-based logging (start/success/failure)
- Structured key-value pairs
- Multiple output sinks (Console, File)
- Configurable minimum log level
- Integration with NuciAPI middleware

### ILogger Abstraction

The application uses `NuciLog.Core`'s `ILogger` interface, not Microsoft's `ILogger<T>`.

**Registration:**
```csharp
// In ServiceCollectionExtensions.AddCustomServices
services.AddTransient<ILogger, NuciLogger>();
```

**Lifetime:** Transient — new instance per service resolution

## Custom Logging Components

### MyLogInfoKey

**File:** `DynamicDnsUpdater.API/Logging/MyLogInfoKey.cs`

**Purpose:** Defines custom log info keys for structured logging.

**Structure:**
```csharp
public static class MyLogInfoKey
{
    public static readonly LogInfoKey Provider = new(nameof(Provider));
    public static readonly LogInfoKey DomainName = new(nameof(DomainName));
    public static readonly LogInfoKey IpAddress = new(nameof(IpAddress));
}
```

**Key Characteristics:**
- Static class with readonly `LogInfoKey` fields
- Each key represents a piece of contextual information
- Used to create `LogInfo` objects for structured logging

**Available Keys:**
| Key | Purpose | Example Value |
|-----|---------|---------------|
| `Provider` | DNS provider name | `"Gandi"` |
| `DomainName` | Domain being updated | `"www.example.com"` |
| `IpAddress` | Target IP address | `"192.0.2.1"` |

### MyOperation

**File:** `DynamicDnsUpdater.API/Logging/MyOperation.cs`

**Purpose:** Defines custom operation identifiers for the application.

**Structure:**
```csharp
public static class MyOperation
{
    public static readonly Operation UpdateDnsRecord =
        new(nameof(UpdateDnsRecord));
}
```

**Key Characteristics:**
- Static class with readonly `Operation` fields
- Each operation represents a logical unit of work
- Used as the first parameter to `ILogger.Info()` and `ILogger.Error()`

**Available Operations:**
| Operation | Purpose |
|-----------|---------|
| `UpdateDnsRecord` | DNS record update workflow |

## Logging Patterns

### Operation-Based Logging

The application follows an operation-based logging pattern where each logical operation is logged with start, success, and failure states.

**Pattern:**
```csharp
// 1. Log operation started
logger.Info(
    MyOperation.UpdateDnsRecord,
    OperationStatus.Started,
    logInfos);

// 2. Perform operation
// ...

// 3. Log operation success
logger.Info(
    MyOperation.UpdateDnsRecord,
    OperationStatus.Success,
    logInfos);

// OR on failure:
catch (Exception ex)
{
    logger.Error(
        MyOperation.UpdateDnsRecord,
        OperationStatus.Failure,
        ex,
        logInfos);
    throw;
}
```

### Structured Log Info

Log info objects provide structured context for log entries.

**Creation:**
```csharp
IEnumerable<LogInfo> logInfos =
[
    new(MyLogInfoKey.Provider, dnsProviderName),
    new(MyLogInfoKey.DomainName, domainName),
    new(MyLogInfoKey.IpAddress, ipAddress)
];
```

**LogInfo Structure:**
| Property | Type | Description |
|----------|------|-------------|
| `Key` | `LogInfoKey` | Identifier for the info item |
| `Value` | `object` | The value to log |

### Log Levels

| Level | Usage | When |
|-------|-------|------|
| `Info` | Operation lifecycle | Start, success, no-op |
| `Error` | Operation failure | Exception with context |
| `Debug` | Detailed diagnostics | Not used in current code |
| `Warning` | Non-critical issues | Not used in current code |

## Log Output Configuration

### NuciLoggerSettings

**Configuration Source:** `appsettings.json` → `NuciLoggerSettings` section

**Default Configuration:**
```json
{
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

### Output Sinks

| Sink | Purpose | Configuration |
|------|---------|---------------|
| Console | Real-time log output | Default format |
| File | Persistent log storage | Daily rolling files |

**File Output:**
- Path: `logs/log-{date}.txt`
- Rolling interval: Daily
- Format: Structured text with timestamps

### Minimum Level

Controls which log entries are emitted:

| Level | Value | Description |
|-------|-------|-------------|
| Verbose | 0 | All messages |
| Debug | 1 | Debug and above |
| Information | 2 | Info, Warning, Error, Fatal |
| Warning | 3 | Warning, Error, Fatal |
| Error | 4 | Error, Fatal |
| Fatal | 5 | Fatal only |

**Default:** `Information` — emits Info, Warning, Error, Fatal

## Log Entry Structure

### Console Output Format

```
[2024-01-15 10:30:45.123 +00:00] [INFO] [UpdateDnsRecord] [Started] Provider=Gandi DomainName=www.example.com IpAddress=192.0.2.1
[2024-01-15 10:30:46.456 +00:00] [INFO] [UpdateDnsRecord] [Success] Provider=Gandi DomainName=www.example.com IpAddress=192.0.2.1
```

### File Output Format

Same as console, with additional structured data:

```
[2024-01-15 10:30:45.123 +00:00] [INFO] [UpdateDnsRecord] [Started] Provider=Gandi DomainName=www.example.com IpAddress=192.0.2.1
[2024-01-15 10:30:46.456 +00:00] [INFO] [UpdateDnsRecord] [Success] Provider=Gandi DomainName=www.example.com IpAddress=192.0.2.1
```

## Logging in Different Layers

### Application Service (DnsRecordService)

**Logs:**
1. Operation started (with provider, domain, IP)
2. Operation success (with provider, domain, IP)
3. Operation failure (with exception and context)

**No-op case:**
```csharp
logger.Info(
    MyOperation.UpdateDnsRecord,
    OperationStatus.Success,
    logInfos);
```

### Integration Service (GandiService)

**Logs:**
1. Operation started (with domain, IP)
2. Operation success (with domain, IP)

**Note:** Uses same `MyOperation.UpdateDnsRecord` and `MyLogInfoKey` values

### Middleware (NuciAPI)

**Logs:**
- Request logging (method, path, status, duration)
- Exception handling (unhandled exceptions)
- Security events (scanner detection, replay attempts)

**Note:** Uses NuciAPI's internal logging, not custom keys

## Security Considerations

### Sensitive Data

**What IS logged:**
- Domain names (not sensitive — public DNS records)
- IP addresses (not sensitive — public routing information)
- Provider names (not sensitive — configuration metadata)

**What is NOT logged:**
- API keys (neither SecuritySettings nor GandiSettings)
- HMAC signatures
- Nonces
- Request bodies (logged by middleware but not application code)

### Log Redaction

No automatic redaction is configured. The application relies on:
- Not logging sensitive values by design
- NuciAPI middleware not logging request bodies
- API keys only used in HTTP headers (not logged)

## Testing Logging

### Unit Test Patterns

**Verifying log calls:**
```csharp
// In DnsRecordServiceTests
[Test]
public async Task Update_GandiProvider_LogsStartedAndSuccess()
{
    // Arrange
    var loggerMock = new Mock<ILogger>();
    var service = new DnsRecordService(gandiServiceMock.Object, loggerMock.Object);

    // Act
    await service.Update("www.example.com", "192.0.2.1", "Gandi");

    // Assert
    loggerMock.Verify(
        x => x.Info(
            MyOperation.UpdateDnsRecord,
            OperationStatus.Started,
            It.IsAny<IEnumerable<LogInfo>>()),
        Times.Once);

    loggerMock.Verify(
        x => x.Info(
            MyOperation.UpdateDnsRecord,
            OperationStatus.Success,
            It.IsAny<IEnumerable<LogInfo>>()),
        Times.Once);
}
```

**Verifying error logging:**
```csharp
[Test]
public async Task Update_GandiThrows_LogsFailure()
{
    // Arrange
    gandiServiceMock
        .Setup(x => x.Update(It.IsAny<string>(), It.IsAny<string>()))
        .ThrowsAsync(new HttpRequestException("API error"));

    // Act & Assert
    var ex = await Assert.ThrowsAsync<HttpRequestException>(
        () => service.Update("www.example.com", "192.0.2.1", "Gandi"));

    loggerMock.Verify(
        x => x.Error(
            MyOperation.UpdateDnsRecord,
            OperationStatus.Failure,
            It.IsAny<Exception>(),
            It.IsAny<IEnumerable<LogInfo>>()),
        Times.Once);
}
```

## Log Analysis

### Key Metrics from Logs

| Metric | Log Source | Calculation |
|--------|-----------|-------------|
| Update success rate | DnsRecordService | Success / (Success + Failure) |
| Short-circuit rate | DnsRecordService | No-op Success / Total Success |
| Average latency | Middleware | Response time from request log |
| Error rate by type | All layers | Count by exception type |
| Provider usage | DnsRecordService | Count by Provider key |

### Log Queries (Example)

**Find all failed updates:**
```
[OperationStatus.Failure] AND [UpdateDnsRecord]
```

**Find updates for specific domain:**
```
[DomainName=www.example.com] AND [UpdateDnsRecord]
```

**Find updates using specific provider:**
```
[Provider=Gandi] AND [UpdateDnsRecord]
```

## Evolution Path

If the logging system needs to evolve:

1. **Add new log keys** — Add to `MyLogInfoKey`
2. **Add new operations** — Add to `MyOperation`
3. **Add new log levels** — Use `Debug` or `Warning` as needed
4. **Add structured output** — Configure JSON sink in NuciLoggerSettings
5. **Add log aggregation** — Configure Seq, ELK, or similar sink
6. **Add correlation IDs** — Use NuciAPI request IDs for tracing
7. **Add metrics** — Emit counters alongside log entries