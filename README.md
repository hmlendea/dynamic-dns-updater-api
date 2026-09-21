[![Donate](https://img.shields.io/badge/-%E2%99%A5%20Donate-%23ff69b4)](https://hmlendea.go.ro/funding)
[![Latest Release](https://img.shields.io/github/v/release/hmlendea/dynamic-dns-updater-api)](https://github.com/hmlendea/dynamic-dns-updater-api/releases/latest)
[![Build Status](https://github.com/hmlendea/dynamic-dns-updater-api/actions/workflows/dotnet.yml/badge.svg)](https://github.com/hmlendea/dynamic-dns-updater-api/actions/workflows/dotnet.yml)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://gnu.org/licenses/gpl-3.0)

# Dynamic DNS Updater API

Lightweight ASP.NET Core API for updating DNS records through DNS provider integrations.

## 📑 Table of Contents

- [Table of Contents](#-table-of-contents)
- [Capabilities](#-capabilities)
- [Usage](#-usage)
- [System Requirements](#-system-requirements)
- [Installation](#-installation)
- [Configuration](#-configuration)
- [Integrations](#-integrations)
- [Authentication and Authorisation](#-authentication-and-authorisation)
- [Development](#-development)
  - [Requirements](#requirements)
  - [Setup](#setup)
  - [Build](#build)
  - [Run](#run)
  - [Test](#test)
  - [Release](#release)
  - [Dependencies](#dependencies)
- [GitHub Actions](#-github-actions)
- [Project Structure](#-project-structure)
  - [Projects and Packages](#projects-and-packages)
  - [Directories](#directories)
- [Architecture](#-architecture)
- [Deployment](#-deployment)
- [Related Projects](#-related-projects)
- [Security](#-security)
- [Contributing](#-contributing)
- [Project Engagement](#-project-engagement)
- [License](#-license)

## ✨ Capabilities

- Update DNS records over HTTP.
- Skip a provider update when public DNS already resolves the requested IPv4 address.
- Protect the update endpoint with API-key authorisation and NuciAPI request controls.
- Prevent replayed requests and record operation status through NuciAPI middleware and NuciLog.
- Integrate with Gandi LiveDNS; the current implementation supports the `Gandi` provider.

## 🚀 Usage

Start the API after configuring its settings, then send a `PUT` request to `/DnsRecords/{domainName}` with the required NuciAPI headers.

```bash
dotnet run
```

The update request body is:

```json
{
	"ip": "203.0.113.42",
	"provider": "Gandi"
}
```

For example:

```bash
curl -X PUT "https://localhost:5001/DnsRecords/home.example.com" \
	-H "Content-Type: application/json" \
	-d '{"ip":"203.0.113.42","provider":"Gandi"}'
```

The domain must include a subdomain, such as `home.example.com`. The Gandi integration updates an A record with TTL 300.

## 🖥️ System Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Network access to the API clients, Quad9 DNS resolvers, and Gandi LiveDNS.

## 📦 Installation

Clone the repository and start the API from the repository root:

```bash
git clone https://github.com/hmlendea/dynamic-dns-updater-api.git
cd dynamic-dns-updater-api
dotnet run --project DynamicDnsUpdater.API/DynamicDnsUpdater.API.csproj
```

## ⚙️ Configuration

The API reads the `securitySettings`, `gandiSettings`, and `nuciLoggerSettings` sections from [appsettings.json](DynamicDnsUpdater.API/appsettings.json) and the standard ASP.NET Core configuration sources.

The recognised settings are:

| Section | Key | Required | Description |
|---------|-----|----------|-------------|
| `securitySettings` | `apiKey` | Yes | API key used to authorise client requests. |
| `gandiSettings` | `apiKey` | Yes | Gandi LiveDNS API key. |
| `nuciLoggerSettings` | `logFilePath` | No | NuciLog file destination when file output is enabled. |
| `nuciLoggerSettings` | `isFileOutputEnabled` | No | Controls NuciLog file output. |

Provide API keys through deployment configuration or another protected configuration source. Do not commit real credentials.

## 🔌 Integrations

| Integration | Compatibility | Purpose | Required |
|-------------|---------------|---------|----------|
| Gandi LiveDNS | Current `Gandi` provider implementation | Updates the requested DNS A record through the Gandi LiveDNS API | Required for Gandi updates |
| Quad9 DNS | DNS A-record queries to `9.9.9.9` and `149.112.112.112` | Avoids an unnecessary provider update when the requested IPv4 address is already resolved | Used by the optimisation path |

## 🔐 Authentication and Authorisation

The DNS update endpoint requires the configured API key and NuciAPI protocol headers. Requests also pass NuciAPI header validation, timestamp validation, replay protection, and scanner protection before reaching the controller.

Clients must provide the authorisation and anti-replay headers expected by the configured NuciAPI middleware. Refer to the client integration for the exact header construction; credentials and tokens must not be placed in source control.

## 🛠️ Development

### Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Setup

Restore dependencies from the repository root:

```bash
dotnet restore DynamicDnsUpdater.API.slnx
```

### Build

```bash
dotnet build DynamicDnsUpdater.API.slnx
```

### Run

```bash
dotnet run --project DynamicDnsUpdater.API/DynamicDnsUpdater.API.csproj
```

### Test

```bash
dotnet test DynamicDnsUpdater.API.slnx
```

### Release

The repository includes `release.sh`, which delegates to the upstream deployment script used by the project maintainer.

```bash
bash ./release.sh 1.0.0
```

This script downloads and executes an external release helper from `https://raw.githubusercontent.com/hmlendea/deployment-scripts/master/release/dotnet/10.0.sh`.

**Note:** Piping into `bash` is an intensely controversial topic. Please review any external scripts before running them in your environment!

### Dependencies

| Package | Version | Scope | Purpose |
|---------|---------|-------|---------|
| ASP.NET Core | .NET 10 | Runtime | Hosts the HTTP API and middleware pipeline. |
| DnsClient | 1.8.0 | Runtime | Queries public DNS A records. |
| NuciAPI packages | 2.0.3-3.6.1 | Runtime | Provide request models, controller processing, middleware, security, logging, and exception handling. |
| NuciLog packages | 1.2.1-3.1.0 | Runtime | Provide structured operation logging. |
| NUnit, Moq, and Microsoft.NET.Test.Sdk | Project-defined versions | Development | Support unit and integration tests. |

## ⚙️ GitHub Actions

| Workflow | Purpose | What it does |
|----------|---------|--------------|
| [dotnet.yml](https://github.com/hmlendea/dynamic-dns-updater-api/blob/master/.github/workflows/dotnet.yml) | .NET validation | Runs the repository's automated .NET workflow. |

## 🗂️ Project Structure

The solution contains the API and its unit and integration test projects.

### Projects and Packages

| Project | Type | Purpose |
|---------|------|---------|
| `DynamicDnsUpdater.API` | ASP.NET Core web project | Hosts the DNS update API and provider integrations. |
| `DynamicDnsUpdater.API.UnitTests` | NUnit test project | Verifies application services, provider models, and Gandi integration behaviour. |
| `DynamicDnsUpdater.API.IntegrationTests` | NUnit integration-test project | Verifies HTTP routing, binding, middleware security, replay protection, and error responses. |

### Directories

| Directory | Purpose |
|-----------|---------|
| `DynamicDnsUpdater.API/Controllers` | HTTP controllers and route handling. |
| `DynamicDnsUpdater.API/Service` | DNS update orchestration and provider contracts. |
| `DynamicDnsUpdater.API/Service/Integrations` | Provider-specific integrations. |
| `DynamicDnsUpdater.API/Configuration` | Configuration models. |
| `DynamicDnsUpdater.API.UnitTests` | Unit verification. |
| `DynamicDnsUpdater.API.IntegrationTests` | HTTP and middleware verification. |

## 🏗️ Architecture

See the [architecture documentation](ARCHITECTURE.md) for the system context, principal components, runtime flows, ownership boundaries, dependencies, constraints, and extension points.

## 🚢 Deployment

Deploy the API as an ASP.NET Core process with protected configuration containing the API and Gandi credentials. The deployment requires outbound network access to Quad9 and Gandi LiveDNS, and should route client traffic through HTTPS.

The application does not own a database or durable local DNS state. Configure log storage and retention according to the host environment.

## 🔗 Related Projects

- [Dynamic DNS Updater Client](https://github.com/hmlendea/dynamic-dns-updater-client): client for interacting with this API.

## 🔒 Security

For information on reporting security vulnerabilities, see [SECURITY.md](SECURITY.md).

## 🤝 Contributing

You are welcome to submit any suggestion, feedback, or modification to this project.

When doing so, please:
- Maintain cross-platform compatibility
- Preserve the existing public contract unless a breaking change is intentional
- Submit focused pull requests that conform to the existing code style
- Maintain your branch synchronised with `master`
- Revise the documentation when functionality changes
- Properly test all modifications, including edge cases and error conditions
- Add tests for additional or modified functionality
- Raise a new [issue](https://github.com/hmlendea/dynamic-dns-updater-api/issues) for problems or suggestions

## 💝 Project Engagement

Discovered a problem or have a suggestion? [Open an issue](https://github.com/hmlendea/dynamic-dns-updater-api/issues)!

If you find this project useful, consider funding it or starring ⭐️ it on GitHub!

[![Donate](https://raw.githubusercontent.com/hmlendea/readme-assets/master/donate_generic.png)](https://hmlendea.go.ro/funding)

## 📄 License

This project is being distributed under the `GNU General Public License v3.0` or later.
See [LICENSE](LICENSE) for further information.