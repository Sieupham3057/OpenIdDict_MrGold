using System.ComponentModel.DataAnnotations;
using AuthDemo.Api.Models.Entities;

namespace AuthDemo.Api.Models.Requests;

public class UpdateOrderStatusRequest
{
    [Required]
    public OrderStatus Status { get; set; }
}
