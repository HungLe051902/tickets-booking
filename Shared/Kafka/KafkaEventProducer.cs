using Confluent.Kafka;
using System.Text.Json;


namespace Shared.Kafka
{
    public interface IEventProducer
    {
        Task ProduceAsync<T>(string topic, T message, string? key = null, CancellationToken ct = default);
    }

    public class KafkaEventProducer : IEventProducer, IDisposable
    {
        private readonly IProducer<string, string> _producer;
        public KafkaEventProducer(string bootstrapServers)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = bootstrapServers,
                Acks = Acks.All,
                EnableIdempotence = true, // idempotent producer
                CompressionType = CompressionType.Lz4
            };
            _producer = new ProducerBuilder<string, string>(config).Build();
        }

        public async Task ProduceAsync<T>(string topic, T message, string? key = null, CancellationToken ct = default)
        {
            var payload = JsonSerializer.Serialize(message);
            var dr = await _producer.ProduceAsync(topic, new Message<string, string>
            {
                Key = key ?? Guid.NewGuid().ToString(),
                Value = payload
            }, ct);
            // Optionally log dr.TopicPartitionOffset
        }

        public void Dispose() => _producer?.Dispose();
    }

}
