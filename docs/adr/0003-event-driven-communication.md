# 0003: Event-Driven Communication

Date: 2025-04-22
Status: Accepted

## Context
With our microservices architecture, we need a reliable communication method between the Transaction and Consolidation services that:
- Preserves independence of services
- Handles service unavailability gracefully
- Provides eventual consistency
- Supports the processing of high transaction volumes

## Decision
Implement event-driven communication using:
- RabbitMQ as the message broker
- MassTransit as the messaging framework
- Publish-subscribe pattern for event distribution
- Event schemas defined in a shared library

Key events:
- TransactionCreatedEvent: Published when a new transaction is recorded

## Consequences
### Positive
- Decouples services temporally (can operate independently)
- Provides natural retry and recovery mechanisms
- Allows for event replay for recovery
- Enables future event consumers without modifying publishers

### Negative
- Eventual consistency (reporting may lag behind transaction recording)
- More complex debugging across service boundaries
- Need for message broker infrastructure and maintenance
- Requires idempotent event handling

## Alternatives Considered
1. Synchronous REST API calls:
    - Simpler implementation but introduces tight coupling
    - Would create direct dependency violating non-functional requirements

2. Database-based integration:
    - Services could share a database or use DB replication
    - Tight coupling at data layer, difficult schema evolution

3. Kafka instead of RabbitMQ:
    - Better for extreme scale, longer retention, stream processing
    - More complex to set up and maintain for this scale