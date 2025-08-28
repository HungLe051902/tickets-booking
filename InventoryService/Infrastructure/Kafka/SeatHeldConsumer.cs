using Confluent.Kafka;
using InventoryService.Infrastructure.Database;
using InventoryService.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using Shared.Kafka;
using Shared.Kafka.MessageTypes;
using StackExchange.Redis;

namespace InventoryService.Infrastructure.Kafka
{
    public class SeatHeldConsumer : KafkaConsumerService<SeatHeld>
    {
        private readonly AppDbContext _db;
        private readonly IEventProducer _producer;
        private readonly IConnectionMultiplexer _redis;
        private readonly IHubContext<SeatHub> _hub;

        public SeatHeldConsumer(AppDbContext db, IEventProducer producer, IConnectionMultiplexer redis, IHubContext<SeatHub>  hub)
            : base("seat.held", "inventory-seat-held", "localhost:9092")
        {
            _db = db;
            _producer = producer;
            _redis = redis;
            _hub = hub;
        }

        protected override async Task HandleMessageAsync(SeatHeld evt, ConsumeResult<string, string> cr, CancellationToken ct)
        {
            // 1. Update DB: set seats to Held
            foreach (var seatNumber in evt.SeatNumbers)
            {
                var seat = await _db.Seats
                    .FirstOrDefaultAsync(s => s.ShowId == evt.ShowId && s.SeatNumber == seatNumber, ct);

                if (seat != null)
                {
                    seat.Status = SeatStatus.Held;
                }
            }

            await _db.SaveChangesAsync(ct);

            // 2. Update Redis cache (if using cache for seat map)
            var redisDb = _redis.GetDatabase();
            foreach (var seatNumber in evt.SeatNumbers)
            {
                await redisDb.HashSetAsync(
                    $"show:{evt.ShowId}:seats",
                    seatNumber,
                    "Held"
                );
            }

            // 3. Publish  UI (via WebSocket)
            await _hub.Clients.Group($"show-{evt.ShowId}")
            .SendAsync("SeatHeld", new
            {
                showId = evt.ShowId,
                seats = evt.SeatNumbers
            });

        }
    }

}
