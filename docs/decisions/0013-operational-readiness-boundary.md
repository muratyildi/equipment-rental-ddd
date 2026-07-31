# ADR-0013 — Make Operational Readiness Boundaries Explicit

- Status: Accepted
- Date: 2026-07-31

## Context

Reliable publication and long-running workflows existed, but unbounded retry,
missing correlation, ambiguous readiness, and unprotected business endpoints
left production behavior unclear.

## Decision

- Use JSON structured logs and safe correlation IDs.
- Keep vendor-neutral `ActivitySource`/`Meter` instrumentation in Building
  Blocks; exporters are deployment adapters.
- Separate liveness from database and operational readiness.
- Dead-letter Outbox messages after five failures.
- Move Process Managers to `RequiresIntervention` after five technical errors.
- Protect business endpoints with API keys, read/write scopes, and rate limits.
- Run the API, migration job, and PostgreSQL through local Compose.

## Consequences

Positive:

- Failures are traceable, poison items cannot retry forever, orchestrators get
  accurate health signals, business endpoints default to deny, and migrations
  are visible deployment steps.

Negative:

- Dead-letter/intervention need runbooks and replay/resume controls.
- API keys do not model human authorization.
- Metrics need an exporter, and Compose values are not production secrets.

## Reconsider when

- Human users or tenants require OIDC/OAuth 2.0.
- SLO/traffic data can tune rate limits.
- A telemetry backend selects an OTLP/exporter adapter.
- Operational ownership can define replay/resume and alert runbooks.

## Evidence

- Outbox dead-letter and Process Manager intervention integration tests
- API-key validator, endpoint policy, and correlation middleware tests
- Five DbContext migration/readiness checks and Compose validation
