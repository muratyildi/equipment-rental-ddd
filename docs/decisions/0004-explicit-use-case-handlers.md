# ADR-0004 — Prefer Explicit Use-Case Handlers Initially

- Status: Accepted
- Date: 2026-07-31

## Context

Separating Commands from Queries is an architectural concept. A package such
as MediatR is only a technical dispatch mechanism for those calls.

The first vertical slice does not need pipeline behaviors, dynamic handler
discovery, or cross-module dispatch.

## Decision

Represent each use case with an explicit handler class and inject it directly
from the API composition root.

## Positive consequences

- Call flow and dependencies remain visible.
- Framework terminology does not obscure domain learning.
- Fewer dependencies keep the initial implementation small.
- Introducing a mediator later remains a reversible decision.

## Negative consequences

- Cross-cutting behaviors do not yet have a central pipeline.
- Composition code can grow with the number of handlers.

## Reconsider when

- Transactions, authorization, logging, or validation require repeated
  pipeline behavior.
- Manual handler discovery becomes unmanageable.
- In-process command or event dispatch provides demonstrable value.
