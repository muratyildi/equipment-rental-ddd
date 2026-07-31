# ADR-0011 — Use a CQRS Read Model for the Availability Calendar

- Status: Accepted
- Date: 2026-07-31

## Context

`AvailabilitySchedule` is a decision model that prevents overbooking. The
calendar needs total, committed, and available capacity per day. Using the
aggregate as the query model would couple an invariant-oriented write shape to
a different read concern.

## Decision drivers

- Preserve the aggregate boundary and strong-consistency decision.
- Keep per-day query cost predictable.
- Remain correct under at-least-once delivery.
- Demonstrate eventual consistency without breaking context ownership.
- Avoid unnecessary microservices, mediators, and Event Sourcing.

## Considered options

1. Load the aggregate and calculate every query.
2. Query write tables with specialized SQL.
3. Maintain a separate denormalized read model from events.

The first scales with aggregate size. The second couples queries to the write
schema and EF mappings. The third adds operational cost but provides an
explicit, independently optimized query model.

## Decision

Create `EquipmentRental.Modules.FleetAvailability.ReadModel` and the
`fleet_availability_read` PostgreSQL schema inside the Fleet Availability
bounded context.

The projection consumes Fleet's versioned `capacity-defined.v1` and
`availability-committed.v1` events. A Transactional Inbox neutralizes duplicate
delivery. Calendar queries read only the read-model DbContext.

## Positive consequences

- The write model remains invariant-focused.
- Query cost follows the number of requested days.
- The query side is independent of the internal write layers.
- Integration tests expose eventual consistency, ordering, and duplicates.
- Distributed deployment cost is deferred within the same bounded context.

## Negative consequences and risks

- Commands and queries are briefly inconsistent.
- A fourth DbContext, schema, and migration lifecycle exist.
- Projection failures require replay, observability, and repair.
- New behavior can require new event contracts and projector changes.

## Reconsider when

- The calendar needs separate scaling or deployment cadence.
- Projection lag receives a stronger SLA.
- Broker replay/partition ordering is selected.
- Capacity changes, releases, or cancellations are introduced.
- Multiple read models consume the same stream.

## Evidence

- `AvailabilityCalendarProjectionTests`
- `AvailabilityCalendarReadModel_DependsOnlyOnPublishedContracts`
- [CQRS Availability Calendar guide](../architecture/cqrs-availability-calendar.md)
