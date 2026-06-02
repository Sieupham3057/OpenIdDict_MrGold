using AuthDemo.Api.Models;

namespace AuthDemo.Api.Models.Entities;

public enum OrderStatus : byte
{
    Pending   = 0,
    Confirmed = 1,
    Shipping  = 2,
    Delivered = 3,
    Cancelled = 4,
}

public class Order
{
    public long Id { get; set; }
    public Guid UserId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal TotalAmount { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public virtual ApplicationUser User { get; set; } = null!;
    public virtual ICollection<OrderItem> Items { get; set; } = [];
}
