# Microservice.Saga.Choreography

A .NET 9 microservices sample demonstrating distributed transaction management with the **Choreography-based Saga pattern**.

There is no central orchestrator. Each service listens only to the events it cares about, does its own work, and publishes the result as a new event. When a step fails, earlier steps are rolled back through **compensating actions**.

## Architecture

| Service | Responsibility | Database | Port (http / https) |
|---|---|---|---|
| **Order.API** | Creates the order, starts the saga, updates the order status | SQL Server (EF Core) | `5258` / `7144` |
| **Stock.API** | Checks and reserves stock, restores stock when payment fails | MongoDB | `5198` / `7011` |
| **Payment.API** | Processes the payment and publishes the result as an event | — | `5065` / `7149` |
| **Shared** | Event/message contracts and queue names | — | — |

Services communicate through **RabbitMQ** using **MassTransit**.

## Saga Flow

```mermaid
sequenceDiagram
    autonumber
    participant C as Client
    participant O as Order.API
    participant S as Stock.API
    participant P as Payment.API

    C->>O: POST /create-order
    O->>O: Save order (Status = Suspend)
    O-)S: OrderCreatedEvent

    alt Stock available
        S->>S: Decrease stock
        S-)P: StockReservedEvent
        alt Payment succeeds
            P-)O: PaymentCompletedEvent
            O->>O: Status = Completed
        else Payment fails
            P-)O: PaymentFailedEvent
            O->>O: Status = Fail
            P-)S: PaymentFailedEvent
            S->>S: Restore stock (compensation)
        end
    else Insufficient stock
        S-)O: StockNotReservedEvent
        O->>O: Status = Fail
    end
```

### Events

| Event | Published by | Consumed by | Queue |
|---|---|---|---|
| `OrderCreatedEvent` | Order.API | Stock.API | `stock-order-created-event-queue` |
| `StockReservedEvent` | Stock.API | Payment.API | `payment-stock-reserved-event-queue` |
| `StockNotReservedEvent` | Stock.API | Order.API | `order-stock-not-reserved-event-queue` |
| `PaymentCompletedEvent` | Payment.API | Order.API | `payment-completed-event-queue` |
| `PaymentFailedEvent` | Payment.API | Order.API, Stock.API | `order-payment-failed-event-queue`, `stock-payment-failed-event-queue` |

Queue names are defined in [Shared/RabbitMQSettings.cs](Shared/RabbitMQSettings.cs).

### Order Statuses

The `OrderStatus` enum: `Suspend` (in progress) → `Completed` (success) or `Fail` (stock/payment failure).

## Tech Stack

- .NET 9 / ASP.NET Core Minimal API
- MassTransit 9 + RabbitMQ
- Entity Framework Core 9 + SQL Server
- MongoDB.Driver 3
- Scalar (OpenAPI UI)

## Project Structure

```
Microservice.Saga.Choreography/
├── Order.API/
│   ├── Consumers/        # PaymentCompleted, PaymentFailed, StockNotReserved
│   ├── Enums/            # OrderStatus
│   ├── Migrations/
│   ├── Models/           # Order, OrderItem, OrderAPIDbContext
│   ├── ViewModels/       # CreateOrderVM, CreateOrderItemVM
│   └── Program.cs        # /create-order endpoint + MassTransit setup
├── Stock.API/
│   ├── Consumers/        # OrderCreated, PaymentFailed (compensation)
│   ├── Models/           # Stock
│   ├── Services/         # MongoDBService
│   └── Program.cs
├── Payment.API/
│   ├── Consumers/        # StockReserved
│   └── Program.cs
└── Shared/
    ├── Events/           # Saga event contracts
    ├── Messages/         # OrderItemMessage
    └── RabbitMQSettings.cs
```

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- SQL Server
- MongoDB
- RabbitMQ (local or a hosted service such as CloudAMQP)

To spin up the infrastructure quickly with Docker:

```bash
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

```bash
docker run -d --name mongodb -p 27017:27017 -e MONGO_INITDB_ROOT_USERNAME=admin -e MONGO_INITDB_ROOT_PASSWORD=<password> mongo
```

### Configuration

Add the following settings to each service's `appsettings.json`:

**Order.API**

```json
{
  "ConnectionStrings": {
    "SQLServer": "Server=localhost;Database=SagaChereographyDB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "RabbitMQ": "amqp://guest:guest@localhost:5672"
}
```

**Stock.API**

```json
{
  "ConnectionStrings": {
    "MongoDB": "mongodb://admin:<password>@localhost:27017/?authSource=admin"
  },
  "RabbitMQ": "amqp://guest:guest@localhost:5672"
}
```

**Payment.API**

```json
{
  "RabbitMQ": "amqp://guest:guest@localhost:5672"
}
```

> `appsettings.json` files contain credentials and should not be committed to the repository.

### Database

Apply the Order.API migration:

```bash
dotnet ef database update --project Order.API
```

Stock.API uses the `stock` collection in the `StockDB` database. Insert a sample stock record for testing (mongosh):

```js
use StockDB
db.stock.insertOne({
  _id: UUID(),
  ProductId: UUID("11111111-1111-1111-1111-111111111111"),
  Count: 100,
  CreatedDate: new Date()
})
```

### Running

Start the three services in separate terminals (or use *Multiple startup projects* in Visual Studio):

```bash
dotnet run --project Order.API
```

```bash
dotnet run --project Stock.API
```

```bash
dotnet run --project Payment.API
```

## Usage

In the Development environment, the Order.API Scalar UI is available at `http://localhost:5258/scalar`.

Sample order request:

```http
POST http://localhost:5258/create-order
Content-Type: application/json

{
  "buyerId": "22222222-2222-2222-2222-222222222222",
  "orderItems": [
    {
      "productId": "11111111-1111-1111-1111-111111111111",
      "count": 2,
      "price": 150.00
    }
  ]
}
```

The order is first saved with the `Suspend` status. When the saga finishes, the `OrderStatus` column in the `Orders` table is updated to `Completed` or `Fail`.

## Notes

- **Payment.API** currently always treats payment as successful (`if (true)`). To try the failure/compensation flow, change the condition in [StockReservedEventConsumer.cs](Payment.API/Consumers/StockReservedEventConsumer.cs).
- This project is for educational purposes; production concerns such as outbox/inbox, idempotency, and retry policies are out of scope.
