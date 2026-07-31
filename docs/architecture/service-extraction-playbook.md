# Service Extraction Playbook

This is not today's deployment plan. It is the safe migration path to use only
when a measured driver crosses the
[assessment thresholds](service-extraction-assessment.md).

## Principles

1. The Bounded Context boundary exists before deployment changes.
2. One context owns every write.
3. Application code never dual-writes two databases.
4. Internal Domain Models are not wire contracts.
5. A network call is not treated like a local method call.
6. Cutover is incremental, observable, and reversible.
7. The old path remains until the new path is proven.

## Common migration stages

### 0. Validate the decision

- Record the hard driver and its baseline.
- Explain why a cheaper option is insufficient.
- Assign context ownership, SLO, error budget, and on-call responsibility.
- Open an extraction ADR with \`Proposed\` status.

### 1. Stabilize contracts

- Version command/request and Integration Event schemas.
- Add contract compatibility tests.
- Document idempotency keys and retry semantics.
- Define parallel-version and sunset policies for breaking changes.

The in-process \`.Contracts\` assembly expresses today's design boundary. After
extraction, do not force two services to release the same binary together.
Publish an independently versioned JSON Schema, AsyncAPI, or Protobuf contract.

### 2. Add a transport adapter

Keep the consumer-owned port and place a remote adapter beside the local one:

\`\`\`text
Rentals Application
    → IEquipmentAvailabilityGateway
        ├─ InProcessFleetAvailabilityGateway
        └─ RemoteFleetAvailabilityGateway
\`\`\`

A feature flag in the composition root chooses the adapter. Domain and
Application layers remain unaware of transport.

### 3. Separate data

- Copy the context schema to an independent database.
- Verify no foreign key or direct SQL crosses context boundaries.
- Choose snapshot plus change-stream/backfill mechanics.
- Produce a reconciliation report.
- After cutover, only the new service writes its data.

Never update two databases for one business change in application code. A
consumer derives its local model from an Integration Event.

### 4. Shadow and canary

- When possible, compare a remote read or decision in shadow mode without
  changing the user result.
- Log differences with primitive identifiers and correlation IDs.
- Route a small traffic percentage to the new service.
- Compare latency, errors, saturation, and business outcomes.

Do not apply a state-changing command to both systems without explicit
idempotency and reconciliation.

### 5. Cut over

- Sequence producers and consumers using backward-compatible contracts.
- Increase traffic gradually.
- Measure Outbox lag, Inbox duplicates, timeouts, and business rejections
  separately.
- Keep the old adapter available during the rollback window.

### 6. Remove the old path

- Delete the local adapter after the stabilization window.
- Make the old schema read-only, then archive it under the retention policy.
- Tighten architecture tests to reflect the new boundary.
- Mark the ADR \`Accepted\` and record measured results.

## Fleet Availability extraction

### Why it needs special care

Rentals must know whether a commitment succeeded before confirmation can
continue. Once remote, every commit/release request has three outcomes:

1. explicit acceptance or rejection;
2. the request never reached Fleet; or
3. Fleet committed it but the response was lost.

A timeout is therefore not equivalent to failure. Retrying with the same
\`DemandId\` must return the previous idempotent result.

### Target communication

- Commit/release decisions: synchronous request/response with bounded timeout.
- Capacity and commitment facts: durable Integration Events through a broker.
- Every request: correlation ID and stable \`DemandId\`.
- Retry: idempotent operations only, with bounded exponential backoff.
- Circuit breaker: technical fast-failure, never a business rejection.
- Process Manager: retries ambiguous outcomes and moves to
  \`RequiresIntervention\` when the budget is exhausted.

### Cutover sequence

1. Run Fleet in an independent host with unchanged contract semantics.
2. Migrate and reconcile data into a Fleet-owned database.
3. Add the remote gateway behind a feature flag.
4. Canary read-only availability queries.
5. Canary idempotent commit/release calls.
6. Publish Fleet Integration Events through the broker.
7. Remove the in-process gateway after reconciliation and stabilization.

### Rollback

- Disable the remote adapter feature flag.
- Do not blindly return writes to the old database after ownership cutover.
- Keep Fleet running while rolling back the caller or routing traffic to the
  previous API version.
- Never transfer ownership back before reconciliation completes.

## Notifications extraction

### Why it is lower risk

Rentals does not wait for notification delivery. The event is already
versioned, its Outbox message has a stable ID, and the Notifications Inbox
deduplicates at-least-once delivery.

### Target communication

\`\`\`text
Rentals transaction
    → Rentals Outbox
    → durable broker
    → Notifications worker
    → Inbox + notification work item transaction
\`\`\`

The broker is acknowledged only after Inbox and work item commit. Poison
messages move to a dead-letter path after a bounded retry budget, while event
identity remains unchanged for replay.

### Cutover sequence

1. Add a broker publisher behind \`IIntegrationEventPublisher\`.
2. Deploy the Notifications consumer as an independent worker.
3. Apply its database/schema migration.
4. Compare local and remote effects in a controlled environment.
5. Switch the production publisher to the broker.
6. monitor Inbox count, duplicates, oldest-message age, and dead letters.
7. Remove local consumer registration after stabilization.

### Rollback

- Keep broker events durable while rolling back the consumer.
- Re-enable the old consumer only when exactly one owner reads the event stream.
- Preserve the Inbox identity so replay cannot create duplicate work.

## Availability Calendar

Running a projection in another process does not automatically make it a
separate Bounded Context. If read traffic grows, deploy the projection worker
or query host independently while Fleet Availability retains model ownership.

\`\`\`text
separate read model ≠ separate bounded context
separate process    ≠ new bounded context
bounded context     ≠ mandatory separate process
\`\`\`

## Completion criteria

- The old code path is removed.
- A single write owner is verified.
- Contract compatibility tests pass.
- SLO and error-budget dashboards are active.
- Outbox/Inbox lag and dead-letter runbooks exist.
- Failure injection covers timeouts, duplicates, and broker outages.
- A rollback rehearsal is recorded.
- Measured benefits justify the additional operational cost.
