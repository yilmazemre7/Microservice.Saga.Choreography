using MassTransit;
using Microsoft.EntityFrameworkCore;
using Order.API.Models.Contexts;
using Shared.Events;

namespace Order.API.Consumers
{
    public class PaymentCompletedEventConsumer(OrderAPIDbContext _context) : IConsumer<PaymentCompletedEvent>
    {
        public async Task Consume(ConsumeContext<PaymentCompletedEvent> context)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(s => s.Id == context.Message.OrderId);
            if (order is null)
                throw new NullReferenceException();
            order.OrderStatus = Enums.OrderStatus.Completed;
            await _context.SaveChangesAsync();

        }
    }
}
