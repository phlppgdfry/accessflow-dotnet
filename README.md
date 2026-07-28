# AccessFlow

> **Content quality, with a human in control.**

[![CI](https://github.com/phlppgdfry/accessflow-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/phlppgdfry/accessflow-dotnet/actions/workflows/ci.yml)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Human review](https://img.shields.io/badge/AI-human--in--the--loop-1F7A5B)](docs/adr/001-human-review-is-the-control-point.md)

AccessFlow is a focused .NET portfolio project for teams that publish information for schools, public services and social organisations. It turns a small set of deterministic accessibility checks into an auditable review queue, while keeping a person in charge of every decision.

## Why this project

Automated checks are valuable, but they are not a conformance certificate and they should not publish content on their own. AccessFlow shows a practical backend workflow that combines:

- **.NET 10 / ASP.NET Core** minimal API and a polished, dependency-free review UI
- **Clean separation of concerns**: Domain, Application, Infrastructure and API projects
- **Accessibility-aware checks** for document language, title, main landmark, heading structure, image alt text and vague link labels
- **Safe AI posture**: explicit consent, provider-neutral adapter and human finality
- **Quality engineering**: unit tests, warnings-as-errors, formatting gate and GitHub Actions CI

## Live workflow

1. An editor submits a small HTML sample and source URL.
2. The inspection service produces transparent, actionable findings.
3. A reviewer approves the item or requests changes.
4. A safe suggestion can be requested only when the editor opted in.

## Run locally

```bash
git clone https://github.com/phlppgdfry/accessflow-dotnet.git
cd accessflow-dotnet
dotnet run --project src/AccessFlow.Api
```

Open the URL shown by ASP.NET Core (usually `http://localhost:5000`). The API includes `/healthz` and an OpenAPI document in Development.

## Test

```bash
dotnet test AccessFlow.slnx
dotnet format AccessFlow.slnx --verify-no-changes
```

## Architecture and production path

The application runs locally with an in-memory repository so reviewers can start it with no cloud account or secret. The production evolution is documented rather than faked: PostgreSQL persistence, Azure Blob Storage, Azure Service Bus and OpenTelemetry/Application Insights are the next adapter-level increments. See [architecture](docs/architecture.md) and the [ADRs](docs/adr/).

`docker compose up -d postgres` starts a PostgreSQL 17 service for the persistence increment.

## Deliberate boundaries

- No claim of WCAG conformance from automated checks alone.
- No external AI call by default, and never without consent.
- No automatic rewriting or publishing.
- No imitation of a vendor product; this is a transparent review workflow built as a learning and portfolio project.

## Repository map

```text
src/
  AccessFlow.Domain/          Core model and review states
  AccessFlow.Application/     Inspection and review use cases
  AccessFlow.Infrastructure/  Local repository adapter
  AccessFlow.Api/             HTTP API and review desk UI
tests/                        Unit and workflow tests
docs/adr/                     Recorded architectural choices
```

## Next increments

1. PostgreSQL adapter with EF Core migrations.
2. Azure Blob Storage and Service Bus worker for document ingestion.
3. OAuth/roles and immutable audit events.
4. Optional Umbraco webhook adapter.
5. OpenTelemetry traces exported to Application Insights.

---

Built by [Philippe Godfroy](https://github.com/phlppgdfry) as a public .NET portfolio project.
