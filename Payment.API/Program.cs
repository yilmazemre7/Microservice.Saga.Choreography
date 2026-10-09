using MassTransit;
using Payment.API.Consumers;
using Shared;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMassTransit(configure =>
{
    configure.AddConsumer<StockReservedEventConsumer>();

    configure.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ"]);

        cfg.ReceiveEndpoint(RabbitMQSettings.Payment_StockReservedEvent
            , e =>
        {
            e.ConfigureConsumer<StockReservedEventConsumer>(context);
        });
    });
});

var app = builder.Build();

app.UseHttpsRedirection();

app.Run();