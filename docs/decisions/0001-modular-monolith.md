# ADR 0001: Begin as a modular monolith

- Status: Accepted
- Date: 2026-07-07

## Context

The product will eventually contain API, retrieval, ingestion, model, workflow, and tool concerns. Splitting each concern into a network service on day one would add deployment, observability, failure, and data-consistency complexity before load or team boundaries justify it.

## Decision

Deploy the initial ASP.NET Core application as one process with separate Domain, Application, Infrastructure, and API projects. Preserve asynchronous boundaries for Kafka ingestion and interfaces for providers. Extract a service only when independent scaling, availability, security, release cadence, or team ownership creates evidence for it.

## Consequences

### Positive

- Faster vertical-slice delivery and simpler local development
- In-process refactoring while the domain is still changing
- Clear dependency direction and testable boundaries
- Lower operating cost for the portfolio deployment

### Negative

- Modules share a deployment lifecycle
- Poor discipline could allow boundaries to erode
- A future extraction may require migration work

## Rejected alternative

Starting with many microservices was rejected because it optimizes for hypothetical scale and would hide the AI/retrieval learning behind infrastructure work.
