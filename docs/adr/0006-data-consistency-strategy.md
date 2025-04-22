# 0006: Data Consistency Strategy

Date: 2025-04-22
Status: Accepted

## Context
In our distributed microservices architecture:
- Transaction and Consolidation services have separate databases
- Communication occurs through asynchronous events
- Service outages could lead to missed events and data inconsistency
- High throughput requires efficient data consistency mechanisms

## Decision
Implement a hybrid consistency strategy:

1. Strong Consistency:
    - Within each service boundary (ACID transactions)
    - For critical operations like transaction recording

2. Eventual Consistency:
    - Between services (Transaction → Consolidation)
    - Daily balance reflects all processed transactions eventually

3. Consistency Mechanisms:
    - Idempotency tables to track processed message IDs
    - Optimistic concurrency control with retry for balance updates
    - Distributed lock for concurrent balance modifications
    - Manual reconciliation tools for recovery

## Consequences
### Positive
- Scalable architecture without distributed transactions
- High availability within service boundaries
- Handles network partitions gracefully
- Recovery possible from most failure scenarios

### Negative
- Reporting may show stale data temporarily
- Increased complexity in handling edge cases
- Need for monitoring of inconsistency states
- Reconciliation required after prolonged outages

## Alternatives Considered
1. Two-phase commit:
    - Would provide strong consistency across services
    - Severe impact on availability and performance

2. Saga pattern:
    - Compensating transactions for failures
    - Excessive for the relatively simple workflows

3. Shared database:
    - Would provide data consistency
    - Violates service independence requirements