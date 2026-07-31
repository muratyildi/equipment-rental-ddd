# ADR-0009 — Publish Integration Events through a Transactional Outbox

- Status: Accepted
- Date: 2026-07-31

## Context

Aggregates raise Domain Events, but notifying other contexts reliably creates
a dual-write problem. Committing the database before publishing can lose a
message after a crash; publishing first can announce a change that never
commits.

## Decision drivers

- Keep broker, JSON, and EF Core concerns out of the Domain model.
- Record the business change and intent to publish atomically.
- Let each bounded context own its integration contracts.
- Separate temporary transport failures from the business transaction.
- Describe delivery guarantees honestly.

## Decision

- Every Domain Event has a stable, unique `EventId`.
- Publish only domain facts that are meaningful to external consumers.
- Map them to versioned, primitive-only Integration Events.
- Each module owns an `outbox_messages` table in its schema.
- Aggregate changes and Outbox inserts share one `SaveChanges` transaction.
- Clear Domain Events only after a successful commit.
- Workers claim messages through `xmin` optimistic concurrency.
- Success records `processed_at_utc`; failure releases the claim, records the
  error/attempt, and schedules exponential backoff.
- Delivery is **at least once**, never claimed as exactly once.

Initial contracts are `rentals.rental-order-confirmed.v1` and
`fleet-availability.equipment-availability-committed.v1`.

## Why Domain Events and Integration Events differ

A Domain Event expresses an internal model fact and may use rich domain types.
An Integration Event is a compatibility contract. Sharing one class would turn
the internal model into a public API and make domain refactoring a breaking
change.

## Why not exactly once

A publisher can crash after broker delivery but before recording completion.
Without a distributed transaction, the message will be delivered again. The
stable `EventId` enables idempotent consumers.

## Consequences and follow-up

The Outbox closes the database/publish loss window, isolates transport errors,
and makes contracts explicit. It also requires retention, idempotent consumers,
and monitoring. The current logging publisher is a local adapter; a selected
broker will replace it. Higher volume may justify CDC or `SKIP LOCKED` batch
claiming based on measurements.
