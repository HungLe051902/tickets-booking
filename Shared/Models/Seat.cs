using Shared.Enums;

namespace Shared.Models
{
    public class Seat
    {
        public int ShowId { get; set; }
        public int SeatNumber { get; set; }
        public Guid? BookingId { get; set; }
        public Booking? Booking { get; set; }
        public SeatStatus Status { get; set; } = SeatStatus.Available;
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
