# Testing

## Overview

This document describes the testing strategy, organization, patterns, and coverage for the Dynamic DNS Updater API.

## Test Strategy

### Test Pyramid

```
                    ┌─────────────────┐
                    │  Integration    │  ← Few, high-value
                    │     Tests       │
                    └─────────────────┘
                ┌───────────────────────┐
                │    Unit Tests         │  ← Many, fast, isolated
                └───────────────────────┘
```

### Test Categories

| Category | Count | Speed | Scope | Purpose |
|----------|-------|-------|-------|---------|
| Unit Tests | ~20 | Fast (<1s) | Single class/method | Verify business logic |
| Integration Tests | ~10 | Medium (5-10s) | Full HTTP pipeline | Verify API contracts |

### Test Principles

1. **Fast Feedback:** Unit tests run in milliseconds
2. **Isolation:** Tests don't depend on external services
3. **Determinism:** Same input always produces same output
4. **Readability:** Tests serve as documentation
5. **Maintainability:** Easy to update when behavior changes

## Test Organization

### Project Structure

```
DynamicDnsUpdater.API.UnitTests/
├── DynamicDnsUpdater.API.UnitTests.csproj
├── Service/
│   ├── DnsRecordServiceTests.cs
│   ├── Integrations/
│   │   └── Gandi/
│   │       └── GandiServiceTests.cs
│   └── Models/
│       └── DnsProviderTests.cs

DynamicDnsUpdater.API.IntegrationTests/
├── DynamicDnsUpdater.API.IntegrationTests.csproj
├── ApiIntegrationTestBase.cs
├── ApiTestHost.cs
├── ApiRequestFactory.cs
├── ApiResponseAssertions.cs
├── DnsRecordsApiTests.cs
├── ExceptionHandlingApiTests.cs
├── ReplayProtectionApiTests.cs
├── RequestBodyApiTests.cs
├── RequestSecurityApiTests.cs
└── ScannerProtectionApiTests.cs
```

### Naming Convention

**Unit Tests:** `Given[Context]_When[Action]_Then[Outcome]`

```csharp
[Test]
public void GivenValidDomainAndIp_WhenUpdateIsCalled_ThenCallsGandiService()
{
    // Given
    var domainName = "www.example.com";
    var ipAddress = "192.0.2.1";
    var provider = "Gandi";

    // When
    await service.Update(domainName, ipAddress, provider);

    // Then
    gandiServiceMock.Verify(x => x.Update(domainName, ipAddress), Times.Once);
}
```

**Integration Tests:** `Should[ExpectedBehavior]When[Condition]`

```csharp
[Test]
public async Task ShouldReturn200WhenValidRequestIsSent()
{
    // Arrange
    var request = ApiRequestFactory.CreatePutDnsRecordRequest("www.example.com", "192.0.2.1");

    // Act
    var response = await client.SendAsync(request);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

## Unit Test Patterns

### Service Tests (DnsRecordServiceTests)

```csharp
[TestFixture]
public class DnsRecordServiceTests
{
    private Mock<IDnsRecordService> serviceMock;
    private Mock<IGandiService> gandiServiceMock;
    private Mock<ILogger> loggerMock;
    private DnsRecordService service;

    [SetUp]
    public void SetUp()
    {
        gandiServiceMock = new Mock<IGandiService>();
        loggerMock = new Mock<ILogger>();
        service = new DnsRecordService(gandiServiceMock.Object, loggerMock.Object);
    }

    [Test]
    public async Task GivenValidInput_WhenUpdateIsCalled_ThenCallsProvider()
    {
        // Arrange
        var domainName = "www.example.com";
        var ipAddress = "192.0.2.1";
        var provider = "Gandi";

        // Act
        await service.Update(domainName, ipAddress, provider);

        // Assert
        gandiServiceMock.Verify(x => x.Update(domainName, ipAddress), Times.Once);
        loggerMock.Verify(
            x => x.Info(MyOperation.UpdateDnsRecord, OperationStatus.Started, It.IsAny<IEnumerable<LogInfo>>()),
            Times.Once);
        loggerMock.Verify(
            x => x.Info(MyOperation.UpdateDnsRecord, OperationStatus.Success, It.IsAny<IEnumerable<LogInfo>>()),
            Times.Once);
    }

