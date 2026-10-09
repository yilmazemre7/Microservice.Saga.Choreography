using MassTransit;
using Microsoft.EntityFrameworkCore;
using Order.API.Models.Contexts;
using Shared.Events;

namespace Order.API.Consumers
{
    public class PaymentCompletedEventConsumer(OrdeAPIDbContext _context) : IConsumer<PaymentCompletedEvent>
    {
        public async Task Consume(ConsumeContext<PaymentCompletedEvent> context)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(s => s.Id == context.Message.OrderId);
            if (order != null)
            {
                order.OrderStatus = Enums.OrderStatus.Completed;
                await _context.SaveChangesAsync();
            }
            {

            }
            throw new NotImplementedException();
        }
    }
}
