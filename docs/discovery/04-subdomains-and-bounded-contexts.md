# Subdomain Analysis and Bounded Context Candidates

Subdomains are discovered business capabilities. Bounded contexts are designed
model boundaries. The mapping below is an initial hypothesis, not a claim that
the two concepts are synonyms.

## Subdomain classification

| Subdomain | Type | Why | Initial implementation posture |
|---|---|---|---|
| Rental commercial lifecycle | Core | Converts demand into profitable, controlled commitments | Rich domain model |
| Fleet availability planning | Core | Utilization without overbooking is a competitive advantage | Rich domain model; CQRS read calendar |
| Maintenance planning | Core/Supporting hotspot | Can differentiate uptime, but strategy must confirm this | Domain model, revisit classification |
| Dispatch and collection | Supporting initially | Necessary and company-specific, but route optimization is out of scope | Transaction script/domain model as complexity emerges |
| Return inspection | Supporting | Required workflow with clear rules | Domain model only where invariants justify it |
| Billing | Supporting | Rental-specific charges; accounting itself is not our differentiator | Explicit charge model; integrate accounting later |
| Customer accounts | Supporting | Contract and site information needed in-house | Simple model |
| Payments | Generic | Mature external capability | Buy/integrate through ACL |
| Identity and access | Generic | Security capability is not the differentiator | Standards-based external provider |
| Notifications | Generic | Commodity delivery capability | Provider abstraction, no domain logic |

## Proposed bounded contexts

### Rentals

Owns:

- rental order lifecycle;
- accepted commercial terms;
- rental periods and line pricing;
- confirmation and cancellation rules.

Does not own:

- physical asset availability;
- maintenance condition;
- invoice and payment state;
- delivery routing.

### Fleet Availability

Owns:

- category capacity;
- holds, reservations, and allocations;
- time/location conflicts;
- availability calendar.

It answers “can we commit?” It does not decide commercial price.

### Maintenance

Owns:

- asset condition;
- inspections;
- quarantine;
- maintenance work orders;
- service thresholds.

It can remove an asset from availability but does not cancel a customer
commitment directly.

### Field Operations

Owns:

- delivery and collection jobs;
- dispatch schedule;
- proof of handover and return;
- site visit execution.

### Billing

Owns:

- charge calculation;
- invoices and credits;
- payment allocation status;
- financial audit explanation.

It consumes agreed terms and observed usage as facts. It must not reach into
Rentals tables.

### Customer Accounts

Owns:

- customer account;
- billing profile;
- sites and contacts;
- rental agreement eligibility.

### Notifications

Owns communication delivery and templates, not business decisions about what a
notification means.

## Why these are not microservices

The contexts begin as modules in one deployable application. Model boundary,
code ownership boundary, data ownership boundary, and deployment boundary are
related but not identical decisions.

Starting with separate services would add:

- network failure;
- message delivery;
- distributed tracing;
- deployment coordination;
- eventual consistency

before the context boundaries have been validated. Modules preserve the
semantic boundaries while keeping early feedback fast.
