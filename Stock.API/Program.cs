using MassTransit;
using MongoDB.Driver;
using Shared;
using Stock.API.Consumers;
using Stock.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderCreatedEventConsumer>();
    x.AddConsumer<PaymentFailedEventConsumer>();
    x.UsingRabbitMq((context, configurator) =>
    {
        configurator.Host(builder.Configuration["RabbitMQ"]);
        configurator.ReceiveEndpoint(RabbitMQSettings.Stock_OrderCreatedEvent, e =>
        {
            e.ConfigureConsumer<OrderCreatedEventConsumer>(context);
        });
        configurator.ReceiveEndpoint(RabbitMQSettings.Stock_PaymentFailedEvent, e =>
        {
            e.ConfigureConsumer<PaymentFailedEventConsumer>(context);
        });
    });
});

builder.Services.AddSingleton<MongoDBService>();

var app = builder.Build();

using IServiceScope scope = app.Services.CreateScope();
MongoDBService mongoDBService = scope.ServiceProvider.GetRequiredService<MongoDBService>();
var stockCollection = mongoDBService.GetCollection<Stock.API.Models.Stock>();
if (!stockCollection.FindSync(session => true).Any())
{
    await stockCollection.InsertOneAsync(new() { ProductId = Guid.NewGuid(), Count = 100, CreatedDate = DateTime.UtcNow });
    await stockCollection.InsertOneAsync(new() { ProductId = Guid.NewGuid(), Count = 200, CreatedDate = DateTime.UtcNow });
    await stockCollection.InsertOneAsync(new() { ProductId = Guid.NewGuid(), Count = 50, CreatedDate = DateTime.UtcNow });
    await stockCollection.InsertOneAsync(new() { ProductId = Guid.NewGuid(), Count = 30, CreatedDate = DateTime.UtcNow });
    await stockCollection.InsertOneAsync(new() { ProductId = Guid.NewGuid(), Count = 5, CreatedDate = DateTime.UtcNow });
}

app.Run();