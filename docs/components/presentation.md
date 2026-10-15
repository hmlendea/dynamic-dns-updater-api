# Presentation Layer

## Overview

The presentation layer consists of ASP.NET Core controllers that expose HTTP endpoints and handle request/response transport. It is the outermost layer that clients interact with directly.

## Responsibilities

- Expose RESTful endpoints for DNS record updates
- Validate and bind incoming HTTP requests (route parameters, headers, JSON body)
- Forward validated request data to application services
- Return HTTP responses based on service results or exceptions
- Contain no business logic or provider-specific implementation details

## Components

### DnsRecordsController

**File:** `DynamicDnsUpdater.API/Controllers/DnsRecordsController.cs`

**Purpose:** Handles `PUT /DnsRecords/{domainName}` requests to update DNS A records.

**Key Characteristics:**
- Thin controller — contains no business logic
- Uses expression-bodied action that delegates to `ProcessRequestAsync`
- Depends on `IDnsRecordService` and `SecuritySettings` via constructor injection
- Constructs `NuciApiAuthorisation` for API key validation
- Returns `ActionResult` (not `ActionResult<T>`) per NuciAPI controller pattern

**Interaction Flow:**
1. Receives HTTP PUT request with route parameter `domainName`
2. Binds JSON body to `PutDnsRecordRequest` model
3. Calls `IDnsRecordService.Update(domainName, ipAddress, dnsProvider)`
4. Returns result from `ProcessRequestAsync` (handles success/exception mapping)

### PutDnsRecordRequest

**File:** `DynamicDnsUpdater.API/Requests/PutDnsRecordRequest.cs`

**Purpose:** Data transfer object for DNS update request body.

**Key Characteristics:**
- Inherits from `NuciApiRequest` (NuciAPI base class)
- Uses `JsonPropertyName` attributes to map camelCase JSON properties
- Contains exactly two properties: `IpAddress` and `DnsProvider`
- No validation logic — validation occurs in service layer or provider adapters

## Boundary Rules

### Controller Boundary
- ✅ **May:** Bind route/body, call service methods, return controller results
- ❌ **Must not:** Contain provider-specific HTTP calls, DNS resolution logic, business rules
- ❌ **Must not:** Depend on concrete provider implementations (only `IDnsRecordService`)

### Transport Boundary
- ✅ **May:** Define request/response shapes, HTTP verbs, routes, status codes
- ❌ **Must not:** Interpret business meaning of data (e.g., validate IP formats, provider names)

## Data Flow

```
HTTP Request
    ↓ (ASP.NET Core routing/model binding)
[Route: domainName] + [Body: {ip, provider}]
    ↓ (Controller binding)
IDnsRecordService.Update(string, string, string)
    ↓ (Application service)
Business logic execution
```

## Cross-Layer Relationships

### Depends On
- **Application Service** (`IDnsRecordService`) — receives transport data, returns operation result
- **Configuration** (`SecuritySettings`) — provides API key for authorisation

### Depended By
- **HTTP Clients** — send DNS update requests to this layer
- **Middleware Pipeline** — processes requests before they reach controllers

## Implementation Details

### NuciAPI Controller Pattern
The controller inherits from `NuciApiController` which provides:
- `ProcessRequestAsync` method for standardised request processing
- Built-in authorisation handling via `NuciApiAuthorisation`
- Consistent error response formatting

### Constructor Injection
```csharp
public DnsRecordsController(
    IDnsRecordService service,
    SecuritySettings securitySettings) : NuciApiController
{
    readonly NuciApiAuthorisation authorisation = NuciApiAuthorisation.ApiKey(securitySettings.ApiKey);
}
```

### Action Method
```csharp
[HttpPut("{domainName}")]
public async Task<ActionResult> Update(
    [FromRoute] string domainName,
    [FromBody] PutDnsRecordRequest request)
    => await ProcessRequestAsync(
        request,
        () => service.Update(
            domainName,
            request.IpAddress,
            request.DnsProvider),
        authorisation);
}
```

## Testing Considerations

### Unit Tests
- Not typically unit tested in isolation due to thin logic
- Covered by integration tests that verify:
  - Route binding and JSON deserialization
  - Authorisation and header validation
  - Correct service invocation with request values
  - Proper HTTP response mapping

### Integration Tests
- Verify end-to-end request processing through `WebApplicationFactory`
- Mock `IDnsRecordService` to isolate controller behaviour
- Test various HTTP scenarios (valid/invalid inputs, auth failures, etc.)

## Evolution Path

If the presentation layer needs to evolve:
1. **Add new endpoints** — Follow same pattern: thin controller → service call
2. **Change transport shape** — Modify route, HTTP verb, or request/response DTOs
3. **Add cross-cutting concerns** — Prefer middleware over controller logic
4. **Version the API** — Introduce new controller or route prefix for breaking changes