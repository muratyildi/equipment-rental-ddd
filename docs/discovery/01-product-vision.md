# Product Vision — Industrial Equipment Rental Platform

## Working name

**Equipment Rental Platform**

The name is intentionally descriptive at this stage. A brand name is not part of
the domain model and must not distract us from discovering the business.

## Vision

For industrial contractors who need the right equipment at the right place and
time, the platform coordinates quotation, reservation, delivery, rental,
collection, inspection, and billing while continuously protecting equipment
availability.

Unlike a generic inventory application, the product treats time, location,
maintenance condition, commercial commitments, and operational capacity as
first-class business concepts.

## Business problem

Industrial rental companies lose revenue and customer trust when:

- sales promises equipment that is not actually available;
- maintenance and rental schedules conflict;
- quote conditions change silently between acceptance and delivery;
- late returns make later reservations impossible;
- damage, fuel, usage, and overtime charges lack an auditable basis;
- operational teams maintain different meanings for terms such as
  “available,” “reserved,” and “returned.”

The product is intended to reduce these conflicts without forcing every
department into one universal data model.

## Target users and domain experts

| Role | Goal | Domain knowledge |
|---|---|---|
| Rental agent | Prepare and convert a commercially valid quote | Pricing, deposits, discounts, customer agreements |
| Availability planner | Protect commitments across time and locations | Fleet capacity, substitutes, holds, allocation |
| Dispatcher | Deliver and collect equipment on time | Routes, vehicles, crews, site constraints |
| Maintenance planner | Keep equipment safe and rentable | Service intervals, inspections, work orders |
| Yard operator | Hand over and receive physical equipment | Condition, meter readings, accessories |
| Billing specialist | Invoice what was contractually and actually consumed | Rates, taxes, damage, overtime, credits |
| Customer | Obtain usable equipment for a job | Required capability, site, time window |

These roles may disagree because they solve different problems. Such
disagreement is evidence for multiple models and possibly multiple bounded
contexts, not something to hide in one large entity.

## Success outcomes

Initial product-level outcomes:

- No confirmed rental is created without an explicit availability commitment.
- Every commercial amount records currency and pricing basis.
- Quote acceptance cannot silently change the accepted price or rental period.
- Maintenance quarantine prevents new availability commitments.
- Every cross-context business transition is observable and auditable.
- A term has one meaning inside a bounded context; translations are explicit at
  context boundaries.

These are outcomes, not yet implementation decisions.

## Non-goals for the first release

- Marketplace functionality between multiple rental companies
- AI-based dynamic pricing
- Full route optimization
- General-purpose accounting
- Payroll and human resources
- IoT telemetry ingestion
- A microservice per bounded context

Excluding a capability now does not mean it can never exist. It keeps the first
model small enough to learn from.

## First walking-skeleton scenario

> A rental agent drafts a rental order for a customer, fulfilment location,
> period, and requested equipment. The order calculates an estimated total from
> explicit line rates.
> The agent submits the order for confirmation. Confirmation is allowed only
> after every line has an active availability commitment for exactly the
> requested period. A confirmed order can no longer have its commercial terms
> edited silently.

This scenario is narrow enough to implement first and rich enough to exercise:

- identity and lifecycle;
- Money and DateRange value objects;
- aggregate invariants;
- domain events;
- application orchestration;
- a boundary between Rentals and Fleet Availability.
