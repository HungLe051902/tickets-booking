using BookingService.Data;
using BookingService.Records;
using Shared.Enums;
using Shared.Kafka;
using Shared.Models;
using StackExchange.Redis;

namespace BookingService.Services
{
    public class BookingService
    {
        private readonly IEventProducer _producer;
        private readonly IConnectionMultiplexer _redis;
        private readonly AppDbContext _db;
        public BookingService(IEventProducer producer, IConnectionMultiplexer redis, AppDbContext db)
        {
            _producer = producer;
            _redis = redis;
            _db = db;
        }

        public async Task<HoldResult> HoldSeatsAsync(Guid bookingId, int showId, int[] seatNumbers, string userId, TimeSpan holdTtl)
        {
            var dbRedis = _redis.GetDatabase();
            var lockVals = new List<(string key, string val)>();

            foreach (var seatNumber in seatNumbers)
            {
                var key = $"lock:show:{showId}:seat:{seatNumber}";
                var val = $"{bookingId}:{userId}:{Guid.NewGuid()}";
                var ok = await dbRedis.StringSetAsync(key, val, holdTtl, When.NotExists);
                if (!ok)
                {
                    // release acquired locks
                    foreach (var (k, v) in lockVals)
                    {
                        if ((string?)await dbRedis.StringGetAsync(k) == v)
                            await dbRedis.KeyDeleteAsync(k);
                    }
                    return new HoldResult(false, $"Seat {seatNumber} is being held by someone else.");
                }
                lockVals.Add((key, val));
                await dbRedis.StringSetAsync($"hold:show:{showId}:seat:{seatNumber}", bookingId.ToString(), holdTtl);
            }

            // create pending booking (Outbox recommended)
            _db.Bookings.Add(new Booking { Id = bookingId, ShowId = showId, Status = BookingStatus.Pending });
            await _db.SaveChangesAsync();

            await _producer.ProduceAsync("seat.held", new
            {
                eventId = Guid.NewGuid(),
                type = "seat.held",
                occurredAt = DateTime.UtcNow,
                bookingId,
                showId,
                seatNumbers,
                userId,
                expiresAt = DateTime.UtcNow.Add(holdTtl)
            }, key: bookingId.ToString());

            return new HoldResult(true, "Held");
        }

        public async Task RequestPaymentAsync(string bookingId, string userId, int amount, string currency, string idemKey)
        {
            await _producer.ProduceAsync("payment.requested", new
            {
                eventId = Guid.NewGuid(),
                type = "payment.requested",
                bookingId,
                amount,
                currency,
                paymentMethod = "VISA",
                idempotencyKey = idemKey,
                userId
            }, key: bookingId);
        }


    }
}

