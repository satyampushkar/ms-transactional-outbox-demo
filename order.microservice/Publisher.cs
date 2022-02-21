using common.models;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;

namespace order.microservice
{
    public interface IPublisher
    {
        void Publish(OutboxEventEntity outboxEventEntity);
    }

    public class Publisher : IPublisher
    {
        private readonly ILogger<Publisher> _logger;
        private readonly IModel _channel;
        //private EventingBasicConsumer _consumer;
        public Publisher(ILogger<Publisher> logger)
        {
            _logger = logger;
            var factory = new ConnectionFactory() { HostName = "rabbitmq", Port = 5672 };
            var connection = factory.CreateConnection();
            _channel = connection.CreateModel();
            _channel.QueueDeclare(queue: "orderEventsQueue",
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);
        }
        public void Publish(OutboxEventEntity outboxEventEntity)
        {
            string message = JsonConvert.SerializeObject(outboxEventEntity);
            var body = Encoding.UTF8.GetBytes(message);

            _channel.BasicPublish(exchange: "",
                                 routingKey: "orderEventsQueue",
                                 basicProperties: null,
                                 body: body);
            _logger.LogInformation("Event pushed to orderEventsQueue........");
        }
    }
}
