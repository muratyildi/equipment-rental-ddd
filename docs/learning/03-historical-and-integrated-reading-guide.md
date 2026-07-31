# 03 — Historical and Integrated DDD Reading Guide

## Historical layers

DDD did not appear in isolation in 2003. It synthesized and redirected several
earlier bodies of practice:

\`\`\`mermaid
flowchart LR
    OOA["Object-oriented analysis and design"] --> EVANS["Eric Evans: DDD (2003)"]
    PAT["Design patterns and pattern languages"] --> EVANS
    AGILE["Agile, XP, and continuous refactoring"] --> EVANS
    DATA["Data and enterprise application patterns"] --> EVANS
    EVANS --> MODEL["Ubiquitous Language and Model-Driven Design"]
    EVANS --> TACTICAL["Tactical building blocks"]
    EVANS --> STRATEGIC["Bounded Context, Context Map, Core Domain"]
    MODEL --> MODERN["Modern DDD practice"]
    MODERN --> ES["Event Sourcing and CQRS"]
    MODERN --> EDA["Event-driven integration"]
    MODERN --> MS["Microservices and cloud operations"]
    MODERN --> KH["Vlad Khononov's modern decision framework"]
\`\`\`

Evans's contribution was not the isolated invention of Entities or Services.
It was placing domain complexity at the center and connecting language,
modeling, implementation, refactoring, and strategy into one pattern language.

## How the two main sources complement each other

| Dimension | Eric Evans, Blue Book | Vlad Khononov, Learning DDD |
| --- | --- | --- |
| Primary role | Establishes the original philosophy and pattern language | Converts DDD into a modern decision path |
| Starting point | Knowledge crunching, language, model and code | Business strategy and Subdomain analysis |
| Tactical view | Entity, Value Object, Service, Aggregate, Factory, Repository | Select Transaction Script, Active Record, Domain Model, or Event-Sourced model by complexity |
| Strategic view | Bounded Context, Context Map, Core Domain, Distillation | Modern Subdomain classification and relationship heuristics |
| Architecture | Layered Architecture and Domain isolation | Layered, Ports and Adapters, CQRS |
| Integration | Context relationship patterns | Outbox, Saga, Process Manager, model translation |
| Distributed systems | Mostly outside its historical scope | Microservices, event-driven systems, and analytical data |
| Reading style | Dense, nuanced, example-driven | Structured, explanatory, decision-oriented |

One does not replace the other:

- the Blue Book explains **why the concepts exist and what they originally
  mean**;
- *Learning DDD* helps decide **when and how to apply them in modern systems**.

## An integrated model of DDD

### 1. Discovery and learning

- domain expert collaboration;
- Event Storming and concrete scenarios;
- Ubiquitous Language;
- explicit assumptions and unknowns;
- continuous knowledge crunching.

The result is not a frozen requirements document. It is shared understanding
that continues to evolve.

### 2. Strategic design

- business domain and Subdomains;
- Core, Supporting, and Generic classification;
- Bounded Contexts and model ownership;
- Context Maps and relationship patterns;
- Domain Vision and Core Domain investment.

The central question is: **Where should we focus, and where must meaning be
bounded?**

### 3. Tactical design

- Entity and Value Object;
- Aggregate and invariant;
- Domain Service and Domain Event;
- Factory and Repository;
- simple Transaction Scripts where complexity is low;
- Event-Sourced models only where temporal requirements justify them.

The central question is: **How should rules inside this specific Bounded
Context be expressed in code?**

Tactical patterns are not the whole of DDD. Without strategic design, they can
produce a more complicated object model without improving business alignment.

### 4. Application architecture and integration

- Application orchestration;
- Ports and Adapters;
- CQRS when decision and read needs diverge;
- Domain Event versus Integration Event;
- Outbox, Inbox, and idempotency;
- Saga or Process Manager for long-running workflows;
- Anti-Corruption Layer and Published Language.

The central question is: **How do we protect the model from technical details
and other models?**

### 5. Evolution and operations

- changing context boundaries and Subdomain classifications;
- observability and SLOs;
- contract and data migrations;
- failure handling and operational ownership;
- evidence-based service extraction.

The central question is: **How does the system keep learning as the business,
knowledge, organization, and workload change?**

## Common historical misreadings

### “DDD means implementing the 2003 model literally”

No. The Blue Book itself emphasizes learning and refactoring. Small Aggregates,
eventual consistency, and modern integration mechanisms continue the same
goals under new operational constraints.

### “DDD equals Clean Architecture”

No. Clean Architecture organizes dependency direction. DDD ranges from domain
discovery and language to strategic boundaries and modeling. The approaches
can support each other but are not interchangeable.

### “DDD equals microservices”

No. A Bounded Context is a semantic/model boundary; a microservice is primarily
an independent deployment and operations boundary. A Bounded Context can be:

- a protected module in a modular monolith;
- an independently deployed service; or
- several collaborating components with one model owner.

### “Every context needs a rich Domain Model”

No. A simple Supporting Subdomain may be clearer as a Transaction Script.
Design complexity should not exceed business complexity.

### “Using events creates loose coupling”

Not automatically. Fine-grained events that expose internal structures can
create logical, temporal, and implementation coupling. Stable ownership,
semantics, versioning, reliability, and consumer autonomy determine coupling.

## Recommended reading sequence

1. Blue Book chapters 1–3: knowledge, language, and model/code binding.
2. Khononov chapters 1–4: Subdomains, Bounded Contexts, and Context Maps.
3. Blue Book chapters 4–7: Domain isolation and tactical building blocks.
4. Khononov chapters 5–10: choosing models and architecture by complexity.
5. Blue Book chapters 8–13: deep models and insight-driven refactoring.
6. Blue Book chapters 14–17: original Strategic Design.
7. Khononov chapters 11–16: evolution, Event Storming, and modern distributed
   systems.
8. Revisit every concept against this repository's decisions and evidence.

Reading the sources side by side shows how the original principles and modern
heuristics address the same problem from different historical positions.

## Why Equipment Rental is a useful teaching domain

The Blue Book's extended example centers on cargo shipping. Reproducing it
would risk copying conclusions instead of transferring understanding. This
repository uses industrial equipment rental and field operations, which
naturally contains:

- quotation and pricing;
- reservations and capacity;
- rental lifecycle;
- delivery and return planning;
- planned and unexpected maintenance;
- damage inspection;
- billing and collection;
- customer/contract management;
- notifications and identity.

It provides meaningful examples of:

- Core, Supporting, and Generic Subdomains;
- time, money, capacity, and state-transition Value Objects/invariants;
- resource conflicts that force Aggregate-boundary decisions;
- a long-running confirmation process;
- a CQRS availability calendar;
- reliability patterns for cross-context facts; and
- the distinction between logical boundaries and service deployment.

The domain is broad enough to teach trade-offs while the implemented slice
remains intentionally small enough to understand.
