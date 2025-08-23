using Confluent.Kafka;
using Shared.Kafka;
using Shared.Kafka.MessageTypes;

namespace PaymentService.Infrastructure.Kafka
{
    public class PaymentConsumer : KafkaConsumerService<PaymentRequested>
    {
        private readonly IEventProducer _producer;

        public PaymentConsumer(IEventProducer producer)
            : base(topic: "payment.requested", groupId: "payment-service", bootstrapServers: "localhost:9092")
        {
            _producer = producer;
        }

        protected override async Task HandleMessageAsync(PaymentRequested message, ConsumeResult<string, string> cr, CancellationToken ct)
        {
            // Idempotency: check store if idempotencyKey processed
            var success = await CallGatewayAsync(message); // giả lập cổng thanh toán

            await _producer.ProduceAsync("payment.completed", new
            {
                eventId = Guid.NewGuid(),
                type = "payment.completed",
                bookingId = message.BookingId,
                success,
                transactionId = Guid.NewGuid().ToString(),
                completedAt = DateTime.UtcNow,
                reason = success ? null : "gateway_declined"
            }, key: message.BookingId, ct);
        }

        private Task<bool> CallGatewayAsync(PaymentRequested req)
            => Task.FromResult(true); // stub
    }

}