    [Test]
    public async Task GivenGandiThrows_WhenUpdateIsCalled_ThenLogsFailureAndRethrows()
    {
        // Arrange
        gandiServiceMock
            .Setup(x => x.Update(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("API error"));

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.Update("www.example.com", "192.0.2.1", "Gandi"));

        loggerMock.Verify(
            x => x.Error(
                MyOperation.UpdateDnsRecord,
                OperationStatus.Failure,
                It.IsAny<Exception>(),
                It.IsAny<IEnumerable<LogInfo>>()),
            Times.Once);
    }
}
```

### Provider Tests (GandiServiceTests)

```csharp
[TestFixture]
public class GandiServiceTests
{
    private Mock<IGandiSettings> settingsMock;
    private Mock<ILogger> loggerMock;
    private GandiService service;

    [SetUp]
    public void SetUp()
    {
        settingsMock = new Mock<IGandiSettings>();
        settingsMock.Setup(x => x.ApiKey).Returns("test-api-key");
        loggerMock = new Mock<ILogger>();
        service = new GandiService(settingsMock.Object, loggerMock.Object);
    }

    [Test]
    public void GivenDomainWithoutSubdomain_WhenUpdateIsCalled_ThenThrowsArgumentException()
    {
        // Arrange
        var domainName = "example.com"; // No subdomain
        var ipAddress = "192.0.2.1";

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(
            () => service.Update(domainName, ipAddress));
    }

    [Test]
    public void GivenInvalidDomainFormat_WhenUpdateIsCalled_ThenThrowsFormatException()
    {
        // Arrange
        var domainName = "invalid"; // No dot
        var ipAddress = "192.0.2.1";

        // Act & Assert
        Assert.ThrowsAsync<FormatException>(
            () => service.Update(domainName, ipAddress));
    }
}
```

### Model Tests (DnsProviderTests)

```csharp
[TestFixture]
public class DnsProviderTests
{
    [TestCase("Gandi", DnsProvider.Gandi)]
    [TestCase("gandi", DnsProvider.Gandi)]
    [TestCase("GANDI", DnsProvider.Gandi)]
    [TestCase("Unknown", DnsProvider.Gandi)] // Default
    public void GivenProviderName_WhenFromStringIsCalled_ThenReturnsCorrectProvider(string input, DnsProvider expected)
    {
        // Act
        var result = DnsProvider.FromString(input);

        // Assert
        result.Should().Be(expected);
    }
}
```

## Integration Test Patterns

### Test Host (ApiTestHost)

```csharp
public class ApiTestHost : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing registrations
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDnsRecordService));
            if (descriptor != null) services.Remove(descriptor);

            descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ILogger));
            if (descriptor != null) services.Remove(descriptor);

            // Add mocks
            services.AddSingleton(Mock.Of<IDnsRecordService>());
            services.AddSingleton(Mock.Of<ILogger>());
        });

        builder.UseEnvironment("Testing");
    }
}
```

### Request Factory (ApiRequestFactory)

```csharp
public static class ApiRequestFactory
{
    public static HttpRequestMessage CreatePutDnsRecordRequest(string domainName, string ipAddress)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"/DnsRecords/{domainName}")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new PutDnsRecordRequest { IpAddress = ipAddress }),
                Encoding.UTF8,
                "application/json")
        };
        request.Headers.Add("X-Api-Key", "test-api-key");
        return request;
    }
}
```

### Response Assertions (ApiResponseAssertions)

```csharp
public static class ApiResponseAssertions
{
    public static async Task AssertSuccess(this HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    public static async Task AssertBadRequest(this HttpResponseMessage response, string expectedErrorCode = null)
    {
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        if (expectedErrorCode != null)
        {
            content.Should().Contain(expectedErrorCode);
        }
    }
}
```

### API Tests (DnsRecordsApiTests)

```csharp
[TestFixture]
public class DnsRecordsApiTests : ApiIntegrationTestBase
{
    [Test]
    public async Task ShouldReturn200WhenValidRequestIsSent()
    {
        // Arrange
        var request = ApiRequestFactory.CreatePutDnsRecordRequest("www.example.com", "192.0.2.1");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        await response.AssertSuccess();
    }

    [Test]
    public async Task ShouldReturn400WhenIpAddressIsMissing()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Put, "/DnsRecords/www.example.com")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Api-Key", "test-api-key");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        await response.AssertBadRequest("BAD_REQUEST");
    }
}
```

## Mocking Patterns

### Service Mocking

```csharp
// Mock interface
var serviceMock = new Mock<IDnsRecordService>();

