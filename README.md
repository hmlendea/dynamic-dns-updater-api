[![Donate](https://img.shields.io/badge/-%E2%99%A5%20Donate-%23ff69b4)](https://hmlendea.go.ro/fund.html) [![Latest Release](https://img.shields.io/github/v/release/hmlendea/dynamic-dns-updater-api)](https://github.com/hmlendea/dynamic-dns-updater-api/releases/latest) [![Build Status](https://github.com/hmlendea/dynamic-dns-updater-api/actions/workflows/dotnet.yml/badge.svg)](https://github.com/hmlendea/dynamic-dns-updater-api/actions/workflows/dotnet.yml)

# Dynamic DNS Updater API

Lightweight ASP.NET Core API for updating DNS records through DNS provider integrations.

## Features

- Update DNS records over HTTP
- API-key protected endpoint
- Replay protection and request logging
- Provider-based integration architecture
- Gandi LiveDNS integration included

## Requirements

- .NET SDK 10.0

## Getting Started

1. Clone the repository.
2. Configure settings in appsettings.json.
3. Run the API:

```bash
dotnet run
```

By default, the API listens on the local ASP.NET Core development URLs.

## Configuration

The app reads settings from appsettings.json.

```json
{
	"securitySettings": {
		"apiKey": "your-api-key"
	},
	"gandiSettings": {
		"apiKey": "your-gandi-api-key"
	},
	"nuciLoggerSettings": {
		"logFilePath": "logfile.log",
		"isFileOutputEnabled": true
	}
}
```

### Settings

- securitySettings.apiKey: API key used to authorize client requests.
- gandiSettings.apiKey: Gandi LiveDNS API key.
- nuciLoggerSettings: NuciLog configuration.

## API

### Update DNS record

- Method: PUT
- Route: /DnsRecords/{domainName}
- Body:

```json
{
	"ip": "203.0.113.42",
	"provider": "Gandi"
}
```

### Example

```bash
curl -X PUT "https://localhost:5001/DnsRecords/home.example.com" \
	-H "Content-Type: application/json" \
	-d '{"ip":"203.0.113.42","provider":"Gandi"}'
```

Include the authorization and anti-replay headers expected by the configured NuciAPI middleware.

## Notes

- Currently supported provider: Gandi
- Domain must include a subdomain (for example: home.example.com)
- The integration updates an A record with TTL 300

## Development

Build:

```bash
dotnet build
```

Run tests (if added later):

```bash
dotnet test
```

## Release

Use the helper script:

```bash
./release.sh
```

## License

This project is licensed under the GPLv3 License. See `LICENSE` for details.
