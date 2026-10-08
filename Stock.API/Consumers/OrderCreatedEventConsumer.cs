using MassTransit;
using MongoDB.Driver;
using Shared.Events;
using Stock.API.Services;

namespace Stock.API.Consumers
{
    public class OrderCreatedEventConsumer(MongoDBService mongoDbService) : IConsumer<OrderCreatedEvent>
    {
        public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
        {
            List<bool> stockResult = new();
            IMongoCollection<Models.Stock> collection = mongoDbService.GetCollection<Models.Stock>();
            foreach (var orderItem in context.Message.OrderItems)
            {
                stockResult.Add(await (await collection.FindAsync(s => s.ProductId == orderItem.ProductId
                && s.Count >= orderItem.Count)).AnyAsync());
            }

            if (stockResult.TrueForAll(s => s.Equals(true)))
            {
                foreach (var orderItem in context.Message.OrderItems)
                {
                    var stock = await (await collection.FindAsync(s => s.ProductId == orderItem.ProductId)).FirstOrDefaultAsync();
                    stock.Count -= orderItem.Count;

                    await collection.FindOneAndReplaceAsync(s => s.ProductId == orderItem.ProductId, stock);
                }
            }
            else
            {
                //stok işlemi başarısız
                // orderı uyaracak event fırlatılacaktır.
            }
        }
    }
}
