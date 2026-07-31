# ADR-0008 — Use PostgreSQL, EF Core, and Aggregate Optimistic Concurrency

- Status: Accepted
- Date: 2026-07-31

## Context

The in-memory adapters validated the first vertical slice but lost data after
restart, did not exercise rehydration, and could not stop concurrent stale
aggregate copies from violating the same invariant. In Fleet Availability,
two stale copies could otherwise sell the same capacity and overbook it.

## Decision drivers

- Keep EF Core out of the Domain model.
- Make bounded-context data ownership physically visible.
- Preserve the aggregate as one consistency boundary.
- Test real PostgreSQL behavior automatically.
- Maintain migration history per module.

## Considered options

1. EF Core InMemory does not prove relational or concurrency behavior.
2. SQLite is fast but does not reproduce PostgreSQL `xmin` semantics.
3. Handwritten SQL/Dapper is viable but adds unnecessary mapping cost here.
4. EF Core, PostgreSQL, and Testcontainers — selected.

## Decision

- Use one PostgreSQL database with `rentals` and `fleet_availability` schemas.
- Each module owns its DbContext, mappings, migrations, and repository adapter.
- Do not create foreign keys across contexts.
- Map the Aggregate Root's PostgreSQL `xmin` column with `IsRowVersion()`.
- Touch the root's `updated_at_utc` when a child changes so `xmin` protects the
  complete aggregate.
- Verify migrations, rehydration, and stale-write conflicts against real
  PostgreSQL with Testcontainers.

## Consequences

- The Domain layer remains persistence-ignorant and retains strongly typed IDs
  and Value Objects through fluent mappings.
- Repositories load complete aggregates, and concurrent capacity commitments
  cannot silently overbook.
- Modules have independent migration histories, but local integration tests
  require Docker.
- Missing `Include` clauses can produce incomplete aggregates; round-trip tests
  guard against this.
- Concurrency conflicts become `409 Conflict`; automatic retry is deliberately
  avoided because the domain decision must be repeated against current state.

## Reconsider when

A context needs its own database/deployment, contention becomes unacceptable,
EF Core has a measured write-performance problem, or evidence shows that an
aggregate boundary is too large.

## Evidence

Aggregate round-trip tests and stale-copy integration tests cover Rentals and
Fleet Availability, including the overbooking race.
