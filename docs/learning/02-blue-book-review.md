# 02 — Eric Evans's Blue Book

## Source and historical importance

- **Book:** *Domain-Driven Design: Tackling Complexity in the Heart of Software*
- **Author:** Eric Evans
- **Published:** 2003
- **Common name:** the Blue Book

Eric Evans did not invent object-oriented modeling, iterative development, or
design patterns. His contribution was to organize decades of practice around a
clear center: when software is dominated by business complexity, teams need a
shared language, an explicit model tied to implementation, and strategic
control over model boundaries.

The book supplied the name *Domain-Driven Design* and a coherent pattern
language. It remains the primary source for the original meanings of concepts
that are often reduced today to folder structures or framework recipes.

This document is an original analytical summary, not a reproduction of the
book.

## The problem the book addresses

Complex software accumulates accidental distance between:

- what domain experts mean;
- what analysts document;
- what developers name and implement; and
- what databases happen to store.

When the model exists only in diagrams, code develops a separate meaning. When
code is only a persistence representation, business rules scatter through
services, controllers, triggers, and UI logic. Each change then requires
translation across several inconsistent languages.

Evans's response is **Model-Driven Design**:

> Select a model that explains the domain, use it as the basis of communication,
> and bind its concepts directly to the software.

The model is not a static analysis artifact. It is a deliberately simplified,
continuously refined interpretation that helps the team make decisions.

## Part I — Putting the Domain Model to Work

### Knowledge crunching

Developers and domain experts learn together through examples, contradictions,
failed explanations, and experiments. A breakthrough often comes when a vague
word is split into precise concepts or an implicit rule becomes explicit.

Effective modeling therefore requires:

- direct access to domain experts;
- repeated discussion of concrete scenarios;
- prototypes and tests that challenge assumptions;
- refactoring language and code together; and
- treating confusion as useful evidence.

A requirements handoff cannot replace this feedback loop.

### Communication and Ubiquitous Language

The Ubiquitous Language is used in speech, diagrams, documentation, tests, and
code within a model boundary. Terms that cannot be spoken naturally by experts
or implemented precisely are signals that the model needs work.

The language must be:

- strict enough to expose ambiguity;
- rich enough to express rules;
- safe to change when understanding improves; and
- bounded, because one enterprise-wide vocabulary cannot preserve every local
  meaning.

### Binding model and implementation

A model earns value when implementation expresses it. If a domain concept is
important in conversation but invisible in code, technical mechanisms will
eventually replace or distort it.

Binding does not mean every noun becomes a class. It means important concepts,
relationships, constraints, and behaviors have an honest software expression.

## Part II — Building Blocks of Model-Driven Design

### Isolating the Domain

The Domain layer contains business meaning and must not be overwhelmed by UI,
database, messaging, or framework concerns. The classic Layered Architecture
separates:

| Layer | Responsibility |
| --- | --- |
| User Interface | Display information and interpret input |
| Application | Coordinate use cases; define no essential business rules |
| Domain | Express business concepts, rules, and state transitions |
| Infrastructure | Provide persistence, messaging, and technical mechanisms |

Layering is valuable only when dependency choices actually protect the model.
Modern Ports and Adapters can serve the same goal with stronger boundary
language.

### Entity

An Entity is defined by identity and continuity, not by all its attributes.
Its model should focus on the operations and attributes required to distinguish
and track it. Identity rules are domain decisions; a database key alone does
not explain them.

### Value Object

A Value Object is defined entirely by its attributes and has no conceptual
identity. It should normally be immutable and validated at construction.
Equality compares values.

Examples in this repository include Money and Rental Period. Their types make
invalid or ambiguous primitive combinations harder to represent.

### Domain Service

A Domain Service represents an important domain operation that does not
naturally belong to one Entity or Value Object. It is named in the Ubiquitous
Language and remains stateless from the domain perspective.

It is not a container for behavior that should live on an Aggregate, and it is
different from an Application Service that coordinates a use case.

### Module

Modules group cohesive model concepts and communicate the model's large-scale
shape. Their names belong to the domain language. A technical package structure
that cuts every feature into generic folders can hide this cohesion.

### Aggregate

An Aggregate is a consistency and transaction boundary:

- one Entity is the Aggregate Root;
- outside objects reference the root, not internal members;
- invariants hold after every successful transaction;
- only the root controls changes inside the boundary; and
- a transaction normally modifies one Aggregate.

Large Aggregates are not “richer.” They increase contention, loading cost, and
coordination. Boundaries should follow invariants, not composition convenience.

### Factory

