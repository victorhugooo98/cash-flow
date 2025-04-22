# 0005: Resilience Patterns

Date: 2025-04-22
Status: Accepted

## Context
The non-functional requirements state:
- Transaction service must remain available if consolidation service is down
- Consolidation service must handle 50 requests/second with max 5% failure rate

We need resilience mechanisms to handle:
- Network failures
- Service outages
- Database connectivity issues
- Message broker unavailability

## Decision
Implement the following resilience patterns:

1. Circuit Breakers:
    - For messaging operations: 5 failures before opening
    - For database operations: 3 failures before opening
    - Reset timers: 30s for messaging, 15s for database

2. Retry Policies:
    - Database: Exponential backoff with 3 attempts
    - Message publishing: Non-blocking with 5 attempts
    - Message consumption: Retry with dead-letter queue

3. Graceful Degradation:
    - Transaction service continues operating if publishing fails
    - Logs failed events for manual recovery if needed

4. Idempotent Message Processing:
    - Track processed messages/transactions to prevent duplicates
    - Optimistic concurrency for balance updates

## Consequences
### Positive
- System remains operational during partial outages
- Automatic recovery from temporary failures
- Protects against cascading failures
- Meets non-functional requirements for availability

### Negative
- Increased complexity in code and deployment
- Need for monitoring of circuit breaker states
- Eventual consistency during recovery periods

## Alternatives Considered
1. No resilience patterns:
    - Simpler code but would not meet non-functional requirements

2. Synchronous retries only:
    - Would block request threads during retries
    - Could lead to resource exhaustion under load

3. Service mesh for resilience:
    - Would provide resilience outside application code
    - Adds significant infrastructure complexity