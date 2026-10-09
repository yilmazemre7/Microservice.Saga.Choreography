# Microservice.Saga.Choreography

**Choreography tabanlı Saga Pattern** ile dağıtık transaction yönetimini gösteren örnek bir .NET 9 mikroservis projesi.

Merkezi bir orkestratör yoktur; her servis yalnızca ilgilendiği event'leri dinler, kendi işini yapar ve sonucunu yeni bir event olarak yayınlar. Bir adım başarısız olduğunda önceki adımlar **compensating (telafi edici) işlemlerle** geri alınır.

## Mimari

| Servis | Sorumluluk | Veritabanı | Port (http / https) |
|---|---|---|---|
| **Order.API** | Siparişi oluşturur, saga'yı başlatır, sipariş durumunu günceller | SQL Server (EF Core) | `5258` / `7144` |
| **Stock.API** | Stok kontrolü ve rezervasyonu, ödeme hatasında stoğu iade eder | MongoDB | `5198` / `7011` |
| **Payment.API** | Ödemeyi işler, sonucu event olarak yayınlar | — | `5065` / `7149` |
| **Shared** | Event/mesaj sözleşmeleri ve kuyruk isimleri | — | — |

Servisler arası iletişim **RabbitMQ** üzerinden **MassTransit** ile sağlanır.

## Saga Akışı

```mermaid
sequenceDiagram
    autonumber
    participant C as Client
    participant O as Order.API
    participant S as Stock.API
    participant P as Payment.API

    C->>O: POST /create-order
    O->>O: Order kaydet (Status = Suspend)
    O-)S: OrderCreatedEvent

    alt Stok yeterli
        S->>S: Stoktan düş
        S-)P: StockReservedEvent
        alt Ödeme başarılı
            P-)O: PaymentCompletedEvent
            O->>O: Status = Completed
        else Ödeme başarısız
            P-)O: PaymentFailedEvent
            O->>O: Status = Fail
            P-)S: PaymentFailedEvent
            S->>S: Stoğu iade et (compensation)
        end
    else Stok yetersiz
        S-)O: StockNotReservedEvent
        O->>O: Status = Fail
    end
```

### Event'ler

| Event | Yayınlayan | Dinleyen | Kuyruk |
|---|---|---|---|
| `OrderCreatedEvent` | Order.API | Stock.API | `stock-order-created-event-queue` |
| `StockReservedEvent` | Stock.API | Payment.API | `payment-stock-reserved-event-queue` |
| `StockNotReservedEvent` | Stock.API | Order.API | `order-stock-not-reserved-event-queue` |
| `PaymentCompletedEvent` | Payment.API | Order.API | `payment-completed-event-queue` |
| `PaymentFailedEvent` | Payment.API | Order.API, Stock.API | `order-payment-failed-event-queue`, `stock-payment-failed-event-queue` |

Kuyruk isimleri [Shared/RabbitMQSettings.cs](Shared/RabbitMQSettings.cs) içinde tanımlıdır.

### Sipariş Durumları

`OrderStatus` enum'u: `Suspend` (işlemde) → `Completed` (başarılı) veya `Fail` (stok/ödeme hatası).

## Teknolojiler

- .NET 9 / ASP.NET Core Minimal API
- MassTransit 9 + RabbitMQ
- Entity Framework Core 9 + SQL Server
- MongoDB.Driver 3
- Scalar (OpenAPI arayüzü)

## Proje Yapısı

```
Microservice.Saga.Choreography/
├── Order.API/
│   ├── Consumers/        # PaymentCompleted, PaymentFailed, StockNotReserved
│   ├── Enums/            # OrderStatus
│   ├── Migrations/
│   ├── Models/           # Order, OrderItem, OrderAPIDbContext
│   ├── ViewModels/       # CreateOrderVM, CreateOrderItemVM
│   └── Program.cs        # /create-order endpoint + MassTransit ayarları
├── Stock.API/
│   ├── Consumers/        # OrderCreated, PaymentFailed (compensation)
│   ├── Models/           # Stock
│   ├── Services/         # MongoDBService
│   └── Program.cs
├── Payment.API/
│   ├── Consumers/        # StockReserved
│   └── Program.cs
└── Shared/
    ├── Events/           # Saga event sözleşmeleri
    ├── Messages/         # OrderItemMessage
    └── RabbitMQSettings.cs
```

## Kurulum

### Gereksinimler

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- SQL Server
- MongoDB
- RabbitMQ (lokal veya CloudAMQP gibi bir servis)

Altyapıyı Docker ile hızlıca ayağa kaldırmak için:

```bash
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

```bash
docker run -d --name mongodb -p 27017:27017 -e MONGO_INITDB_ROOT_USERNAME=admin -e MONGO_INITDB_ROOT_PASSWORD=<sifre> mongo
```

### Yapılandırma

Her servisin `appsettings.json` dosyasına aşağıdaki ayarları ekleyin:

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
    "MongoDB": "mongodb://admin:<sifre>@localhost:27017/?authSource=admin"
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

> `appsettings.json` dosyaları kimlik bilgisi içerdiği için repoya eklenmemelidir.

### Veritabanı

Order.API için migration'ı uygulayın:

```bash
dotnet ef database update --project Order.API
```

Stock.API, `StockDB` veritabanındaki `stock` koleksiyonunu kullanır. Test için örnek stok kaydı ekleyin (mongosh):

```js
use StockDB
db.stock.insertOne({
  _id: UUID(),
  ProductId: UUID("11111111-1111-1111-1111-111111111111"),
  Count: 100,
  CreatedDate: new Date()
})
```

### Çalıştırma

Üç servisi ayrı terminallerde başlatın (veya Visual Studio'da *Multiple startup projects* seçin):

```bash
dotnet run --project Order.API
```

```bash
dotnet run --project Stock.API
```

```bash
dotnet run --project Payment.API
```

## Kullanım

Development ortamında Order.API'nin Scalar arayüzü: `http://localhost:5258/scalar`

Örnek sipariş isteği:

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

Sipariş önce `Suspend` durumunda kaydedilir; saga tamamlandığında `Orders` tablosundaki `OrderStatus` alanı `Completed` veya `Fail` olarak güncellenir.

## Notlar

- **Payment.API** şu an ödemeyi her zaman başarılı kabul eder (`if (true)`). Hata/compensation akışını denemek için [StockReservedEventConsumer.cs](Payment.API/Consumers/StockReservedEventConsumer.cs) içindeki koşulu değiştirin.
- Proje eğitim amaçlıdır; outbox/inbox, idempotency ve retry politikaları gibi üretim gereksinimleri kapsam dışıdır.
