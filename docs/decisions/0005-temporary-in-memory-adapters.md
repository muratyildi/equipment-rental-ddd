# ADR-0005 — Use Temporary In-Memory Adapters for the Walking Skeleton

- Status: Superseded by [ADR-0008](0008-postgresql-ef-core-and-optimistic-concurrency.md)
- Date: 2026-07-31

## Context

The first goal is to validate the domain model and the vertical flow from HTTP
to an aggregate. A permanent database schema should not be designed before the
context boundaries are understood.

## Decision

Use temporary in-process adapters for the `IRentalOrderRepository` and
`IAvailabilityScheduleRepository` ports.

## What this decision does not claim

The adapters do not:

- provide production persistence;
- prove transaction or concurrency behavior;
- retain data after application restart;
- replace the PostgreSQL design; or
- provide optimistic concurrency for simultaneous writes.

## Positive consequences

- Domain and Application behavior can be validated independently of database
  details.
- The first end-to-end flow works at low cost.
- Real usage clarifies the repository ports before durable adapters are built.

## Risks

- Keeping them too long could make the system appear more reliable than it is.
- Holding object references does not exercise real persistence rehydration.

## Replacement condition

The next infrastructure milestone must add PostgreSQL and EF Core adapters,
optimistic concurrency, and integration tests. In-memory adapters may remain
only as a fast demonstration or test option.