// Setup return value
serviceMock
    .Setup(x => x.Update(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
    .Returns(Task.CompletedTask);

// Setup exception
serviceMock
    .Setup(x => x.Update(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
    .ThrowsAsync(new HttpRequestException("Provider error"));

// Verify call
serviceMock.Verify(x => x.Update("www.example.com", "192.0.2.1", "Gandi"), Times.Once);
```

### Logger Mocking

```csharp
var loggerMock = new Mock<ILogger>();

// Verify info log
loggerMock.Verify(
    x => x.Info(
        MyOperation.UpdateDnsRecord,
        OperationStatus.Started,
        It.Is<IEnumerable<LogInfo>>(l => l.Any(li => li.Key == MyLogInfoKey.Provider && li.Value == "Gandi"))),
    Times.Once);

// Verify error log
loggerMock.Verify(
    x => x.Error(
        MyOperation.UpdateDnsRecord,
        OperationStatus.Failure,
        It.IsAny<Exception>(),
        It.IsAny<IEnumerable<LogInfo>>()),
    Times.Once);
```

### Settings Mocking

```csharp
var settingsMock = new Mock<IGandiSettings>();
settingsMock.Setup(x => x.ApiKey).Returns("test-api-key");
```

## Test Data

### Standard Test Values

| Value | Purpose |
|-------|---------|
| `www.example.com` | Valid domain with subdomain |
| `example.com` | Domain without subdomain (invalid) |
| `192.0.2.1` | Valid IPv4 address (TEST-NET-1) |
| `2001:db8::1` | Valid IPv6 address (documentation) |
| `test-api-key` | Test API key |
| `Gandi` | Valid provider name |

### Edge Cases

| Input | Expected Behavior |
|-------|-------------------|
| Empty domain | 400 Bad Request |
| Domain without subdomain | 400 Bad Request (ArgumentException) |
| Invalid IP format | 400 Bad Request |
| Missing API key | 401 Unauthorized |
| Invalid API key | 401 Unauthorized |
| Replay attack | 401 Unauthorized |
| Scanner detected | 403 Forbidden |

## Running Tests

### Unit Tests

```bash
# Run all unit tests
dotnet test DynamicDnsUpdater.API.UnitTests/DynamicDnsUpdater.API.UnitTests.csproj

# Run with coverage
dotnet test DynamicDnsUpdater.API.UnitTests/DynamicDnsUpdater.API.UnitTests.csproj --collect:"XPlat Code Coverage"

# Run specific test
dotnet test DynamicDnsUpdater.API.UnitTests/DynamicDnsUpdater.API.UnitTests.csproj --filter "FullyQualifiedName~DnsRecordServiceTests"
```

### Integration Tests

```bash
# Run all integration tests
dotnet test DynamicDnsUpdater.API.IntegrationTests/DynamicDnsUpdater.API.IntegrationTests.csproj

# Run specific test
dotnet test DynamicDnsUpdater.API.IntegrationTests/DynamicDnsUpdater.API.IntegrationTests.csproj --filter "FullyQualifiedName~DnsRecordsApiTests"
```

### All Tests

```bash
# Run all tests in solution
dotnet test DynamicDnsUpdater.API.slnx
```

## Coverage Targets

| Metric | Target | Current |
|--------|--------|---------|
| Line Coverage | >80% | TBD |
| Branch Coverage | >70% | TBD |
| Method Coverage | >90% | TBD |

## CI/CD Integration

### GitHub Actions

```yaml
# .github/workflows/dotnet.yml
- name: Run Tests
  run: dotnet test DynamicDnsUpdater.API.slnx --no-build --verbosity normal

- name: Upload Coverage
  uses: codecov/codecov-action@v3
  with:
    files: ./coverage.cobertura.xml
```

## Test Maintenance

### Adding New Tests

1. **Identify the behavior** to test
2. **Choose test type** (unit vs integration)
3. **Follow naming convention**
4. **Use existing patterns** and helpers
5. **Add test data** to standard values if needed
6. **Run tests** to verify

### Updating Tests

1. **Update test** when behavior changes
2. **Don't change tests** to make them pass
3. **Refactor tests** when patterns improve
4. **Remove obsolete tests** when behavior is removed

### Test Quality Checklist

- [ ] Test name follows convention
- [ ] Test is isolated (no external dependencies)
- [ ] Test is deterministic
- [ ] Test covers happy path
- [ ] Test covers error paths
- [ ] Test covers edge cases
- [ ] Test uses standard test values
- [ ] Test verifies logging/observability
- [ ] Test runs fast (<100ms for unit)
- [ ] Test doesn't duplicate coverage

## Future Testing Improvements

1. **Contract Testing:** Add Pact tests for provider API contracts
2. **Performance Testing:** Add load tests for high-throughput scenarios
3. **Chaos Testing:** Add resilience tests for network failures
4. **Mutation Testing:** Add Stryker.NET for test quality validation
5. **Property-Based Testing:** Add FsCheck for edge case discovery
6. **Snapshot Testing:** Add snapshot tests for response formats
7. **End-to-End Testing:** Add Playwright tests for full workflows