# Changelog

This file records user-visible and architectural changes to the reference
implementation.

## [Unreleased]

No changes yet.

## [1.0.0] - 2026-07-31

### Changed

- Promoted the validated reference implementation from release candidate to
  its first stable release.
- Published architecture guides, ADRs, and learning material in consistent
  English for an international audience.
- Replaced Turkish documentation filenames and repaired all internal Markdown
  links.
- Updated the GitHub Actions and .NET test toolchain through validated
  Dependabot pull requests.
- Excluded local `tmp/` artifacts from repository publication.

### Quality evidence

- 81 executable tests pass.
- Release build completes with zero warnings and zero errors.
- All five EF Core models match their latest migration snapshots.
- Repository hygiene, Markdown links, Compose configuration, and the non-root
  runtime container image pass locally and on GitHub-hosted CI.

## [1.0.0-rc.1] - 2026-07-31

### Added

- Domain discovery artifacts: product vision, Event Storming, Ubiquitous
  Language, subdomain analysis, and Context Map.
- .NET 10 modular monolith with Rentals, Fleet Availability, and Notifications
  bounded contexts.
- Rich `RentalOrder` and `AvailabilitySchedule` aggregates with explicit Value
  Objects, invariants, and Domain Events.
- Consumer-owned ports, anticorruption adapters, and versioned Published
  Contracts between contexts.
- PostgreSQL persistence with one schema and DbContext per owner, EF Core
  migrations, and optimistic concurrency.
- Transactional Outbox publication, transactional Inbox consumption,
  idempotency, bounded retries, dead-letter state, and operator intervention.
- CQRS Availability Calendar projection and a persisted Rental Confirmation
  Process Manager with timeout and compensation.
- Scoped API-key authentication, authorization, rate limiting, Problem
  Details, correlation IDs, structured logs, health checks, and runtime
  instrumentation.
- Multi-stage non-root .NET container, migration job, and complete Docker
  Compose environment.
- Evidence-based service-extraction assessment, fitness functions, ADR, and
  reversible extraction playbook.
- GitHub release-candidate gates for formatting, repository hygiene, Markdown
  links, migration drift, build, test, coverage reports, Compose validation,
  and container image creation.

### Quality evidence

- 81 executable tests: 22 domain, 4 application, 18 architecture, 24
  PostgreSQL integration, and 13 API tests.
- Release build completes with zero warnings and zero errors.
- All five EF Core models match their latest migration snapshots.

[Unreleased]: https://github.com/muratyildi/equipment-rental-ddd/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/muratyildi/equipment-rental-ddd/releases/tag/v1.0.0
[1.0.0-rc.1]: https://github.com/muratyildi/equipment-rental-ddd/releases/tag/v1.0.0-rc.1
