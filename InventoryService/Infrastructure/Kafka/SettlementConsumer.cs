using Confluent.Kafka;
using Shared.Kafka.MessageTypes;
using Shared.Kafka;
using StackExchange.Redis;
using InventoryService.Infrastructure.Database;
using Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Infrastructure.Kafka
{
    public class SettlementConsumer : KafkaConsumerService<PaymentCompleted>
    {
        private readonly AppDbContext _db;
        private readonly IEventProducer _producer;
        private readonly IConnectionMultiplexer _redis;

        public SettlementConsumer(AppDbContext db, IEventProducer producer, IConnectionMultiplexer redis)
            : base("payment.completed", "booking-settlement", "localhost:9092")
        {
            _db = db; _producer = producer; _redis = redis;
        }

        protected override async Task HandleMessageAsync(PaymentCompleted evt, ConsumeResult<string, string> cr, CancellationToken ct)
        {
            var booking = await _db.Bookings.FindAsync(evt.BookingId);
            if (booking is null) return; // hoặc DLQ

            var dbRedis = _redis.GetDatabase();

            var seats = await _db.Seats.Where(x => x.BookingId.HasValue && x.BookingId.Value == booking.Id).ToListAsync();

            if (evt.Success)
            {
                // Optimistic update seat -> Sold (pseudo)
                foreach (var seat in seats)
                {
                    var updated = await MarkSeatSoldAsync(booking.ShowId, seat.SeatNumber); // bạn triển khai với WHERE Version/X
                    if (!updated)
                    {
                        booking.Status = BookingStatus.Failed;
                        await _db.SaveChangesAsync();
                        await _producer.ProduceAsync("seat.released", new { showId = booking.ShowId, seats = booking.Seats, reason = "race" }, key: booking.Id.ToString());
                        return;
                    }
                }

                booking.Status = BookingStatus.Paid;
                await _db.SaveChangesAsync();

                // Cleanup Redis holds/locks
                foreach (var seat in booking.Seats)
                {
                    await dbRedis.KeyDeleteAsync($"hold:show:{booking.ShowId}:seat:{seat}");
                    await dbRedis.KeyDeleteAsync($"lock:show:{booking.ShowId}:seat:{seat}");
                }

                await _producer.ProduceAsync("ticket.confirmed", new
                {
                    eventId = Guid.NewGuid(),
                    type = "ticket.confirmed",
                    bookingId = booking.Id,
                    showId = booking.ShowId,
                    seats = booking.Seats,
                    userId = booking.UserId,
                    issuedAt = DateTime.UtcNow
                }, key: booking.Id.ToString(), ct);
            }
            else
            {
                booking.Status = BookingStatus.Failed;
                await _db.SaveChangesAsync();

                // Release seats
                foreach (var seat in seats)
                {
                    await dbRedis.KeyDeleteAsync($"hold:show:{booking.ShowId}:seat:{seat}");
                    await dbRedis.KeyDeleteAsync($"lock:show:{booking.ShowId}:seat:{seat}");
                }
                await _producer.ProduceAsync("seat.released", new { showId = booking.ShowId, seats = seats.Select(x => x.SeatNumber), reason = "payment_failed" }, key: booking.Id.ToString(), ct);
            }
        }


        public async Task<bool> MarkSeatSoldAsync(int showId, int seatNumber)
        {
            try
            {
                var seat = _db.Seats.FirstOrDefault(x => x.ShowId == showId && x.SeatNumber == seatNumber);
                if (seat != null)
                {
                    seat.Status = SeatStatus.Sold;
                    await _db.SaveChangesAsync();
                    return true;
                }
                else { return false; }
            }
            catch (DbUpdateConcurrencyException)
            {
                return false;
            }
        }
    }
}
