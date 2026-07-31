# Persistence and Optimistic Concurrency

The goal is not merely to use EF Core. It is to implement DDD consistency and
data-ownership decisions honestly in storage.

## Aggregate repositories

A repository is not a collection of tables. `IRentalOrderRepository` manages
the `RentalOrder` aggregate lifecycle; the Fleet repository manages
`AvailabilitySchedule`. Repositories are accessed through Aggregate Roots,
load all state required for a decision, expose no child-Entity repository, and
complete a transaction through `SaveChangesAsync`. `RentalLine` and
`AvailabilityCommitment` use separate tables but are not separate aggregates.

## Persistence-ignorant Domain model

Fluent mappings live in Infrastructure. Domain objects contain no `[Key]`,
`[Column]`, or `[Timestamp]` attributes. Private parameterless constructors and
setters support EF rehydration without granting callers mutation access;
changes still go through domain behaviors such as `Draft`, `AddLine`, `Quote`,
and `Commit`.

Strongly typed IDs map to PostgreSQL `uuid` through `HasConversion`. `Money`,
`RentalPeriod`, and `AvailabilityPeriod` remain single Value Objects in the
model while owned mappings expand them into relational columns.

## Schema per owner

```text
equipment_rental database
├── rentals
├── fleet_availability
├── fleet_availability_read
├── rentals_process_manager
└── notifications
```

Each schema owns its tables, Outbox/Inbox where relevant, and migration
history. Contexts do not create cross-schema foreign keys or query each other's
tables; they use Published Contracts. One database is a deployment choice, not
shared model ownership.

## Optimistic concurrency

Two requests can read capacity 2 and both decide locally that 2 units remain.
PostgreSQL's hidden `xmin` value is mapped with `IsRowVersion()`. EF includes
the version read earlier in the root update:

```sql
UPDATE fleet_availability.availability_schedules
SET updated_at_utc = ...
WHERE id = ... AND xmin = <version-read>;
```

After the first transaction changes `xmin`, the second updates zero rows and
EF raises `DbUpdateConcurrencyException`. The API maps this to `409 Conflict`
with `persistence.optimistic_concurrency_conflict`. Clients must reload current
state and resubmit intent; blind retry could apply a stale domain decision.

Child changes would normally update only child tables, so each DbContext also
touches the tracked root's `updated_at_utc`. This makes `xmin` protect the whole
aggregate boundary.

## Explicit migrations

The API does not migrate on startup. Multiple replicas racing to change schema
creates operational and privilege risks. Migrations are an explicit deployment
job for all five DbContexts, with the connection overridden through
`ConnectionStrings__Database` outside local development.

## Test strategy

EF Core InMemory cannot reproduce PostgreSQL types, migrations, or `xmin`.
Testcontainers runs real PostgreSQL and proves migrations, aggregate/Value
Object round trips, rehydration without new Domain Events, stale Rentals
conflicts, and prevention of stale Fleet overbooking. Unit tests prove domain
decisions; integration tests prove adapters preserve them.
