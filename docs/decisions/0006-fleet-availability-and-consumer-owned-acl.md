# ADR-0006 — Isolate Fleet Availability with a Consumer-Owned ACL

- Status: Accepted
- Date: 2026-07-31

## Context

Rentals needs a physical-capacity commitment before confirming an order.
However, Rental Line is a commercial concept while Availability Commitment is
a capacity-planning concept. The contexts must not share one model.

## Considered options

1. Reference the Fleet Domain project directly from Rentals.
2. Share a common Equipment or Reservation model.
3. Translate between a Rentals-owned port and Fleet's Published Contract using
   an Anti-Corruption Layer (ACL).

## Decision

Choose the third option.

- Rentals Application depends on `IEquipmentAvailabilityGateway`.
- `FleetAvailabilityGateway` in Rentals Infrastructure performs translation.
- The adapter references only the Fleet Availability Contracts assembly.
- Fleet Domain and Application models are not exposed to consumers.
- Identical GUID values are converted into context-specific identity types.

`AvailabilitySchedule` is the Aggregate Root for an equipment-category and
location pair. Capacity and commitments within one schedule share a single
consistency boundary.

Insufficient capacity is an expected rejection, not an exception. Repeating
the same external demand under identical conditions is idempotent.

## Positive consequences

- Assembly dependencies enforce model boundaries.
- Rentals is insulated from Fleet's internal language.
- A future transport can replace the in-process call.
- Retries do not create additional commitments.

## Negative consequences

- Translation code and separate identity/Value Object sets are required.
- The synchronous contract currently assumes in-process execution.
- A schedule may eventually become a large, contentious aggregate.

## Reconsider when

- Independent deployment is justified.
- Measured write contention is high.
- Commitment volume per schedule becomes unmanageable.
- Fleet latency harms Rentals availability.
