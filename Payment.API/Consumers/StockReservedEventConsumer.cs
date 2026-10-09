using MassTransit;
using Shared.Events;

namespace Payment.API.Consumers
{
    public class StockReservedEventConsumer(IPublishEndpoint publishEndpoint) : IConsumer<StockReservedEvent>
    {
        public async Task Consume(ConsumeContext<StockReservedEvent> context)
        {
            if (true)
            {
                PaymentCompletedEvent paymentCompletedEvent = new PaymentCompletedEvent()
                {
                    OrderId = context.Message.OrderId
                };
                await publishEndpoint.Publish(paymentCompletedEvent);
            }
            else
            {
                PaymentFailedEvent paymentFailedEvent = new PaymentFailedEvent()
                {
                    OrderId = context.Message.OrderId,
                    Message = "Insufficent budget",
                    OrderItems = context.Message.OrderItems
                };
                await publishEndpoint.Publish(paymentFailedEvent);
            }
        }
    }
}
