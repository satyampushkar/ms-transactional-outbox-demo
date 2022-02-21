using common.models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using order.microservice;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//InMemory Db Context
//builder.Services.AddDbContext<OrderDb>(opt => opt.UseInMemoryDatabase("Orders"), ServiceLifetime.Singleton);
//
//Sql server Db Context
var connection = @"Server=sql-server-db;Database=orders-demo;User=sa;Password=Password123;";
builder.Services.AddDbContext<OrderDb>(opt => opt.UseSqlServer(connection), ServiceLifetime.Singleton);
//
builder.Services.AddHostedService<OutboxPublisher>();
builder.Services.AddSingleton<IPublisher, Publisher>();

var app = builder.Build();
var x = app.Services.GetService<OrderDb>();
x.Database.EnsureCreated();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


#region rest endpoint implementations
app.MapGet("/order", async (OrderDb db) =>
await db.Orders.Include(o => o.OrderItems).ToListAsync()
)
.WithName("GetOrders");

app.MapGet("/order/{id}", async (Guid id, OrderDb db) =>
    await db.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == id)
        is Order order
            ? Results.Ok(order)
            : Results.NotFound()
)
.WithName("GetOrder");

app.MapPost("/order/{customerId}", async (int customerId, OrderDTO inputOrder, OrderDb db) =>
{
    var orderId = Guid.NewGuid();
    double orderAmount = 0;

    foreach (var orderItem in inputOrder.Items.ToList())
    {
        orderAmount += (double)(orderItem.Units * orderItem.UnitPrice);
    }

    Order order = new Order
    {
        Id = orderId,
        CustomerId = customerId,
        ResturantId = inputOrder.ResturantId,
        OrderDate = DateTime.Now,
        OrderAmount = orderAmount,
        OrderItems = inputOrder.Items.ToList().Select(x => new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ItemId = x.ItemId,
            UnitPrice = x.UnitPrice,
            Units = x.Units
        }).ToList()

    };

    var orderEvent = new common.models.OrderEvent
    {
        OrderId = orderId,
        CustomerId = customerId, 
        ResturantId = inputOrder.ResturantId,
        OrderAmount = orderAmount, 
        OrderItems = inputOrder.Items.Select(x => 
            new common.models.OrderItem 
            {   ItemId = x.ItemId, 
                UnitPrice = x.UnitPrice, 
                Units = x.Units }).ToList()
    };

    using var transaction = db.Database.BeginTransaction();

    db.Orders.Add(order);
    await db.SaveChangesAsync();

    db.OutboxEntity.Add(
        new OutboxEventEntity 
        { 
            Event = "order.add",
            Data = JsonConvert.SerializeObject(orderEvent)
        });
    await db.SaveChangesAsync();

    transaction.Commit();

    //return Results.Created($"/order/{order.Id}", order);
    return Results.Ok(order.Id);
})
.WithName("CreateOrder");

app.MapDelete("/order/{id}", async (Guid id, OrderDb db) =>
{
    var order = await db.Orders.FindAsync(id);
    if (order is null)
    {
        return Results.NotFound();
    }

    db.Orders.Remove(order);
    await db.SaveChangesAsync();

    return Results.NoContent();
})
.WithName("CancelOrder");
#endregion
app.Run();
app.Run("https://localhost:3001");



#region Entities
public class Order
{
    public Guid Id { get; set; }
    public int CustomerId { get; set; }
    public int ResturantId { get; set; }
    public DateTime OrderDate { get; set; }
    public double OrderAmount { get; set; }


    public List<OrderItem> OrderItems { get; set; }
}

public class OrderItem
{
    public Guid Id { get; set; }
    public int ItemId { get; set; } 
    public decimal UnitPrice { get; set; }
    public int Units { get; set; }


    public Guid? OrderId { get; set; }
    //public Order Order { get; set; }
}

public class OrderDTO
{
    public int ResturantId { get; set; }
    public List<OrderItemDTO> Items { get; set;}
}

public class OrderItemDTO
{
   public int ItemId { get; set; }
    public decimal UnitPrice { get; set; }
    public int Units { get; set; }
}

public class OrderDb : DbContext
{
    public OrderDb(DbContextOptions<OrderDb> options) : base(options)
    { }
    
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OutboxEventEntity> OutboxEntity => Set<OutboxEventEntity>();
}
#endregion