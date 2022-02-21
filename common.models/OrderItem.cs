namespace common.models
{
    public class OrderItem
    {
        public Guid Id { get; set; }
        public int ItemId { get; set; }
        public decimal UnitPrice { get; set; }
        public int Units { get; set; }
    }
}
