# ADR-0002 — Select the Equipment Rental Domain

- Status: Accepted
- Date: 2026-07-31

## Context

The learning system needs a natural mix of simple and complex subdomains,
time- and money-based invariants, multiple bounded contexts, and long-running
business processes.

The Blue Book's extended example uses cargo shipping. Reusing that domain
would make it harder to demonstrate that DDD knowledge transfers to a new
problem.

## Considered options

1. Cargo or last-mile delivery
2. Events and ticketing
3. Industrial equipment rental and field operations

## Decision

Use industrial equipment rental. Its quotation, availability, reservation,
maintenance, delivery, return, damage, and billing concerns create meaningful
model boundaries. Time ranges, capacity, money, and physical condition also
produce strong business rules.

## Consequences

- The project demonstrates knowledge transfer instead of copying the Blue
  Book example.
- Rentals and Fleet Availability have a genuine model boundary.
- A CQRS calendar, Outbox, and Process Manager can be introduced for domain
  reasons instead of pattern demonstration alone.
- Because no domain expert is currently available, uncertain rules must be
  recorded explicitly as hotspots.

## Reconsider when

If the domain cannot support the learning goals or realistic scenarios, run a
new discovery exercise before changing the product vision.
