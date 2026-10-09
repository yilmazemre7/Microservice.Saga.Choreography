namespace Shared
{
    public static class RabbitMQSettings
    {
        public const string Stock_OrderCreatedEvent = "stock-order-created-event-queue";
        public const string Payment_StockReservedEvent = "payment-stock-reserved-event-queue";
        public const string Order_PaymentCompletedEvent = "payment-completed-event-queue";
        public const string Order_PaymentFailedEvent = "order-payment-failed-event-queue";
        public const string Stock_PaymentFailedEvent = "stock-payment-failed-event-queue";
        public const string Order_StockNotReservedEvent = "order-stock-not-reserved-event-queue";
    }
}
