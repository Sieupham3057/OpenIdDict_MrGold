using System.ComponentModel.DataAnnotations;

namespace AuthDemo.Api.Models.Requests;

public class CreateProductRequest
{
    [Required]
    public int CategoryId { get; set; }

    [Required, MaxLength(300)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required, Range(0, 999_999_999)]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    public int Stock { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }
}
