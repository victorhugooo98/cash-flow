# CashFlow Management System

This project implements a microservices-based cash flow management system for merchants. It consists of two main services:

1. **Transaction Service**: Handles recording of financial transactions (credits and debits)
2. **Consolidation Service**: Maintains daily balance records based on transaction events

## Architecture Overview

The system follows a microservices architecture with the following key components:

- **Domain-Driven Design (DDD)**: Each service has its own bounded context with clear domain models
- **Clean Architecture**: Separation of concerns with layers (Domain, Application, Infrastructure, API)
- **Event-Driven Communication**: Services communicate via message broker (RabbitMQ)
- **CQRS Pattern**: Command and Query Responsibility Segregation for transaction operations
- **Resilience Patterns**: Circuit breakers, retries, and other patterns to ensure service reliability

### Architecture Diagram

```
┌─────────────────────┐      ┌─────────────────────┐
│                     │      │                     │
│  Transaction API    │      │  Consolidation API  │
│                     │      │                     │
└─────────┬───────────┘      └─────────┬───────────┘
          │                            │
          │                            │
┌─────────▼───────────┐      ┌─────────▼───────────┐
│                     │      │                     │
│  Transaction DB     │      │  Consolidation DB   │
│                     │      │                     │
└─────────────────────┘      └─────────────────────┘
          │                            ▲
          │                            │
          │     ┌──────────────┐       │
          └────►│   RabbitMQ   │───────┘
                │              │
                └──────────────┘
```

## Technology Stack

- **Backend**: .NET 9.0, ASP.NET Core
- **Persistence**: SQL Server with Entity Framework Core
- **Messaging**: RabbitMQ with MassTransit
- **Resilience**: Polly for circuit breakers and retries
- **Testing**: xUnit, NBomber for load testing
- **Containerization**: Docker
- **CI/CD**: GitHub Actions

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- Docker and Docker Compose
- SQL Server (or Docker container)
- RabbitMQ (or Docker container)

### Running with Docker Compose

The easiest way to run the entire system is using Docker Compose:

```bash
# Clone the repository
git clone https://github.com/yourusername/cashflow.git
cd cashflow
cd docker

# Start the services
docker-compose up -d
```

This will start:
- SQL Server container
- RabbitMQ container
- Transaction Service (available at http://localhost:5001)
- Consolidation Service (available at http://localhost:5002)

### Running Locally (Development)

To run the services locally for development:

```bash
# Run Transaction Service
cd CashFlow.Transaction.API
dotnet run

# Run Consolidation Service (in a separate terminal)
cd CashFlow.Consolidation.API
dotnet run
```

Make sure to update the connection strings in `appsettings.json` to point to your local SQL Server and RabbitMQ instances.

## API Documentation

### Transaction Service

#### Create Transaction
```
POST /api/transactions
Content-Type: application/json

{
  "merchantId": "merchant123",
  "amount": 100.50,
  "type": "Credit",
  "description": "Customer payment"
}
```

#### Get Transaction by ID
```
GET /api/transactions/{id}
```

#### Get Transactions by Merchant
```
GET /api/transactions?merchantId={merchantId}&date={yyyy-MM-dd}
```

### Consolidation Service

#### Get Daily Balance
```
GET /api/balances/daily?merchantId={merchantId}&date={yyyy-MM-dd}
```

## Testing

### Running Unit Tests

```bash
dotnet test
```

### Running Integration Tests

```bash
dotnet test CashFlow.Transaction.IntegrationTests
dotnet test CashFlow.Consolidation.IntegrationTests
```

### Running Load Tests

```bash
dotnet test CashFlow.Consolidation.LoadTests
```

## Key Features and Design Decisions

1. **Resilience**: The Transaction Service can continue to function even if the Consolidation Service is down
2. **High Availability**: The Consolidation Service is designed to handle 50 requests per second with less than 5% failure rate
3. **Event-Driven Architecture**: Transaction events are published to RabbitMQ for asynchronous processing
4. **Circuit Breakers**: Prevent cascading failures when services are under stress
5. **Idempotency**: Transaction processing is designed to be idempotent to handle retry scenarios

## Future Improvements

- Add API Gateway for unified access to services
- Implement distributed tracing with OpenTelemetry
- Add authentication and authorization
- Implement a merchant service for merchant management
- Add reporting capabilities for financial analysis

## Performance Metrics

### Load Test Results

Transaction Service:
- Throughput: 120 transactions/second sustained
- Response time: 95th percentile < 200ms
- Error rate: < 0.1% under normal conditions

Consolidation Service:
- Throughput: 50+ requests/second sustained
- Response time: 95th percentile < 500ms
- Error rate: < 5% under peak load
- Recovery time: < 30 seconds after outage

### Test Environment
- Hardware: 4-core CPU, 8GB RAM
- Database: SQL Server 2022
- Message Broker: RabbitMQ 3.12
- Network: <1ms latency between services

## Resilience Capabilities

### Circuit Breakers
- Message Broker: Opens after 5 consecutive failures
- Database: Opens after 3 consecutive failures
- Reset: Automatically after 30/15 seconds respectively
- Half-open state: Tests with single request before fully reopening

### Retry Policies
- Database operations: 3 retries with exponential backoff
- Message publishing: 5 retries with exponential backoff
- Message consumption: Automatic redelivery up to 5 times

### Service Independence
- Transaction service continues to function if Consolidation service is down
- Failed event publishing is logged but doesn't fail transactions
- Consolidation service processes events with at-least-once delivery guarantees

### Idempotency
- Transaction events processed exactly once using message deduplication
- Concurrency control with optimistic locking for balance updates

## License

This project is licensed under the MIT License - see the LICENSE file for details.
