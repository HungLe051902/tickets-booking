using BookingService.Data;
using BookingService.Helpers.Enums;
using BookingService.Models;
using BookingService.Records;
using Shared.Kafka;
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

        public async Task<HoldResult> HoldSeatsAsync(string bookingId, int showId, string[] seats, string userId, TimeSpan holdTtl)
        {
            var dbRedis = _redis.GetDatabase();
            var lockVals = new List<(string key, string val)>();

            foreach (var seat in seats)
            {
                var key = $"lock:show:{showId}:seat:{seat}";
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
                    return new HoldResult(false, $"Seat {seat} is being held by someone else.");
                }
                lockVals.Add((key, val));
                await dbRedis.StringSetAsync($"hold:show:{showId}:seat:{seat}", bookingId, holdTtl);
            }

            // create pending booking (Outbox recommended)
            _db.Bookings.Add(new Booking { Id = bookingId, ShowId = showId, Seats = seats, Status = BookingStatus.Pending });
            await _db.SaveChangesAsync();

            await _producer.ProduceAsync("seat.held", new
            {
                eventId = Guid.NewGuid(),
                type = "seat.held",
                occurredAt = DateTime.UtcNow,
                bookingId,
                showId,
                seats,
                userId,
                expiresAt = DateTime.UtcNow.Add(holdTtl)
            }, key: bookingId);

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

