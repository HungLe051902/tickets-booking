namespace Shared.Kafka.MessageTypes
{
    public class SeatHeld
    {
        public int ShowId { get; set; }
        public int[] SeatNumbers { get; set; } = Array.Empty<int>();
        public string BookingId { get; set; } = default!;
        public string UserId { get; set; } = default!;
    }
}
