using System.ComponentModel.DataAnnotations;

namespace AuthDemo.Api.Models.Requests;

public class CreateCategoryRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }
}
