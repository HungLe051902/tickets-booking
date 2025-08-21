namespace Shared.Kafka.MessageTypes
{
    public class PaymentCompleted
    {
        public string BookingId { get; set; } = default!;
        public bool Success { get; set; }
        public string? TransactionId { get; set; }

    }
}
