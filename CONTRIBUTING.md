# Contributing

## Before changing code

For business behavior:

1. identify the bounded context;
2. update the ubiquitous language if terminology changes;
3. write the business example;
4. place the rule in the domain model that owns it;
5. record a decision when boundaries or trade-offs change.

Do not introduce a shared domain entity to avoid model translation.

## Quality gate

```bash
dotnet restore EquipmentRental.slnx
dotnet tool restore
dotnet format EquipmentRental.slnx --no-restore --verify-no-changes
dotnet build EquipmentRental.slnx --configuration Release --no-restore
./scripts/verify-migrations.sh
dotnet test EquipmentRental.slnx --configuration Release --no-build
./scripts/verify-repository.sh
```

All warnings are errors. Test names may use
`Behavior_Scenario_ExpectedOutcome` because they serve as executable business
documentation.

## Pull requests

Describe:

- the business problem;
- the bounded context;
- the behavior before and after;
- rejected alternatives;
- tests proving the behavior;
- migration or compatibility concerns.
