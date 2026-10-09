using MassTransit;
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

app.Run();