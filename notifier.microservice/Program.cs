using common.models;
using Microsoft.EntityFrameworkCore;
using notifier.microservice;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//InMemory Db Context
builder.Services.AddDbContext<NotificationDb>(opt => opt.UseInMemoryDatabase("Notifications"), ServiceLifetime.Singleton);
//
builder.Services.AddHostedService<EventsListener>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

#region rest endpoint implementations
app.MapGet("/notification", async (NotificationDb db) =>
    await db.Notifications.ToListAsync()
)
.WithName("GetNotifications");

#endregion
app.Run();
app.Run("https://localhost:3002");

#region Entities
public class Notification
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public int CustomerId { get; set; }
    public int ResturantId { get; set; }
    //public List<OrderItem> OrderItems { get; set; }
    public NotificationType NotificationType { get; set; }
}

public enum NotificationType
{
    NotifyToResturant,
    NotifyToDeliveryAgent
}
public class NotificationDb : DbContext
{
    public NotificationDb(DbContextOptions<NotificationDb> options) : base(options)
    { }

    public DbSet<Notification> Notifications => Set<Notification>();
}
#endregion
