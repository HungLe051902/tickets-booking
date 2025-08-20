namespace BookingService.Models
{
    public class Booking
    {
        public Guid Id { get; set; }
        public int ShowId { get; set; }
        public int SeatNumber { get; set; }
    }
}
