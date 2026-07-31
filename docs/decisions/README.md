# Architecture Decision Log

An Architecture Decision Record (ADR) preserves not only an important
decision's outcome, but also the context and trade-offs understood when it was
made.

## Statuses

- `Proposed`: open for discussion
- `Accepted`: approved for implementation
- `Superseded`: replaced by a newer ADR
- `Rejected`: considered but not selected

## Record structure

Each ADR captures the context, decision drivers, considered options, decision,
positive and negative consequences, reconsideration conditions, and related
domain scenarios or executable evidence.

## Records

- [ADR-0001 — Start with a Modular Monolith](0001-start-with-a-modular-monolith.md) (`Accepted`)
- [ADR-0002 — Select the Equipment Rental Domain](0002-select-the-equipment-rental-domain.md) (`Accepted`)
- [ADR-0003 — Use .NET 10, C# 14, and SLNX](0003-use-dotnet-10-and-slnx.md) (`Accepted`)
- [ADR-0004 — Prefer Explicit Use-Case Handlers Initially](0004-explicit-use-case-handlers.md) (`Accepted`)
- [ADR-0005 — Temporary In-Memory Adapters](0005-temporary-in-memory-adapters.md) (`Superseded`)
- [ADR-0006 — Fleet Availability and Consumer-Owned ACL](0006-fleet-availability-and-consumer-owned-acl.md) (`Accepted`)
- [ADR-0007 — Line-by-Line Availability Process](0007-line-by-line-availability-process.md) (`Superseded by ADR-0012`)
- [ADR-0008 — PostgreSQL, EF Core, and Optimistic Concurrency](0008-postgresql-ef-core-and-optimistic-concurrency.md) (`Accepted`)
- [ADR-0009 — Domain Events and Transactional Outbox](0009-domain-events-and-transactional-outbox.md) (`Accepted`)
- [ADR-0010 — Idempotent Consumer and Transactional Inbox](0010-idempotent-consumer-and-transactional-inbox.md) (`Accepted`)
- [ADR-0011 — CQRS Availability Calendar Read Model](0011-cqrs-availability-calendar-read-model.md) (`Accepted`)
- [ADR-0012 — Rental Confirmation Process Manager](0012-rental-confirmation-process-manager.md) (`Accepted`)
- [ADR-0013 — Operational Readiness Boundaries](0013-operational-readiness-boundary.md) (`Accepted`)
- [ADR-0014 — No Service Extraction without Measured Drivers](0014-no-service-extraction-without-measured-drivers.md) (`Accepted`)

The foundational learning principles are recorded in the
[source review](../learning/00-source-review.md).
