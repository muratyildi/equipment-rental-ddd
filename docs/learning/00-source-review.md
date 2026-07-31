# 00 — Source Review and a First View of DDD

## Source

- **Book:** *Learning Domain-Driven Design*
- **Author:** Vlad Khononov
- **Purpose here:** understand the concepts, compare design choices, and apply
  them to an original Equipment Rental domain.

The supplied PDF is an early-release edition whose physical page order is not
always the same as its conceptual chapter order. This review therefore follows
chapter subjects instead of page numbers. It summarizes ideas in original
language; it is not a replacement for the copyrighted source.

## Central thesis

Software projects rarely fail because a programming language lacks expressive
power. They fail because teams solve the wrong problem, misunderstand the
right problem, or let business meaning disappear inside technical structures.
DDD therefore starts with learning and communication, not with a framework.

Its questions fall into two connected groups:

1. **Strategic design — what and why?** Which business problem matters, where
   does the company gain advantage, where does meaning change, and how do teams
   collaborate?
2. **Tactical design — how?** Which model and implementation style best express
   the rules inside a specific boundary?

Starting with Aggregates, Repositories, or CQRS before answering the strategic
questions can produce code that looks like DDD but does not model the business.

## A practical definition

Domain-Driven Design is a way to make software structure, language, and
behavior reflect how a business actually works. It combines:

- continuous collaboration with people who know the domain;
- a Ubiquitous Language shared by conversation, models, tests, and code;
- explicit boundaries where terms have one consistent meaning;
- investment in the business capabilities that create differentiation; and
- implementation patterns chosen in proportion to business complexity.

DDD is not a folder convention, a mandatory layered architecture, or a synonym
for microservices.

## Domain, Subdomain, and Bounded Context

### Business domain

The domain is the organization's broad area of activity: for example,
industrial equipment rental and field operations.

### Subdomain

A subdomain is a business capability within that domain. Khononov's modern
classification helps decide where design effort belongs:

| Type | Meaning | Typical treatment |
| --- | --- | --- |
| Core | Creates competitive advantage or unique business value | Invest in a tailored model and expert collaboration |
| Supporting | Necessary and business-specific, but not differentiating | Prefer the simplest adequate custom solution |
| Generic | A solved problem used by many businesses | Buy, outsource, or use a standard product when sensible |

The classification is strategic and can change. A formerly generic capability
can become core when the business strategy changes.

### Bounded Context

A Bounded Context is the boundary within which one model and one Ubiquitous
Language are consistent. The same word can legitimately mean different things
in different contexts:

- a **Customer** in Rentals may be the party signing a rental;
- a **Customer** in Billing may be an account responsible for payment; and
- a **Customer** in Support may be a contact and service history.

Trying to force all meanings into one universal model creates ambiguous fields,
unrelated invariants, and coordination overhead. A Bounded Context protects
meaning; it does not prescribe a process or deployment boundary.

## Why Ubiquitous Language is central

Language is not a glossary written after implementation. It is an executable
part of the model:

- domain experts use it in scenarios and rules;
- developers use it in code;
- tests express examples with it; and
- documentation records the decisions behind it.

If experts say “reserve capacity,” code should not silently call the concept
`UpdateInventoryRow`. When the language changes because understanding
improves, the model and code should be refactored with it.

## Strategic design

Strategic DDD includes more than drawing context boxes:

1. understand business goals and capabilities;
2. classify subdomains;
3. establish model boundaries;
4. define each context's language and ownership;
5. map upstream/downstream relationships; and
6. choose an integration pattern that reflects power, dependency, and change
   dynamics.

Common Context Map relationships include Partnership, Shared Kernel,
Customer/Supplier, Conformist, Anti-Corruption Layer, Open Host Service,
Published Language, and Separate Ways. These patterns describe collaboration
and model influence; they are not transport technologies.

## Tactical modeling choices

Khononov emphasizes choosing a model according to the complexity of the
subdomain.

### Transaction Script

A procedural operation coordinates validation and persistence. It is often the
right choice for simple business logic, such as recording a notification work
item.

### Active Record

An object wraps a simple data model and persistence behavior. It can suit
record-oriented rules but becomes fragile when invariants span multiple
records.

### Domain Model

A rich object model protects complex rules through Entities, Value Objects,
Aggregates, Domain Events, and Domain Services. Behavior belongs near the state
whose validity it protects.

An Aggregate is a transaction and consistency boundary, not an arbitrary
object graph. Outside code references its root; one transaction changes one
Aggregate whenever possible.

### Event-Sourced Domain Model

State is reconstructed from an ordered history of domain events. This provides
strong auditability and temporal reasoning but adds event evolution, storage,
rebuild, and operational complexity. It should be selected for a concrete
domain need, not because events are already used for integration.

## Architecture patterns

DDD does not mandate one application architecture:

- **Layered Architecture** separates presentation, application, domain, and
  infrastructure responsibilities.
- **Ports and Adapters** protects the model through consumer-owned ports.
- **CQRS** uses different models for decisions and reads when their needs
  diverge.

CQRS does not require Event Sourcing, two physical databases, or MediatR.
Architecture should make the model easier to understand and change.

## Reliable context integration

Bounded Contexts exchange explicit contracts rather than internal model
objects. Reliable asynchronous integration usually needs:

- versioned Integration Events;
- a transactional Outbox at the producer;
- at-least-once delivery;
- an idempotent consumer and Inbox;
- retry budgets and dead-letter handling; and
- correlation and observability.

A Domain Event expresses a fact inside one model. An Integration Event is a
published contract for other contexts. Treating them as automatically
identical leaks internal design and makes external consumers control the
publisher's evolution.

## Warnings carried into this repository

- Do not apply rich models to every CRUD operation.
- Do not model database tables before discovering business language.
- Do not equate a Bounded Context with a microservice.
- Do not share a universal Domain Model across contexts.
- Do not call an unreliable dual write “event-driven.”
- Do not use eventual consistency without explaining user-visible behavior.
- Do not claim operational needs that have not been measured.

## Effect on the implementation

This repository uses these principles to:

1. discover an original Equipment Rental domain;
2. separate Rentals, Fleet Availability, and Notifications;
3. classify their complexity before selecting modeling styles;
4. keep one deployable modular monolith while protecting context boundaries;
5. demonstrate rich Aggregates only where invariants justify them;
6. implement Outbox, Inbox, CQRS, and a Process Manager for concrete problems;
   and
7. evaluate service extraction using evidence rather than fashion.

The result is intended to show decision quality, not merely a catalog of DDD
pattern names.
