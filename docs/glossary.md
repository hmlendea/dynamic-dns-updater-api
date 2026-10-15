# Glossary

## Terms

| Term | Definition |
|------|------------|
| **A Record** | DNS record type mapping a domain name to an IPv4 address |
| **API Key** | Secret token used for request authentication |
| **Bearer Token** | Authentication scheme where token is sent in Authorization header |
| **DNS** | Domain Name System - translates domain names to IP addresses |
| **DNS Provider** | Service managing DNS records (e.g., Gandi, Cloudflare, Route53) |
| **Dynamic DNS** | Service automatically updating DNS records when IP changes |
| **Gandi LiveDNS v5** | Gandi's REST API for DNS management |
| **HMAC** | Hash-based Message Authentication Code for request signing |
| **IPv4** | Internet Protocol version 4 (32-bit addresses) |
| **IPv6** | Internet Protocol version 6 (128-bit addresses) |
| **Quad9** | Public DNS resolver service (9.9.9.9, 149.112.112.112) |
| **Replay Attack** | Attack reusing valid request to cause duplicate actions |
| **Scanner** | Automated tool probing for vulnerabilities |
| **Subdomain** | Domain part before the main domain (e.g., `www` in `www.example.com`) |
| **TTL** | Time To Live - DNS record cache duration in seconds |
| **Webhook** | HTTP callback for event notifications |

## Acronyms

| Acronym | Expansion |
|---------|-----------|
| **API** | Application Programming Interface |
| **DNS** | Domain Name System |
| **HTTP** | Hypertext Transfer Protocol |
| **HTTPS** | HTTP Secure |
| **IP** | Internet Protocol |
| **JSON** | JavaScript Object Notation |
| **REST** | Representational State Transfer |
| **SDK** | Software Development Kit |
| **SLA** | Service Level Agreement |
| **TTL** | Time To Live |
| **UTC** | Coordinated Universal Time |
| **UUID** | Universally Unique Identifier |

## Project-Specific Terms

| Term | Definition |
|------|------------|
| **DnsRecordService** | Application service orchestrating DNS updates |
| **GandiService** | Provider integration for Gandi LiveDNS API |
| **DnsProvider** | Enumeration of supported DNS providers |
| **MyOperation** | NuciLog operation identifiers |
| **MyLogInfoKey** | NuciLog structured logging keys |
| **NuciAPI** | Middleware framework for cross-cutting concerns |
| **NuciLog** | Structured logging framework |
| **ProcessRequestAsync** | Controller method handling authenticated requests |
| **Quad9LookupClient** | Static DNS client for verification queries |