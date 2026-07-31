# ADR-0014 — Do Not Extract Services without Measured Drivers

- Status: Accepted
- Date: 2026-07-31

## Context

Rentals, Fleet Availability, and Notifications are isolated through modules,
schemas, and Published Contracts, making later extraction technically possible.
The repository has no production traffic, incidents, team delivery data, or
compliance requirements. Equating every bounded context with a deployment
boundary would accept distributed-systems cost without evidence.

## Decision drivers

- Preserve the modular monolith's low development/operational cost.
- Treat model boundaries independently of deployment topology.
- Keep extraction reversible.
- Separate measured evidence from architectural hypotheses.
- Accept network, broker, and distributed-data cost only for business reasons.

## Considered options

1. Extract every implemented context immediately.
2. Extract Notifications only as a teaching exercise.
3. Keep the monolith indefinitely without review criteria.
4. Keep the monolith and define fitness functions and measurable thresholds.

## Decision

Choose option four. No context is extracted today. Cross-module dependencies
may target only the provider's Published Contract assembly; business modules
cannot depend on the API composition root; contracts remain independent of
transport and persistence technology.

Reconsider extraction only after measuring at least one driver: independent
scaling, fault isolation, deployment independence, team ownership, security,
or compliance.

## Candidate assessment

- Notifications is the lowest-risk technical pilot because it already has an
  asynchronous contract and Inbox boundary.
- Fleet Availability is the strongest operational candidate if a different
  scale/concurrency profile is proven.
- Rentals remains central because it owns the core workflow and Process
  Manager.
- Availability Calendar is a read model, not automatically a new context or
  microservice.

This is review priority, not an extraction decision.

## Consequences

The repository does not use microservice count as a quality metric. Local
development, transactions, and debugging stay simple; executable tests protect
boundaries; and a future extraction can reuse ACL, Outbox, Inbox, and
idempotency investments. The cost is one deployment without process-level
isolation, plus in-process calls that do not expose network failures.

## Reconsideration process

Detailed thresholds live in the
[Service Extraction Assessment](../architecture/service-extraction-assessment.md).
At least one hard driver must be measured over a representative window, and
the decision must explain why a cheaper in-process remedy is insufficient.
An extraction creates a new ADR defining migration, cutover, rollback, and
ownership; it does not rewrite this historical decision.

## Evidence

- `ServiceExtractionFitnessTests`
- [Service Extraction Assessment](../architecture/service-extraction-assessment.md)
- [Service Extraction Playbook](../architecture/service-extraction-playbook.md)
- [ADR-0001](0001-start-with-a-modular-monolith.md)
