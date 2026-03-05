using DnsClient.Internal;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BusinessLogicLayer.RabbitMQ;

public class RabbitMQProductNameUpdateConsumer : IRabbitMQProductNameUpdateConsumer
{
    private readonly ILogger<RabbitMQProductNameUpdateConsumer> _logger;
    private readonly IConfiguration _configuration;
    private readonly IConnection _connection;
    private readonly IChannel _channel;

    public RabbitMQProductNameUpdateConsumer(IConfiguration configuration, ILogger<RabbitMQProductNameUpdateConsumer> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var rabbitMQHostName = _configuration["RabbitMQ_HostName"];
        var rabbitMQPort = _configuration["RabbitMQ_Port"];
        var rabbitMQUserName = _configuration["RabbitMQ_UserName"];
        var rabbitMQPassword = _configuration["RabbitMQ_Password"];

        ConnectionFactory connectionFactory = new ConnectionFactory()
        {
            HostName = rabbitMQHostName,
            Port = int.Parse(rabbitMQPort!),
            UserName = rabbitMQUserName,
            Password = rabbitMQPassword
        };

        _connection = connectionFactory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
    }

    public void Comsumer()
    {
        string routingKey = "product.updated.name";
        string queueName = "orders.product.update.name.queue";

        string exchangeName = _configuration["RabbitMQ_Products_Exchange"]!;
        _channel.ExchangeDeclareAsync(
            exchange: exchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            arguments: null
        ).GetAwaiter().GetResult();

        // Create message queue if not exists
        _channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null
        ).GetAwaiter().GetResult();

        _channel.QueueBindAsync(
            queue: queueName,
            exchange: exchangeName,
            routingKey: routingKey,
            arguments: null
        ).GetAwaiter().GetResult();

        AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (sender, eventArgs) =>
        {
            string message = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            ProductNameUpdateMessage? productNameUpdateMessage = JsonSerializer.Deserialize<ProductNameUpdateMessage>(message);
            
            _logger.LogInformation("Received product name update message: {Message} product name: {NewName}", message, productNameUpdateMessage.NewName);

            if (productNameUpdateMessage != null)
            {
                // Update the product name in the orders collection
                // await RabbitMQProductNameUpdateHandler.HandleProductNameUpdate(productNameUpdateMessage);
            }
        };

        _channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: true,
            consumer: consumer
        ).GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        _channel.Dispose();
        _connection.Dispose();
    }
}
