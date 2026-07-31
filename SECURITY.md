# Security Policy

## Supported versions

Until the first stable release, security fixes target the latest release
candidate on the `main` branch. After `v1.0.0`, only the latest minor release
will receive fixes unless a later policy states otherwise.

## Reporting a vulnerability

Do not open a public issue for a suspected vulnerability or exposed secret.
Use GitHub Private Vulnerability Reporting after it is enabled for the public
repository. If that feature is unavailable, contact the repository owner
privately through their GitHub profile.

Please include:

- the affected version or commit;
- reproduction steps;
- expected impact;
- whether credentials or personal data may be exposed;
- a suggested mitigation, if known.

The initial response target is five business days. A report will be
acknowledged before details or a remediation timeline are published.

## Demonstration credentials

Credentials in `compose.yaml`, `appsettings.Development.json`, and documented
local examples are intentionally development-only values. They must never be
used in a shared or production environment. Real API keys belong in a secret
manager or deployment environment and must not be committed.
