# ADR-0012 — Coordinate Rental Confirmation with a Persisted Process Manager

- Status: Accepted
- Date: 2026-07-31
- Supersedes: ADR-0007

## Context

A multi-line Rental Order needs commitments from different Fleet Availability
aggregates. Partial success can retain capacity unnecessarily, and in-memory
progress is lost after a process or instance restart.

## Considered options

1. One database/distributed transaction across aggregate boundaries
2. Sequential calls from a stateless Application Service
3. Event choreography
4. A Process Manager with persisted state and explicit compensation

## Decision

Choose option four. A separate ProcessManagers project uses Rentals' consumer-
owned Fleet port. Commit every Rental Line idempotently, release successful
commitments in reverse order after rejection or timeout, and confirm the
Rental Order only after all commitments succeed.

The Process Manager does not reference Fleet internals. It owns a PostgreSQL
schema/migration lifecycle and uses `xmin`-based worker claims.

## Consequences

Positive:

- Work resumes after restart; partial success is visible and compensated.
- Duplicate start, commit, and release operations are safe.
- HTTP request lifetime is separated from business-process lifetime.
- Aggregate and bounded-context transaction boundaries remain intact.

Negative:

- There is eventual completion rather than strong global atomicity.
- A state machine, worker, schema, and operational monitoring are required.
- Compensation can fail and require retry or operator intervention.
- New process versions need a policy for in-flight state migration.

## Evidence

- `RentalConfirmationProcessManagerTests`
- Fleet and Rentals release Domain tests
- `RentalProcessManager_UsesRentalsPortsWithoutFleetInternals`
- [Process Manager guide](../architecture/rental-confirmation-process-manager.md)
