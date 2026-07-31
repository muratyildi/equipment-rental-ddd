# 01 — Applied Learning and Delivery Roadmap

This roadmap connects DDD theory to repository evidence. Each stage begins with
a question, records a decision, implements the smallest meaningful example,
and verifies the result.

## Stage 1 — Discover the problem

**Question:** What business are we modeling, and which uncertainty matters?

Activities:

- write the product vision and business outcomes;
- identify domain experts and missing knowledge;
- run Big Picture Event Storming;
- capture commands, events, policies, actors, hotspots, and external systems;
- maintain a Ubiquitous Language glossary.

Evidence:

- [Product vision](../discovery/01-product-vision.md)
- [Big Picture Event Storming](../discovery/02-big-picture-event-storming.md)
- [Ubiquitous Language](../discovery/03-ubiquitous-language.md)

## Stage 2 — Make strategic boundaries explicit

**Question:** Where does meaning change, and where should investment go?

Activities:

- classify Core, Supporting, and Generic Subdomains;
- propose Bounded Contexts;
- assign model and data ownership;
- draw upstream/downstream relationships;
- choose Context Map patterns deliberately.

Evidence:

- [Subdomains and Bounded Contexts](../discovery/04-subdomains-and-bounded-contexts.md)
- [Context Map](../discovery/05-context-map.md)
- ADRs 0001–0003

## Stage 3 — Build a walking skeleton

**Question:** Can the boundaries compile and run before detailed behavior grows?

Activities:

- create the .NET solution and API composition root;
- create module-level Domain, Application, Infrastructure, and Contracts
  projects only where responsibilities require them;
- enforce dependency directions with architecture tests;
- add repeatable local and CI commands.

The skeleton is evidence of boundaries, not the architecture's final value.

## Stage 4 — Select models by complexity

**Question:** Which implementation model is the simplest one that safely
expresses each subdomain?

Use contrasting examples:

- a Transaction Script for simple Notifications work;
- a rich Domain Model for Rentals and Fleet Availability invariants;
- a denormalized CQRS model for calendar reads;
- no Event Sourcing without a temporal/audit requirement that justifies it.

This stage demonstrates that DDD is judgment, not uniform ceremony.

## Stage 5 — Implement tactical DDD

**Question:** Which rules must remain true inside one transaction?

Activities:

- model Entities and Value Objects from language and examples;
- define Aggregate boundaries around consistency;
- keep behavior in the model rather than anemic service scripts;
- use Domain Events for facts inside a context;
- expose Repositories as domain-facing abstractions;
- test rules using domain language.

Evidence includes Rental Order and Availability Schedule tests, rather than
classes named after patterns with no protected invariant.

## Stage 6 — Choose application architecture and read models

**Question:** How do use cases coordinate the model without taking over its
business decisions?

Activities:

- implement explicit command/query handlers;
- isolate infrastructure behind consumer-owned ports;
- use EF Core mappings without leaking persistence behavior into Domain code;
- create a CQRS projection where read shape and decision shape diverge;
- document consistency and freshness expectations.

## Stage 7 — Integrate contexts reliably

**Question:** How can one context consume another without sharing its model or
losing business facts?

Activities:

- publish primitive, versioned contracts;
- translate through an Anti-Corruption Layer;
- distinguish Domain Events from Integration Events;
- commit Outbox records with Aggregate changes;
- make consumers idempotent with an Inbox;
- coordinate long-running work with a persisted Process Manager.

Failure cases—duplicates, reversed delivery, partial success, retry exhaustion,
and compensation—are part of the design.

## Stage 8 — Add operational quality

**Question:** Can operators understand and recover the system under failure?

Activities:

- structured logging, correlation, traces, and metrics;
- liveness/readiness endpoints;
- bounded retries and terminal failure states;
- authentication, authorization, secret handling, and rate limiting;
- container and migration workflows;
- CI gates and repository hygiene.

See [Operational Readiness](../architecture/operational-readiness.md).

## Stage 9 — Evolve using evidence

**Question:** Does any Bounded Context need independent deployment?

Activities:

- establish measurable extraction drivers;
- preserve modular boundaries and contract ownership;
- compare benefits with network and operational costs;
- prepare migration, canary, reconciliation, and rollback plans;
- keep the modular monolith until evidence crosses a threshold.

See the [Service Extraction Assessment](../architecture/service-extraction-assessment.md)
and [Playbook](../architecture/service-extraction-playbook.md).

## Decision-explanation template

Every milestone should answer:

1. What business or operational problem exists?
2. Which DDD concept is relevant?
3. Which alternatives were considered?
4. Why is the selected option proportional to complexity?
5. What trade-offs or failure modes are accepted?
6. Where is the decision visible in code or documentation?
7. Which automated test or operational signal proves it?
8. What evidence would cause us to revisit it?

This template prevents “best practice” from becoming an explanation.

## Completion definition

The roadmap is complete when a reader can move from problem discovery to code,
tests, operational behavior, and evolutionary decisions without encountering
an unexplained pattern. The repository is not expected to implement every DDD
pattern; it is expected to justify every pattern it does implement.
