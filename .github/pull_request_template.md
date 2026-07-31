## Problem and bounded context

<!-- What business or technical problem is being solved? Which context owns it? -->

## Behavior and design

<!-- Describe behavior before/after and where the invariant or policy lives. -->

## Alternatives and trade-offs

<!-- Which realistic alternatives were rejected, and why? -->

## Compatibility and operations

<!-- Contract, migration, event version, rollout, observability, or rollback impact. -->

## Evidence

- [ ] Business behavior has executable examples where applicable.
- [ ] Module boundaries and Published Contracts remain explicit.
- [ ] Database model changes include migrations.
- [ ] Documentation or an ADR is updated for a significant decision.
- [ ] `bash scripts/verify-repository.sh` passes.
- [ ] `dotnet format EquipmentRental.slnx --no-restore --verify-no-changes` passes.
- [ ] Release build and all tests pass.
