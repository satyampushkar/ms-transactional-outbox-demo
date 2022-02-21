using common.models;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace notifier.microservice
{
    public class EventsListener : BackgroundService
    {
        private readonly IModel _channel;
        private readonly ILogger<EventsListener> _logger;
        private readonly NotificationDb _notificationDb;

        public EventsListener(ILogger<EventsListener> logger, NotificationDb notificationDb)
        {
            var factory = new ConnectionFactory() { HostName = "rabbitmq", Port = 5672 };
            var connection = factory.CreateConnection();
            _channel = connection.CreateModel();
            _channel.QueueDeclare(queue: "orderEventsQueue",
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            _logger = logger;
            _notificationDb = notificationDb;
        }

        protected override async Task<Task> ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                _logger.LogInformation($"Received message: {message}");

                var outboxEventEntity = JsonConvert.DeserializeObject<OutboxEventEntity>(message);
                if(outboxEventEntity != null)
                {
                    var orderEvent = JsonConvert.DeserializeObject<OrderEvent>(outboxEventEntity.Data);
                    if (orderEvent != null)
                    {
                        _notificationDb.Notifications.Add(new Notification 
                        {
                            Id = Guid.NewGuid(),
                            OrderId = orderEvent.OrderId,
                            CustomerId = orderEvent.CustomerId,
                            ResturantId = orderEvent.ResturantId,
                            //OrderItems = orderEvent.OrderItems,
                            NotificationType = NotificationType.NotifyToResturant
                        });

                        _logger.LogInformation($"Notifying to Resturant about OrderId: {orderEvent.OrderId}");

                        await _notificationDb.SaveChangesAsync();

                        _notificationDb.Notifications.Add(new Notification
                        {
                            Id = Guid.NewGuid(),
                            CustomerId = orderEvent.CustomerId,
                            ResturantId = orderEvent.ResturantId,
                            //OrderItems = orderEvent.OrderItems,
                            NotificationType = NotificationType.NotifyToDeliveryAgent
                        });

                        _logger.LogInformation($"Notifying to Delivery Agent about OrderId: {orderEvent.OrderId}");

                        await _notificationDb.SaveChangesAsync();
                    }    
                }
                
            };

            _channel.BasicConsume(queue: "orderEventsQueue", autoAck: true, consumer: consumer);
            return Task.CompletedTask;
        }
    }
}

