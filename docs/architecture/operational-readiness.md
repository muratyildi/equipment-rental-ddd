# Operational Readiness: Observability, Resilience, and Security

The goal of this milestone is not to apply a vague “production-ready” label.
It is to make failures diagnosable, distinguish transient from terminal
failures, and protect business endpoints by default.

## Observability

The system treats observability as three complementary signals:

- **Logs** record discrete events with structured fields.
- **Traces** describe the causal path of a request and its child operations.
- **Metrics** describe numerical behavior over time.

### Correlation

Every HTTP request has an `X-Correlation-ID`. A caller-supplied value is
preserved only when it contains safe characters and is at most 128 characters;
otherwise the current trace identifier or a new GUID is used.

The selected value is:

- returned in the response header;
- assigned to `HttpContext.TraceIdentifier`;
- added to the structured logging scope as `CorrelationId`; and
- included in Problem Details and health responses.

Whitespace, line breaks, and control characters are rejected to prevent a
caller-controlled value from becoming a log-injection vector.

### Structured logs and telemetry APIs

Production console logs use JSON. Background workers use message templates
instead of interpolated strings so fields remain searchable.

The Eventing building block exposes vendor-neutral .NET `ActivitySource` and
`Meter` APIs. It records Outbox publication, failure, and dead-letter signals,
plus Process Manager transitions and interventions. A deployment can attach
OpenTelemetry, Application Insights, or another listener in the composition
root without adding exporter dependencies to Domain projects.

### Liveness and readiness

```text
GET /health/live
GET /health/ready
```

Liveness answers only whether the process can respond. A database outage does
not fail liveness, avoiding an orchestrator restart storm.

Readiness checks PostgreSQL connectivity for all five DbContexts:

- no connection returns `Unhealthy / 503`;
- connectivity with dead-lettered work or a `RequiresIntervention` process
  returns `Degraded / 200`; and
- no detected problem returns `Healthy / 200`.

A degraded instance can still receive traffic but requires an operational
alert. Health endpoints are anonymous and exempt from application rate limits
so an ingress or network policy must expose them only to the control plane.

## Resilience

### Bounded retries

A retry is safe only when the operation is idempotent. Outbox publication and
Process Manager steps use stable message or demand identifiers and can
therefore be retried.

Retries are bounded:

| Operation | Budget | Terminal state |
| --- | ---: | --- |
| Outbox publication | 5 | `DeadLetteredAtUtc` |
| Process Manager technical failure | 5 | `RequiresIntervention` |

Outbox retries use exponential backoff and never automatically reclaim a
dead-lettered message. The worker does not claim a Process Manager in its
terminal intervention state. A poison item therefore cannot occupy the worker
forever.

A terminal state is not automatic deletion. An operator must fix the cause and
use a controlled replay or resume capability. Editing status fields directly
in the database is not the recovery mechanism.

### Rate limiting

Business endpoints allow 100 requests per minute in a fixed window, partitioned
by authenticated principal or client IP. Exceeding the limit returns
`429 Too Many Requests`. This is a first abuse-control layer; production
values must be calibrated from traffic and SLO evidence.

Rate limiting does not replace domain concurrency control. PostgreSQL `xmin`
checks and Aggregate invariants remain the final consistency boundary.

## Security

The current domain has no human-user, tenant, or Identity bounded context.
Inventing a fake role model would add ceremony without a requirement, so the
API uses machine-to-machine API-key authentication.

Two authorization scopes exist:

- `equipment-rental.read`
- `equipment-rental.write`

GET business endpoints require read access; state-changing endpoints require
write access. Keys are compared in constant time using SHA-256 digests.
Production keys are supplied through environment variables or a secret store
and are never committed.

Development keys are for local use only. Docker Compose reads overrides from
`.env`, while `.env.example` contains no real secret.

Known limits of the current approach:

- it provides no end-user identity or delegated authorization;
- it has no key rotation or revocation API; and
- it does not provide transport-level TLS.

When human users, tenants, or fine-grained permissions become requirements,
the API should adopt OIDC/OAuth 2.0 JWT validation. TLS belongs at the ingress
or reverse proxy.

## Containerized local environment

The multi-stage Dockerfile:

1. restores and publishes Release output in the SDK image;
2. provides a separate migration target for all five DbContexts; and
3. produces a non-root ASP.NET runtime image.

Compose starts services in this order:

```text
PostgreSQL healthy
    → one-shot migrations complete
    → API starts
```

The API does not migrate automatically at startup. This prevents multiple
replicas from racing to change the schema and keeps migration as an explicit
deployment step.

```bash
cp .env.example .env
# Replace both example keys in .env
docker compose up --build
```
