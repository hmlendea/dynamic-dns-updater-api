# Security Policy

This policy covers security vulnerabilities in Dynamic DNS Updater API v1.2.4 distributed through GitHub Releases. Reports concerning API-key authorisation, request validation, replay protection, secret handling, DNS update integrity, and security-relevant dependencies are welcome through the private reporting channels below.

## 📑 Table of Contents

- [Supported Versions](#-supported-versions)
- [Reporting a Vulnerability](#-reporting-a-vulnerability)
- [Scope](#-scope)
- [Disclosure Policy](#-disclosure-policy)

## 🛡️ Supported Versions

Use this table to indicate which project versions currently receive security maintenance.

| Version | Distribution Channel | Supported |
|---------|--------------------|-----------|
| Latest version | GitHub Releases | ✅ |
| Preceding versions | Any distribution channel | ❌ |

## 🚨 Reporting a Vulnerability

Please do not disclose suspected vulnerabilities publicly before maintainers have had an opportunity to validate and remediate them.

To report a vulnerability:
- [GitHub Security Advisories](https://github.com/hmlendea/dynamic-dns-updater-api/security/advisories)
- Contact the maintainers directly through the repository's private communication channels

## 📌 Scope

The subsequent report categories are in scope for this repository:
- API-key authentication and authorisation bypasses
- Request validation, replay protection, and DNS update integrity
- Accidental exposure of configured secrets or sensitive operational data
- Security vulnerabilities in dependencies that affect the deployed API

The subsequent categories are out of scope unless explicitly stated to the contrary:
- Vulnerabilities in third-party services, including DNS providers, that are not caused by this repository
- Issues requiring already-compromised credentials or access to the deployment environment

## 📢 Disclosure Policy

This project follows coordinated disclosure:
1. Vulnerabilities are investigated privately.
2. A remediation plan is prepared and validated.
3. Public disclosure is published after a fix, mitigation, or agreed risk decision is available.
4. Credit is attributed in accordance with reporter preference and project policy.
