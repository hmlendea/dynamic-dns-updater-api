# Privacy and Personal Data

This document describes how the Dynamic DNS Updater API handles personal data. It covers the application behaviour and verified integrations for self-hosted deployments. Instance operators control their instance's configuration, local storage, logs, backups, access controls, retention, and request handling.

## 📑 Table of Contents

- [What This Document Covers](#-what-this-document-covers)
- [Self-Hosted Deployments](#-self-hosted-deployments)
- [Data We Handle](#-data-we-handle)
- [Processing and Use](#-processing-and-use)
- [Storage, Retention, and Deletion](#-storage-retention-and-deletion)
- [External Processing and Integrations](#-external-processing-and-integrations)
- [Data Protection and Security](#-data-protection-and-security)
- [Document Changes](#-document-changes)
- [Contact](#-contact)

## 🔎 What This Document Covers

This document describes how Dynamic DNS Updater API at https://github.com/hmlendea/dynamic-dns-updater-api handles personal data. It covers the application behaviour and verified integrations described below. Where the software is self-hosted, the instance operator may have separate responsibilities described below.

## 🏠 Self-Hosted Deployments

This document covers the application behaviour as implemented in the repository. Self-hosted instance operators control their instance's configuration, local storage, logs, backups, access controls, retention, and request handling. The project maintainers do not operate a central service and do not receive data from self-hosted instances.

The application sends the following data from a self-hosted instance to external services:
- DNS A-record queries to Quad9 DNS resolvers at `9.9.9.9` and `149.112.112.112` to check whether a domain already resolves to the requested IPv4 address.
- DNS update requests to Gandi LiveDNS API at `api.gandi.net` when the configured provider is Gandi.

Operators can disable the Quad9 optimisation check by modifying the application code. Operators can avoid Gandi data transfer by not configuring a Gandi API key or by implementing a different provider integration. No telemetry, update checks, crash reports, or other data are sent to project maintainers.

## 📥 Data We Handle

### Data Provided to the Application

- **API keys**: The instance operator provides an API key for client authorisation (`securitySettings.apiKey`) and a Gandi LiveDNS API key (`gandiSettings.apiKey`) through configuration. These are secrets managed by the operator.
- **Domain names**: Clients provide a domain name (including subdomain, e.g., `home.example.com`) in the request path.
- **IP addresses**: Clients provide an IPv4 address in the request body.
- **DNS provider name**: Clients specify the DNS provider (currently only `Gandi` is supported) in the request body.

### Data Generated or Collected by the Application

- **Structured operation logs**: NuciLog generates log entries for each DNS update operation, including the operation name (`UpdateDnsRecord`), status (`Started`, `Success`, `Failure`), domain name, IP address, and provider name. Logs are written to a file when `nuciLoggerSettings.isFileOutputEnabled` is `true` and `nuciLoggerSettings.logFilePath` is configured.
- **DNS query data**: The application queries Quad9 DNS resolvers for the A record of the requested domain to check current resolution. The domain name is sent to Quad9; the operator's IP address is visible to Quad9 as the query source.

### Data Received from Integrations

- **Gandi LiveDNS API responses**: The application receives HTTP responses from the Gandi API containing status codes and, on error, error messages. These responses may include the domain, subdomain, and IP address that were sent in the request.
- **Quad9 DNS responses**: The application receives DNS A-record responses from Quad9 containing the currently resolved IP addresses for the queried domain.

## 🧭 Processing and Use

The application processes the data described above for these verified functions:
- **Client authorisation** — API key from configuration and request headers
- **Request validation** — Domain format, IP address format, provider name, and NuciAPI protocol headers
- **DNS resolution check** — Domain name sent to Quad9 DNS to avoid unnecessary provider updates
- **DNS record update** — Domain name, subdomain, IP address, and Gandi API key sent to Gandi LiveDNS API
- **Operation logging** — Domain name, IP address, provider name, and operation status logged via NuciLog

## 🗄️ Storage, Retention, and Deletion

- **Configuration**: API keys and settings are stored in the ASP.NET Core configuration system (typically `appsettings.json`, environment variables, or other configured sources). The instance operator controls configuration storage, access, and rotation.
- **Logs**: When file output is enabled, NuciLog writes structured log entries to the file specified by `nuciLoggerSettings.logFilePath`. The instance operator controls log file retention, rotation, backup, and deletion. No automatic log retention or deletion is implemented by the application.
- **In-memory data**: Request data (domain name, IP address, provider) is held in memory only for the duration of the request processing.
- **No database**: The application does not use a database or persistent data store for request data.
- **Backups**: The instance operator is responsible for any backups of configuration files, log files, or the deployment environment.

For self-hosted deployments, the instance operator controls all storage, deletion, and backup decisions.

## 🔗 External Processing and Integrations

| Service or integration | Purpose | Data involved | Configuration or documentation |
|-----------------------|---------|---------------|--------------------------------|
| Quad9 DNS (`9.9.9.9`, `149.112.112.112`) | DNS A-record lookup to check if domain already resolves to requested IP | Domain name (queried); operator's source IP visible to Quad9 | Hardcoded in `DnsRecordService.cs`; see [Quad9](https://www.quad9.net/) |
| Gandi LiveDNS API (`api.gandi.net`) | Update DNS A record for the specified domain and subdomain | Domain, subdomain, IP address, Gandi API key (Authorization header) | Requires `gandiSettings.apiKey`; see [Gandi LiveDNS API](https://api.gandi.net/docs/livedns/) |

The application has no other built-in external data transfers.

## 🛡️ Data Protection and Security

- **Transport encryption**: The application enforces HTTPS redirection (`app.UseHttpsRedirection()`). All external API calls (Quad9 DNS over UDP/TCP, Gandi LiveDNS over HTTPS) use standard transport encryption where supported by the target service.
- **Authentication**: Client requests require a valid API key configured by the operator. The NuciAPI middleware validates request headers, timestamps, and provides replay protection and scanner protection.
- **Secrets management**: API keys are read from configuration. The instance operator is responsible for providing secrets through secure configuration sources (environment variables, secret managers, etc.) and not committing them to source control.
- **Access controls**: The instance operator controls network exposure, firewall rules, reverse proxy configuration, and host-level access to the application.
- **Logging**: Structured logs include domain names, IP addresses, and provider names. The instance operator controls log file permissions, access, and retention.
- **No absolute security**: This description covers implemented technical controls. The instance operator is responsible for deployment hardening, updates, secret rotation, access management, backup protection, and log security.

## 🔄 Document Changes

Update this document when application data flows, storage, integrations, or deployment responsibilities change. The current version is published at https://github.com/hmlendea/dynamic-dns-updater-api/blob/master/PRIVACY.md.

## 📬 Contact

For questions about application data handling, contact the project maintainers at https://github.com/hmlendea/dynamic-dns-updater-api. For a self-hosted instance, contact the instance operator, unless the project explicitly handles the request. Include the domain name and approximate request time to identify the deployment or data flow; do not send passwords, access tokens, or other secrets.