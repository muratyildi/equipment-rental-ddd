# ADR-0010 — Use Idempotent Consumers and a Transactional Inbox

- Status: Accepted
- Date: 2026-07-31

## Context

The Transactional Outbox prevents message loss but at-least-once delivery can
produce duplicates. Recreating a notification work item for every delivery
could notify a customer more than once.

## Decision drivers

- Treat duplicate delivery as normal.
- Commit deduplication and the business effect atomically.
- Keep the consumer independent of the publisher's Domain model.
- Resolve concurrent duplicates with a database constraint.
- Avoid a rich model where a supporting subdomain has simple behavior.

## Decision

- Add a Notifications supporting bounded context.
- Its Infrastructure adapter consumes
  `rentals.rental-order-confirmed.v1` and translates it into an Application
  command.
- A transaction script creates the notification work item.
- Use `(consumer, message_id)` as the Inbox primary key.
- Commit the Inbox insert, work-item insert, and `processed_at_utc` together.
- A previously processed message returns a successful no-op.
- For concurrent first deliveries, the unique constraint chooses the winner;
  the loser rolls back, confirms the Inbox record, and returns a no-op.
- If business handling fails, roll back the Inbox so transport can retry.

The consumer name is part of the key because one context can have multiple
independent consumers of the same Integration Event.

## Why Notifications is not a rich Domain Model

The current rule is only “create a pending notification work item for a
confirmed rental.” There is no complex invariant or Entity lifecycle.
Reconsider when templates, channels, quiet hours, localization, or retry policy
introduce meaningful domain decisions.

## Consequences and evidence

Sequential and concurrent duplicates produce one effect, failures are not
marked as processed, and Notifications Application does not reference Rentals.
The trade-offs are Inbox retention, unique violations as expected control flow,
and the absence of a real email/SMS transport. Integration tests cover
sequential duplicates, races, rollback, and the complete Outbox-to-Inbox flow.
