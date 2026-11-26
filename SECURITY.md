# Security Policy

## Supported versions

Runesvaner is pre-1.0. Security fixes are applied to the latest preview line only until 1.0.

## Reporting a vulnerability

Do **not** open a public issue for a security vulnerability.

Use GitHub's private vulnerability reporting:

1. Open the repository's **Security** tab.
2. Select **Report a vulnerability**.
3. Include affected version, reproduction steps, and impact.

If private reporting is unavailable, contact the maintainer through GitHub (`@karlmoriaty`) and ask for a private channel. Do not include secrets, tokens, or customer data in the first message.

## Response targets

- Initial acknowledgement: within 48 hours.
- Triage and severity assessment: within 5 business days.
- Fix target: P0 within one release cycle; P1 within two release cycles.

## Scope

In scope:

- Runesvaner packages and CLI.
- Local Studio authentication and isolation.
- Content capture, redaction, and export behavior.
- Build and release supply chain.

Out of scope:

- Vulnerabilities in third-party providers or model services.
- Issues caused by running Studio on a public interface against the documented warning.
- Vulnerabilities in unsupported .NET versions.

## Disclosure

We prefer coordinated disclosure. After a fix is available, we publish a GitHub Security Advisory and credit the reporter if they agree.