using System.ComponentModel.DataAnnotations;

namespace AuthDemo.Api.Models.Requests;

public class CreateOrderRequest
{
    [Required, MinLength(1)]
    public List<OrderItemRequest> Items { get; set; } = [];

    [MaxLength(500)]
    public string? Note { get; set; }
}

public class OrderItemRequest
{
    [Required, Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Required, Range(1, 1000)]
    public int Quantity { get; set; }
}
