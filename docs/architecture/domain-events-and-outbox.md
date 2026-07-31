# Domain Events, Integration Events, and the Transactional Outbox

These concepts have different owners and purposes:

| Concept | Owner | Purpose | Persistence |
|---|---|---|---|
| Domain Event | Bounded Context's Domain model | Express a business fact inside the model | Held by the aggregate until commit |
| Integration Event | Publishing Bounded Context | Provide a stable contract to other contexts | Serialized as an Outbox payload |
| Outbox Message | Infrastructure | Record the intent to publish reliably | Table in the module's schema |

## Flow

```text
HTTP command
  → Application handler
  → Aggregate behavior raises a Domain Event
  → SaveChanges
      ├─ aggregate changes
      └─ versioned Integration Event Outbox INSERT
         [same PostgreSQL transaction]
  → commit
  → worker claims message
  → transport adapter publishes
  → processed_at_utc is recorded
```

The aggregate does not know about brokers, JSON, or the Outbox. It speaks only
its own business language.

## Stable event identity

Every event receives an `EventId` when it is created. The same value becomes
the Outbox primary key, survives retries, supports consumer deduplication, and
improves log/trace correlation. It does not make delivery exactly once; it
makes duplicates recognizable.

## Selective publication

Not every internal fact becomes public. Current mappings are:

```text
RentalOrderConfirmed
  → rentals.rental-order-confirmed.v1

EquipmentAvailabilityCommitted
  → fleet-availability.equipment-availability-committed.v1
```

Contract assemblies do not reference Domain assemblies. Contracts contain
portable primitives such as `Guid`, `DateOnly`, `decimal`, and `string`.

## Atomic persistence

At the start of `SaveChangesAsync`, each DbContext collects Domain Events from
tracked aggregates and maps selected events to Outbox entities. EF Core writes
aggregate and Outbox changes in one transaction.

- On success, Domain Events are cleared from the aggregate.
- On failure, both writes roll back and events remain available for retry.
- A second tracked Outbox entity is not created for the same `EventId`.

A real PostgreSQL rollback test forces a duplicate aggregate key to prove this
behavior.

## Multi-worker claiming

Workers select only messages that are unprocessed, due for retry, and either
unclaimed or past their claim expiry. `claimed_by` and `claimed_until_utc` are
updated under PostgreSQL `xmin` concurrency. If two workers read the same row,
only one claim update succeeds. Claims are leases, not permanent locks; another
worker can recover work after a crashed worker's lease expires.

## Retry and dead-letter behavior

A publish failure does not roll back the original business transaction. It
increments `attempts`, records `last_error`, clears the claim, and schedules
`next_attempt_at_utc` with exponential backoff. After five failures,
`DeadLetteredAtUtc` makes the message terminal and removes it from normal claim
queries. Readiness reports this as `Degraded`; replay and retention still need
an operator-controlled runbook.

## Why at-least-once can duplicate

```text
broker publish succeeds
  → process crashes
  → processed_at_utc is not stored
  → message is published again
```

Notifications therefore records the `EventId` in its Inbox atomically with the
notification work item.

## Current transport boundary

For local development, an in-process adapter dispatches matching consumers and
writes the envelope to structured logs. Outbox, Inbox, and consumers are
transport-independent. Selecting RabbitMQ, Azure Service Bus, or Kafka changes
publisher/receiver adapters, not the reliability model or Domain model.
