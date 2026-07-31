# ADR-0003 — Use .NET 10, C# 14, and SLNX

- Status: Accepted
- Date: 2026-07-31

## Context

The project should demonstrate current .NET practices on a long-term support
platform.

## Decision

- Target framework: `net10.0`
- Language: C# 14
- Initial SDK version: `10.0.103`
- Solution format: `.slnx`
- Central Package Management
- Nullable reference types
- Warnings as errors
- `latest-Recommended` .NET analyzer level

`global.json` permits newer feature bands within the same major/minor SDK but
does not accept preview SDKs.

## Consequences

- The repository uses the .NET 10 LTS baseline.
- SLNX is easier to read and produces cleaner diffs than classic SLN.
- Central version management reduces dependency drift.
- Analyzer upgrades can introduce warnings that must be addressed
  intentionally.

## Rejected options

- .NET 8 is a stable LTS but does not meet the project's .NET 10 objective.
- Preview .NET 11 adds unnecessary version risk to a portfolio project.
- `LangVersion=latest` could silently change language behavior with a new SDK.
