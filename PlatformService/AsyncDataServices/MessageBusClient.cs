using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using PlatformService.Dtos;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PlatformService.AsyncDataServices
{
    public class MessageBusClient : IMessageBusClient
    {
        private readonly IConfiguration _configuration;
        private IConnection? _connection;
        private IChannel? _channel;

        public MessageBusClient(IConfiguration configuration)
        {
            _configuration = configuration;
            
            // In RabbitMQ 7.x, we must handle the connection asynchronously.
            // Since constructors cannot be async, we initialize in a separate call 
            // or use .GetAwaiter().GetResult() for startup (common in simple microservices).
            InitializeRabbitMQ().GetAwaiter().GetResult();
        }

        private async Task InitializeRabbitMQ()
        {
            var factory = new ConnectionFactory()
            {
                HostName = _configuration["RabbitMQHost"],
                Port = int.Parse(_configuration["RabbitMQPort"] ?? "5672")
            };

            try
            {
                // v7.x uses CreateConnectionAsync
                _connection = await factory.CreateConnectionAsync();
                
                // v7.x uses CreateChannelAsync (Replaces CreateModel)
                _channel = await _connection.CreateChannelAsync();

                // v7.x uses ExchangeDeclareAsync
                await _channel.ExchangeDeclareAsync(exchange: "trigger", type: ExchangeType.Fanout);
                
                _connection.ConnectionShutdownAsync += RabbitMQ_ConnectionShutdown;

                Console.WriteLine("--> Connected to MessageBus (RabbitMQ v7)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--> Could not connect to the Message Bus: {ex.Message}");
            }
        }

        public async Task PublishNewPlatform(PlatformPublishedDto platformPublishedDto)
        {
            var message = JsonSerializer.Serialize(platformPublishedDto);

            if (_connection != null && _connection.IsOpen)
            {
                Console.WriteLine("--> RabbitMQ Connection Open, sending message...");
                await SendMessage(message);
            }
            else
            {
                Console.WriteLine("--> RabbitMQ connection is closed, not sending");
            }
        }

        private async Task SendMessage(string message)
        {
            var body = Encoding.UTF8.GetBytes(message);

            // v7.x uses BasicPublishAsync
            // Note: In v7, routingKey is mandatory (empty string for fanout)
            await _channel!.BasicPublishAsync(
                exchange: "trigger",
                routingKey: string.Empty,
                body: body);

            Console.WriteLine($"--> We have sent {message}");
        }

        public void Dispose()
        {
            Console.WriteLine("MessageBus Disposed");
            if (_channel != null && _channel.IsOpen)
            {
                // In v7, Close is replaced by async disposal, 
                // but for a standard IDisposable, we can use the sync version or just let it clean up.
                _channel.CloseAsync().GetAwaiter().GetResult();
                _connection?.CloseAsync().GetAwaiter().GetResult();
            }
        }

        private Task RabbitMQ_ConnectionShutdown(object? sender, ShutdownEventArgs e)
        {
            Console.WriteLine("--> RabbitMQ Connection Shutdown");
            return Task.CompletedTask;
        }
    }
}