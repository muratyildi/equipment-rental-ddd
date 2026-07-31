# ADR-0007 — Process Multi-Line Availability One Line at a Time Initially

- Status: Superseded by [ADR-0012](0012-rental-confirmation-process-manager.md)
- Date: 2026-07-31
- Nature: Temporary, explicit limitation

## Context

A Rental Order can contain multiple equipment categories. Each category
belongs to a different `AvailabilitySchedule` aggregate. Committing all lines
in one transaction would cross aggregate boundaries.

## Considered options

1. Enlist multiple schedules in one database transaction.
2. Assume a distributed transaction.
3. Introduce an atomic batch contract.
4. Commit lines independently and idempotently, then complete the workflow
   later with a Process Manager.

## Decision

Choose the fourth option. In the initial milestone:

- each line is requested independently;
- accepted commitments are retained;
- a rejected line leaves the Rental unconfirmed;
- retry is safe; and
- release/compensation is not yet automated.

A batch contract would hide, rather than solve, atomicity across separate
schedule aggregates.

## Risk

A partial set of commitments can hold capacity longer than necessary.

## Follow-up decision

The Process Manager milestone must explicitly model completed lines, timeout,
release commands after failure, retry/idempotency state, and the Rental
confirmation trigger. [ADR-0012](0012-rental-confirmation-process-manager.md)
supersedes this temporary limitation.
