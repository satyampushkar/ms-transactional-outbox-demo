using Microsoft.EntityFrameworkCore.Metadata;

namespace order.microservice
{
    public class OutboxPublisher : BackgroundService
    {
        private readonly ILogger<OutboxPublisher> _logger;
        private readonly IPublisher _publisher;
        private readonly OrderDb _orderDb;

        public OutboxPublisher(ILogger<OutboxPublisher> logger, IPublisher publisher, OrderDb orderDb)
        {
            _logger = logger;
            _publisher = publisher;
            _orderDb = orderDb;
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var outboxEvents = _orderDb.OutboxEntity.OrderBy(o => o.ID).ToList();
                    foreach (var outboxEvent in outboxEvents)
                    {
                        _publisher.Publish(outboxEvent);
                        _orderDb.OutboxEntity.Remove(outboxEvent);
                        await _orderDb.SaveChangesAsync();
                    } 
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error in publishing outboxevnt: {ex.Message}");
                }
                finally
                {
                    await Task.Delay(2500, stoppingToken);
                }
            }
        }
    }
}
