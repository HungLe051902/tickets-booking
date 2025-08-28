using Shared.Enums;

namespace Shared.Models
{
    public class Booking
    {
        public Guid Id { get; set; }
        public int ShowId { get; set; }
        public int SeatNumber { get; set; }
        public ICollection<Seat> Seats { get; set; } = new List<Seat>();
        public string UserId { get; set; } = default!;
        public BookingStatus Status { get; set; } = default!;
        public DateTime CreateAt { get; set; } = DateTime.UtcNow;

        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
