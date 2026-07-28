# ADR 001: Human review remains the publication control point

## Context

Accessibility checks and AI-generated suggestions are useful, but can be wrong, incomplete or inappropriate for a user's context.

## Decision

Every item starts in `NeedsReview`. A suggestion is optional, requires explicit consent and cannot alter the review status. Only a named reviewer can approve an item or request changes.

## Consequences

- The audit trail is understandable to editors and stakeholders.
- The system can change AI providers without changing the core workflow.
- Automation assists the team instead of hiding a business decision.
