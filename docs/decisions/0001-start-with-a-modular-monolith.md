# ADR-0001 — Start with a Modular Monolith

- Status: Accepted
- Date: 2026-07-29
- Accepted on: 2026-07-31

## Context

The reference system must teach multiple bounded contexts and integration
patterns, but those boundaries have not yet been validated. Running every
context in a separate process and database at this stage would put networking,
distributed transactions, observability, and deployment complexity ahead of
domain learning.

## Decision drivers

- Evolve the domain model and its boundaries with fast feedback.
- Preserve isolation between contexts.
- Keep local development and testing inexpensive.
- Retain a path to extracting a service later.
- Accept distributed-systems complexity only when a business or operational
  driver justifies it.

## Considered options

1. An unstructured monolith
2. A modular monolith
3. Microservices from the beginning

## Decision

The solution will begin as a modular monolith whose modules preserve bounded
context boundaries. A module cannot access another module's Domain or
persistence implementation directly. Contexts communicate through explicit
contracts.

This is a deployment-topology decision. It does not require every module to
use the same tactical pattern or internal architecture.

## Positive consequences

- A single process is easy to run and debug.
- Boundaries can be discovered without distributed failure modes.
- Cross-context calls and contracts remain visible.
- A selected module can be extracted later through a deliberate migration.

## Negative consequences and risks

- Compile-time access is technically possible, so architecture tests and team
  discipline must enforce boundaries.
- A single deployment initially prevents independent context releases.
- In-process communication can hide the cost of a future network boundary.

## Reconsider when

- A context has a measured, distinct scaling requirement.
- Separate team ownership requires an independent delivery cadence.
- Security or compliance requires process or data isolation.
- A module's failures or resource consumption have an unacceptable impact on
  the others.

## Enforcing evidence

- Architecture tests reject forbidden module dependencies.
- Visibility tests restrict access to public contracts.
- Each module owns its integration-test fixture.
