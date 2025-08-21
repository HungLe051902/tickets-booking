using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using System.Text.Json;


namespace Shared.Kafka
{
    public abstract class KafkaConsumerService<T> : BackgroundService
    {
        private readonly string _topic;
        private readonly IConsumer<string, string> _consumer;

        protected KafkaConsumerService(string topic, string groupId, string bootstrapServers)
        {
            _topic = topic;
            var config = new ConsumerConfig
            {
                BootstrapServers = bootstrapServers,
                GroupId = groupId,
                EnableAutoCommit = false,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                AllowAutoCreateTopics = true
            };
            _consumer = new ConsumerBuilder<string, string>(config).Build();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _consumer.Subscribe(_topic);
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var cr = _consumer.Consume(stoppingToken);
                    try
                    {
                        var msg = JsonSerializer.Deserialize<T>(cr.Message.Value);
                        await HandleMessageAsync(msg!, cr, stoppingToken);
                        _consumer.Commit(cr);
                    }
                    catch (Exception)
                    {

                        // TODO: publish to DLQ topic or log & skip
                    }
                }
            }
            finally
            {
                _consumer.Close();
            }
        }

        protected abstract Task HandleMessageAsync(T message, ConsumeResult<string, string> cr, CancellationToken ct);
    }

}
