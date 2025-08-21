namespace Shared.Kafka.MessageTypes
{
    public class PaymentRequested
    {
        public string BookingId { get; set; } = default!;
        public int Amount { get; set; }
        public string Currency { get; set; } = "VND";
        public string IdempotencyKey { get; set; } = default!;
        public string UserId { get; set; } = default!;

    }
}
