# Ubiquitous Language — Initial Glossary

This glossary is scoped. A word may legitimately mean something different in
another bounded context, but it must not have two meanings inside the same
context.

## Rentals context

| Term | Meaning | Not the same as |
|---|---|---|
| Rental Order | Commercial commitment being negotiated or executed with a customer | Invoice, delivery job |
| Rental Period | Half-open time interval `[start, end)` requested for use | Delivery window |
| Rental Line | Requested equipment requirement and agreed commercial rate | Physical asset |
| Fulfilment Location | Operational location expected to supply the equipment | Customer job site, Fleet's shared object |
| Draft | Editable order with no commitment | Quote |
| Quoted | Commercial terms offered until a stated expiry | Confirmed rental |
| Confirmed | Commercial terms locked and all availability commitments obtained | Delivered |
| Cancelled | Order intentionally terminated before completion | Expired quote |
| Daily Rate | Money charged for one billable rental day | Final charge |
| Estimated Total | Sum derived from the agreed period and line rates | Invoice total |

## Fleet Availability context

| Term | Meaning | Not the same as |
|---|---|---|
| Equipment Category | Capability requested by Rentals | Physical asset |
| Availability Schedule | Capacity and commitments for one category at one operational location | Read-only calendar screen |
| Total Capacity | Maximum quantity that can be committed concurrently by one schedule | Number currently present in the yard |
| Asset | Individually tracked piece of equipment | Rental line |
| Availability | Ability to make a commitment for category, period, and location | Current physical presence |
| Hold | Short-lived, expiring protection while a quote is being finalized | Reservation |
| Reservation | Durable availability commitment for a confirmed rental | Rental order |
| Allocation | Selection of a specific asset to satisfy a reservation | Reservation itself |
| Conflict | Two commitments that cannot both be fulfilled | Any overlapping date |
| Substitute | Different category/model approved to satisfy the same capability | Arbitrary replacement |
| External Demand | Opaque consumer request whose retries must be idempotent | A Fleet-owned rental line |

## Maintenance context

| Term | Meaning | Not the same as |
|---|---|---|
| Quarantine | Asset is prohibited from being rented | Asset is merely unavailable at a location |
| Inspection | Recorded assessment of condition and safety | Repair |
| Work Order | Authorized maintenance work with lifecycle and tasks | Rental order |
| Service Due | Maintenance threshold has been reached | Equipment is necessarily unsafe |
| Return Condition | Observed state at return | Customer liability decision |

## Billing context

| Term | Meaning | Not the same as |
|---|---|---|
| Charge | Explainable monetary consequence of a commercial term or observed fact | Payment |
| Invoice | Issued request for payment containing charges | Rental estimate |
| Credit | Amount reducing customer debt | Refund transaction |
| Damage Charge | Approved charge derived from damage assessment | Damage observation |
| Overage | Usage beyond an included threshold | Late return |

## Language rules

- Use these terms in conversation, documentation, tests, API contracts, and
  source code.
- Avoid generic verbs such as `Process`, `Handle`, or `Update` when a domain
  verb exists.
- Avoid a universal `Status` model shared across contexts.
- Do not expose database terminology as domain language.
- Add examples and counterexamples when a definition is disputed.
- Change the glossary and code together when the model changes.
