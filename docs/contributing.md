# Contributing

Thank you for your interest in contributing to the Dynamic DNS Updater API!

## Code of Conduct

This project follows our [Code of Conduct](CODE_OF_CONDUCT.md). By participating, you agree to uphold this code.

## How to Contribute

### Reporting Bugs

1. Check existing issues first
2. Create a new issue with:
   - Clear title
   - Steps to reproduce
   - Expected vs actual behavior
   - Environment details
   - Logs (if applicable)

### Suggesting Features

1. Check existing issues and roadmap
2. Create a feature request issue with:
   - Clear description
   - Use case
   - Proposed solution (if any)
   - Alternatives considered

### Pull Requests

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/your-feature`
3. Make your changes
4. Run tests: `dotnet test`
5. Commit with clear messages
6. Push to your fork
7. Create a Pull Request

## Development Setup

### Prerequisites

- .NET 10.0 SDK
- Git
- IDE (VS Code, Visual Studio, Rider)

### Clone and Build

```bash
git clone https://github.com/your-org/dynamic-dns-updater-api.git
cd dynamic-dns-updater-api
dotnet restore
dotnet build
```

### Run Tests

```bash
# All tests
dotnet test

# Unit tests only
dotnet test DynamicDnsUpdater.API.UnitTests

# Integration tests only
dotnet test DynamicDnsUpdater.API.IntegrationTests
```

### Run Locally

```bash
# Create development config
cat > DynamicDnsUpdater.API/appsettings.Development.json << EOF
{
  "SecuritySettings": { "ApiKey": "dev-api-key" },
  "GandiSettings": { "ApiKey": "dev-gandi-key" }
}
EOF

# Run
dotnet run --project DynamicDnsUpdater.API
```

## Coding Standards

### C# Style

- Follow [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use `var` when type is obvious
- Prefer expression-bodied members
- Use nullable reference types
- Enable `<Nullable>enable</Nullable>` in project

### Naming

| Element | Convention |
|---------|------------|
| Classes, Interfaces, Methods, Properties | PascalCase |
| Parameters, Local Variables | camelCase |
| Constants | PascalCase |
| Private Fields | _camelCase |
| Test Methods | Given_When_Then |

### File Organization

- One type per file
- File name matches type name
- Group related files in folders
- Use namespaces matching folder structure

### Async/Await

- Use `async`/`await` for all async operations
- Avoid `.Result` and `.Wait()`
- Use `ConfigureAwait(false)` in library code
- Return `Task`/`Task<T>` not `void`

### Error Handling

- Let exceptions propagate to middleware
- Log at operation boundaries
- Use structured logging
- Don't catch and swallow

### Logging

- Use NuciLog operation-based logging
- Log at Start/Success/Failure
- Include structured context
- Use MyOperation and MyLogInfoKey

## Testing Standards

### Unit Tests

- Test single class/method
- Mock all dependencies
- Use Given/When/Then naming
- Cover happy path and error cases
- Verify logging calls

### Integration Tests

- Test full HTTP pipeline
- Use WebApplicationFactory
- Mock external services
- Test authentication/authorization
- Test error responses

### Test Data

- Use standard test values
- `www.example.com` for domains
- `192.0.2.1` for IPv4 (TEST-NET-1)
- `test-api-key` for API keys

## Commit Messages

Follow [Conventional Commits](https://www.conventionalcommits.org/):

```
type(scope): description

[optional body]

[optional footer]
```

### Types

| Type | Description |
|------|-------------|
| feat | New feature |
| fix | Bug fix |
| docs | Documentation |
| style | Formatting |
| refactor | Code restructuring |
| test | Test changes |
| chore | Maintenance |

### Examples

```
feat(api): add IPv6 support for AAAA records

fix(gandi): handle rate limit responses correctly

docs(readme): update quick start guide

test(service): add DNS verification failure tests
```

## Pull Request Process

### Before Submitting

- [ ] Tests pass locally
- [ ] Code follows style guidelines
- [ ] Self-review completed
- [ ] Documentation updated
- [ ] Changelog entry added (if applicable)

### PR Requirements

- Clear title and description
- Linked issue (if applicable)
- Small, focused changes
- No unrelated changes
- CI passes

### Review Process

1. Automated checks run
2. Maintainer reviews code
3. Address feedback
4. Approve and merge

## Adding a New DNS Provider

### Steps

1. **Add to DnsProvider enum:**
   ```csharp
   public enum DnsProvider
   {
       Gandi,
       Cloudflare,  // New
       Route53      // New
   }
   ```

2. **Create interface:**
   ```csharp
   public interface ICloudflareService
   {
       Task Update(string domainName, string ipAddress);
   }
   ```

3. **Implement service:**
   ```csharp
   public class CloudflareService : ICloudflareService
   {
       // Implementation
   }
   ```

4. **Register in DI:**
   ```csharp
   services.AddTransient<ICloudflareService, CloudflareService>();
   ```

5. **Update DnsRecordService:**
   ```csharp
   var provider = DnsProvider.FromString(dnsProviderName);
   return provider switch
   {
       DnsProvider.Gandi => gandiService,
       DnsProvider.Cloudflare => cloudflareService,
       _ => throw new NotImplementedException()
   };
   ```

6. **Add tests:**
   - Unit tests for service
   - Integration tests for API

7. **Update documentation:**
   - README.md
   - Configuration docs
   - API reference

## Architecture Guidelines

### Layer Boundaries

```
Controllers → Services → Integrations → External APIs
```

- Controllers: HTTP concerns only
- Services: Business logic, orchestration
- Integrations: Provider-specific code
- Models: Shared data structures

### Dependency Direction

```
Controllers → Services → Integrations
     ↓           ↓            ↓
  Interfaces  Interfaces   Interfaces
```

- Depend on abstractions, not concretions
- Use interfaces for all external dependencies
- Register interfaces in DI container

### Cross-Cutting Concerns

Handled by NuciAPI middleware:
- Exception handling
- Authentication/Authorization
- Replay protection
- Scanner protection
- Request logging
- Header validation
- HTTPS redirection

**Don't implement these in application code.**

## Documentation

### When to Update

- New features
- Changed behavior
- New configuration options
- New API endpoints
- Breaking changes

### Where to Update

| Change | Files |
|--------|-------|
| New feature | README.md, relevant docs/ |
| API change | docs/api-reference/ |
| Config change | docs/configuration.md |
| Architecture change | docs/architecture.md, docs/design-decisions.md |
| Deployment change | docs/build-and-deployment.md |

## Release Process

### Versioning

Semantic Versioning (MAJOR.MINOR.PATCH):
- MAJOR: Breaking changes
- MINOR: New features (backward compatible)
- PATCH: Bug fixes (backward compatible)

### Release Steps

1. Update version in `.csproj`
2. Update CHANGELOG.md
3. Create release branch
4. Run full test suite
5. Build and publish
6. Create GitHub release
7. Deploy to staging
8. Validate
9. Deploy to production

## Getting Help

- **Questions:** GitHub Discussions
- **Bugs:** GitHub Issues
- **Features:** GitHub Issues
- **Security:** See SECURITY.md

## Recognition

Contributors are recognized in:
- GitHub contributors graph
- Release notes
- README.md (optional)

Thank you for contributing!