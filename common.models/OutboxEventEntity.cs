namespace common.models
{
    public class OutboxEventEntity
    {
        public int ID { get; set; }
        public string Event { get; set; }
        public string Data { get; set; }
    }
}