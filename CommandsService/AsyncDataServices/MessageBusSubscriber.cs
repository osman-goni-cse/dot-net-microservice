using System.Text;
using CommandsService.EventProcessing;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CommandsService.AsyncDataServices
{
    public class MessageBusSubscriber : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly IEventProcessor _eventProcessor;
        private IConnection? _connection;
        private IChannel? _channel;
        private string? _queueName;

        public MessageBusSubscriber(IConfiguration configuration, IEventProcessor eventProcessor)
        {
            _configuration = configuration;
            _eventProcessor = eventProcessor;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var factory = new ConnectionFactory()
            {
                HostName = _configuration["RabbitMQHost"],
                Port = int.Parse(_configuration["RabbitMQPort"] ?? "5672")
            };

            try
            {
                // v7.x: CreateConnectionAsync
                _connection = await factory.CreateConnectionAsync(stoppingToken);
                
                // v7.x: CreateChannelAsync
                _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

                // v7.x: ExchangeDeclareAsync
                await _channel.ExchangeDeclareAsync(exchange: "trigger", type: ExchangeType.Fanout, cancellationToken: stoppingToken);

                // v7.x: QueueDeclareAsync (returns a QueueDeclareOk object containing the name)
                var queueDeclareResult = await _channel.QueueDeclareAsync(cancellationToken: stoppingToken);
                _queueName = queueDeclareResult.QueueName;

                // v7.x: QueueBindAsync
                await _channel.QueueBindAsync(queue: _queueName,
                    exchange: "trigger",
                    routingKey: string.Empty,
                    cancellationToken: stoppingToken);

                Console.WriteLine("--> Listening on the Message Bus...");

                // v7.x: Use AsyncEventingBasicConsumer
                var consumer = new AsyncEventingBasicConsumer(_channel);

                consumer.ReceivedAsync += async (model, ea) =>
                {
                    Console.WriteLine("--> Event Received!");

                    var body = ea.Body.ToArray();
                    var notificationMessage = Encoding.UTF8.GetString(body);

                    // Process the event
                    _eventProcessor.ProcessEvent(notificationMessage);
                    
                    // Task.CompletedTask satisfies the async requirement
                    await Task.CompletedTask;
                };

                // v7.x: BasicConsumeAsync
                await _channel.BasicConsumeAsync(queue: _queueName, autoAck: true, consumer: consumer, cancellationToken: stoppingToken);

                // Keep the service alive
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--> Could not connect to the Message Bus: {ex.Message}");
            }
        }

        public override void Dispose()
        {
            if (_channel != null && _channel.IsOpen)
            {
                _channel.CloseAsync().GetAwaiter().GetResult();
                _connection?.CloseAsync().GetAwaiter().GetResult();
            }
            base.Dispose();
        }
    }
}