A Factory encapsulates complex creation when construction would expose internal
structure or when a valid object requires coordinated steps. Simple creation
does not need a ceremonial Factory.

### Repository

A Repository provides collection-like access to Aggregate Roots while hiding
persistence mechanics. Its contract follows domain needs, not a universal CRUD
interface. Query-oriented reporting may use a different model entirely.

### Domain Event

The original book predates today's widespread event-driven architectures, but
later DDD practice names significant completed facts as Domain Events. Their
primary meaning belongs inside a Bounded Context. Publication to other contexts
requires a deliberate Integration Event contract.

## Part III — Refactoring Toward Deeper Insight

DDD is not completed when the first model compiles. The book's most important
message is that useful models emerge through sustained refactoring.

### Breakthrough

Incremental learning can suddenly reveal a model that explains several awkward
rules at once. Such a breakthrough may require substantial refactoring. The
cost is justified when the new model makes the domain simpler and future
changes safer.

### Making implicit concepts explicit

Look for:

- rules hidden in conditionals;
- repeated calculations with no domain name;
- constraints explained orally but absent from tests;
- important processes represented only by application orchestration; and
- terms whose meanings shift between conversations.

An explicit concept might become a Value Object, policy, specification,
process, relationship, or invariant—not necessarily an Entity.

### Supple Design

A supple design helps developers use the model correctly. Evans describes
ideas such as Intention-Revealing Interfaces, Side-Effect-Free Functions,
Assertions, Conceptual Contours, Standalone Classes, and Closure of Operations.

The shared aim is lower cognitive load: names reveal purpose, effects are
predictable, rules are visible, and boundaries match meaningful concepts.

### Analysis and design patterns

Patterns can accelerate modeling, but they are starting hypotheses rather than
finished answers. A pattern borrowed without adapting its language and
constraints can suppress discovery.

### Refactoring toward deeper insight

Deep refactoring combines:

- living close to the domain;
- investigating awkward code as a modeling symptom;
- maintaining a language shared with experts; and
- changing implementation safely through automated tests.

## Part IV — Strategic Design

### Bounded Context

A Bounded Context defines where one model is internally consistent. It protects
teams from attempting a single enterprise model and makes translation explicit
where meanings differ.

This is a semantic boundary. It may be implemented as a module, a deployment,
or several components. Equating it automatically with a microservice confuses
model design with operational topology.

### Continuous Integration

Within a Bounded Context, model fragments must be integrated frequently.
Otherwise developers create subtly incompatible dialects and duplicate
concepts. Continuous Integration here includes conversation and model
integration, not only a build server.

### Context Map

A Context Map describes existing model boundaries and relationships. Common
patterns include:

- Shared Kernel
- Customer/Supplier
- Conformist
- Anti-Corruption Layer
- Open Host Service
- Published Language
- Separate Ways

The map records influence and translation. A message broker does not determine
which relationship exists.

### Distillation and Core Domain

Not every part of a system deserves equal investment. Distillation separates
the **Core Domain**—the model that creates strategic advantage—from supporting
and generic concerns.

Techniques include:

- a concise Domain Vision Statement;
- highlighting or segregating the Core;
- extracting a Generic Subdomain; and
- defining an Abstract Core shared by closely related models.

Senior design judgment includes deciding what *not* to build.

### Large-scale structure

When a model becomes too large to understand locally, a light organizing
structure can reveal its overall shape. The structure must clarify the model
without forcing all details into an artificial framework.

### Bringing strategy together

Strategic design is continuous. Business priorities, team boundaries, and
models evolve. Context relationships and Core Domain investment must be
revisited when those forces change.

## What the Blue Book does not prescribe

Published in 2003, the book does not provide today's implementation recipes for
cloud-native operations, modern brokers, Outbox/Inbox, container orchestration,
or observability stacks. It also does not say that DDD requires:

- microservices;
- CQRS or Event Sourcing;
- a particular ORM or mediator;
- Clean Architecture;
- one repository per context; or
- rich Domain Models in simple subdomains.

Modern techniques should be judged by whether they protect the book's core
goals under contemporary operational constraints.

## Principles applied in this repository

1. Start with learning and language, not project templates.
2. Keep the model and implementation connected.
3. Protect different meanings with explicit Bounded Contexts.
4. Invest rich modeling only where business complexity warrants it.
5. Treat Aggregates as consistency boundaries.
6. Translate across contexts using owned ports and published contracts.
7. Make reliability and failure semantics explicit.
8. Preserve a modular monolith until measured drivers justify extraction.
9. Use tests and ADRs as evidence that the architecture expresses decisions.
