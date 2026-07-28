# ADR 002: Deterministic checks run before any AI assistance

## Context

Several common content issues are objectively detectable: document language, title, landmarks, image alternative text, generic links and heading jumps.

## Decision

AccessFlow performs those checks locally first. The suggestion endpoint only summarizes the most urgent finding and is deliberately provider-neutral.

## Consequences

- The essential review queue works offline and without a model account.
- Results are reproducible in tests.
- Data minimisation is the default behaviour.
