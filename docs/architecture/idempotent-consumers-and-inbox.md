# Idempotent Consumers and the Transactional Inbox

## Problem

Outbox delivery is at least once. Receiving the same Integration Event more
than once is expected distributed-systems behavior, not an exceptional case.

```text
publish succeeds
  → publisher crashes
  → Outbox completion is not stored
  → the same EventId is published again
```

Without deduplication, notification, payment, or inventory effects can repeat.

## Notifications example

Notifications Infrastructure consumes
`rentals.rental-order-confirmed.v1`:

```text
IntegrationEventEnvelope
  → deserialize Rentals Published Contract
  → Notifications-owned adapter
  → RequestRentalConfirmationNotification
  → transaction script
  → notification_work_items INSERT
```

Notifications Application does not reference Rentals. Translation from the
external contract into its own command belongs to Infrastructure.

## Transaction boundary

The consumer commits three changes in one PostgreSQL transaction:

1. Inbox row keyed by `(consumer, message_id)`;
2. notification work item; and
3. Inbox `processed_at_utc`.

If business handling fails, everything rolls back and transport can retry.

## Sequential duplicates

```text
first delivery  → no Inbox row → create effect → commit
next delivery   → Inbox row exists → successful no-op
```

## Concurrent duplicates

Checking first is insufficient because two transactions can both observe no
row. The composite primary key is the actual correctness guarantee:

```text
PRIMARY KEY (consumer, message_id)
```

One transaction wins. The other receives a unique violation, rolls back, then
confirms the winning Inbox row and returns a successful no-op.

The consumer name belongs in the key because independent consumers can process
the same message for different purposes. Idempotency scope is **consumer plus
message**, not message alone.

## Effectively-once local effects

Transport is not exactly once, but the consumer applies its local database
effect once per `EventId`. This guarantee covers only effects committed in the
same Inbox transaction. Calling an external email provider inside that
transaction would break it; a separate reliable worker must deliver pending
notification work items.
