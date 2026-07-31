# Notifications Bounded Context

## Purpose

Notifications is a supporting subdomain that turns business events from other
contexts into customer-communication work. It does not own rental rules,
capacity, or customer eligibility.

Initial use case:

```text
RentalOrderConfirmedV1
  → pending work item for the rental-order-confirmed template
```

## Why there is no rich Domain Model

Current behavior is a simple Transaction Script:

1. validate contract fields;
2. create a notification work item; and
3. commit it with the Inbox record.

Adding an Aggregate or Value Object would not protect a real invariant today.
Reconsider a richer model when template selection, channel preferences, quiet
hours, localization, or retry policies become complex.

## Context boundary

- Application does not reference Rentals.
- Infrastructure consumes only the Rentals Contracts assembly.
- Rental domain types do not enter Notifications.
- Customer and Rental Order identities are external references.

## Data ownership

Notifications owns its PostgreSQL schema:

- `inbox_messages`
- `notification_work_items`
- `__ef_migrations_history`

No other context may write these tables.

## Idempotency

The Inbox key is `(consumer, message_id)`. Sequential and concurrent duplicate
deliveries produce one notification work item. If business handling fails, the
Inbox change rolls back with it.
